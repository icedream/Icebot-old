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
    public class NumericReply : Reply
    {
        internal NumericReply(string line, IcebotServer server)
        {
            __construct(line, server);
        }

        private string _command;
        private new string Command
        {
            set
            {
                int num = -1;

                if (!int.TryParse(value, out num))
                    throw new FormatException("This is not a valid numeric reply (" + value + ")");

                Numeric = (Numeric)num;
            }
        }

        private string[] _args = { };
        public new string[] Arguments
        {
            get { return _args; }
            private set
            {
                _args = value;

                Target = _args[0];
                _args = _args.Skip(1).ToArray();
            }
        }

        public string Target
        {
            get;
            private set;
        }

        public Numeric Numeric
        {
            get;
            private set;
        }
    }
}
