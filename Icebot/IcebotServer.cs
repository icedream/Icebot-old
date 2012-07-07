/**
 * Icebot - Extensible, multi-functional C# IRC bot
 * Copyright (C) 2012 Carl Kittelberger
 *
 * This program is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 *
 * You should have received a copy of the GNU General Public License
 * along with this program.  If not, see <http://www.gnu.org/licenses/>.
 */

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Net.Security;
using System.Reflection;
using System.Security;
using System.Security.Authentication;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using log4net;
using Icebot.Irc;
using Icebot.Api;

namespace Icebot
{
    public class IcebotPluginImplementation
    {
    }

    public class IcebotServer
    {
        public IcebotServerConfiguration Configuration { get; internal set; }

        #region Plugin implementation
        private List<ServerPlugin> _plugins = new List<ServerPlugin>();
        public ServerPlugin[] Plugins { get { return _plugins.ToArray(); } }
        public string[] PluginNames
        {
            get
            {
                List<string> l = new List<string>();
                foreach (ServerPlugin sp in _plugins)
                    l.Add(sp.PluginName);
                return l.ToArray();
            }
        }

        protected int GetPluginTypeCount(string pluginname)
        {
            int loaded = 0;
            foreach (Plugin pl in _plugins)
                if (pl.PluginName == pluginname)
                    loaded++;
            return loaded;
        }

        public void LoadAllPlugins()
        {
            foreach (IcebotServerPluginConfiguration config in Configuration.Plugins)
                LoadPlugin(config);
        }

        public void LoadPlugin(IcebotServerPluginConfiguration config)
        {
            DirectoryInfo dir = new DirectoryInfo("plugins");
            dir.Create();

            List<FileInfo> pluginfiles = new List<FileInfo>();
            pluginfiles.Add(new FileInfo(System.Diagnostics.Process.GetCurrentProcess().ProcessName));
            pluginfiles.AddRange(dir.GetFiles("*.dll", SearchOption.TopDirectoryOnly));
            foreach (FileInfo pluginfileinfo in pluginfiles)
            {
                try
                {
                    Assembly pluginfile = Assembly.LoadFrom(pluginfileinfo.FullName);
                    int pluginsfromfile = 0;
                    int pluginsfoundinfile = 0;

                    // Search for loadable plugin classes
                    foreach (Type exportedType in pluginfile.GetExportedTypes())
                    {
                        if (
                            !exportedType.IsAbstract
                            && exportedType.IsClass
                            && exportedType.IsPublic
                            && exportedType.IsSubclassOf(typeof(Plugin))
                            )
                        {
                            try
                            {
                                ServerPlugin plugininstance = (ServerPlugin)Activator.CreateInstance(exportedType);
                                plugininstance._server = this;
                                plugininstance._serverPluginConf = config;
                                plugininstance.PluginName = exportedType.Name;
                                plugininstance.InstanceNumber = 1 + GetPluginTypeCount(plugininstance.PluginName);
                                _plugins.Add(plugininstance);
                                _log.Info("Successfully loaded " + plugininstance.PluginName + " (Instance #" + plugininstance.InstanceNumber + ")");
                            }
                            catch (Exception instanceerror)
                            {
                                _log.Error("Found " + exportedType.Name + ", but failed loading as server plugin (" + instanceerror.Message + "). Check if the plugin supports this Icebot version.");
                            }
                        }
                    }

                    if(pluginsfromfile == pluginsfoundinfile)
                        _log.Info("Loaded " + pluginsfromfile + " of " + pluginsfoundinfile + " plugins from assembly");
                    else
                        _log.Warn("Loaded only " + pluginsfromfile + " of " + pluginsfoundinfile + " plugins from assembly! Please check your configuration and be sure to have the newest version of all plugins.");
                }
                catch (Exception assemblyloaderror)
                {
                    _log.Warn("Could not load " + pluginfileinfo.Name + ": Not a valid assembly (" + assemblyloaderror.Message + "). Remove from plugins folder or to something other than *.dll.");
                }
            }
        }

        public void UnloadPlugin(ServerPlugin plugin)
        {
            _plugins.Remove(plugin);
            ((IDisposable)plugin).Dispose();
        }
        #endregion

