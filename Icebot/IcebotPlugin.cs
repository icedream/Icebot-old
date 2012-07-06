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

namespace Icebot
{
    public class IcebotPlugin
    {
        public virtual void RegisterCommands()
        {
            Log.Debug("No commands to register for this plugin.");
        }

        internal IcebotChannel _channel;
        protected IcebotChannel Channel
        { get { return _channel; } }

        internal IcebotServer _server;
        protected IcebotServer Server
        { get { return _server; } }

        protected ILog Log
        { get { return LogManager.GetLogger(this.GetType().Name); } }

        internal IcebotServerPluginConfiguration _serverPluginConf;
        protected IcebotServerPluginConfiguration ServerConfiguration
        { get { return _serverPluginConf; } set { _serverPluginConf = value; } }

        internal IcebotServerPluginConfiguration _channelPluginConf;
        protected IcebotServerPluginConfiguration ChannelConfiguration
        { get { return _channelPluginConf; } set { _channelPluginConf = value; } }

        //internal IcebotCommandsContainer _public = new IcebotCommandsContainer();
        //internal IcebotCommandsContainer _private = new IcebotCommandsContainer();
        protected IcebotCommandsContainer PublicCommands { get { return Channel.PublicCommands; } }
        protected IcebotCommandsContainer PrivateCommands { get { return Server.PrivateCommands; } }

        internal void Serialize(XmlWriter xml)
        {
            XmlSerializer serializer = new XmlSerializer(this.GetType());
            serializer.Serialize(xml, this);
        }
    }

    public delegate void IcebotCommandDelegate(IcebotCommand command);

    public class IcebotCommandsContainer
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
}
