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
            SourceUser = reply.Server.GetUser(reply.Sender);
            switch (reply.Command)
            {
                case "privmsg":
                    SourceType = MessageType.PrivateMessage;
                    break;
            }
        }

        internal Message(User targetuser, MessageType type, string message)
        {

        }

        public User SourceUser { get; internal set; }
        public User TargetUser { get; internal set; }
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

        DccRequest = 6,
        DccReply = 7,

        Public = PublicMessage | PublicNotice | PublicAction,
        Private = PrivateMessage | PrivateAction | PrivateNotice
    }
}