        internal TcpClient _tcp = new TcpClient();
        internal Stream _ns;
        private bool gotMotdOnce = false;
        public StreamWriter Writer { get; internal set; }
        public StreamReader Reader { get; internal set; }
        public Icebot Host { get; internal set; }
        public string MOTD { get { return string.Join(Environment.NewLine, _motd);  } }
        public string[] MOTDLines { get { return _motd.ToArray();  } }
        private List<string> _motd = new List<string>();
        private List<Irc.RfcCommandsDefinition> RfcDefinitions = new List<Irc.RfcCommandsDefinition>();
        public bool Connected { get { return _tcp == null ? false : _tcp.Connected; } }
        public bool Registered { get; internal set; }
        public Dictionary<string, string> ServerInfo { get; internal set; }
        private List<IcebotChannel> _channels = new List<IcebotChannel>();
        public IcebotChannel[] Channels { get { return _channels.ToArray(); } }
        private NumericReply last_reply = null;
        private NumericReply last_error = null;
        public User Me { get; internal set; }
        private List<string> _ignores = new List<string>();
        public User[] Users { get { return _users.ToArray(); } }
        private List<User> _users = new List<User>();

        private Dictionary<char, char> _prefixes = new Dictionary<char, char>();
        public Dictionary<char, char> AvailablePrefixes { get { return _prefixes; } }
        public Dictionary<char, char> AvailableChannelUserModes
        {
            get
            {
                Dictionary<char, char> _modes = new Dictionary<char,char>();
                foreach (char key in _prefixes.Keys)
                    _modes[_prefixes[key]] = key;
                return _modes;
            }
        }

        public event OnPrivateBotCommandHandler BotPrivateCommandReceived;
        public event OnPublicBotCommandHandler BotPublicCommandReceived;
        public event OnNumericReplyHandler NumericReply;
        public event OnRawHandler RawSent;
        public event OnRawHandler RawReceived;
        public event OnReceivedServerSupport ReceivedISupport;

        internal IcebotServer(Icebot host, IcebotServerConfiguration c)
        {
            Host = host;
            Configuration = c;
            PrivateCommands = new IcebotCommandsContainer();
            RfcDefinitions.Add(new Irc.Rfc2812.Commands());
            Registered = false;
        }

        internal bool InternalConnect()
        {
            _users.Clear();

            Me = new User(this);
            Me.Hostname = "unknown";
            Me.IdleTime = new TimeSpan(0);
            Me.IsIrcOp = false;
            Me.Nickname = Configuration.Nickname;
            Me.Realname = Configuration.Realname;

            try
            {
                _log.Info("Connecting to " + Configuration.ServerHost + ":" + Configuration.ServerPort + "...");
                _tcp.Connect(Configuration.ServerHost, Configuration.ServerPort);
                Me.ServerHost = Configuration.ServerHost;
                Me.Username = Configuration.Username;
                _log.Info("Connected.");
                _ns = _tcp.GetStream();
                _tcp.ReceiveBufferSize = 1024;
                Reader = new StreamReader(_ns);
                Writer = new StreamWriter(_ns);
                Writer.AutoFlush = true;
                ServerInfo = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                return true;
            }
            catch (SocketException socketerror)
            {
                _log.Error("Socket error while connecting: " + socketerror.Message + " (" + socketerror.SocketErrorCode.ToString() + ")");
                return false;
            }
        }

        Thread _loopread;
        private void _loopread_worker()
        {
            try
            {
                _tcp.ReceiveTimeout = 20000;
                _tcp.SendTimeout = 20000;
                while (_tcp.Connected)
                {
                    string line = Recv();
                    if (line == null)
                        break;
                    if (line.ToLower().StartsWith("ping"))
                    {
                        SendCommand("pong", string.Join(" ", line.Split(' ').Skip(1)).TrimStart(':'));
                        continue;
                    }

                    HandleReply(new Reply(line, this));
                }
                _log.Info("Disconnected from server, reconnecting in 5 seconds...");
                ForceDisconnect();
                Thread.Sleep(5);
                Connect();
            }
            catch (Exception e)
            {
                Disconnect("Bot error: " + e.Message);
#if !DEBUG
                _log.Error("Read thread generated an exception: " + e.Message);
#else
                _log.Error("Read thread generated an exception: " + e.ToString());
#endif
            }
        }

