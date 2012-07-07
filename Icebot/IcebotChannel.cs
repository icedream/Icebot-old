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
using Icebot.Irc;

namespace Icebot
{
    public class IcebotChannel
    {
        public IcebotChannel(IcebotServer server, IcebotChannelConfiguration conf)
        {
            PublicCommands = new IcebotCommandsContainer();
            Configuration = conf;

            Server = server;
        }

        public IcebotServer Server { get; set; }

        public IcebotChannelConfiguration Configuration { get; internal set; }

        public IcebotCommandsContainer PublicCommands { get; internal set; }

        public event OnPublicBotCommandHandler BotCommandReceived;
        public event OnUserHandler UserJoined;
        public event OnUserHandler UserParted;

        internal List<ChannelUser> _users
        {
            get
            {
                List<ChannelUser> u = new List<ChannelUser>();
                foreach (User user in Server.Users)
                    if (user.Channels.Contains(this))
                    {
                        ChannelUser cu = new ChannelUser(user);
                        cu.Channel = this;
                        u.Add(cu);
                    }
                return u;
            }
        }
        public ChannelUser[] Users
        { get { return _users.ToArray(); } }

        public DateTime CreationTime;

        public string Topic { get; internal set; }

        public void SendMessage(string text)
        {
            Server.SendMessage(Configuration.ChannelName, text);
        }
        public void SendNotice(string text)
        {
            Server.SendNotice(Configuration.ChannelName, text);
        }
        public void Kick(string who)
        {
            Server.SendCommand("kick", who);
        }
        public void Ban(string who)
        {
            Mode("+b " + who);
        }
        public void Unban(string who)
        {
            Mode("-b " + who);
        }
        public void Voice(string who)
        {
            Mode("+v " + who);
        }
        public void Devoice(string who)
        {
            Mode("-v " + who);
        }
        public void Op(string who)
        {
            Mode("+o " + who);
        }
        public void Deop(string who)
        {
            Mode("-o " + who);
        }
        public void Halfop(string who)
        {
            Mode("+h " + who);
        }
        public void Dehalfop(string who)
        {
            Mode("-h " + who);
        }
        public void Protect(string who)
        {
            Mode("+a " + who);
        }
        public void Deprotect(string who)
        {
            Mode("-a " + who);
        }
        public void SetTopic(string text)
        {
            Server.SendCommand("topic", text);
        }
        public void Mode(params string[] modes)
        {
            Server.Mode(this.Configuration.ChannelName, modes);
        }
        public void Leave()
        {
            Server.Part(this);
        }
        public bool HasUser(Hostmask hostmask)
        {
            foreach (ChannelUser u in _users)
                if (u.User.Hostmask == hostmask)
                    return true;
            return false;
        }
        public bool HasUser(User user)
        { return HasUser(user.Hostmask); }
        public bool HasUser(ChannelUser user)
        { return HasUser(user.User.Hostmask); }

        public ChannelUser GetUser(Hostmask hostmask)
        {
            foreach (ChannelUser cu in _users)
                if (cu.User.Hostmask == hostmask)
                    return cu;
            return null;
        }

        internal ChannelUser ForceJoinUser(User user)
        {
            ChannelUser cu = new ChannelUser(user, this);
            if (UserJoined != null)
                UserJoined.Invoke(cu);
            return cu;
        }

        internal void ForcePartUser(ChannelUser user)
        {
            _users.Remove(user);
            if (UserParted != null)
                UserParted.Invoke(user);
        }

        public string ChannelModes { get; internal set; }
    }
}
