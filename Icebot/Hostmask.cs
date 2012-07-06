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
