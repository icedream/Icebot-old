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
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Icebot.Irc
{
    public class User
    {
        internal User() { }

        public User(IcebotServer server)
        {
            IsIrcOp = false;
            ServerHost = server.Configuration.ServerHost;
        }

        public Hostmask Hostmask { get { return Nickname + (Username != null ? "!" + Username : "") + "@" + Hostname;  } }
        public string Nickname { get; internal set; }
        public string Username { get; internal set; }
        public string Hostname { get; internal set; }
        public string Realname { get; internal set; }
#if MANAGED_MODES
        public Mode[] Modes { get { return _modes; } }
        private List<Mode> _modes = new List<Mode>();
#endif
        public IcebotServer Server { get; internal set; }
        public string ServerHost { get; internal set; }
        public int Hopcount { get; internal set; }
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

        public event OnMessageHandler MessageReceived;

        public void SendPrivateMessage(string text)
        {
            Server.SendMessage(Nickname, text);
        }
        public void SendNotice(string text)
        {
            Server.SendNotice(Nickname, text);
        }
        public void Mode(string flags)
        {
            Server.Mode(this.Nickname, flags);
        }
#if MANAGED_MODES
        public void SetMode(Mode mode)
        {
            Server.SetMode(this.Nickname, mode);
        }
        public void UnsetMode(Mode mode)
        {
            Server.UnsetMode(this.Nickname, mode);
        }
#endif
        internal void ForceMessageReceived(Message msg)
        {
            if (this.MessageReceived != null)
                MessageReceived.Invoke(msg);
        }
    }

    public class ChannelUser
    {
        internal ChannelUser() { }

        public ChannelUser(IcebotChannel chan)
        {
            Channel = chan;
        }

        public ChannelUser(User user)
        {
            User = user;
        }
        
        public ChannelUser(User user, IcebotChannel chan)
        {
            User = user;
            Channel = chan;
        }

        private void ApplyChannel()
        {
            if(!User._channels.Contains(Channel))
                User._channels.Add(Channel);
        }

        //public string ChannelModes { get; internal set; }
        public IcebotChannel Channel { get; internal set; }
        public User User { get; internal set; }

    }

}
