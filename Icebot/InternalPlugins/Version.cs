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
using System.Runtime;
using System.Reflection;
using Icebot.Api;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;


namespace Icebot.InternalPlugins
{
    public class Version : IcebotServerPlugin
    {
        public override void Run()
        {
            Server.RegisterCommand(new IcebotCommandDeclaration(
                Plugin: this,
                Name: "version",
                MessageType: Irc.IrcMessageType.All, // "/ctcp Icebot VERSION", "!version" and "/msg Icebot VERSION"
                Description: "Displays the bot's version",
                Callback: new IcebotCommandDelegate(cmd_Version)
            ));
            base.Run();
        }

        public System.Version Version
        {
            get { return Assembly.GetExecutingAssembly().GetName().Version; }
        }

        public string ProgramName
        {
            get { return ((AssemblyTitleAttribute)Assembly.GetExecutingAssembly().GetCustomAttributes(typeof(AssemblyName), false).First()).Title; }
        }

        public string VersionString
        {
            get { return ProgramName + " " + Version.ToString(); }
        }

        private void cmd_Version(IcebotCommand cmd)
        {
            // TODO: Version query
            if (cmd.Target is Bot.ChannelListener)
                ((Bot.ChannelListener)cmd.Target).SendMessage("Bot version is " + VersionString + ".");
            else if (cmd.Type == Irc.IrcMessageType.CtcpRequest)
                ((Bot.IrcListener)cmd.Target).Irc.SendCtcpReply(cmd.SenderNickname, "VERSION", VersionString);
            else if (cmd.Type == Irc.IrcMessageType.Private)
                ((Bot.IrcListener)cmd.Target).Irc.SendNotice(cmd.SenderNickname, "Bot version is " + VersionString + ".");
        }
    }
}