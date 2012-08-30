using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Icebot.Api;
using Icebot.Irc;
using System.IO;
using System.Reflection;
using System.Runtime;
using System.Runtime.InteropServices;
using log4net;

namespace Icebot.Bot
{
    public class ChannelListener : IDisposable
    {
        internal ChannelListener(IrcListener server, string channelName)
        {
            //IsJoined = false;
            Topic = "";
            Users = new List<IrcChannelUser>();
            BanList = new IrcMaskedUser[0];
            InviteList = new IrcMaskedUser[0];
            ExceptList = new IrcMaskedUser[0];
            Modes = new List<string>();
            Users = new List<IrcChannelUser>();

            Server = server;
            ChannelName = channelName;
            Log = LogManager.GetLogger("Icebot/" + Server.DisplayName + "#" + ChannelName);

            Server.Irc.MessageReceived += new EventHandler<IrcMessageEventArgs>(Irc_MessageReceived);
            //Server.Irc.Disconnecting += new EventHandler(Irc_Disconnecting);
            Server.Irc.NumericReceived += new EventHandler<IrcNumericReplyEventArgs>(Irc_NumericReceived);
        }

        void Irc_NumericReceived(object sender, IrcNumericReplyEventArgs e)
        {
            if (!e.Parameters.Contains(ChannelName, StringComparer.OrdinalIgnoreCase))
                return;
            switch (e.Numeric)
            {
                case IrcNumericMethod.RPL_WHOISCHANNELS:
                    {
                        string nick = e.Parameters[0];
                        foreach (string channel in e.Parameters.Skip(1))
                        {
                            if (Server.Irc.ServerInfo.SupportedChannelUserPrefixes.Contains(channel[0]))
                            {
                                var u = GetUser(nick);
                                if (!u.Prefix.Contains(channel[0]))
                                    u.Prefix += channel[0];
                            }
                            else
                            {
                                var u = GetUser(nick);
                                u.Prefix = "";
                            }
                        }
                    }
                    break;
                case IrcNumericMethod.RPL_TOPIC:
                    this.Topic = e.Parameters[1];
                    break;
                case IrcNumericMethod.RPL_NOTOPIC:
                    this.Topic = "";
                    break;
                case IrcNumericMethod.RPL_WHOREPLY:
                    {
                        var u = GetUser(e.Parameters[4]);
                        u.User.Username = e.Parameters[1];
                        u.User.Hostname = e.Parameters[2];
                        //u.User.Server = e.Parameters[3];
                        u.User.Nickname = e.Parameters[4];
                        u.User.IsAway = e.Parameters[5][1] == 'G';
                        u.User.IsIrcOp = e.Parameters[5][2] == '*';
                        u.User.Realname = e.Parameters[6].Substring(e.Parameters[6].IndexOf(' ') + 1);

                        // Add channel user mode from prefix
                        var modeChar = Server.Irc.ServerInfo.SupportedChannelUserModes[new string(Server.Irc.ServerInfo.SupportedChannelUserPrefixes).IndexOf(e.Parameters[5].Last())];
                        if (!u.Modes.Contains(modeChar))
                            u.Modes += modeChar;
                    }
                    break;
                case IrcNumericMethod.RPL_NAMREPLY:
                    {
                        string ourprefix = e.Parameters[0];
                        foreach (string prefixednick in e.Parameters)
                        {
                            char prefix = prefixednick[0];
                            if (new string(Server.Irc.ServerInfo.SupportedChannelUserPrefixes).Contains(prefix))
                            {
                                var u = GetUser(prefixednick.Substring(1));
                                var modeChar = Server.Irc.ServerInfo.SupportedChannelUserModes[new string(Server.Irc.ServerInfo.SupportedChannelUserPrefixes).IndexOf(e.Parameters[5].Last())];
                                if (!u.Modes.Contains(modeChar))
                                    u.Modes += modeChar;
                            }
                        }
                    }
                    break;
                case IrcNumericMethod.RPL_BANLIST:
                    {
                        var channel = this;
                        var u = new IrcMaskedUser(channel, e.Parameters[1], e.Parameters.Skip(2).ToArray());
                        channel._temporaryBanList.Add(u);
                    }
                    break;
                case IrcNumericMethod.RPL_ENDOFBANLIST:
                    this._syncBanList();
                    break;
                case IrcNumericMethod.RPL_INVITELIST:
                    {
                        var c = this;
                        c._temporaryInviteList.Add(new IrcMaskedUser(c, e.Parameters[1], e.Parameters.Skip(2).ToArray()));
                    }
                    break;
                case IrcNumericMethod.RPL_ENDOFINVITELIST:
                    this._syncInviteList();
                    break;
                case IrcNumericMethod.RPL_EXCEPTLIST:
                    {
                        this._temporaryExceptList.Add(new IrcMaskedUser(this, e.Parameters[1], e.Parameters.Skip(2).ToArray()));
                    }
                    break;
                case IrcNumericMethod.RPL_ENDOFEXCEPTLIST:
                    this._syncExceptList();
                    break;
                case IrcNumericMethod.RPL_CHANNELMODEIS:
                    {
                        this.Modes.Clear();
                        int paramIndex = 1;
                        foreach (char m in e.Parameters[1])
                        {
                            if (
                                Server.Irc.ServerInfo.SupportedATypeChannelModes.Contains(m) // user- or mask-specific mode, always with parameter
                                || Server.Irc.ServerInfo.SupportedBTypeChannelModes.Contains(m) // always with parameter
                                || Server.Irc.ServerInfo.SupportedCTypeChannelModes.Contains(m) // only with parameter when set
                                )
                                this.Modes.Add(m + " " + e.Parameters[++paramIndex]);
                            else
                                this.Modes.Add(m.ToString());
                        }
                    }
                    break;
            }
        }

