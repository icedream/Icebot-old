using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Icebot.Irc
{
    public class User
    {
        public User(IcebotServer server)
        {
            IsIrcOp = false;
            ServerHost = server.Configuration.ServerHost;
        }

        public Hostmask Hostmask { get { return Nickname + (Username != null ? "!" + Username) + "@" + Hostname;  } }
        public string Nickname { get; internal set; }
        public string Username { get; internal set; }
        public string Hostname { get; internal set; }
        public string Realname { get; internal set; }
        public IcebotServer Server { get; internal set; }
        public string ServerHost { get; internal set; }
        public IcebotChannel[] Channels { get { return _channels.ToArray(); } }
        public TimeSpan IdleTime
        {
            internal set
            {
                IdlingSince = DateTime.Now.Subtract(value);
            }
            get
            {
                return DateTime.Now.Subtract(IdlingSince);
            }
        }
        public DateTime IdlingSince
        { get; internal set; }
        public bool IsAway { get; internal set; }
        public bool IsIrcOp { get; internal set; }
        public string AwayMessage { get; internal set; }
        internal List<IcebotChannel> _channels = new List<IcebotChannel>();

        public void Refresh()
        {
            User newInfo = new User(Server);
            Server.WhoIs(Hostmask);
        }
    }

    public class ChannelUser : User
    {
        //public string ChannelModes { get; internal set; }
        public IcebotChannel Channel { get; internal set; }
    }
}
