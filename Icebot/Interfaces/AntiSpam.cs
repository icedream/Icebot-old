using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Icebot.Irc;
using Icebot.Bot;

namespace Icebot
{
        internal class AntiSpam
        {
            internal ChannelListener Channel { get; set; }

            public int KickAfterPoints = 80;
            public int TempBanAfterPoints = 120;
            public int BanAfterPoints = 250;
            private log4net.ILog Log { get { return log4net.LogManager.GetLogger("Icebot/" + Channel.Server.DisplayName + "#" + Channel.ChannelName + ":AntiSpam"); } }

            internal AntiSpam(ChannelListener channel)
            {
                Channel = channel;
            }

            // Nick, User, Host, Points, Time
            public Stack<Tuple<string, string, string, int, DateTime>> AntiSpamTickets = new Stack<Tuple<string, string, string, int, DateTime>>();

            public void Cleanup()
            {
                DateTime now = DateTime.Now;
                while (AntiSpamTickets.Count > 0 && now.Subtract(AntiSpamTickets.First().Item5).TotalMinutes > 1)
                {
                    var ticket = AntiSpamTickets.Pop();
                    Log.Info("Removed " + ticket.Item5 + " points (" + ticket.Item5.ToString() + ") from " + ticket.Item1 + "!" + ticket.Item2 + "@" + ticket.Item3);
                }
            }

            public void AddPoints(string nick, string user, string host, string channel, int points)
            {
                Cleanup();

                Log.Info("Adding " + points + " points to " + nick + "!" + user + "@" + host);
                AntiSpamTickets.Push(new Tuple<string, string, string, int, DateTime>(nick, user, host, points, DateTime.Now));
            }

            public int CheckPoints(string nick, string user, string host, string channel)
            {
                Cleanup();

                if (Program.ChannelUserHasMode(channel, nick, "~"))
                    return 0;

                var asi = (
                    from ticket in AntiSpamTickets
                    where ticket.Item2.Equals(user)
                    && ticket.Item3.Equals(host, StringComparison.OrdinalIgnoreCase)
                    select ticket.Item4
                    );
                var ast = (
                    from ticket in AntiSpamTickets
                    where ticket.Item2.Equals(user)
                    && ticket.Item3.Equals(host, StringComparison.OrdinalIgnoreCase)
                    select ticket.Item5
                    ).ToArray<DateTime>();
                if (asi == null || asi.Count() == 0)
                    return 0;

                // long spamming
                int s = asi.Sum();

                // short spamming
                if (asi.Count() > 1)
                    s = (int)(s * (1 + 1 / (DateTime.Now - ast.Last()).TotalSeconds));

                // Admins get less points (founder no points, see beginning of the function)
                if (Channel.UserHasMode(nick, "%"))
                    s /= 2;
                if (Channel.UserHasMode(nick, "@"))
                    s /= 2;
                if (Channel.UserHasMode(nick, "&"))
                    s /= 2;

                return s;
            }

            public bool ToBeKicked(string n, string u, string h, string c)
            {
                return CheckPoints(n, u, h, c) > KickAfterPoints;
            }

            public bool ToBeTempBanned(string n, string u, string h, string c)
            {
                return CheckPoints(n, u, h, c) > TempBanAfterPoints;
            }

            public int TempBanTime(string n, string u, string h, string c)
            {
                return (CheckPoints(n, u, h, c) - TempBanAfterPoints) * 2;
            }

            public bool ToBeBanned(string n, string u, string h, string c)
            {
                return CheckPoints(n, u, h, c) > BanAfterPoints;
            }
        }
}
