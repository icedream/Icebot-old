using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using log4net;
using log4net.Core;
using log4net.Config;

namespace Icedream.Icebot
{
    // TODO: Split up ServerListener.cs

    internal class Logging
    {
        protected ILog Log { get { return LogManager.GetLogger(GetType()); } }
    }

    public class ServerListener : Logging
    {
        // TODO: Do the server networking stuff
        public string Prefix = "!";

        public string Hostname = "localhost";
        public int Port = 6667;
        public bool Ssl = false; // TODO: Implement SSL connection + SSL validation

        public IrcUser GetUser(string hostmask);

    }

    public class Sender
    {
        public IrcUser User { get { return Server.GetUser(Hostmask); } }
        public ServerListener Server { get; private set; }

        public string Hostmask { get; private set; }
        public string Nickname { get { return Hostmask.Split('@', '!')[0]; } }
        public string Username { get { return Hostmask.Split('@', '!')[1]; } }
        public string Hostname { get { return Hostmask.Split('@', '!')[2]; } }
        public bool IsVirtualHostname { get { try { return System.Net.Dns.GetHostEntry(Hostname).AddressList.Length > 0; } catch { return true; } } }
    }

    public class ChannelListener : Logging
    {
        // TODO: Do the channel listening stuff
    }

    public class IrcUser
    {
        // TODO: IrcUser

        public bool IsHostmaskMatch(string hostmask);
    }

    public class ChannelUser
    {
        // TODO: ChannelUser
    }

    public class Message
    {
        // TODO: Do message parsing
    }

    public class Command
    {
        // TODO: Do the command parsing
    }
}
