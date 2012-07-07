using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

#if MANAGED_MODES
namespace Icebot.Irc
{
    public class ModeDefinition
    {
        public char Mode { get; internal set; }
        public bool HasParameter { get; internal set; }

        public Mode CreateMode()
        {
            if (HasParameter)
                throw new InvalidOperationException("Mode " + Mode + " needs a parameter");
            Mode m = this as Mode;
            return m;
        }
        public Mode CreateMode(string param)
        {
            if (!HasParameter)
                throw new InvalidOperationException("Mode " + Mode + " must not have a parameter");
            Mode m = this as Mode;
            m.Parameter = param;
            return m;
        }
    }

    public class Mode : ModeDefinition
    {
        public string Parameter { get; internal set; }
    }

    public class ChannelMode : Mode
    {
        public char Prefix { get; internal set; }
    }
}
#endif
