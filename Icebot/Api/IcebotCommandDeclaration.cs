using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Icebot.Api
{
    public class IcebotCommandDeclaration
    {
        public string Name { get; internal set; }
        public string Description { get; internal set; }
        public string[] ArgumentNames { get; internal set; }
        public Type[] ArgumentTypes { get; internal set; }
        public Irc.IrcMessageType MessageType { get; internal set; }
        public IcebotCommandDelegate Callback { get; internal set; }
        public IcebotBasePlugin Plugin { get; internal set; }

        public IcebotCommandDeclaration(
            IcebotBasePlugin Plugin,
            Irc.IrcMessageType MessageType,
            string Name,
            string Description = null,
            string[] ArgumentNames = null,
            Type[] ArgumentTypes = null, // Does not even work atm
            IcebotCommandDelegate Callback = null
            )
        {

            if(ArgumentNames == null)
                ArgumentNames = new string[0];

            if(ArgumentTypes == null)
                ArgumentTypes = new Type[0];

            this.MessageType = MessageType;
            this.Plugin = Plugin;
            this.Description = Description;
            this.Name = Name;
            this.ArgumentNames = ArgumentNames;
            this.ArgumentTypes = ArgumentTypes;
            this.Callback = Callback;
        }
    }

    public delegate void IcebotCommandDelegate(Api.IcebotCommand command);
}
