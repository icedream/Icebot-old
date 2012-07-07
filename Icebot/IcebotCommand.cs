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

    /// <summary>
    /// Represents a command sent to the Icebot.
    /// </summary>
    public class IcebotCommand
    {
        /// <summary>
        /// The name of the command (mostly the first word in the message)
        /// </summary>
        public string Command
        { get; internal set; }

        /// <summary>
        /// The arguments of the command
        /// </summary>
        public string[] Arguments
        { get; internal set; }

        /// <summary>
        /// From where does the command come from?
        /// </summary>
        public MessageType SourceType
        { get; internal set; }

        /// <summary>
        /// Specific sender, may be an IrcUser or IrcChannel.
        /// </summary>
        public string Source
        { get; internal set; }

        /// <summary>
        /// Specific targets, mainly IrcChannels.
        /// </summary>
        public string Targets
        { get; internal set; }

        /// <summary>
        /// Returns where the bot will write the response.
        /// Use it in combination with SendMessage/SendNotice.
        /// </summary>
        public string ResponseTarget
        {
            get
            {
                if (IsPublic())
                    return Targets;
                else
                    return Source;
            }
        }

        public bool IsPublic()
        {
            return (MessageType.Public & SourceType) == SourceType;
        }
    }

}
