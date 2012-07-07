using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Icebot.Irc
{
    public class ChannelInfo
    {
        internal ChannelInfo(string name, int visible, string topic)
        {
            Name = name;
            VisibleUserCount = visible;
            Topic = topic;
        }

        public string Name { get; private set; }
        public string Topic { get; private set; }
        public int VisibleUserCount { get; private set; }
    }
}
