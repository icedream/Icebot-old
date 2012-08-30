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
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

/*
 * This may be considered an example plugin for implementing commands
 * properly.
 * 
 * Besides the usage of assembly-internal data, you can basically
 * hold on the structure of this plugin to make your own.
 */

namespace Icedream.Icebot.InternalPlugins
{
    public class Version : Plugin
    {
        /*
         * These plugins are not used by the bot, but by the plugin.
         */

        /// <summary>
        /// Gives back the assembly version of the bot
        /// </summary>
        public System.Version Version
        { get { return Assembly.GetExecutingAssembly().GetName().Version; } }

        /// <summary>
        /// Gives back the assembly name of the bot
        /// </summary>
        public string ProgramName
        { get { return ((AssemblyTitleAttribute)Assembly.GetExecutingAssembly().GetCustomAttributes(typeof(AssemblyName), false).First()).Title; }}

        /// <summary>
        /// Gives back the name and version of the bot as a string
        /// </summary>
        public string VersionString
        { get { return ProgramName + " " + Version.ToString(); } }


        /*
         * Here actually starts the declaration of all commands, the bot should register.
         * It is even possible to declare multiple commands under the same name. The bot
         * will then use multiple methods to fit the situation:
         * 
         *  1)  If a command is already declared under this name, but not under the same message type,
         *      the commands will be MERGED.
         *  2)  If a command is already declared under this name and under this message type,
         *      it will be automatically RENAMED by appending the next free available number. This is
         *      a primitive method of renaming, but the bot should show you the new name in the console.
         *      The new name will also be visible in the reply of the help command implemented by the
         *      internal "Help" plugin.
         *
         * The structure is as following:
         * 
         * [CommandInfo(commandName, description, messageType)]
         * public void SomeRandomName(Command command, (ChannelUser or IrcUser) user [ , someType argumentName1 [ ..., someTypeN argumentNameN ] ])
         * {
         *     // Your code here
         * }
         * 
         * The second parameter (user) is a ChannelUser instance only for the message type ChannelMessage
         * or ChannelNotice. For all other message types it is an IrcUser instance.
         * You can not declare commands for commands with message type CtcpReply. Such commands will be
         * ignored by the bot.
         * 
         * A plugin may not only register commands, but can also interact directly with the server on special
         * events. For more info on this, check the internal NickServ plugin.
         */

        // Example for declaring a public command !version to show the bot's command
        [CommandInfo("version", "Shows the bot's version in the channel", Icebot.MessageType.ChannelMessage)]
        public void Command_Public_version(Command command, ChannelUser user)
        {
            user.Channel.SendMessage(user.User.Nickname + ", Bot version is " + VersionString + ".");
        }

        // Example for declaring a private version of the above command
        [CommandInfo("version", "Shows the bot's version to the user with a NOTICE", Icebot.MessageType.PrivateMessage | Icebot.MessageType.PrivateNotice)]
        public void Command_Private_version(Command command, IrcUser user)
        {
            user.SendNotice("Bot version is " + VersionString + ".");
        }

        // A third variation for CTCP requests
        [CommandInfo("version", "Shows the bot's version to the user with a CTCP VERSION reply", Icebot.MessageType.CtcpRequest)]
        private void Command_CTCP_version(Command command, IrcUser user)
        {
            user.SendCtcpReply("VERSION", VersionString);
        }
    }
}