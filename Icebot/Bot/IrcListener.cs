using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Icebot.Api;
using System.IO;
using System.Reflection;
using System.Runtime;
using System.Runtime.InteropServices;
using log4net;

namespace Icebot.Bot
{
    public class IrcListener
    {
        /*
        public event EventHandler<PluginEventArgs<IcebotServerPlugin>> PluginLoaded;

        protected virtual void OnPluginLoaded(IcebotServerPlugin plugin)
        {
            if (PluginLoaded != null)
                PluginLoaded.Invoke(this, new PluginEventArgs<IcebotServerPlugin>(plugin));
        }
        internal List<Api.IcebotServerPlugin> Plugins = new List<Api.IcebotServerPlugin>();

        internal void LoadAllPluginsFromFolder(string folder)
        {
            if (Plugins == null)
                Plugins = new List<IcebotServerPlugin>();

            var dir = new DirectoryInfo(folder);
            if (!dir.Exists)
            {
                Log.Warn("Plugin directory \"" + folder + "\" does not exist. No plugins loaded from there.");
                return;
            }
            Log.Debug("Loading plugins from " + dir.FullName + "...");

            foreach (FileInfo file in dir.GetFiles())
            {
                LoadAllPluginsFromFile(file);
            }
        }

        internal void LoadAllPluginsFromFile(string file)
        {
            LoadAllPluginsFromFile(new FileInfo(file));
        }

        internal void LoadAllPluginsFromFile(FileInfo file)
        {
            Assembly asm;

            try
            { asm = Assembly.LoadFrom(file.FullName); }
            catch (Exception ex)
            { Log.Debug(file.Name + " is not a valid assembly (" + ex.Message + ")"); return; }

            var pluginsToLoad =
                from ps in Settings.PluginSettings
                select ps.Name;

            Log.Debug("Loading plugins from " + file.Name + "...");
            Log.Debug("Plugins configured: " + string.Join("; ", pluginsToLoad));

            var types =
                from type in asm.GetExportedTypes()
                where pluginsToLoad.Contains(type.Name, StringComparer.OrdinalIgnoreCase)
                select type;

            foreach (Type type in types)
            {
                if (type.IsPublic
                    && !type.IsAbstract
                    && !type.IsGenericType
                    && type.IsSubclassOf(typeof(IcebotServerPlugin))
                    )
                {
                    Log.Debug("Found plugin " + type.Name);
                    IcebotServerPlugin pluginobj;

                    try
                    {
                        pluginobj = (IcebotServerPlugin)Activator.CreateInstance(type);
                        if (pluginobj == null)
                            throw new Exception("Returned object of the activation was null.");

                        pluginobj.Log = LogManager.GetLogger(type.Name);
                        pluginobj.Settings =
                            (
                                from pl in Settings.PluginSettings
                                where pl.Name.Equals(type.Name, StringComparison.OrdinalIgnoreCase)
                                select pl
                            ).First();
                        pluginobj.Server = this;

                        Log.Info("Loaded plugin " + type.Name);
                        Plugins.Add(pluginobj);

                        pluginobj.Run();
                    }
                    catch (Exception loadexception)
                    { Log.Debug("Can't load plugin \"" + type.Name + "\": " + loadexception.ToString()); }
                }
            }
        }

        internal void LoadAllPlugins()
        {
            LoadAllPluginsFromFile(System.Reflection.Assembly.GetExecutingAssembly().Location); // internal plugins
            LoadAllPluginsFromFolder(Path.Combine(Environment.CurrentDirectory, "plugins")); // external plugins
        }

        internal void UnloadAllPlugins()
        {
            foreach (var plugin in Plugins)
            {
                Log.Debug("Unloading " + plugin.GetType().Name + "...");
                Plugins.Remove(plugin);
                ((IDisposable)plugin).Dispose();
            }
        }

         */
        
        protected ILog Log { get { return LogManager.GetLogger("IrcListener/" + DisplayName); } }

        public string Hostname { get; set; }
        public ushort Port { get; set; }
        public string Password { get; set; }
        public string Nickname { get; set; }
        public string Username { get; set; }
        public string Realname { get; set; }
        public string DisplayName { get; set; }
        public bool ReceiveWallops { get; set; }
        public bool Invisible { get; set; }
        public bool UseSSL { get; set; }
        public string Prefix { get; set; }

        public Irc.IrcClient Irc { get; set; }

