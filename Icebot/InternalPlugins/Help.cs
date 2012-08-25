using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Icebot.Api;
using Icebot.Bot;

namespace Icebot.InternalPlugins
{
    public class Help : IcebotServerPlugin
    {
        public override void Run()
        {
            base.Run();
        }

        public void public_help(IcebotCommand help)
        {
            if (help.Arguments.Length == 0)
            {
                
            }
            else
            {
                var commands =
                    from cmd in Channel._registeredCommands
                    where cmd.Name.Equals(help.Arguments.First())
                    select cmd;
            }
        }
    }
}
