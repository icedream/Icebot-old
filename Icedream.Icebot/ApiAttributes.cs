using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Icedream.Icebot
{
    [AttributeUsage(AttributeTargets.Method, Inherited = false, AllowMultiple = false)]
    public sealed class CommandInfoAttribute : Attribute
    {
        // See the attribute guidelines at 
        //  http://go.microsoft.com/fwlink/?LinkId=85236
        readonly MessageType messageType;
        readonly string description;
        readonly string name;

        public CommandInfoAttribute(string commandName, string description, MessageType msgType)
        {
            this.name = commandName;
            this.messageType = msgType;
            this.description = description;

            if (string.IsNullOrEmpty(commandName) || string.IsNullOrWhiteSpace(commandName))
                throw new InvalidOperationException("You can not declare a bot command without a valid command name.");
            if (msgType == null)
                throw new InvalidOperationException("You can not declare a bot command without a valid message type.");
        }

        public string Name
        {
            get { return name; }
        }

        public MessageType MessageType
        {
            get { return messageType; }
        }

        public string Description
        {
            get { return description; }
        }
    }
}
