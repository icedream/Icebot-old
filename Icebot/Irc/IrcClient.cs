using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Icebot.Databases;

namespace Icebot.Irc
{
    public class IrcClient : IrcLayer
    {
        // Private variables
        private IrcServerInfo _serverInfo = new IrcServerInfo();
        private List<IrcUser> _knownUsers = new List<IrcUser>();
        private List<IrcChannel> _buildingChannelList = new List<IrcChannel>();
        private IrcChannel[] _knownChannels = new IrcChannel[0];
        private IrcUser _me = new IrcUser();

        // Public properties
        public IrcServerInfo ServerInfo { get { return _serverInfo; } }
        public IrcChannel[] Channels { get { return _knownChannels.ToArray(); } }
        public IrcUser Me { get { return _me; } }
        
        // Events
        public event EventHandler Registered;
        public event EventHandler<IrcMessageEventArgs> MessageReceived;

        // Event wrappers
        protected virtual void OnRegistered()
        {
            if (Registered != null)
                Registered.Invoke(this, new EventArgs());
        }
        protected override void OnNumericReceived(IrcNumericReplyEventArgs e)
        {
            switch (e.Numeric)
            {
                case IrcNumericMethod.RPL_WHOISUSER:
                case IrcNumericMethod.RPL_WHOWASUSER:
                    {
                        var u = GetUserByNickname(e.Parameters[0]);
                        u.Nickname = e.Parameters[0];
                        u.Username = e.Parameters[1];
                        u.Hostname = e.Parameters[2];
                        u.Realname = e.Parameters.Last();
                        u.IsOnline = e.Numeric == IrcNumericMethod.RPL_WHOISUSER;
                    }
                    break;
                case IrcNumericMethod.RPL_WHOISOPERATOR:
                    {
                        var u = GetUserByNickname(e.Parameters[0]);
                        u.IsIrcOp = true;
                    }
                    break;
                case IrcNumericMethod.RPL_WHOISIDLE:
                    {
                        var u = GetUserByNickname(e.Parameters[0]);
                        u.LastActivity = DateTime.Now.AddSeconds(-long.Parse(e.Parameters[1]));
                    }
                    break;
                case IrcNumericMethod.RPL_WHOISCHANNELS:
                    {
                        string nick = e.Parameters[0];
                        foreach (string channel in e.Parameters.Skip(1))
                        {
                            if(_serverInfo.SupportedChannelUserPrefixes.Contains(channel[0]))
                            {
                                var u = GetChannelUser(channel.Substring(1), nick);
                                if(!u.Prefix.Contains(channel[0]))
                                    u.Prefix += channel[0];
                            } else {
                                var u = GetChannelUser(channel, nick);
                                u.Prefix = "";
                            }
                        }
                    }
                    break;
                case IrcNumericMethod.RPL_MYINFO:
                    if (e.Parameters == null)
                        throw new Exception("Parameters = null, something's going wrong");
                    // Normal: <servername> <version> <available user modes> <available channel modes>
                    _serverInfo.ServerName = e.Parameters[0];
                    _serverInfo.ServerVersion = e.Parameters[1];
                    _serverInfo.AvailableUserModes = e.Parameters[2].ToCharArray();
                    _serverInfo.AvailableChannelModes = e.Parameters[3].ToCharArray();
                    // Extended: 
                    break;
                case IrcNumericMethod.RPL_UNAWAY:
                    Me.IsAway = false;
                    break;
                case IrcNumericMethod.RPL_NOWAWAY:
                    Me.IsAway = true;
                    break;
                case IrcNumericMethod.RPL_AWAY:
                    {
                        var u = GetUserByNickname(e.Parameters[0]);
                        u.IsAway = true;
                        u.AwayMessage = e.Parameters[1];
                    }
                    break;
                case IrcNumericMethod.RPL_YOUREOPER:
                    Me.IsIrcOp = true;
                    break;
                case IrcNumericMethod.RPL_YOURESERVICE:
                    Me.IsService = true;
                    break;
                case IrcNumericMethod.RPL_MOTDSTART:
                case IrcNumericMethod.ERR_NOMOTD:
                    _serverInfo._motdLines.Clear();
                    break;
                case IrcNumericMethod.RPL_MOTD:
                    _serverInfo._motdLines.Add(e.Parameters[0].TrimStart('-'));
                    break;
                case IrcNumericMethod.RPL_ENDOFINFO:
                    _serverInfo._syncInfo();
                    break;
                case IrcNumericMethod.RPL_INFO:
                    _serverInfo._infoLines.Add(e.Parameters[0]);
                    break;
                case IrcNumericMethod.RPL_ISUPPORT:
                    ServerInfo.ParseISupportLine(e);
                    break;
                case IrcNumericMethod.RPL_LISTEND:
                    _knownChannels = new IrcChannel[_buildingChannelList.Count];
                    _buildingChannelList.CopyTo(_knownChannels);
                    _buildingChannelList.Clear();
                    break;
                case IrcNumericMethod.RPL_LIST:
                    {
                        var c = new IrcChannel();
                        c.Name = e.Parameters[0];
                        c.UserCount = int.Parse(e.Parameters[1]);
                        c.Topic = e.Parameters[2];
                        _buildingChannelList.Add(c);
                    }
                    break;
                case IrcNumericMethod.RPL_TOPIC:
                    GetChannel(e.Parameters[0]).Topic = e.Parameters[1];
                    break;
                case IrcNumericMethod.RPL_NOTOPIC:
                    GetChannel(e.Parameters[0]).Topic = "";
                    break;
                case IrcNumericMethod.RPL_VERSION:
                    _serverInfo.ServerVersion = e.Parameters[1] + " " + e.Parameters[0];
                    break;
                case IrcNumericMethod.RPL_WHOREPLY:
                    {
                        var u = GetChannelUser(e.Parameters[0], e.Parameters[4]);
                        u.User.Username = e.Parameters[1];
                        u.User.Hostname = e.Parameters[2];
                        //u.User.Server = e.Parameters[3];
                        u.User.Nickname = e.Parameters[4];
                        u.User.IsAway = e.Parameters[5][1] == 'G';
                        u.User.IsIrcOp = e.Parameters[5][2] == '*';
                        u.User.Realname = e.Parameters[6].Substring(e.Parameters[6].IndexOf(' ') + 1);

                        // Add channel user mode from prefix
                        var modeChar = _serverInfo.SupportedChannelUserModes[new string(_serverInfo.SupportedChannelUserPrefixes).IndexOf(e.Parameters[5].Last())];
                        if (!u.Modes.Contains(modeChar))
                            u.Modes += modeChar;
                    }
                    break;
                case IrcNumericMethod.RPL_NAMREPLY:
                    {
                        string ourprefix = e.Parameters[0];
                        string channel = e.Parameters[1];
                        foreach (string prefixednick in e.Parameters)
                        {
                            char prefix = prefixednick[0];
                            if (new string(_serverInfo.SupportedChannelUserPrefixes).Contains(prefix))
                            {
                                var u = GetChannelUser(channel, prefixednick.Substring(1));
                                var modeChar = _serverInfo.SupportedChannelUserModes[new string(_serverInfo.SupportedChannelUserPrefixes).IndexOf(e.Parameters[5].Last())];
                                if (!u.Modes.Contains(modeChar))
                                    u.Modes += modeChar;
                            }
                        }
                    }
                    break;
                case IrcNumericMethod.RPL_BANLIST:
                    {
                        var channel = GetChannel(e.Parameters[0]);
                        var u = new IrcMaskedUser(channel, e.Parameters[1], e.Parameters.Skip(2).ToArray());
                        channel._temporaryBanList.Add(u);
                    }
                    break;
                case IrcNumericMethod.RPL_ENDOFBANLIST:
                    GetChannel(e.Parameters[0])._syncBanList();
                    break;
                case IrcNumericMethod.RPL_INVITELIST:
                    {
                        var c = GetChannel(e.Parameters[0]);
                        c._temporaryInviteList.Add(new IrcMaskedUser(c, e.Parameters[1], e.Parameters.Skip(2).ToArray()));
                    }
                    break;
                case IrcNumericMethod.RPL_ENDOFINVITELIST:
                    GetChannel(e.Parameters[0])._syncInviteList();
                    break;
                case IrcNumericMethod.RPL_EXCEPTLIST:
                    {
                        var c = GetChannel(e.Parameters[0]);
                        c._temporaryExceptList.Add(new IrcMaskedUser(c, e.Parameters[1], e.Parameters.Skip(2).ToArray()));
                    }
                    break;
                case IrcNumericMethod.RPL_ENDOFEXCEPTLIST:
                    GetChannel(e.Parameters[0])._syncExceptList();
                    break;
                case IrcNumericMethod.RPL_CHANNELMODEIS:
                    {
                        var c = GetChannel(e.Parameters[0]);
                        c.Modes.Clear();
                        int paramIndex = 1;
                        foreach (char m in e.Parameters[1])
                        {
                            if (
                                _serverInfo.SupportedATypeChannelModes.Contains(m) // user- or mask-specific mode, always with parameter
                                || _serverInfo.SupportedBTypeChannelModes.Contains(m) // always with parameter
                                || _serverInfo.SupportedCTypeChannelModes.Contains(m) // only with parameter when set
                                )
                                c.Modes.Add(m + " " + e.Parameters[++paramIndex]);
                            else
                                c.Modes.Add(m.ToString());
                        }
                    }
                    break;
                    // TODO: Implement RPL_UNIQOPIS
            }

            base.OnNumericReceived(e);
        }
        protected override void OnRawReceived(IrcRawReceiveEventArgs e)
        {
            if (IrcMessageEventArgs.IsValid(e))
                this.OnMessageReceived(new IrcMessageEventArgs(e));
            else
                switch (e.Command)
                {
                    case "JOIN":
                        break;
                    case "PART":
                        break;
                }
    
            base.OnRawReceived(e);
        }
        protected virtual void OnMessageReceived(IrcMessageEventArgs e)
        {
            if (MessageReceived != null)
                MessageReceived.Invoke(this, e);
        }

