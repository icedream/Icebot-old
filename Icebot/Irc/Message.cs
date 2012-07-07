using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Icebot.Irc
{
    public class Message
    {
        internal Message()
        { }

        internal Message(Reply reply)
        {
            Message = reply.ArgumentLine.Substring(reply.Arguments[0].Length + 1);
            Source = reply.Sender;
            Target = reply.Arguments[0];
            switch (reply.Command)
            {
                case "privmsg":
                    if (reply.Server.IsValidChannelName(Target))
                        if (Message.StartsWith((char)(01) + "ACTION"))
                            SourceType = MessageType.PublicAction;
                        else
                            SourceType = MessageType.PublicMessage;
                    else
                        if (Message.StartsWith("\x01"))
                        {
                            Message = Message.Trim((char)(01));
                            if (Message.StartsWith("ACTION"))
                            {
                                Message = Message.Substring(7);
                                SourceType = MessageType.PrivateAction;
                            }
                            else
                            {
                                SourceType = MessageType.CtcpRequest;
                            }
                        }
                        else
                            SourceType = MessageType.PrivateMessage;
                    break;
                case "notice":
                    if (reply.Server.IsValidChannelName(Target))
                    {
                        SourceType = MessageType.PublicNotice;
                    }
                    else
                        if (Message.StartsWith("\x01"))
                            SourceType = MessageType.CtcpReply;
                        else
                            SourceType = MessageType.PrivateNotice;
                    break;
            }
        }

        internal Message(string source, string target, MessageType type, string message)
        {
            Source = source;
            Target = target;
            SourceType = type;
            Message = message;
        }

        public string Source { get; internal set; }
        public string Target { get; internal set; }
        public MessageType SourceType { get; internal set; }
        public string Message { get; internal set; }
    }

    /// <summary>
    /// Represents the source from which the message came.
    /// </summary>
    [Flags]
    public enum MessageType
    {
        PrivateMessage = 0,
        PrivateAction = 1,
        PrivateNotice = 2,

        PublicNotice = 3,
        PublicAction = 4,
        PublicMessage = 5,

        CtcpRequest = 6,
        CtcpReply = 7,
        Ctcp = CtcpReply | CtcpRequest,

        Public = PublicMessage | PublicNotice | PublicAction,
        Private = PrivateMessage | PrivateAction | PrivateNotice
    }
}