        internal ChannelUser[] ReadWhoReply()
        {
            List<ChannelUser> users = new List<ChannelUser>();
            bool eod = false;

            do
            {
                NumericReply r = ReadNumReply();

                if (last_reply.Numeric.ToString().StartsWith("ERR_"))
                    return null;

                switch (r.Numeric)
                {
                    case Numeric.RPL_WHOREPLY:
                        // "<channel> <user> <host> <server> <nick> ( "H" / "G" > ["*"] [ ( "@" / "+" ) ] :<hopcount> <real name>"
                        ChannelUser user = new ChannelUser(GetUser(r.Arguments[4]));
                        user.Channel = GetChannel(r.Arguments[0]);
                        user.User.Username = r.Arguments[1];
                        user.User.Hostname = r.Arguments[2];
                        // TODO: What about <server> in WHO reply?
                        user.User.Hopcount = int.Parse(r.Arguments.Last().Split(' ')[0]);
                        user.User.Realname = r.Arguments.Last().Substring(r.Arguments.Last().IndexOf(' ') + 1);
                        break;
                    case Numeric.RPL_ENDOFWHO:
                        eod = true;
                        break;
                }
            } while (!eod);
            return users.ToArray();
        }

        public bool IsValidChannelName(string channelname)
        {
            if (!ServerInfo["CHANTYPES"].Contains(channelname[0]))
                return false;

            return true;
        }

        public string CombineModes(params string[] modes)
        {
            // Sort modes alphabetically.
            Array.Sort(modes);

            List<char> plusmodes = new List<char>();
            List<char> minusmodes = new List<char>();
            List<string> plusarg = new List<string>();
            List<string> minusarg = new List<string>();

            foreach (string m in modes)
            {
                string[] ms = m.Split(' ');
                char pre = m[0];
                char mode = m[1];
                string arg = null;
                if(ms.Length > 1)
                    arg = ms[1];
                else if (ms.Length > 2)
                    _log.Warn("Ignoring wrongly placed parameters in inputted modes.");
                switch (mode)
                {
                    case '+':
                        plusmodes.Add(mode);
                        if (arg != null)
                            plusarg.Add(arg);
                        break;
                    case '-':
                        minusmodes.Add(mode);
                        if (arg != null)
                            minusarg.Add(arg);
                        break;
                }
            }

            string cm = "";
            if (minusmodes.Count > 0)
                cm += "-" + string.Join("", minusmodes);
            if (plusmodes.Count > 0)
                cm += "+" + string.Join("", plusmodes);
            cm += " "
                + string.Join(" ", minusarg)
                + string.Join(" ", plusarg)
                ;

            return cm;
        }

        public void Mode(string target, params string[] modes)
        {
            SendCommand("mode", target, CombineModes(modes));
        }

        public void Ignore(string who)
        {
            _ignores.Add(who.ToLower());
            if (ServerInfo.ContainsKey("CALLERID")) // Server supports +g ignoring?
                Mode(who, "+g");
        }

        public void Unignore(string who)
        {
            _ignores.Remove(who.ToLower());
        }

        public bool IsPrefix(char prefix)
        {
            return _prefixes.ContainsKey(prefix);
        }
        public bool IsChannelUserMode(char mode)
        {
            return _prefixes.ContainsValue(mode);
        }

