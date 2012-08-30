using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using System.IO;
using System.Xml;
using System.Xml.Serialization;
using System.Xml.Linq;
using Icebot.Bot;
using Icebot.Api;

namespace Icebot
{
    /// <summary>
    /// The Icebot host, which can host multiple IrcListeners.
    /// </summary>
    public class Icebot
    {
        internal List<IrcListener> _ircListeners = new List<IrcListener>();
        internal List<ChannelListener> _channelListeners = new List<ChannelListener>();
        // Dictionary<Channel, PluginsList>
        internal Dictionary<ChannelListener, List<Plugin>> _enabledPlugins = new Dictionary<ChannelListener, List<Plugin>>();
        internal List<Plugin> _loadedPlugins = new List<Plugin>();
        internal List<CommandDeclaration> _registeredCommands = new List<CommandDeclaration>();

        public Icebot()
        {
            _ircListeners = new List<IrcListener>();
        }

        public void Start()
        {
            foreach (var server in _ircListeners)
                server.Start();
        }
        public void Stop()
        {
            foreach (var server in _ircListeners)
                server.Stop();
        }

        public Plugin[] GetLoadedPluginsOnHost()
        {
            return _loadedPlugins.ToArray<Plugin>();
        }
        public Plugin[] GetEnabledPluginsOnChannel(ChannelListener channel)
        {
            return _enabledPlugins[channel].ToArray<Plugin>();
        }
        public CommandDeclaration[] GetCommandsInChannel(ChannelListener channel)
        {
            // TODO: Make a short query (from ... in ... where ... select) out of this
            List<CommandDeclaration> a = new List<CommandDeclaration>();
            foreach (Plugin pl in GetEnabledPluginsOnChannel(channel))
                a.AddRange(pl._registeredCommands);
            return a.ToArray<CommandDeclaration>();
        }
        public ChannelListener GetChannelListener(IrcListener server, string channelName)
        {
            var cl =
                from c in _channelListeners
                where c.ChannelName.Equals(channelName, StringComparison.OrdinalIgnoreCase)
                && c.Server == server
                select c;
            //if (cl.Count() > 1) throw new MultipleResultsFoundException("Multiple channels found.");
            if (cl.Count() == 0) throw new NoResultsFoundException("No channels found.");

            return cl.First();
        }
        public ChannelListener[] GetChannelListeners(IrcListener server)
        {
            var cl =
                from c in _channelListeners
                where c.Server == server
                select c;

            return cl.ToArray<ChannelListener>();
        }
        public ChannelListener CreateChannelListener(IrcListener server, string channelName)
        {
            var cl = new ChannelListener(server, channelName);
            cl.CommandReceived += new EventHandler<IcebotCommandEventArgs>(OnCommandReceived);
            _enabledPlugins.Add(cl, new List<Plugin>());
            Log.Debug("Created channel listener for " + server.DisplayName + " on channel " + channelName);
            return cl;
        }
        private void OnCommandReceived(object sender, IcebotCommandEventArgs e)
        {
            IEnumerable<CommandDeclaration> commands = null;
            var arglength = e.Command.Arguments.Length;
            var msgtype = e.Command.Type;
            if (sender is ChannelListener)
            {
                commands =
                    from cmd in _registeredCommands
                    where GetEnabledPluginsOnChannel(e.Channel).Contains(cmd.Plugin)
                    select cmd;
                commands =
                    from cmd in commands
                    where cmd.Name.Equals(e.Command.CommandName, StringComparison.OrdinalIgnoreCase)
                    select cmd;
                commands =
                    from cmd in commands
                    where (cmd.MessageType & msgtype) != 0
                    select cmd;
                commands = 
                    from cmd in commands
                    where cmd.ArgumentNames.Length.Equals(arglength)
                    select cmd;
                commands =
                    from cmd in commands
                    where cmd.Callback != null
                    select cmd;
            }
            else
                commands =
                    from cmd in _registeredCommands
                    where
                        cmd.Name.Equals(e.Command.CommandName, StringComparison.OrdinalIgnoreCase)
                        && (cmd.MessageType & e.Command.Type) != 0
                        && cmd.ArgumentNames.Count().Equals(e.Command.Arguments.Count())
                        && cmd.Callback != null
                    select cmd;

            if (commands.Count() > 1)
                throw new Exception("Invalid logic: Found more than 1 command declaration for !" + e.Command.CommandName + " with " + e.Command.Arguments.Count() + " arguments!");
            if (commands.Count() == 0)
                return;

            e.Declaration = commands.First();
            e.Declaration.Callback.Invoke(this, e);
        }
        public void RemoveChannelListener(ChannelListener channel)
        {
            channel.Stop();
            _enabledPlugins.Remove(channel);
            Log.Debug("Removed channel listener for " + channel.Server.DisplayName + " on channel " + channel.ChannelName);
        }
        public void RemoveChannelListener(params ChannelListener[] channel)
        {
            foreach (var c in channel)
                RemoveChannelListener(c);
        }
        public IrcListener GetIrcListener(string hostname, int port)
        {
            var il = GetIrcListeners(hostname, port);

            //if(il.Count() > 1) throw new MultipleResultsFoundException("Multiple servers found.");
            if (il.Count() == 0) throw new NoResultsFoundException("No servers found.");

            return il.First();
        }
        public IrcListener[] GetIrcListeners(string hostname, int port)
        {
            var il =
                from s in _ircListeners
                where s.Hostname.Equals(hostname, StringComparison.OrdinalIgnoreCase)
                && s.Port == port
                select s;

            return il.ToArray<IrcListener>();
        }
        public IrcListener GetIrcListener(string hostname)
        {
            var il = GetIrcListeners(hostname);

            //if(il.Count() > 1) throw new MultipleResultsFoundException("Multiple servers found.");
            if (il.Count() == 0) throw new NoResultsFoundException("No servers found.");

            return il.First();
        }
        public IrcListener[] GetIrcListeners(string hostname)
        {
            var il =
                from s in _ircListeners
                where s.Hostname.Equals(hostname, StringComparison.OrdinalIgnoreCase)
                select s;

            return il.ToArray<IrcListener>();
        }
        public IrcListener CreateIrcListener(string hostname, int port = 6667)
        {
            var listener = new IrcListener();
            listener.Hostname = hostname;
            listener.Port = 6667;
            listener.Irc.NumericReceived += new EventHandler<IrcNumericReplyEventArgs>(Irc_NumericReceived);
            _ircListeners.Add(listener);
            return listener;
        }
        public void RemoveIrcListener(IrcListener listener)
        {
            foreach (var c in from cl in _channelListeners where cl.Server == listener select cl)
                RemoveChannelListener(c);
            Log.Debug("Stopping listener for server " + listener.DisplayName);
            listener.Stop();
        }
        public void RemoveIrcListener(string hostname, int port = -1)
        {
            foreach (var i in from il in _ircListeners where il.Hostname == hostname && (port > 0 ? il.Port == port : true) select il)
                RemoveIrcListener(i);
        }

