using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Icebot.Irc
{
    public class RenamedUser
    {
        public RenamedUser(User user, string oldnick)
        {
            User = user;
            OldNick = oldnick;
        }

        public User User { get; private set; }
        public String OldNick { get; private set; }
        public String NewNick { get { return User.Nickname; } }
    }
}
