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
        List<IrcListener> _ircListeners = new List<IrcListener>();
        List<ChannelListener> _channelListeners = new List<ChannelListener>();
        List<IcebotBasePlugin> _loadedPlugins = new List<IcebotBasePlugin>();

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
                c.Stop();
            listener.Stop();
        }
        public void RemoveIrcListener(string hostname, int port = -1)
        {
            foreach (var i in from il in _ircListeners where il.Hostname == hostname && (port > 0 ? il.Port == port : true) select il)
                RemoveIrcListener(i);
        }

        void Irc_NumericReceived(object sender, IrcNumericReplyEventArgs e)
        {
            IrcListener listener = sender as IrcListener;

            if (e.Numeric == Irc.IrcNumericMethod.RPL_WELCOME)
            {
                foreach (var c in from cl in _channelListeners where cl.Server == listener select cl)
                    c.Start();
            }
        }

        internal log4net.ILog Log { get { return log4net.LogManager.GetLogger("Icebot"); } }

        public void UnloadPlugin(string pluginName)
        {
            (
                from p in _loadedPlugins
                where p.GetType().Name.Equals(pluginName, StringComparison.OrdinalIgnoreCase)
                select p
                ).First().Dispose();
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
        public void LoadPlugin(IcebotBasePlugin plugin)
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
                var plugin = (IcebotBasePlugin)Activator.CreateInstance(pluginType);

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
                where t.IsSubclassOf(typeof(IcebotBasePlugin))
                && !t.IsAbstract // Even needed?
                select t;

            foreach (var pl in plugins)
                LoadPlugin(pl);
        }
        public void LoadPluginFromAssembly(string assemblyFile, string pluginname)
        {
            LoadPluginFromAssembly(Assembly.LoadFrom(assemblyFile, pluginname));
        }
        public void LoadPluginFromAssembly(Assembly assembly, string pluginname)
        {
            var plugins =
                from t in assembly.GetTypes()
                where t.IsSubclassOf(typeof(IcebotBasePlugin))
                && !t.IsAbstract // Even needed?
                && t.Name.Equals(pluginname, StringComparison.OrdinalIgnoreCase)
                select t;

            foreach (var pl in plugins)
                LoadPlugin(pl);
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
