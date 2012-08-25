using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Icebot.Bot;
using Icebot.Irc;

namespace Icebot.Api
{
    public class IcebotCommand
    {
        public string Command { get; private set; }
        public string[] Arguments { get; private set; }
        public string SenderMask { get; private set; }
        public string SenderNickname { get { return SenderMask.Split('!', '@')[0]; } }
        public string SenderUsername { get { return SenderMask.Split('!', '@')[1]; } }
        public string SenderHostname { get { return SenderMask.Split('!', '@')[2]; } }
        public Irc.IrcMessageType Type { get; private set; }

        public static bool IsValid(object o, IrcMessageEventArgs e)
        {
            if (!e.Text.StartsWith(GetPrefix(o, e)))
                return false;

            try
            {
                new IcebotCommand(o, e);
                return true;
            }
                // TODO: Make a cleaner bugfix for "crash on notice".
            catch { return false; }
        }

        public IcebotCommand(IrcMessageEventArgs e, string prefix)
        {
            Type = e.MessageType;
            SenderMask = e.SenderMask;

            // Check if prefix is applicable
            if (prefix.Length > 0 && !e.Text.StartsWith(prefix))
                throw new InvalidOperationException("Not a valid bot command");

            string t = e.Text.Substring(prefix.Length);

            Console.WriteLine("== MTYPE: " + e.MessageType + "; PREFIX IS " + prefix + ", T IS " + t + " ==");

            // Parse command name
            if (t.Contains(' '))
            {
                Command = t.Substring(0, t.IndexOf(' '));
                t = t.Substring(Command.Length + 1);
            }
            else
            {
                Command = t;
                t = "";
            }

            // Parse arguments
            List<string> arguments = new List<string>();
            while (t.Length > 0)
            {
                string argument = "";
                // TODO: Make this shorter.
                if (t[0] == '"')
                {
                    int i = t.IndexOf("\"");
                    if (i < 0)
                        throw new Exception("Invalid syntax: Missing double quote char.");
                    while (t[i - 1] == '\\') // escaped?
                        i = t.IndexOf("\"", i + 1);
                    if (t[i + 1] != ' ')
                        throw new Exception("Invalid syntax: Expected space after ending double quote char.");
                    argument = t.Substring(0, i);
                    t = t.Substring(i + 2);
                }
                else if (t[0] == '\'')
                {
                    int i = t.IndexOf("'");
                    if (i < 0)
                        throw new Exception("Invalid syntax: Missing single quote char.");
                    while (t[i - 1] == '\\') // escaped?
                        i = t.IndexOf("'", i + 1);
                    if (t[i + 1] != ' ')
                        throw new Exception("Invalid syntax: Expected space after ending single quote char.");
                    argument = t.Substring(0, i);
                    t = t.Substring(i + 2);
                }
                else
                {
                    int i = t.Length - 1;
                    if (t.Contains(" "))
                        i = t.IndexOf(" ");
                    while (t[i - 1] == '\\') // escaped?
                        i = t.IndexOf(" ", i + 1);
                    argument = t.Substring(0, i);
                    if (t.Length <= i + 2)
                        t = "";
                    else
                        t = t.Substring(i + 2);
                }
                arguments.Add(argument);
            }
        }
    }
}
