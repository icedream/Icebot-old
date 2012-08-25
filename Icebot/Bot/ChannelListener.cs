using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Icebot.Api;
using Icebot.Irc;
using System.IO;
using System.Reflection;
using System.Runtime;
using System.Runtime.InteropServices;
using log4net;

namespace Icebot.Bot
{
    public class ChannelListener : IDisposable
    {
        internal ChannelListener(IrcListener server, IcebotChannelSettings settings)
        {
            Server = server;
            Log = LogManager.GetLogger("Icebot/" + Server.DisplayName + "#" + ChannelName);

            Server.Irc.MessageReceived += new EventHandler<IrcMessageEventArgs>(Irc_MessageReceived);
            //Server.Irc.Disconnecting += new EventHandler(Irc_Disconnecting);
        }

        void Irc_Disconnecting(object sender, EventArgs e)
        {
            // Automatically stop listening when disconnected
            //Stop();
        }

        protected ILog Log { get; private set; }
        public IrcListener Server { get; internal set; }

        protected string _prefix = "!";

        public string ChannelName { get; set; }
        public string Key { get; set; }
        public string Prefix
        { get { return _prefix; } }

        public string Topic { get; set; }
        public List<char> Modes { get; set; }
        public List<IrcChannelUser> Users { get; set; }

        void Irc_MessageReceived(object sender, IrcMessageEventArgs e)
        {
            if (e.Target.Split(',').Contains(Settings.Name, StringComparer.OrdinalIgnoreCase))
            {
                if (MessageReceived != null)
                    MessageReceived.Invoke(this, e);

                if (IcebotCommand.IsValid(this, e))
                    OnCommandReceived(new IcebotCommandEventArgs(new IcebotCommand(this, e)));
            }
        }
        public event EventHandler<IrcMessageEventArgs> MessageReceived;

        internal List<IcebotCommandDeclaration> _registeredCommands = new List<IcebotCommandDeclaration>();
        public event EventHandler<IcebotCommandEventArgs> CommandReceived;

        protected void OnCommandReceived(IcebotCommandEventArgs e)
        {
            if (CommandReceived != null)
                CommandReceived.Invoke(this, e);

            var c =
                from cmd in _registeredCommands
                where cmd.Name.Equals(e.Command.Command, StringComparison.OrdinalIgnoreCase) && ((cmd.MessageType & e.Command.Type) != 0) && cmd.Callback != null
                select cmd;

            foreach (var cmd in c)
                cmd.Callback.Invoke(e.Command);
        }

        public void Dispose()
        {
            Stop();
        }

        public void Start()
        {
            if (!Server.Irc.IsConnected)
                throw new Exception("Not connected to the server.");
            
            if(string.IsNullOrEmpty(ChannelName))
                throw new Exception("No channel name given.");

            if (Key == null)
                Server.Irc.SendCommand("join", ChannelName);
            else
                Server.Irc.SendCommand("join", ChannelName, Key);

            //LoadAllPlugins();
        }
        public void Stop()
        {
            if (Server.Irc.IsConnected)
                Server.Irc.SendCommand("part", ChannelName);
        }   

        public void SendMessage(string text)
        { Server.Irc.SendMessage(ChannelName, text); }
        public void SendNotice(string text)
        { Server.Irc.SendNotice(ChannelName, text); }
        public void SendAction(string text)
        { this.SendNotice("\x01ACTION " + text + "\x01");  }
    }
}
