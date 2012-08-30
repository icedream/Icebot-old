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
        private IrcUser _me = new IrcUser();

        // Public properties
        public IrcServerInfo ServerInfo { get { return _serverInfo; } }
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
                case IrcNumericMethod.RPL_VERSION:
                    _serverInfo.ServerVersion = e.Parameters[1] + " " + e.Parameters[0];
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
                }
    
            base.OnRawReceived(e);
        }
        protected virtual void OnMessageReceived(IrcMessageEventArgs e)
        {
            if (MessageReceived != null)
                MessageReceived.Invoke(this, e);
        }

        // Public functions
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
        /*
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
        public IrcChannel GetChannel(string channel)
        {
            foreach (IrcChannel c in _knownChannels)
                if(c.Name.Equals(channel, StringComparison.OrdinalIgnoreCase))
                    return c;
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
         */
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
