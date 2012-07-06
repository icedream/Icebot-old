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
using System.Text.RegularExpressions;

namespace Icebot
{
    public struct Hostmask
    {
        private string _mask;

        public Hostmask(string mask)
        {
            _mask = mask;
        }

        public static Hostmask Create(string nickname, string hostname)
        {
            return new Hostmask(nickname + "@" + hostname);
        }
        public static Hostmask Create(string nickname, string username, string hostname)
        {
            return new Hostmask(nickname + "!" + username + "@" + hostname);
        }

        public string Value { get { return _mask; } set { _mask = value; } }

        public override bool Equals(object obj)
        {
            return ((Regex)this).IsMatch(obj.ToString());
        }

        public override int GetHashCode()
        {
            return _mask.GetHashCode();
        }

        public override string ToString()
        {
            return _mask;
        }

        public string Nickname { get { return _mask.Split('!', '@').First(); } }
        // TODO: Make splitting faster for Hostmask.Username
        public string Username { get { return _mask.Split('!', '@').Length > 2 ? _mask.Split('!').Last().Split('@').First() : null; } }
        public string Hostname { get { return _mask.Split('@').Last(); } }

        static public implicit operator Hostmask(string mask)
        {
            return new Hostmask(mask);
        }

        static public implicit operator string(Hostmask mask)
        {
            return mask.Value;
        }

        static public explicit operator Regex(Hostmask mask)
        {
            return new Regex(Regex.Escape(mask._mask).Replace(@"\*", ".*").Replace(@"\?", "."), RegexOptions.IgnoreCase | RegexOptions.Singleline);
        }

    }
}
