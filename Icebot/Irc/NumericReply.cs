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
    public class NumericReply
    {
        internal NumericReply(string line)
        {
            int num = -1;

            // Syntax: :sender NUM yournick :text
            string[] spl = line.Split(' ');

            if (
                spl.Length < 3
                || !int.TryParse(spl[1], out num)
                )
                throw new FormatException("Raw line is not a valid numeric reply.");

            Sender = spl[0];
            Numeric = (Numeric)num; // int.Parse(spl[1]);
            Target = spl[2];
            if (spl[3].StartsWith(":"))
                Data = string.Join(" ", spl.Skip(3)).Substring(1);
            else
                Data = spl[3];

            // Generate splitted data array
            List<string> s = new List<string>();
            string[] s1 = Data.Split(new string[] { " :" }, StringSplitOptions.RemoveEmptyEntries);
            string lastParam = s1.Last();
            s.AddRange(s1[0].Split(' '));
            s.Add(lastParam);
            DataSplit = s.ToArray();
        }

        public string Sender { get; internal set; }
        public Numeric Numeric { get; internal set; }
        public string Target { get; internal set; }
        public string Data { get; internal set; }
        public string[] DataSplit { get; internal set; }
    }
}
