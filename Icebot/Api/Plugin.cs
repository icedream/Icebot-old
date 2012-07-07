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
using log4net;

namespace Icebot.Api
{
    public class Plugin : IDisposable
    {
        protected ILog Log
        { get { return LogManager.GetLogger(this.GetType().Name + this.InstanceNumber.ToString()); } }

        internal IcebotServerPluginConfiguration _serverPluginConf;
        protected IcebotServerPluginConfiguration ServerConfiguration
        { get { return _serverPluginConf; } set { _serverPluginConf = value; } }

        internal string PluginName;
        public int InstanceNumber { get; internal set; }

        internal PluginInfo _pluginInfo = new PluginInfo();
        public PluginInfo PluginInfo { get { return _pluginInfo; } protected set { _pluginInfo = value; } }

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
        public string CommandName { get; set; }
        public Type[] Arguments { get; set; }
    }

    public delegate void CommandDelegate(IcebotCommand command);

    public class PluginCommandContainer
    {
        List<Tuple<string, string, string[], IcebotCommandDelegate>> _regCommands = new List<Tuple<string, string, string[], IcebotCommandDelegate>>();

        public void Add(string command, string description, IcebotCommandDelegate callback)
        {
            _regCommands.Add(new Tuple<string, string, string[], IcebotCommandDelegate>(command.ToLower(), description, new string[] { }, callback));
        }
        public void Add(string command, string description, string semicolonSeparatedArgumentNameList, IcebotCommandDelegate callback)
        {
            _regCommands.Add(new Tuple<string, string, string[], IcebotCommandDelegate>(command.ToLower(), description, semicolonSeparatedArgumentNameList.Split(';'), callback));
        }
        public void Add(string command, string description, string[] argumentNameList, IcebotCommandDelegate callback)
        {
            _regCommands.Add(new Tuple<string, string, string[], IcebotCommandDelegate>(command.ToLower(), description, argumentNameList, callback));
        }

        public void Remove(string command)
        {
            var x = _getCommandByName(command);
            if (x == null)
                throw new Exception("Could not find registered command " + command);
            _regCommands.Remove(x);
        }

        public string GetDescription(string command)
        {
            var x = _getCommandByName(command);
            if (x == null) return null;
            return x.Item2;
        }

        public string[] GetArguments(string command)
        {
            var x = _getCommandByName(command);
            if (x == null) return null;
            return x.Item3;
        }

        public IcebotCommandDelegate GetCallback(string command)
        {
            var x = _getCommandByName(command);
            if (x == null) return null;
            return x.Item4;
        }

        private Tuple<string, string, string[], IcebotCommandDelegate> _getCommandByName(string name)
        {
            foreach (Tuple<string, string, string[], IcebotCommandDelegate> cmd in _regCommands)
            {
                if (cmd.Item1 == name.ToLower())
                {
                    return cmd;
                }
            }
            return null;
        }

        public string[] GetRegisteredCommandsList()
        {
            List<string> cmd = new List<string>();
            foreach (Tuple<string, string, string[], IcebotCommandDelegate> t in _regCommands)
                cmd.Add(t.Item1);
            return cmd.ToArray();
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