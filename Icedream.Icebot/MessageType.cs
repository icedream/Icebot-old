using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Icedream.Icebot
{
    public enum MessageType
    {
        PublicMessage       = 0x0001,
        PublicNotice        = 0x0002,

        PrivateMessage      = 0x0004,
        PrivateNotice       = 0x0008,

        CtcpRequest         = 0x0010,
        CtcpReply           = 0x0020,

        Private             = PrivateMessage | PrivateNotice,
        Public              = PublicMessage | PublicNotice,
        Ctcp                = CtcpRequest | CtcpReply,

        All                 = Private | Public | Ctcp
    }
}
