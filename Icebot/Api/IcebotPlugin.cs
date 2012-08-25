using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Icebot.Irc;
using Icebot.Bot;
using log4net;

namespace Icebot.Api
{
    public class IcebotBasePlugin : IDisposable
    {
        // TODO: Implement command handling without OnPublicCommand/OnPrivateCommand implementation
        public ILog Log { get; internal set; }
        public IcebotPluginSettings Settings { get; internal set; }

        public string Author { get; protected set; }
        public string Title { get; protected set; }
        public Version Version { get; protected set; }
        public string Description { get; protected set; }

        public IcebotBasePlugin()
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

        public void Dispose()
        {
        }
    }

    public class IcebotChannelPlugin : IcebotBasePlugin
    {
        public ChannelListener Channel { get; internal set; }
        public IrcListener Server { get { return Channel.Server; } }
    }

    public class IcebotServerPlugin : IcebotBasePlugin
    {
        public new IrcListener Server { get; internal set; }
    }
}