        internal User ReadWhoIsReply()
        {
            User user = new User(this);
            bool eod = false;
            
            do
            {
                ReadNumReply();

                if (last_reply.Numeric.ToString().StartsWith("ERR_"))
                    return null;

                switch (last_reply.Numeric)
                {
                    case Numeric.RPL_WHOWASUSER:
                    case Numeric.RPL_WHOISUSER:
                        // "<nick> <user> <host> * :<real name>"
                        user.Nickname = last_reply.Arguments[0];
                        user.Username = last_reply.Arguments[1];
                        user.Hostname = last_reply.Arguments[2];
                        user.Realname = last_reply.Arguments.Last();
                        break;
                    case Numeric.RPL_WHOISCHANNELS:
                        // "<nick> :{[@|+]<channel><space>}"
                        if (!user.Nickname.Equals(last_reply.Arguments[0]))
                            break;
                        foreach (string channel in last_reply.Arguments.Last().Split(' '))
                        {
                            string channame = channel;
                            char prefix = (char)0;
                            if (IsPrefix(channel[0]))
                            {
                                prefix = channel[0];
                                channame = channel.Substring(1);
                            }
                            IcebotChannel chan = GetChannel(channame);
                            user._channels.Add(chan);
                            if (!chan.HasUser(user))
                            {
                                ChannelUser cu = new ChannelUser(user);
                                cu.Channel = chan;
                                chan._users.Add(cu);
                            }
                                
                        }
                        break;
                    case Numeric.RPL_WHOISSERVER:
                        // "<nick> <server> :<server info>"
                        if (!user.Nickname.Equals(last_reply.Arguments[0]))
                            break;

                        // TODO: Implement RPL_WHOISSERVER handling
                        break;
                    case Numeric.RPL_WHOISOPERATOR:
                        // "<nick> :is an IRC operator"
                        if (!user.Nickname.Equals(last_reply.Arguments[0]))
                            break;
                        user.IsIrcOp = true;
                        break;
                    case Numeric.RPL_WHOISIDLE:
                        // "<nick> <integer> :seconds idle"
                        if (!user.Nickname.Equals(last_reply.Arguments[0]))
                            break;
                        user.IdleTime = TimeSpan.FromSeconds(ulong.Parse(last_reply.Arguments[1]));
                        break;
                    case Numeric.RPL_ENDOFWHOIS:
                    case Numeric.RPL_ENDOFWHOWAS:
                        //SyncUserInfo(user);
                        eod = true; // Reply ends here. End of data.
                        break;
                }
            }
            while (!eod);
            return user;
        }

        public bool HasUser(Hostmask hostmask)
        {
            foreach (User u in _users)
                if (u.Hostmask == hostmask)
                    return true;
            return false;
        }
        public bool HasUser(User user)
        { return HasUser(user.Hostmask); }
        public bool HasUser(ChannelUser user)
        { return HasUser(user.User.Hostmask); }

        public User GetUser(string nickOrHostmask)
        {
            if (nickOrHostmask.Contains("@"))
                return GetUserByHostmask(nickOrHostmask);
            else
                return GetUserByNick(nickOrHostmask);
        }
        public User GetUserByHostmask(string hostmask)
        {
            foreach (User u in _users)
                if (u.Hostmask == hostmask)
                    return u;
            return null;
        }
        public User GetUsersByHostmask(Hostmask hostmask)
        {
            foreach (User u in _users)
                if(hostmask == u.Hostmask)
                    return u;
            return null;
        }
        public User GetUserByNick(string nick)
        {
            foreach(User u in _users)
                if(u.Nickname.ToLower() == nick.ToLower())
                    return u;
            return null;
        }
        public User[] GetUsersByHost(string hostname)
        {
            List<User> users = new List<User>();
            foreach(User u in _users)
                if(u.Hostname.ToLower() == hostname.ToLower())
                    users.Add(u);
            return users.ToArray();
        }

        public ChannelInfo[] GetAvailableChannels()
        {
            List<ChannelInfo> _chanlist = new List<ChannelInfo>();

            SendCommand("list");
            while (ReadNumReply().Numeric != Numeric.RPL_LISTEND)
                if (last_reply.Numeric == Numeric.RPL_LIST)
                    _chanlist.Add(new ChannelInfo(
                        last_reply.Arguments[0],
                        int.Parse(last_reply.Arguments[1]),
                        last_reply.Arguments[2]
                        ));

            return _chanlist.ToArray();
        }
        public ChannelInfo[] GetAvailableChannels(string server)
        {
            List<ChannelInfo> _chanlist = new List<ChannelInfo>();

            SendCommand("list", server);
            while (ReadNumReply().Numeric != Numeric.RPL_LISTEND)
                if (last_reply.Numeric == Numeric.RPL_LIST)
                    _chanlist.Add(new ChannelInfo(
                        last_reply.Arguments[0],
                        int.Parse(last_reply.Arguments[1]),
                        last_reply.Arguments[2]
                        ));

            return _chanlist.ToArray();
        }

        public NumericReply GetLastError()
        {
            return last_error;
        }

        public NumericReply ReadNumReply()
        {
            // TODO: Implement ReadNumReply timeout
            NumericReply rep = last_reply;
            while (rep == last_reply)
                Thread.SpinWait(10);
            return last_reply;
        }

