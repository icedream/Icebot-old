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
            RegisterCommand(new CommandDeclaration(
                Name: "version",
                MessageType: Irc.IrcMessageType.PublicMessage,
                Description: "Displays the bot's version",
                Callback: new EventHandler<IcebotCommandEventArgs>(version_public)
            ));
            RegisterCommand(new CommandDeclaration(
                Name: "version",
                MessageType: Irc.IrcMessageType.Private,
                Description: "Returns the bot version in a private message",
                Callback: new EventHandler<IcebotCommandEventArgs>(version_private)
            ));
            RegisterCommand(new CommandDeclaration(
                Name: "version",
                MessageType: Irc.IrcMessageType.CtcpRequest,
                Description: "Returns the bot version as a CTCP VERSION reply",
                Callback: new EventHandler<IcebotCommandEventArgs>(version_ctcp)
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

        private void version_public(object o, IcebotCommandEventArgs cmd)
        {
            cmd.Channel.SendMessage(cmd.Command.Sender.Nickname + ", Bot version is " + VersionString + ".");
        }

        private void version_private(object o, IcebotCommandEventArgs cmd)
        {
            cmd.Command.Sender.SendNotice("Bot version is " + VersionString + ".");
        }

        private void version_ctcp(object o, IcebotCommandEventArgs cmd)
        {
            cmd.Server.Irc.SendCtcpReply(cmd.Command.SenderNickname, "VERSION", VersionString);
        }
    }
}