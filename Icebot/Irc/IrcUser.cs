using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Text;

namespace Icebot.Irc
{
    public class IrcUser
    {
        public string Nickname { get; internal set; }
        public string Username { get; internal set; }
        public string Hostname { get; internal set; }
        public string Realname { get; internal set; }

        public bool IsAway { get; internal set; }
        public string AwayMessage { get; internal set; }

        public bool IsIrcOp { get; internal set; }
        public bool IsService { get; internal set; }
        public bool IsOnline { get; internal set; }

        public DateTime LastActivity { get; internal set; }
        public TimeSpan IdleTime { get { return DateTime.Now - LastActivity; } }

        public bool IsHostmaskMatch(string hostmask)
        { return new Regex("^" + hostmask.Replace("*", ".*") + "$", RegexOptions.IgnoreCase | RegexOptions.Singleline).IsMatch(hostmask); }

        public IrcUser()
        {
            Nickname = Username = Hostname = Realname = AwayMessage = null;
            IsAway = IsIrcOp = IsService = false;
            IsOnline = true;
        }
    }

    public class IrcMaskedUser
    {
        public IrcChannel Channel { get; internal set; }
        public string Hostmask { get; internal set; }
        public string[] ExtraData { get; internal set; }

        public IrcMaskedUser(IrcChannel c, string mask, string[] data)
        {
            Channel = c;
            Hostmask = mask;
            ExtraData = data;
        }
    }

    public class IrcChannelUser
    {
        public IrcUser User { get; internal set; }
        public IrcServerInfo ServerInfo { get; internal set; }

        public string Prefix { get; internal set; }
        public string Modes { get; internal set; } // TODO: Parse Modes so that the highest mode will decide which prefix the user gets in the channel
    }
}