        private void Irc_NumericReceived(object sender, IrcNumericReplyEventArgs e)
        {
            IrcListener listener = sender as IrcListener;

            if (e.Numeric == Irc.IrcNumericMethod.RPL_WELCOME)
            {
                // Join all channels which are being listened on by the host
                foreach (var c in from cl in _channelListeners where cl.Server == listener select cl)
                    c.Start();
            }
        }

        internal log4net.ILog Log { get { return log4net.LogManager.GetLogger("Icebot"); } }

        public void UnloadPlugin(string pluginName)
        {
            UnloadPlugin((
                from p in _loadedPlugins
                where p.GetType().Name.Equals(pluginName, StringComparison.OrdinalIgnoreCase)
                select p
                ).First());
        }
        public void UnloadPlugin(Plugin plugin)
        {
            Log.Debug("Unloading plugin " + plugin.GetType().Name);
            _loadedPlugins.Remove(plugin);
            plugin.Dispose();
        }
        public bool IsPluginLoaded(string pluginName)
        {
            return (
                from p in _loadedPlugins
                where p.GetType().Name.Equals(pluginName, StringComparison.OrdinalIgnoreCase)
                select p
                ).Count() > 0;
        }
        public void LoadPlugin(string pluginName)
        {
            DirectoryInfo pluginDir = new DirectoryInfo("plugins");
            LoadPluginFromAssembly(Assembly.GetExecutingAssembly(), pluginName);
            foreach (var file in pluginDir.EnumerateFiles("*.dll", SearchOption.TopDirectoryOnly))
            {
                try
                {
                    LoadPluginFromAssembly(file.FullName, pluginName);
                }
                catch { { } }
            }
        }
        public void LoadPlugin(Plugin plugin)
        {
            // Already loaded?
            if (IsPluginLoaded(plugin.GetType().Name))
                throw new Exception("Plugin already loaded.");

            _loadedPlugins.Add(plugin);
        }
        internal void LoadPlugin(Type pluginType)
        {
            // TODO: Support for multiple instances of a plugin.
            try
            {
                var plugin = (Plugin)Activator.CreateInstance(pluginType);

                LoadPlugin(plugin);
            }
            catch (Exception e)
            {
                Log.Warn("Could not load plugin \"" + pluginType.Name + "\": " + e.Message);
                return;
            }
        }
        public void LoadPluginsFromAssembly(string assemblyFile)
        {
            LoadPluginsFromAssembly(Assembly.LoadFrom(assemblyFile));
        }
        public void LoadPluginsFromAssembly(Assembly assembly)
        {
            var plugins =
                from t in assembly.GetTypes()
                where t.IsSubclassOf(typeof(Plugin))
                && !t.IsAbstract // Even needed?
                select t;

            foreach (var pl in plugins)
                LoadPlugin(pl);
        }
        public void LoadPluginFromAssembly(string assemblyFile, string pluginname)
        {
            LoadPluginFromAssembly(Assembly.LoadFrom(assemblyFile), pluginname);
        }
        public void LoadPluginFromAssembly(Assembly assembly, string pluginname)
        {
            var plugins =
                from t in assembly.GetTypes()
                where t.IsSubclassOf(typeof(Plugin))
                && !t.IsAbstract // Even needed?
                && t.Name.Equals(pluginname, StringComparison.OrdinalIgnoreCase)
                select t;

            foreach (var pl in plugins)
                LoadPlugin(pl);
        }
        public void EnablePlugin(ChannelListener channel, string pluginname)
        {
            foreach (var p in
                from plugin in _loadedPlugins
                where plugin.GetType().Name.Equals(pluginname)
                && !_loadedPlugins.Contains(plugin)
                select plugin)
                EnablePlugin(channel, p);
        }
        public void EnablePlugin(ChannelListener channel, Plugin plugin)
        {
            if (!_loadedPlugins.Contains(plugin))
                throw new InvalidOperationException("Plugin not loaded by host.");
            if (!_channelListeners.Contains(channel))
                throw new InvalidOperationException("Channel not loaded by host.");
            if (!_enabledPlugins[channel].Contains(plugin))
                throw new InvalidOperationException("Plugin already enabled (multiple instances not supported).");

            Log.Debug("Enabling " + plugin.GetType().Name + " on channel " + channel.ChannelName);

            _enabledPlugins[channel].Add(plugin);
        }
        public void DisablePlugin(ChannelListener channel, Plugin plugin)
        {
            if (!_loadedPlugins.Contains(plugin))
                throw new InvalidOperationException("Plugin not loaded by host.");
            if (!_channelListeners.Contains(channel))
                throw new InvalidOperationException("Channel not loaded by host.");
            if (_enabledPlugins[channel].Contains(plugin))
                throw new InvalidOperationException("Plugin not enabled.");

            _enabledPlugins[channel].Remove(plugin);
        }

        public void RegisterCommand(CommandDeclaration declaration)
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
        }
        public void UnregisterCommand(CommandDeclaration declaration)
        {
            // remove command
            _registeredCommands.Remove(declaration);
        }
    }

    [Serializable]
    public class MultipleResultsFoundException : Exception
    {
        public MultipleResultsFoundException() { }
        public MultipleResultsFoundException(string message) : base(message) { }
        public MultipleResultsFoundException(string message, Exception inner) : base(message, inner) { }
        protected MultipleResultsFoundException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context)
            : base(info, context) { }
    }

    [Serializable]
    public class NoResultsFoundException : Exception
    {
        public NoResultsFoundException() { }
        public NoResultsFoundException(string message) : base(message) { }
        public NoResultsFoundException(string message, Exception inner) : base(message, inner) { }
        protected NoResultsFoundException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context)
            : base(info, context) { }
    }
}
