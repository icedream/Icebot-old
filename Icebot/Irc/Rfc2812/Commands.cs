using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Icebot.Irc.Rfc2812
{
    /// <summary>
    /// Definition of all client commands in RFC 2812.
    /// </summary>
    class Commands : RfcCommandsDefinition
    {
        public Commands()
        {
            Add("USER", "{0}", "{1} *", ":{2}");
            Add("NICK {0}");
            Add("PASS :{0}");
            Add("OPER {0} :{1}");
            Add("MODE {0} {1}"); // User & channel mode general definition
            Add("SERVICE", "{0} *", "{1}", "{2} 0", ":{3}");
            Add("QUIT");
            Add("QUIT :{0}");
            Add("SQUIT {0} :{1}");
            Add("JOIN {0}");
            Add("JOIN {0} {1}");
            Add("PART {0}");
            Add("PART {0} :{1}");
            Add("TOPIC {0}");
            Add("TOPIC {0} :{1}");
            Add("NAMES");
            Add("NAMES {0}");
            Add("NAMES {0} {1}");
            Add("LIST");
            Add("LIST {0}");
            Add("LIST {0} {1}");
            Add("INVITE {0} {1}");
            Add("KICK {0} {1}");
            Add("KICK {0} {1} :{2}");
            Add("PRIVMSG {0} :{1}");
            Add("NOTICE {0} :{1}");
            Add("MOTD");
            Add("MOTD {0}");
            Add("LUSERS");
            Add("LUSERS {0}");
            Add("LUSERS {0} {1}");
            Add("VERSION");
            Add("VERSION {0}");
            Add("STATS");
            Add("STATS {0}");
            Add("STATS {0} {1}");
            Add("LINKS");
            Add("LINKS {0}");
            Add("LINKS {0} {1}");
            Add("TIME");
            Add("TIME {0}");
            Add("CONNECT {0} {1}");
            Add("CONNECT {0} {1} {2}");
            Add("TRACE");
            Add("TRACE {0}");
            Add("ADMIN");
            Add("ADMIN {0}");
            Add("INFO");
            Add("INFO {0}");
            Add("SERVLIST");
            Add("SERVLIST {0}");
            Add("SERVLIST {0} {1}");
            Add("SQUERY {0} {1}");
            Add("WHO");
            Add("WHO {0}");
            Add("WHO {0} o"); // Special case, where arg #2 is ignored (see RFC 2812/3.6.1 for info)
            Add("WHOIS {0}");
            Add("WHOIS {0} {1}");
            Add("WHOWAS {0}");
            Add("WHOWAS {0} {1}");
            Add("WHOWAS {0} {1} {2}");
            Add("KILL {0} {1}");
            Add("PING {0}");
            Add("PING {0} {1}");
            Add("PONG {0}");
            Add("PONG {0} {1}");
            Add("ERROR :{0}");
            Add("AWAY :{0}");
            Add("REHASH");
            Add("DIE");
            Add("RESTART");
            Add("SUMMON {0}");
            Add("SUMMON {0} {1}");
            Add("SUMMON {0} {1} {2}");
            Add("USERS");
            Add("USERS {0}");
            Add("WALLOPS :{0}");
            AddIncremental("USERHOST", 1, 10); 
            AddIncremental("ISON", 1, 10);
        }
    }
}