        void Irc_Disconnecting(object sender, EventArgs e)
        {
            // Automatically stop listening when disconnected
            //Stop();
        }

        protected ILog Log { get; private set; }
        public IrcListener Server { get; internal set; }
        internal List<IrcMaskedUser> _temporaryBanList = new List<IrcMaskedUser>();
        internal List<IrcMaskedUser> _temporaryInviteList = new List<IrcMaskedUser>();
        internal List<IrcMaskedUser> _temporaryExceptList = new List<IrcMaskedUser>();
        //public bool IsJoined { get; internal set; }
        public string Name { get; internal set; }
        public string Topic { get; internal set; }
        public List<string> Modes { get; internal set; }
        public List<IrcChannelUser> Users { get; internal set; }
        public IrcMaskedUser[] BanList { get; internal set; }
        public IrcMaskedUser[] InviteList { get; internal set; }
        public IrcMaskedUser[] ExceptList { get; internal set; }

        protected string _prefix = "!";

        public string ChannelName { get; set; }
        public string Key { get; set; }
        public string Prefix
        { get { return _prefix; } }

        void Irc_MessageReceived(object sender, IrcMessageEventArgs e)
        {
            if (e.Target.Split(',').Contains(ChannelName, StringComparer.OrdinalIgnoreCase))
            {
                if (MessageReceived != null)
                    MessageReceived.Invoke(this, e);

                if (Command.IsValid(this, e, Prefix))
                    OnCommandReceived(new IcebotCommandEventArgs(new Command(e, null, Prefix), Server, null));
            }
        }
        public event EventHandler<IrcMessageEventArgs> MessageReceived;

        //internal List<IcebotCommandDeclaration> _registeredCommands = new List<IcebotCommandDeclaration>();
        public event EventHandler<IcebotCommandEventArgs> CommandReceived;

        protected void OnCommandReceived(IcebotCommandEventArgs e)
        {
            if (CommandReceived != null)
                CommandReceived.Invoke(this, e);

            /*
            var c =
                from cmd in _registeredCommands
                where cmd.Name.Equals(e.Command.Command, StringComparison.OrdinalIgnoreCase) && ((cmd.MessageType & e.Command.Type) != 0) && cmd.Callback != null
                select cmd;

            foreach (var cmd in c)
                cmd.Callback.Invoke(e.Command);
             */
        }

        public void Dispose()
        {
            Stop();
        }

        public void Start()
        {
            if (!Server.Irc.IsConnected)
                throw new Exception("Not connected to the server.");
            
            if(string.IsNullOrEmpty(ChannelName))
                throw new Exception("No channel name given.");

            if (Key == null)
                Server.Irc.SendCommand("join", ChannelName);
            else
                Server.Irc.SendCommand("join", ChannelName, Key);

            //LoadAllPlugins();
        }
        public void Stop()
        {
            if (Server.Irc.IsConnected)
                Server.Irc.SendCommand("part", ChannelName);
        }   

        public void SendMessage(string text)
        { Server.Irc.SendMessage(ChannelName, text); }
        public void SendNotice(string text)
        { Server.Irc.SendNotice(ChannelName, text); }
        public void SendAction(string text)
        { this.SendNotice("\x01ACTION " + text + "\x01"); }

        public IrcChannelUser[] GetUsers(string hostmask)
        {
            return (from u in Users where u.IsHostmaskMatch(hostmask) select u).ToArray<IrcChannelUser>();
        }
        public IrcChannelUser GetUser(string hostmask)
        {
            return GetUsers(hostmask).First();
        }

        internal void _syncBanList()
        {
            BanList = _temporaryBanList.ToArray();
            _temporaryBanList.Clear();
        }
        internal void _syncInviteList()
        {
            InviteList = _temporaryInviteList.ToArray();
            _temporaryInviteList.Clear();
        }
        internal void _syncExceptList()
        {
            ExceptList = _temporaryExceptList.ToArray();
            _temporaryExceptList.Clear();
        }
    }
}