        public event EventHandler<IcebotCommandEventArgs> CommandReceived;
        protected void OnCommandReceived(IcebotCommandEventArgs e)
        {
            if (CommandReceived != null)
                CommandReceived.Invoke(this, e);

            /*
            var c =
                from cmd in _registeredCommands
                where cmd.Name.Equals(e.Command.Command, StringComparison.OrdinalIgnoreCase) && ((cmd.MessageType & e.Command.Type) != 0) && cmd.Callback != null
                select cmd;

            if (c.Count() == 1)
                c.First().Callback.Invoke(e.Command);
             */
        }

        /*internal List<IcebotCommandDeclaration> _registeredCommands = new List<IcebotCommandDeclaration>();
        public void RegisterCommand(IcebotCommandDeclaration declaration)
        {
            // garbage collect
            foreach (var cmd in from c in _registeredCommands where c.Plugin == null || c.Callback == null select c)
                _registeredCommands.Remove(cmd);

            // check if command is already registered
            var m =
                from c in _registeredCommands
                where c.Name.Equals(declaration.Name, StringComparison.OrdinalIgnoreCase) && ((c.MessageType & declaration.MessageType) != 0)
                select c;
            if (m.Count() > 0)
            {
                Log.Warn("Command " + declaration.Name + " by " + declaration.Plugin.GetType().Name
                    + " is already occupied by " + m.First().Plugin.GetType().Name + "!");
                int i = 0;
                while (m.Count() > 0)
                {
                    m =
                        from c in _registeredCommands
                        where c.Name.Equals(declaration.Name + i.ToString(), StringComparison.OrdinalIgnoreCase)
                        select c;
                }
                declaration.Name += i.ToString();
                Log.Warn("Command by " + declaration.Plugin.GetType().Name + " renamed to " + declaration.Name + "!");
            }

            // add command
            _registeredCommands.Add(declaration);
            Log.Info("Command " + declaration.Name + " registered by plugin " + declaration.Plugin.GetType().Name);
        }
        public void UnregisterCommand(IcebotCommandDeclaration declaration)
        {
            _registeredCommands.Remove(declaration);
        }
         */
        
        public IrcListener()
        {
            this.__construct();
        }
        ~IrcListener()
        {
            Stop();
        }
  
        private void __construct()
        {
            Irc = new Irc.IrcClient();

            // Setup events
            Irc.Connected += new EventHandler(Irc_Connected);
            Irc.Disconnected += new EventHandler(Irc_Disconnected);
            Irc.NumericReceived += new EventHandler<IrcNumericReplyEventArgs>(Irc_NumericReceived);
            Irc.MessageReceived += new EventHandler<IrcMessageEventArgs>(Irc_MessageReceived);
        }

        void Irc_MessageReceived(object sender, IrcMessageEventArgs e)
        {
            try
            {
                object o = this;
                //if ((e.MessageType & global::Icebot.Irc.IrcMessageType.Public) != 0)
                //    o = (from c in ChannelInstances where c.Settings.Name.Equals(e.Target, StringComparison.OrdinalIgnoreCase) select c).First();

                if (Command.IsValid(o, e, Prefix))
                    OnCommandReceived(new IcebotCommandEventArgs(new Command(e, this, this.Prefix), this, null));
            }
            catch (Exception ex)
            {
                // exception printing
                if ((e.MessageType & global::Icebot.Irc.IrcMessageType.Public) != 0)
                    Irc.SendMessage(e.Target, "Exception: " + ex.Message);
                else
                    Irc.SendNotice(e.SenderNickname, "Exception: " + ex.Message);

                // stacktrace printing
                string[] st = ex.StackTrace.Split(new string[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries);
                Irc.SendNotice(e.SenderNickname, "Stacktrace:");
                foreach (string s in st)
                    Irc.SendNotice(e.SenderNickname, "> " + s);
                   
            }
        }
        void Irc_NumericReceived(object sender, IrcNumericReplyEventArgs e)
        {
            switch (e.Numeric)
            {
                // TODO: Parse channel-specific numeric replies
                case global::Icebot.Irc.IrcNumericMethod.RPL_BANLIST:
                    break;
            }
        }
        void Irc_Disconnected(object sender, EventArgs e)
        {
        }
        void Irc_Connected(object sender, EventArgs e)
        {
            Irc.Login(Nickname, Username,  Realname,  Password, ReceiveWallops, Invisible);
        }
        internal void Start()
        {
            Irc.Connect(Hostname, Port, UseSSL);
        }
        internal void Stop()
        {
            Irc.Disconnect();
        }
    }
}
