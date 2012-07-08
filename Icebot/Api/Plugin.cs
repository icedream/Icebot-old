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
using System.Xml;
using System.Xml.Serialization;
using System.Reflection;
using System.Linq.Expressions;
using log4net;
using Icebot.Irc;

namespace Icebot.Api
{
    public class Plugin : IDisposable
    {
        protected ILog Log
        { get { return LogManager.GetLogger(this.GetType().Name + this.InstanceNumber.ToString()); } }

        internal string PluginName;
        public int InstanceNumber { get; internal set; }

        internal PluginInfo _pluginInfo = new PluginInfo();
        public PluginInfo PluginInfo { get { return _pluginInfo; } protected set { _pluginInfo = value; } }

        internal PluginCommandContainer _cmd = new PluginCommandContainer();
        public PluginCommandContainer Commands
        { get { return _cmd; } }

        public virtual void OnAfterLoad()
        {
        }

        public virtual void OnRegisterCommands()
        {
            Log.Debug("No commands to register.");
        }
    }

    public class PluginCommand
    {
        public override string ToString()
        {
            return Description;
        }
        public string Name { get; set; }
        public Dictionary<string, Type[]> Arguments { get; set; }
        public string Description { get; set; }
        public CommandDelegate Callback { get; set; }
        public MessageType SourceType = MessageType.Public;

        public PluginCommand(MessageType sources, string name, CommandDelegate cmd)
        {
            __construct();
            Name = name; Callback = cmd;
        }

        public PluginCommand(MessageType sources, string name, string description, CommandDelegate cmd)
        {
            __construct();
            Name = name; Callback = cmd;
        }

        public PluginCommand(MessageType sources, string name, string description, CommandDelegate cmd, Dictionary<string, Type[]> argumentsdefinition)
        {
            __construct();
            Name = name; Callback = cmd;
            Arguments = argumentsdefinition;
        }

        public PluginCommand(MessageType sources, string name, string description, CommandDelegate cmd, Dictionary<string, Type> argumentsdefinition)
        {
            __construct();
            Name = name; Callback = cmd;
            Arguments = new Dictionary<string,Type[]>();
            foreach (string key in argumentsdefinition.Keys)
                Arguments.Add(key, new[] { argumentsdefinition[key] });
        }

        public PluginCommand(MessageType sources, string name, string description, CommandDelegate cmd, Dictionary<string, string> argumentsdefinition)
        {
            __construct();
            Name = name; Callback = cmd;
            Arguments = new Dictionary<string, Type[]>();
            foreach (string key in argumentsdefinition.Keys)
                Arguments.Add(key, new[] { Type.GetType(argumentsdefinition[key]) });
        }

        private void __construct()
        {
            Arguments = new Dictionary<string, Type[]>();
            Description = "No description available";
        }
    }

    public delegate void CommandDelegate(IcebotCommand command);

    public class PluginCommandContainer
    {
        List<PluginCommand> _regCommands = new List<PluginCommand>();

        public void Add(PluginCommand cmd)
        {
            _regCommands.Add(cmd);
        }
        public void Remove(MessageType source, string command)
        {
            var x = GetCommand(source, command);
            if (x == null)
                throw new Exception("Could not find registered command " + command);
            else
                Remove(x);
        }
        public void Remove(PluginCommand command)
        {
            _regCommands.Remove(command);
        }
        public PluginCommand GetCommand(MessageType source, string command)
        {
            foreach (PluginCommand pl in _regCommands)
                if (pl.Name.Equals(command, StringComparison.OrdinalIgnoreCase) && pl.SourceType == source)
                    return pl;
            return null;
        }
        public PluginCommand GetCommand(IcebotCommand cmd)
        {
            return GetCommand(cmd.SourceType, cmd.Command);
        }
        public string[] GetCommandNameList()
        {
            List<string> cmd = new List<string>();
            foreach (var x in _regCommands)
                cmd.Add(x.Name);
            return cmd.ToArray();
        }
        public PluginCommand[] GetCommandList()
        {
            return _regCommands.ToArray();
        }
    }

    public class PluginInfo
    {
        AssemblyName an = Assembly.GetExecutingAssembly().GetName();

        public PluginInfo()
        {
            title = an.FullName;
        }

        public PluginInfo(string author)
        {
            this.author = author;
            this.version = an.Version;
        }

        public PluginInfo(string title, string author)
        {
            this.title = title;
            this.author = author;
            this.version = an.Version;
        }

        public PluginInfo(string title, string author, string description)
        {
            this.title = title;
            this.author = author;
            this.Description = description;
            this.version = an.Version;
        }

        public PluginInfo(string title, string author, string description, Version version)
        {
            this.title = title;
            this.author = author;
            this.Description = description;
            this.version = version;
        }

        public string Title { get { return title; } set { title = value; } }
        public string Author { get { return author; } set { author = value; } }
        public string Description { get; set; }
        public Version Version { get { return version; } set { version = value; } }

        private string title = "";
        private string author = "";
        private Version version;
    }

    public class ChannelPlugin : Plugin
    {
        internal IcebotChannel _channel;
        protected IcebotChannel Channel
        { get { return _channel; } }

        internal IcebotChannelPluginConfiguration _config;
        protected IcebotChannelPluginConfiguration Configuration
        { get { return _config; } set { _config = value; } }
    }

    public class ServerPlugin : Plugin
    {
        internal IcebotServer _server;
        protected IcebotServer Server
        { get { return _server; } }

        internal IcebotServerPluginConfiguration _config;
        protected IcebotServerPluginConfiguration Configuration
        { get { return _config; } set { _config = value; } }
    }
}