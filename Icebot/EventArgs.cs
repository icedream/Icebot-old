using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Icebot.Api;
using Icebot.Irc;
using Icebot.Bot;

namespace Icebot
{
    public class PluginEventArgs<T> : EventArgs where T : Plugin
    {
        public T Plugin { get; internal set; }

        internal PluginEventArgs(T plugin)
        {
            this.Plugin = plugin;
        }
    }

    public class ErrorEventArgs : EventArgs
    {
        public Exception Exception { get; private set; }

        internal ErrorEventArgs(Exception e)
        {
            this.Exception = e;
        }
    }

    public class IrcRawSendEventArgs : EventArgs
    {
        public string RawLine { get; protected set; }

        /// <summary>
        /// The received command, always uppercase.
        /// </summary>
        public string Command { get; protected set; }
        public string[] Parameters { get; protected set; }
        
        internal IrcRawSendEventArgs()
        {
        }
        internal IrcRawSendEventArgs(string rawline)
        {
            RawLine = rawline;

            string[] spl = rawline.Split(' ');
            Command = spl[0].ToUpper();

            rawline = string.Join(" ", spl.Skip(1).ToArray()); // TODO: Find a faster method

            List<string> parameters = new List<string>();
            var p1 = rawline.Split(':');
            parameters.AddRange(p1[0].Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries));
            if(p1.Length > 1)
                parameters.Add(string.Join(":", p1.Skip(1).ToArray()));
            Parameters = parameters.ToArray();
        }
    }

    public class IrcRawReceiveEventArgs : IrcRawSendEventArgs
    {
        public string SenderMask { get; private set; }
        public string SenderNickname { get { return SenderMask.Split('!', '@')[0]; } }
        public string SenderUsername { get { return SenderMask.Split('!', '@')[1]; } }
        public string SenderHostname { get { return SenderMask.Split('!', '@')[2]; } }
        internal IrcRawReceiveEventArgs()
        {
        }
        internal IrcRawReceiveEventArgs(string rawline)
        {
            RawLine = rawline;

            string[] spl = rawline.Split(' ');
            SenderMask = spl[0].TrimStart(':');
            spl = spl.Skip(1).ToArray();

            Command = spl[0].ToUpper();

            rawline = string.Join(" ", spl.Skip(1).ToArray()); // TODO: Find a faster method

            List<string> parameters = new List<string>();
            var p1 = rawline.Split(':');
            parameters.AddRange(p1[0].Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries));
            if(p1.Length > 1)
                parameters.Add(string.Join(":", p1.Skip(1).ToArray()));
            Parameters = parameters.ToArray();
        }
    }

    public class IrcNumericReplyEventArgs : IrcRawReceiveEventArgs
    {
        private IrcRawReceiveEventArgs _baseargs;

        public new string SenderMask { get { return _baseargs.SenderMask; } }
        public string SenderNickname { get { return SenderMask.Split('!', '@')[0]; } }
        public string SenderUsername { get { return SenderMask.Split('!', '@')[1]; } }
        public string SenderHostname { get { return SenderMask.Split('!', '@')[2]; } }
        public IrcNumericMethod Numeric { get; private set; }
        public new string[] Parameters { get { return _baseargs.Parameters; } }

        internal IrcNumericReplyEventArgs(IrcRawReceiveEventArgs origin)
        {
            _baseargs = origin;

            Numeric = (IrcNumericMethod)short.Parse(origin.Command);
        }

        internal static bool IsValid(IrcRawReceiveEventArgs e)
        {
            uint i = 0;
            return uint.TryParse(e.Command, out i);
        }
    }

    public class IrcMessageEventArgs : EventArgs
    {
        private IrcRawReceiveEventArgs _baseargs;

        public string SenderMask { get { return _baseargs.SenderMask; } }
        public string SenderNickname { get { return SenderMask.Split('!', '@')[0]; } }
        public string SenderUsername { get { return SenderMask.Split('!', '@')[1]; } }
        public string SenderHostname { get { return SenderMask.Split('!', '@')[2]; } }
        public string Target { get { return _baseargs.Parameters[0]; } }
        public string Text { get { return _baseargs.Parameters[1].Trim('\x01'); } }
        public IrcMessageType MessageType
        {
            get
            {
                if (_baseargs.Command.Equals("PRIVMSG"))
                    // PRIVMSG
                    if (_baseargs.Parameters[1].StartsWith("\x01"))
                        // PRIVMSG + CTCP
                        return IrcMessageType.CtcpRequest;
                    else
                        if (_baseargs.Parameters[0].StartsWith("#")) // TODO: Dynamic channel prefix from ISUPPORT reply
                            // PRIVMSG + in channel
                            return IrcMessageType.PublicMessage;
                        else
                            // PRIVMSG + not in channel
                            return IrcMessageType.PrivateMessage;
                else
                    // NOTICE
                    if (_baseargs.Parameters[1].StartsWith("\x01"))
                        // NOTICE + CTCP
                        return IrcMessageType.CtcpReply;
                    else
                        if (_baseargs.Parameters[0].StartsWith("#")) // TODO: Dynamic channel prefix from ISUPPORT reply
                            // NOTICE + in channel
                            return IrcMessageType.PublicNotice;
                        else
                            // NOTICE + not in channel
                            return IrcMessageType.PrivateNotice;
            }
        }

        internal IrcMessageEventArgs(IrcRawReceiveEventArgs origin)
        {
            _baseargs = origin;
        }

        internal static bool IsValid(IrcRawReceiveEventArgs e)
        {
            return e.Command.Equals("privmsg", StringComparison.OrdinalIgnoreCase)
                || e.Command.Equals("notice", StringComparison.OrdinalIgnoreCase);
        }
    }

    public class IcebotCommandEventArgs : EventArgs
    {
        public Command Command { get; private set; }
        public IrcListener Server { get; private set; }
        public ChannelListener Channel { get; private set; }
        public CommandDeclaration Declaration { get; internal set; }

        internal IcebotCommandEventArgs(Command cmd, IrcListener server, CommandDeclaration declaration)
        {
            this.Command = cmd;
            this.Server = server;
        }
        internal IcebotCommandEventArgs(Command cmd, IrcListener server, ChannelListener channel, CommandDeclaration declaration)
        {
            this.Command = cmd;
            this.Server = server;
            this.Channel = channel;
        }
    }
}

