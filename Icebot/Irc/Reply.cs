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
    public class Reply
    {
        internal Reply() { }

        internal Reply(string line, IcebotServer server)
        {
            __construct(line, server);
        }

        protected void __construct(string line, IcebotServer server)
        {
            Raw = line;

            // Syntax: :Name COMMAND parameter list
            string[] spl = line.Split(' ');

            if (
                spl.Length < 2
                )
                throw new FormatException("Raw line is not a valid command reply.");

            Sender = spl[0];
            Command = spl[1].ToLower();
            ArgumentLine = line.Substring(Sender.Length + Command.Length + 2);

            // Generate splitted arguments array
            List<string> s = new List<string>();
            string[] s1 = ArgumentLine.Split(new string[] { " :" }, StringSplitOptions.RemoveEmptyEntries);
            if (s1.Length > 2)
                Console.WriteLine("LOGIC FAIL: s1.Length > 2 should not be true!");
            s.AddRange(s1[0].Split(' '));
            if (s1.Length > 1 && !string.IsNullOrEmpty(s1[1]))
                s.Add(s1[1]);
            Arguments = s.ToArray();

            Server = server;
        }

        public string Raw { get; private set; }
        public IcebotServer Server { get; private set; }
        public string Sender { get; private set; }
        public string Command { get; private set; }
        public string ArgumentLine { get; private set; }
        public string[] Arguments { get; private set; }
    }
}
