using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Net.Security;
using System.Security;
using System.Security.Authentication;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using log4net;
using Icebot.Irc;

namespace Icebot
{
    public class IcebotServer
    {
        public IcebotServerConfiguration Configuration { get; internal set; }

        internal TcpClient _tcp = new TcpClient();
        internal Stream _ns;
        private bool gotMotdOnce = false;
        public StreamWriter Writer { get; internal set; }
        public StreamReader Reader { get; internal set; }
        public Icebot Host { get; internal set; }
        public IcebotCommandsContainer PrivateCommands { get; internal set; }
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
        public User[] Users { get { return _users.ToArray(); } }
        private List<User> _users = new List<User>();

        public event OnPrivateBotCommandHandler BotPrivateCommandReceived;
        public event OnPublicBotCommandHandler BotPublicCommandReceived;
        public event OnNickChange NickChange;
        public event OnNickListUpdate NickListUpdate;
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
                while (_tcp.Connected)
                {
                    string line = Recv();
                    string[] split = line.Split(' ');
                    int numeric = -1;

                    // Check for server-side auth messages
                    if (split.Length > 2)
                    {
                        if (int.TryParse(split[1], out numeric)) // Numeric reply handling
                        {
                            HandleNumericReply(line);
                            continue;
                        }

                        // Command handling

                    }
                }
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
                        user.Nickname = last_reply.DataSplit[0];
                        user.Username = last_reply.DataSplit[1];
                        user.Hostname = last_reply.DataSplit[2];
                        user.Realname = last_reply.DataSplit.Last();
                        break;
                    case Numeric.RPL_WHOISCHANNELS:
                        // "<nick> :{[@|+]<channel><space>}"
                        if (!user.Nickname.Equals(last_reply.DataSplit[0]))
                            break;
                        foreach (string channel in last_reply.DataSplit.Last().Split(' '))
                        {
                            string channame = channel;
                            char prefix = (char)0;
                            if (IsPrefix(channel[0]))
                            {
                                prefix = channel[0];
                                channame = channel.Substring(1);
                            }
                            if(!IsChannelName(channame))
                                channame = MakeChannelString(channame);
                            IcebotChannel chan = GetChannel(channame);
                            user._channels.Add(chan);
                            if (!chan.HasUser(user))
                            {
                                ChannelUser cu = (ChannelUser)user;
                                cu.Prefix = prefix;
                                cu.Channel = chan;
                                chan._users.Add(cu);
                            }
                                
                        }
                        break;
                    case Numeric.RPL_WHOISSERVER:
                        // "<nick> <server> :<server info>"
                        if (!user.Nickname.Equals(last_reply.DataSplit[0]))
                            break;
                        break;
                    case Numeric.RPL_WHOISOPERATOR:
                        // "<nick> :is an IRC operator"
                        if (!user.Nickname.Equals(last_reply.DataSplit[0]))
                            break;
                        user.IsIrcOp = true;
                        break;
                    case Numeric.RPL_WHOISIDLE:
                        // "<nick> <integer> :seconds idle"
                        if (!user.Nickname.Equals(last_reply.DataSplit[0]))
                            break;
                        user.IdleTime = TimeSpan.FromSeconds(ulong.Parse(last_reply.DataSplit[1]));
                        break;
                    case Numeric.RPL_ENDOFWHOIS:
                    case Numeric.RPL_ENDOFWHOWAS:
                        SyncUserInfo(user);
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
        { return HasUser(user.Hostmask); }

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

        protected void HandleNumericReply(string line)
        {
            last_reply = new NumericReply(line);
            if (last_reply.Numeric.ToString().StartsWith("ERR_"))
                last_error = last_reply;
            User user = null;

            switch (last_reply.Numeric)
            {
                case Numeric.RPL_AWAY:
                    user = GetUser(last_reply.DataSplit[0]);
                    user.IsAway = true;
                    user.AwayMessage = last_reply.DataSplit.Last();
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
                    _motd.Add(last_reply.Data);
                    break;
                case Numeric.RPL_ENDOFMOTD:
                    if (!gotMotdOnce)
                    {
                        gotMotdOnce = true;
                        AutoJoinChannels();
                    }
                    break;
                case Numeric.RPL_TIME:
                    _log.Info("Server time: " + last_reply.Data);
                    break;
                case Numeric.RPL_MYINFO:
                    _mynick = last_reply.Target;
                    if (Configuration.Nickname != _mynick)
                        _log.WarnFormat("Configured nickname {0} is already in use, took {1}!",
                            Configuration.Nickname,
                            _mynick);

                    ServerInfo.Add("host", last_reply.DataSplit[0]);
                    ServerInfo.Add("software", last_reply.DataSplit[1]);
                    ServerInfo.Add("available_usermodes", last_reply.DataSplit[2]);
                    ServerInfo.Add("available_chanmodes", last_reply.DataSplit[3]);

                    // TODO: Implement extended MYINFO (google!)
                    if (last_reply.DataSplit.Length > 4)
                        _log.WarnFormat("Ignoring extended info in MYINFO");
                    break;
                case Numeric.RPL_ISUPPORT:
                    foreach (string s in last_reply.DataSplit)
                    {
                        if (s.StartsWith(":"))
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
                    GetChannel(last_reply.DataSplit[0]).ChannelModes = string.Join(" ", last_reply.DataSplit.Skip(1));
                    break;
                default:
                    if (last_reply.Numeric.ToString() == ((int)last_reply.Numeric).ToString()) // Numeric has no name in enum
                        _log.Warn("Ignoring undefined numeric from reply: " + last_reply.Numeric);
                    else
                        _log.Debug("Ignoring known numeric from reply: " + last_reply.Numeric);
                    break;
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
                _mynick = nick;
            }
        }

        public User WhoIs(string nickmask)
        {
            SendCommand("whois", nickmask);
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
            _log.Debug("(C => S) " + rawline);
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
                ForceDisconnect();
            string line = Reader.ReadLine();
            _log.Debug("(C <= S) " + line);
            return line;
        }

        protected ILog _log
        {
            get { return LogManager.GetLogger(this.GetType().Name + ":" + this.Configuration.ServerName); }
        }
    }
}