        public NumericReply ReadNumReply(Numeric numeric)
        {
            // TODO: Implement ReadNumReply timeout
            while (ReadNumReply().Numeric != numeric) { }
            return last_reply;
        }

        protected void HandleNumericReply(NumericReply reply)
        {
            if (last_reply.Numeric.ToString().StartsWith("ERR_"))
                last_error = last_reply;
            User user = null;

            switch (last_reply.Numeric)
            {
                case Numeric.RPL_AWAY:
                    user = GetUserByNick(reply.Arguments[0]);
                    user.IsAway = true;
                    user.AwayMessage = reply.Arguments.Last();
                    break;
                case Numeric.ERR_NICKCOLLISION:
                case Numeric.ERR_NICKNAMEINUSE:
                    SetNick(Configuration.Nickname + new Random().Next(1000, 9999));
                    break;
                case Numeric.RPL_WELCOME:
                    // In this case, the user is now known to
                    // the server.
                    ServerInfo.Clear();
                    _log.Info("Welcome message received.");
                    Registered = true;
                    break;
                case Numeric.RPL_MOTDSTART:
                    _log.Info("Receiving Message Of The Day...");
                    _motd.Clear();
                    break;
                case Numeric.RPL_MOTD:
                    _motd.Add(reply.ArgumentLine);
                    break;
                case Numeric.RPL_ENDOFMOTD:
                    if (!gotMotdOnce)
                    {
                        gotMotdOnce = true;
                        if(Configuration.SetBotFlag)
                            SendCommand("mode", Me.Nickname, "+B");
                        AutoJoinChannels();
                    }
                    break;
                case Numeric.RPL_MYINFO:
                    Me.Nickname = reply.Target;
                    if (Configuration.Nickname != Me.Nickname)
                        _log.WarnFormat("Configured nickname {0} is already in use, took {1}!",
                            Configuration.Nickname,
                            Me.Nickname);

                    _log.Debug("Got MYINFO from " + reply.Sender + " to " + last_reply.Target + ": " + string.Join("//", last_reply.Arguments));

                    ServerInfo.Add("host", reply.Arguments[0]);
                    ServerInfo.Add("software", reply.Arguments[1]);
                    ServerInfo.Add("available_usermodes", reply.Arguments[2]);
                    ServerInfo.Add("available_chanmodes", reply.Arguments[3]);

                    // TODO: Implement extended MYINFO (google!)
                    if (last_reply.Arguments.Length > 4)
                        _log.WarnFormat("Ignoring extended info in MYINFO");
                    break;
                case Numeric.RPL_ISUPPORT:
                    foreach (string s in reply.Arguments)
                    {
                        if (s.Contains(" "))
                            break;

                        string[] s2 = s.Split('=');
                        string name = s2[0];
                        string value = null;

                        if (s2.Length > 1)
                            value = s.Substring(name.Length + 1);
                        if (!ServerInfo.ContainsKey(name))
                            if (value != null)
                                ServerInfo.Add(name, value);
                            else
                                ServerInfo.Add(name, true.ToString());
                        else
                        {
                            _log.Warn("Ignoring ISUPPORT duplicate: " + name);
                            if (value != null)
                                ServerInfo[name] = value;
                            //else
                            //    ServerInfo[name] = true;
                        }

                        if (ReceivedISupport != null)
                            ReceivedISupport.Invoke(this, name, value);

                        switch (name.ToLower())
                        {
                            case "charset":
                                Writer = new StreamWriter(_ns, Encoding.GetEncoding(value));
                                Reader = new StreamReader(_ns, Encoding.GetEncoding(value));
                                _log.Info("Auto-adjusted charset to " + value);
                                break;
                        }
                    }
                    break;
                case Numeric.RPL_CHANNELMODEIS:
                    GetChannel(reply.Arguments[0]).ChannelModes = string.Join(" ", reply.Arguments.Skip(1));
                    break;
                case Numeric.RPL_TOPIC:
                    GetChannel(reply.Arguments[0]).Topic = reply.Arguments.Last();
                    break;
                case Numeric.RPL_NOTOPIC:
                    GetChannel(reply.Arguments[0]).Topic = null;
                    break;
                default:
                    if (last_reply.Numeric.ToString() == ((int)last_reply.Numeric).ToString()) // Numeric has no name in enum
                        _log.Warn("Ignoring undefined numeric from reply: " + last_reply.Numeric);
                    else
                        _log.Debug("Ignoring known numeric from reply: " + last_reply.Numeric);
                    break;
            }

            if (NumericReply != null)
                NumericReply.Invoke(this, last_reply);
        }

