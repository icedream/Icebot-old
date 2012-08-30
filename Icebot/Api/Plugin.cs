using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Icebot.Irc;
using Icebot.Bot;
using log4net;

namespace Icebot.Api
{
    public class Plugin : IDisposable
    {
        public ILog Log { get; internal set; }
        public PluginSettings Settings { get; internal set; }
        public Icebot Host { get; internal set; }
        internal List<CommandDeclaration> _registeredCommands = new List<CommandDeclaration>();

        public string Author { get; protected set; }
        public string Title { get; protected set; }
        public Version Version { get; protected set; }
        public string Description { get; protected set; }

        public Plugin()
        {
            // Default metadata to assembly metadata
            var ass = System.Reflection.Assembly.GetExecutingAssembly();
            var name = ass.GetName();
            Version = name.Version;
            Title = GetType().Name;
            Author = "Unknown";
            object[] descriptions = ass.GetCustomAttributes(typeof(System.Reflection.AssemblyDescriptionAttribute), false);
            object[] titles = ass.GetCustomAttributes(typeof(System.Reflection.AssemblyTitleAttribute), false);
            if (descriptions.Length > 0)
                Description = ((System.Reflection.AssemblyDescriptionAttribute)(descriptions[0])).Description;
            if (titles.Length > 0)
                Title = ((System.Reflection.AssemblyTitleAttribute)(titles[0])).Title;
        }

        public virtual void Run()
        {
        }
        public void RegisterCommand(CommandDeclaration declaration)
        {
            Log.Info("Registering command \"" + declaration.Name + "\"");

            declaration.Plugin = this;
            declaration.Host = this.Host;

            // add command
            _registeredCommands.Add(declaration);
            Host.RegisterCommand(declaration);
        }
        public void UnregisterCommand(CommandDeclaration declaration)
        {
            Log.Info("Unregistering command \"" + declaration.Name + "\"");

            Host.UnregisterCommand(declaration);
            _registeredCommands.Remove(declaration);
        }

        public virtual void Dispose()
        {
            foreach (var cmd in _registeredCommands)
                UnregisterCommand(cmd);
        }
    }

    public class IcebotChannelPlugin : Plugin
    {
        /*
        public ChannelListener Channel { get; internal set; }
        public IrcListener Server { get { return Channel.Server; } }
         */
    }

    public class IcebotServerPlugin : Plugin
    {
        /*
        public new IrcListener Server { get; internal set; }
         */
    }
}