        // Public functions
        public IrcChannel GetChannel(string channel)
        {
            foreach (IrcChannel c in _knownChannels)
                if(c.Name.Equals(channel, StringComparison.OrdinalIgnoreCase))
                    return c;
            return null;
        }
        public IrcUser[] GetUsersByHostmask(string hostmask)
        {
            List<IrcUser> users = new List<IrcUser>();
            foreach (IrcUser u in _knownUsers)
                if (u.IsHostmaskMatch(hostmask))
                    users.Add(u);
            return users.ToArray();
        }
        public IrcUser GetSingleUserByHostmask(string hostmask)
        {
            foreach (IrcUser u in _knownUsers)
                if (u.IsHostmaskMatch(hostmask))
                    return u;
            return null;
        }
        public IrcUser GetUserByNickname(string nickname)
        {
            return GetSingleUserByHostmask(nickname + "!*@*");
        }
        public IrcChannelUser GetChannelUser(string channel, string nickname)
        {
            var co = GetChannel(channel);
            if (co == null)
                return null;
            foreach (var u in co.Users)
                if (u.User.Nickname == nickname)
                    return u;
            return null;
        }
        public IrcChannel[] GetChannelsOfUser(string nickname)
        {
            return GetChannelsOfUser(GetUserByNickname(nickname));
        }
        public IrcChannel[] GetChannelsOfUser(IrcUser user)
        {
            List<IrcChannel> channels = new List<IrcChannel>();
            foreach (var c in _knownChannels)
                foreach (IrcChannelUser cu in c.Users)
                    if (cu.User == user)
                    {
                        channels.Add(c);
                        break;
                    }
            return channels.ToArray();
        }
        public void Login(string nickname, string username, string realname, string password, bool wallops, bool invisible)
        {
            byte mode = 0;
            mode = (byte)(mode | (byte)(wallops ? 1 : 0 << 2));
            mode = (byte)(mode | (byte)(invisible ? 1 : 0 << 3));

            if (!string.IsNullOrEmpty(password))
                this.SendCommand("pass", password);
            else
                this.SendCommand("pass");
            this.SendCommand("nick", nickname);
            this.SendCommand("user", username, mode.ToString(), "*", realname);
        }
        public void SendMessage(string target, string message)
        {
            this.SendCommand("privmsg", target, message);
        }
        public void SendNotice(string target, string message)
        {
            this.SendCommand("notice", target, message);
        }
        public void SendCtcpRequest(string target, string command)
        {
            SendCtcpRequest(target, command, null);
        }
        public void SendCtcpRequest(string target, string command, params string[] arguments)
        {
            if(arguments != null && arguments.Length > 0)
            {
                //if (arguments[arguments.Length - 1].Contains(' '))
                //    arguments[arguments.Length - 1] = ":" + arguments[arguments.Length - 1];
                command += " " + string.Join(" ", arguments);
            }
            this.SendMessage(target, "\x01" + command + "\x01");
        }
        public void SendCtcpReply(string target, string command)
        {
            SendCtcpReply(target, command, null);
        }
        public void SendCtcpReply(string target, string command, params string[] arguments)
        {
            if (arguments != null && arguments.Length > 0)
            {
                //if (arguments.Last().Contains(' '))
                //    arguments[arguments.Length - 1] = ":" + arguments.Last();
                command += " " + string.Join(" ", arguments);
            }
            this.SendNotice(target, "\x01" + command + "\x01");
        }
    }
}