        protected void HandleReply(Reply reply)
        {
            try
            {
                // Try parsing as a numeric reply
                try
                {
                    NumericReply nr = new NumericReply(reply.Raw, this);
                    return;
                }
                catch (FormatException)
                { { } }

                // Parse as command reply
                string[] channels;
                User user;
                switch (reply.Command)
                {
                    case "privmsg":

                        break;
                        
                    case "notice":

                        break;

                    case "join":
                        channels = reply.ArgumentLine.Split(',');
                        user = GetUser(reply.Sender);
                        foreach (string c in channels)
                        {
                            IcebotChannel ch = GetChannel(c);
                            ch.ForceJoinUser(user);
                        }
                        break;

                    case "part":
                        channels = reply.ArgumentLine.Split(',');
                        user = GetUser(reply.Sender);
                        foreach (string c in channels)
                        {
                            IcebotChannel ch = GetChannel(c);
                            if(ch.HasUser(user))
                                ch.ForcePartUser(ch.GetUser(user.Hostmask));
                        }
                        break;
                }
            }
            catch (Exception e)
            {
                _log.Error("Error in reply handler: " + e.Message);
            }
        }

        public void SetNick(string nick)
        {
            if (!Connected)
            {
                _log.Warn("Tried renaming while not connected.");
                return;
            }
            else
            {
                SendCommand("nick", nick);
                Me.Nickname = nick;
            }
        }

        public User WhoIs(string nickmask)
        {
            SendCommand("whois", nickmask);
            return ReadWhoIsReply();
        }
        public User WhoWas(params string[] nicknames)
        {
            SendCommand("whowas", nicknames);
            return ReadWhoIsReply();
        }
        public User Who(string searchpattern)
        {
            SendCommand("who", searchpattern);
            return ReadWhoIsReply();
        }

        public bool Connect()
        {
            if (!InternalConnect())
                return false;
            return Authenticate();
        }

        public bool ConnectSsl()
        {
            if (!InternalConnect() && !ApplySsl())
                return false;
            return Authenticate();
        }

        private bool ApplySsl()
        {
            try
            {
                _ns = new SslStream(_ns);
                Reader = new StreamReader(_ns);
                Writer = new StreamWriter(_ns);
                ((SslStream)_ns).AuthenticateAsClient(Configuration.ServerHost);
                _log.Info("Initialized SSL layer");
                return Authenticate();
            }
            catch (AuthenticationException autherror)
            {
                _log.Error("SSL authentication failed: " + autherror.Message);
                return false;
            }
        }

        private void AutoJoinChannels()
        {
            _log.Info("Auto-joining " + Configuration.Channels.Count + " channels...");
            foreach (IcebotChannelConfiguration conf in Configuration.Channels)
                ApplyChannel(new IcebotChannel(this, conf), false);
        }

        private void ApplyChannel(string chan)
        {
            IcebotChannel ch = new IcebotChannel(this, new IcebotChannelConfiguration());
            ch.Configuration.ChannelName = chan;
            ApplyChannel(ch);
        }

        private void ApplyChannel(string chan, bool addToConf)
        {
            IcebotChannel ch = new IcebotChannel(this, new IcebotChannelConfiguration());
            ch.Configuration.ChannelName = chan;
            ApplyChannel(ch, addToConf);
        }

        public void ApplyChannel(IcebotChannel ch)
        {
            bool addToConf = true;
            foreach (IcebotChannelConfiguration conf in Configuration.Channels)
                if (conf.ChannelName.ToLower() == ch.Configuration.ChannelName)
                {
                    addToConf = false;
                    break;
                }
            ApplyChannel(ch, addToConf);
        }

        public void ApplyChannel(IcebotChannel ch, bool addToConfig)
        {
            SendCommand("join", ch.Configuration.ChannelName);
            if(addToConfig)
                this.Configuration.Channels.Add(ch.Configuration);
        }

