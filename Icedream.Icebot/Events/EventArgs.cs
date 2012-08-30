using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Icedream.Icebot
{
    public class IrcRawMessageEventArgs : EventArgs
    {
        public string Sender { get; private set; }
        public string Command { get; private set; }
        public string[] Arguments { get; private set; }

        public IrcRawMessageEventArgs(string sender, string command, params string[] arguments)
        {
            Sender = sender;
            Command = command.ToUpper();
            Arguments = arguments;
        }
    }

    public class IrcNumericMessageEventArgs : EventArgs
    {
        public string Sender { get; private set; }
        public IrcNumericMethod Numeric { get; private set; }
        public string[] Arguments { get; private set; }
        
        public IrcNumericMessageEventArgs(string sender, ushort numeric, params string[] arguments)
        {
            Sender = sender;
            Numeric = (IrcNumericMethod)numeric;
            Arguments = arguments;
        }
        public IrcNumericMessageEventArgs(IrcRawMessageEventArgs raw)
        {
            Sender = raw.Sender;
            Numeric = (IrcNumericMethod)ushort.Parse(raw.Command);
            Arguments = raw.Arguments;
        }
    }

    public class MessageEventArgs : EventArgs
    {
        public string Sender { get; private set; }
    }
}
