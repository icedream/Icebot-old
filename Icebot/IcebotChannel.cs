/**
 * Icebot - Extensible, multi-functional C# IRC bot
 * Copyright (C) 2012 Carl Kittelberger
 *
 * This program is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 *
 * You should have received a copy of the GNU General Public License
 * along with this program.  If not, see <http://www.gnu.org/licenses/>.
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using Icebot.Irc;
using Icebot.Api;
using log4net;

namespace Icebot
{
    public class IcebotChannel
    {
        public IcebotChannel(IcebotServer server, IcebotChannelConfiguration conf)
        {
            PublicCommands = new PluginCommandContainer();
            Configuration = conf;

            Server = server;
        }


        #region Plugin implementation
        private List<ChannelPlugin> _plugins = new List<ChannelPlugin>();
        public ChannelPlugin[] Plugins { get { return _plugins.ToArray(); } }
        public string[] PluginNames
        {
            get
            {
                List<string> l = new List<string>();
                foreach (ChannelPlugin sp in _plugins)
                    l.Add(sp.PluginName);
                return l.ToArray();
            }
        }

        protected int GetPluginTypeCount(string pluginname)
        {
            int loaded = 0;
            foreach (Plugin pl in _plugins)
                if (pl.PluginName == pluginname)
                    loaded++;
            return loaded;
        }

        public void LoadAllPlugins()
        {
            foreach (IcebotChannelPluginConfiguration config in Configuration.Plugins)
                LoadPlugin(config);
        }

        public void LoadPlugin(IcebotChannelPluginConfiguration config)
        {
            DirectoryInfo dir = new DirectoryInfo("plugins");
            dir.Create();

            List<FileInfo> pluginfiles = new List<FileInfo>();
            pluginfiles.Add(new FileInfo(System.Diagnostics.Process.GetCurrentProcess().ProcessName));
            pluginfiles.AddRange(dir.GetFiles("*.dll", SearchOption.TopDirectoryOnly));
            foreach (FileInfo pluginfileinfo in pluginfiles)
            {
                try
                {
                    Assembly pluginfile = Assembly.LoadFrom(pluginfileinfo.FullName);
                    int pluginsfromfile = 0;
                    int pluginsfoundinfile = 0;

                    // Search for loadable plugin classes
                    foreach (Type exportedType in pluginfile.GetExportedTypes())
                    {
                        if (
                            !exportedType.IsAbstract
                            && exportedType.IsClass
                            && exportedType.IsPublic
                            && exportedType.IsSubclassOf(typeof(Plugin))
                            )
                        {
                            try
                            {
                                ChannelPlugin plugininstance = (ChannelPlugin)Activator.CreateInstance(exportedType);
                                plugininstance._channel = this;
                                plugininstance._config = config;
                                plugininstance.PluginName = exportedType.Name;
                                plugininstance.InstanceNumber = 1 + GetPluginTypeCount(plugininstance.PluginName);
                                _plugins.Add(plugininstance);
                                _log.Info("Successfully loaded " + plugininstance.PluginName + " (Instance #" + plugininstance.InstanceNumber + ")");
                            }
                            catch (Exception instanceerror)
                            {
                                _log.Error("Found " + exportedType.Name + ", but failed loading as server plugin (" + instanceerror.Message + "). Check if the plugin supports this Icebot version.");
                            }
                        }
                    }

                    if (pluginsfromfile == pluginsfoundinfile)
                        _log.Info("Loaded " + pluginsfromfile + " of " + pluginsfoundinfile + " plugins from assembly");
                    else
                        _log.Warn("Loaded only " + pluginsfromfile + " of " + pluginsfoundinfile + " plugins from assembly! Please check your configuration and be sure to have the newest version of all plugins.");
                }
                catch (Exception assemblyloaderror)
                {
                    _log.Warn("Could not load " + pluginfileinfo.Name + ": Not a valid assembly (" + assemblyloaderror.Message + "). Remove from plugins folder or to something other than *.dll.");
                }
            }
        }

        public void UnloadPlugin(ChannelPlugin plugin)
        {
            _plugins.Remove(plugin);
            ((IDisposable)plugin).Dispose();
        }
        #endregion

        public IcebotServer Server { get; set; }

        public IcebotChannelConfiguration Configuration { get; internal set; }

        public PluginCommandContainer PublicCommands { get; internal set; }

        protected ILog _log
        {
            get { return LogManager.GetLogger(this.Server.Configuration.ServerName + ":" + this.Configuration.ChannelName); }
        }

        public event OnPublicBotCommandHandler BotCommandReceived;
        public event OnChannelUserHandler UserJoined;
        public event OnChannelUserHandler UserParted;
        public event OnMessageHandler MessageReceived;

        internal List<ChannelUser> _users
        {
            get
            {
                List<ChannelUser> u = new List<ChannelUser>();
                foreach (User user in Server.Users)
                    if (user.Channels.Contains(this))
                    {
                        ChannelUser cu = new ChannelUser(user);
                        cu.Channel = this;
                        u.Add(cu);
                    }
                return u;
            }
        }
        public ChannelUser[] Users
        { get { return _users.ToArray(); } }

        public DateTime CreationTime;

        public string Topic { get; internal set; }

        public void SendMessage(string text)
        {
            Server.SendMessage(Configuration.ChannelName, text);
        }
        public void SendNotice(string text)
        {
            Server.SendNotice(Configuration.ChannelName, text);
        }
        public void Kick(string who)
        {
            Server.SendCommand("kick", who);
        }
        public void Ban(string who)
        {
            Mode("+b " + who);
        }
        public void Unban(string who)
        {
            Mode("-b " + who);
        }
        public void Voice(string who)
        {
            Mode("+v " + who);
        }
        public void Devoice(string who)
        {
            Mode("-v " + who);
        }
        public void Op(string who)
        {
            Mode("+o " + who);
        }
        public void Deop(string who)
        {
            Mode("-o " + who);
        }
        public void Halfop(string who)
        {
            Mode("+h " + who);
        }
        public void Dehalfop(string who)
        {
            Mode("-h " + who);
        }
        public void Protect(string who)
        {
            Mode("+a " + who);
        }
        public void Deprotect(string who)
        {
            Mode("-a " + who);
        }
        public void SetTopic(string text)
        {
            Server.SendCommand("topic", text);
        }
        public void Mode(params string[] modes)
        {
            Server.Mode(this.Configuration.ChannelName, modes);
        }
        public void Leave()
        {
            Server.Part(this);
        }
        public bool HasUser(Hostmask hostmask)
        {
            foreach (ChannelUser u in _users)
                if (u.User.Hostmask == hostmask)
                    return true;
            return false;
        }
        public bool HasUser(User user)
        { return HasUser(user.Hostmask); }
        public bool HasUser(ChannelUser user)
        { return HasUser(user.User.Hostmask); }

        public ChannelUser GetUser(Hostmask hostmask)
        {
            foreach (ChannelUser cu in _users)
                if (cu.User.Hostmask == hostmask)
                    return cu;
            return null;
        }

        internal ChannelUser ForceJoinUser(User user)
        {
            ChannelUser cu = new ChannelUser(user, this);
            if (UserJoined != null)
                UserJoined.Invoke(cu);
            return cu;
        }

        internal void ForceMessageReceived(Message msg)
        {
            if (this.MessageReceived != null)
                MessageReceived.Invoke(msg);
        }

        internal void ForcePartUser(ChannelUser user)
        {
            _users.Remove(user);
            if (UserParted != null)
                UserParted.Invoke(user);
        }

        public string ChannelModes { get; internal set; }
    }
}