        private bool Authenticate()
        {
            if (_ns == null || Writer == null || Reader == null)
                return false;

            // TODO: Faster implementation of usermode combination on authentication
            BitArray modeBits = new BitArray(new bool[] {
                false, false, // Insignificant bits
                Configuration.ReceiveWallops, // +w bit
                Configuration.Invisible // +i bit
            });
            int[] modeArr = { 0 };
            modeBits.CopyTo(modeArr, 0);
            modeBits = null;
            int mode = modeArr[0];
            modeArr = null;

            SendCommand("user", Configuration.Username, mode, Configuration.Realname);
            SendCommand("nick", Configuration.Nickname);

            Thread _loopread = new Thread(new ThreadStart(_loopread_worker));
            _loopread.IsBackground = true;
            _loopread.Start();

            return true;
        }

        public void Disconnect()
        { Disconnect(null); }

        public void Disconnect(string text)
        {
            _users.Clear();
            _ns.ReadTimeout = 1000;
            _ns.WriteTimeout = 1000;
            if (text != null)
                SendCommand("quit", text);
            else
                SendCommand("quit");
            Registered = false;
            ServerInfo = null;
        }

        public bool SendCommand(string command, params object[] arguments)
        {
            foreach (Irc.RfcCommandsDefinition cd in RfcDefinitions)
            {
                string f = cd.Format(command, arguments);
                if (f != null)
                {
                    Send(f);
                    return true;
                }
            }
            _log.WarnFormat("Send request for command {0} has been rejected because it is not defined in the RFC definitions.", command);
            return false;
        }

        private void ForceDisconnect()
        {
            Registered = false;
            ServerInfo = null;
            Writer.Dispose();
            Reader.Dispose();
            _ns.Dispose();
            _loopread.Abort();
            _tcp.Client.Disconnect(false);
        }

        public IcebotChannel GetChannel(string chan)
        {
            foreach (IcebotChannel c in Channels)
                if (c.Configuration.ChannelName.ToLower() == chan.ToLower())
                    return c;
            return null;
        }

        public void Join(string chan)
        {
            ApplyChannel(chan);
        }

        public void Part(string chan, bool removeFromConfig)
        {
            Part(GetChannel(chan), removeFromConfig);
        }

        public void Part(IcebotChannel chan)
        {
            Part(chan.Configuration.ChannelName);
        }

        public void Part(IcebotChannel chan, bool removeFromConfig)
        {
            Part(chan.Configuration.ChannelName);
            _channels.Remove(chan);

            if (removeFromConfig)
                Configuration.Channels.Remove(chan.Configuration);

        }

        public void Part(string chan)
        {
            SendCommand("part", chan);
        }

        public void Send(string rawline)
        {
            if (!_tcp.Connected)
                ForceDisconnect();
            Writer.WriteLine(rawline);
            Writer.Flush();
            _log.Debug("(C => S) " + rawline);
            if (RawSent != null)
                RawSent.Invoke(this, rawline);
        }

        public void SendMessage(string target, string message)
        {
            SendCommand("privmsg", target, message);
        }

        public void SendMessage(string[] targets, string message)
        {
            SendMessage(string.Join(",", targets), message);
        }

        public void SendMessage(string[] targets, string[] messages)
        {
            foreach (string msg in messages)
                SendMessage(targets, msg);
        }

        public void SendMessage(string target, string[] messages)
        {
            foreach (string msg in messages)
                SendMessage(target, msg);
        }

        public void SendNotice(string target, string message)
        {
            SendCommand("notice", target, message);
        }

        public void SendNotice(string[] targets, string message)
        {
            SendNotice(string.Join(",", targets), message);
        }

        public void SendNotice(string[] targets, string[] messages)
        {
            foreach (string msg in messages)
                SendNotice(targets, msg);
        }

        public void SendNotice(string target, string[] messages)
        {
            foreach (string msg in messages)
                SendNotice(target, msg);
        }

        internal string Recv()
        {
            if (!_tcp.Connected)
            {
                ForceDisconnect();
                return null;
            }
            try
            {
                string line = Reader.ReadLine();
                _log.Debug("(C <= S) " + line);
                if (RawReceived != null)
                    RawReceived.Invoke(this, line);
                return line;
            }
            catch(Exception e)
            {
                _log.Warn("Error while receiving: " + e.Message);
                return null;
            }
        }

        protected ILog _log
        {
            get { return LogManager.GetLogger(this.GetType().Name + ":" + this.Configuration.ServerName); }
        }
    }
}
