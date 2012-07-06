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
                        ChannelUser cu = (ChannelUser)user;
                        cu.Channel = this;
                        u.Add(cu);
                    }
                return u;
            }
        }
        public ChannelUser[] Users
        { get { return _users.ToArray(); } }

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
            Server.SendCommand("mode", Configuration.ChannelName, "+b " + who);
        }
        public void Unban(string who)
        {
            Server.SendCommand("mode", Configuration.ChannelName, "-b " + who);
        }
        public void Voice(string who)
        {
            Server.SendCommand("mode", Configuration.ChannelName, "+v " + who);
        }
        public void Devoice(string who)
        {
            Server.SendCommand("mode", Configuration.ChannelName, "-v " + who);
        }
        public void Op(string who)
        {
            Server.SendCommand("mode", Configuration.ChannelName, "+o " + who);
        }
        public void Deop(string who)
        {
            Server.SendCommand("mode", Configuration.ChannelName, "-o " + who);
        }
        public void Halfop(string who)
        {
            Server.SendCommand("mode", Configuration.ChannelName, "+h " + who);
        }
        public void Dehalfop(string who)
        {
            Server.SendCommand("mode", Configuration.ChannelName, "-h " + who);
        }
        public void Protect(string who)
        {
            Server.SendCommand("mode", Configuration.ChannelName, "+a " + who);
        }
        public void Deprotect(string who)
        {
            Server.SendCommand("mode", Configuration.ChannelName, "-a " + who);
        }
        public void Mode(string who, string flag, bool plus)
        {
            Server.SendCommand("mode", Configuration.ChannelName, (plus ? "+" : "-") + flag + " " + who);
        }
        public void Mode(string who, string flag)
        {
            Server.SendCommand("mode", Configuration.ChannelName, flag + " " + who);
        }
        public void Mode(string chanflags)
        {
            Server.SendCommand("mode", Configuration.ChannelName, chanflags);
        }
        public void Leave()
        {
            Server.Part(this);
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

        public string ChannelModes { get; internal set; }
    }
}
