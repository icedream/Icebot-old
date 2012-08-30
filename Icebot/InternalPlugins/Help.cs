using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Icebot.Api;
using Icebot.Bot;
using Icebot.Irc;

namespace Icebot.InternalPlugins
{
    public class Help : IcebotServerPlugin
    {
        public override void Run()
        {
            RegisterCommand(new CommandDeclaration(
                MessageType: IrcMessageType.PublicMessage,
                Name: "help",
                Description: "Shows public commands help",
                ArgumentNames: new string[] { },
                ArgumentTypes: new Type[] { },
                Callback: new EventHandler<IcebotCommandEventArgs>(public_help_0args)
            ));
            RegisterCommand(new CommandDeclaration(
                MessageType: IrcMessageType.PublicMessage,
                Name: "help",
                Description: "Shows help for a command",
                ArgumentNames: new string[] { "commandname" },
                ArgumentTypes: new Type[] { typeof(string) },
                Callback: new EventHandler<IcebotCommandEventArgs>(public_help_0args)
            ));

            base.Run();
        }

        public void public_help_specific(object sender, IcebotCommandEventArgs cmd)
        {
            int i = 0;
            foreach (var c in
                from cm in cmd.Command.Declaration.Host._registeredCommands
                where cm.Name.Contains(cmd.Command.Arguments.First())
                select cm
                )
            {
                // Example:
                //   PublicMessage \x02!testmsg <argument1> <argument2>\x02     Shows a test message
                //   PublicNotice \x02!testnotice <argument1> <argument2>\x02   Shows a test notice
                cmd.Command.Sender.SendNotice("  " + c.MessageType.ToString() + " \x02" + cmd.Channel.Prefix + c.Name + " " + string.Join(" ",
                    from a in c.ArgumentNames select "<" + a + ">")
                    + "\x02\t => " + c.Description);
                if (++i == 3)
                    break;
            }
        }

        public void public_help_0args(object sender, IcebotCommandEventArgs cmd)
        {
            cmd.Command.Sender.SendNotice("Following commands are available on this channel:");

            var commands =
                (from c in cmd.Command.Declaration.Host.GetEnabledPluginsOnChannel(cmd.Channel)
                 select c).AsEnumerable() as Stack<CommandDeclaration>;

            var tc = new List<CommandDeclaration>();

            while (commands.Count > 0)
            {
                tc.Add(commands.Pop());
                if (tc.Count == 7)
                {
                    cmd.Command.Sender.SendNotice(
                        "  " + 
                        string.Join(", ",
                            from t in tc
                            select "\x02" + cmd.Channel.Prefix + t.Name + "\x02"
                        )
                    );
                    tc.Clear();
                }
            }

            if (tc.Count > 0)
            {
                cmd.Command.Sender.SendNotice(
                    "  " + 
                    string.Join(", ",
                        from t in tc
                        select "\x02" + cmd.Channel.Prefix + t.Name + "\x02"
                    )
                    + "."
                );
                cmd.Command.Sender.SendNotice(
                    "To get the help for all private commands, type \x02/msg " + cmd.Server.Irc.Me.Nickname + " help"
                );
            }

            tc = null;
        }
    }
}
