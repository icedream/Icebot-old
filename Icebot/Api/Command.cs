using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Icebot.Bot;
using Icebot.Irc;

namespace Icebot.Api
{
    /*
    public class PluginCommand : Command
    {
        public CommandDeclaration Declaration { get; private set; }
        
        internal PluginCommand(
            CommandDeclaration d,
            IrcMessageEventArgs e,
            IrcListener server,
            string prefix
            )
        {
            Declaration = d;
            __construct(e, server, prefix);
        }
    }
     */

    public class Command
    {
        public string CommandName { get; private set; }
        public string[] Arguments { get; private set; }
        public IrcListener Server { get; private set; }
        public IrcUser Sender { get; private set; }
        public string SenderMask { get; private set; }
        public string SenderNickname { get { return SenderMask.Split('!', '@')[0]; } }
        public string SenderUsername { get { return SenderMask.Split('!', '@')[1]; } }
        public string SenderHostname { get { return SenderMask.Split('!', '@')[2]; } }
        public IrcMessageType Type { get; private set; }

        public static bool IsValid(object o, IrcMessageEventArgs e, string prefix)
        {
            if (!e.Text.StartsWith(prefix))
                return false;

            // TODO: "crash on notice"??????
            return true;
        }

        internal Command(
            )
        {
        }

        internal Command(
            IrcMessageEventArgs e,
            IrcListener server,
            string prefix
            )
        {
            __construct(e, server, prefix);
        }

        protected void __construct(
            IrcMessageEventArgs e,
            IrcListener server,
            string prefix
            )
        {
            Type = e.MessageType;
            SenderMask = e.SenderMask;
            Server = server;
            Sender = server.Irc.GetUserByNickname(SenderNickname);

            // Check if prefix is applicable
            if (prefix.Length > 0 && !e.Text.StartsWith(prefix))
                throw new InvalidOperationException("Not a valid bot command");

            string t = e.Text.Substring(prefix.Length);

            Console.WriteLine("== MTYPE: " + e.MessageType + "; PREFIX IS " + prefix + ", T IS " + t + " ==");

            // Parse command name
            if (t.Contains(' '))
            {
                CommandName = t.Substring(0, t.IndexOf(' '));
                t = t.Substring(CommandName.Length + 1);
            }
            else
            {
                CommandName = t;
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
