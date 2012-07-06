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
using System.Xml.Linq;
using System.Xml.Serialization;
using System.Runtime.Serialization.Formatters;
using System.Runtime.Serialization.Formatters.Binary;
using System.Xml.XPath;

namespace Icebot
{
    #region XML serialization child/parent relation fix
    /**
     * This was actually too hard for me to implement myself, so I took the code from
     *  http://www.thomaslevesque.com/2009/06/12/c-parentchild-relationship-and-xml-serialization/
     * and implemented it here.
     * 
     * Many thanks go to Thomas Levesque for his idea which he published on his website!
     */
    public interface IChildItem<P> where P : class
    {
        [XmlIgnore()]
        P Parent { get; set; }
    }

    public class ChildItemCollection<P, T> : IList<T>
        where P : class
        where T : IChildItem<P>
    {
        private P _parent;
        private IList<T> _collection;

        public ChildItemCollection(P parent)
        {
            this._parent = parent;
            this._collection = new List<T>();
        }

        public ChildItemCollection(P parent, IList<T> collection)
        {
            this._parent = parent;
            this._collection = collection;
        }

        #region IList<T> Members

        public int IndexOf(T item)
        {
            return _collection.IndexOf(item);
        }

        public void Insert(int index, T item)
        {
            if (item != null)
                item.Parent = _parent;
            _collection.Insert(index, item);
        }

        public void RemoveAt(int index)
        {
            T oldItem = _collection[index];
            _collection.RemoveAt(index);
            if (oldItem != null)
                oldItem.Parent = null;
        }

        public T this[int index]
        {
            get
            {
                return _collection[index];
            }
            set
            {
                T oldItem = _collection[index];
                if (value != null)
                    value.Parent = _parent;
                _collection[index] = value;
                if (oldItem != null)
                    oldItem.Parent = null;
            }
        }

        #endregion

        #region ICollection<T> Members

        public void Add(T item)
        {
            if (item != null)
                item.Parent = _parent;
            _collection.Add(item);
        }

        public void Clear()
        {
            foreach (T item in _collection)
            {
                if (item != null)
                    item.Parent = null;
            }
            _collection.Clear();
        }

        public bool Contains(T item)
        {
            return _collection.Contains(item);
        }

        public void CopyTo(T[] array, int arrayIndex)
        {
            _collection.CopyTo(array, arrayIndex);
        }

        public int Count
        {
            get { return _collection.Count; }
        }

        public bool IsReadOnly
        {
            get { return _collection.IsReadOnly; }
        }

        public bool Remove(T item)
        {
            bool b = _collection.Remove(item);
            if (item != null)
                item.Parent = null;
            return b;
        }

        #endregion

        #region IEnumerable<T> Members

        public IEnumerator<T> GetEnumerator()
        {
            return _collection.GetEnumerator();
        }

        #endregion

        #region IEnumerable Members

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return (_collection as System.Collections.IEnumerable).GetEnumerator();
        }

        #endregion
    }
    #endregion

    [XmlRoot("icebot")]
    [XmlType("IcebotConfiguration")]
    public class IcebotConfiguration
    {
        public IcebotConfiguration()
        {
            CommandPrefix = IcebotConfigurationDefaults.DefaultPrefix;
            Servers = new ChildItemCollection<IcebotConfiguration, IcebotServerConfiguration>(this);
        }

        public void Save(string filename)
        {
            XmlNode log4netConfig = null;
            if (System.IO.File.Exists(filename))
            {
                XmlDocument doc = new XmlDocument();
                doc.Load(filename);
                log4netConfig = doc.SelectSingleNode("//configuration/log4net");
                doc = null;
            }
            XmlWriterSettings setup = new XmlWriterSettings();
            setup.ConformanceLevel = ConformanceLevel.Document;
            setup.Encoding = Encoding.UTF8;
            setup.Indent = true;
            setup.IndentChars = "  ";
            //setup.NewLineHandling = NewLineHandling.Replace;
            setup.NewLineChars = Environment.NewLine;
            //setup.NewLineOnAttributes = true;
            setup.NamespaceHandling = NamespaceHandling.OmitDuplicates;
            setup.CheckCharacters = true;
            setup.CloseOutput = true;
            setup.OmitXmlDeclaration = true;
            XmlWriter w = XmlWriter.Create(filename, setup);
            w.WriteStartElement("configuration");
            if (log4netConfig != null)
                w.WriteRaw(log4netConfig.OuterXml);
            else
                w.WriteRaw(Properties.Resources.l4nStandardConfiguration);
            IcebotConfiguration.Serialize(w, this);
            w.WriteEndElement();
            w.Flush();
            w.Close();
        }

        public static void Serialize(XmlTextWriter xml, IcebotConfiguration config)
        {
            xml.IndentChar = ' ';
            xml.Indentation = 2;
            XmlSerializer serializer = new XmlSerializer(typeof(IcebotConfiguration));
            serializer.Serialize(xml, config);
        }
        public static void Serialize(XmlWriter xml, IcebotConfiguration config)
        {
            XmlSerializer serializer = new XmlSerializer(typeof(IcebotConfiguration));
            serializer.Serialize(xml, config);
        }
        public static IcebotConfiguration Deserialize(XmlReader xml)
        {
            XmlSerializer serializer = new XmlSerializer(typeof(IcebotConfiguration));
            return (IcebotConfiguration)serializer.Deserialize(xml);
        }
        public static IcebotConfiguration Deserialize(XmlTextReader xml)
        {
            XmlSerializer serializer = new XmlSerializer(typeof(IcebotConfiguration));
            return (IcebotConfiguration)serializer.Deserialize(xml);
        }

        [XmlElement("prefix")]
        public string CommandPrefix { get; set; }

        [XmlArray("servers")]
        [XmlArrayItem("server")]
        public ChildItemCollection<IcebotConfiguration, IcebotServerConfiguration> Servers { get; set; }
    }

    public static class IcebotConfigurationDefaults
    {
        public const string DefaultPrefix = "!";
        public const ushort DefaultIrcPort = 6667;
    }

    [XmlType("IcebotServerConfiguration")]
    public class IcebotServerConfiguration : IChildItem<IcebotConfiguration>
    {
        internal IcebotServerConfiguration()
        {
            __CONSTRUCT();
        }

        private void __CONSTRUCT()
        {
            Plugins = new ChildItemCollection<IcebotServerConfiguration, IcebotServerPluginConfiguration>(this);
            Channels = new ChildItemCollection<IcebotServerConfiguration, IcebotChannelConfiguration>(this);
            ServerHost = "localhost";
            ServerPort = IcebotConfigurationDefaults.DefaultIrcPort;
            ServerName = "IRC Server";
            ServerPassword = null;
            Nickname = Icebot._asm.GetName().Name;
            Username = Nickname;
            Realname = Nickname + " v" + Icebot._asm.GetName().Version.ToString();
            ReceiveWallops = false;
            Invisible = false;
            if(Parent != null)
                CommandPrefix = Parent.CommandPrefix;
        }

        [XmlIgnore]
        public IcebotConfiguration Parent { get; set; }

        [XmlAttribute("serverhost")]
        public string ServerHost { get; set; }

        [XmlAttribute("serverport")]
        public ushort ServerPort { get; set; }

        [XmlAttribute("servername")]
        public string ServerName { get; set; }

        [XmlAttribute("serverpassword")]
        public string ServerPassword { get; set; }

        [XmlAttribute("nickname")]
        public string Nickname { get; set; }

        [XmlAttribute("username")]
        public string Username { get; set; }

        [XmlAttribute("realname")]
        public string Realname { get; set; }

        [XmlAttribute("wallops")]
        public bool ReceiveWallops { get; set; }

        [XmlAttribute("invisible")]
        public bool Invisible { get; set; }

        [XmlAttribute("prefix")]
        public string CommandPrefix { get; set; }

        [XmlArray("plugins")]
        [XmlArrayItem("plugin")]
        public ChildItemCollection<IcebotServerConfiguration, IcebotServerPluginConfiguration> Plugins { get; set; }

        [XmlArray("channels")]
        [XmlArrayItem("channel")]
        public ChildItemCollection<IcebotServerConfiguration, IcebotChannelConfiguration> Channels { get; set; }
    }

    [XmlType("IcebotChannelConfiguration")]
    public class IcebotChannelConfiguration : IChildItem<IcebotServerConfiguration>
    {
        internal IcebotChannelConfiguration() { __construct();  }

        private void __construct()
        {
            Plugins = new ChildItemCollection<IcebotChannelConfiguration, IcebotChannelPluginConfiguration>(this);
            if (Parent != null)
                CommandPrefix = Parent.CommandPrefix;
        }

        [XmlIgnore]
        public IcebotServerConfiguration Parent { get; set; }

        [XmlAttribute("name")]
        public string ChannelName { get; set; }

        [XmlAttribute("prefix")]
        public string CommandPrefix { get; set; }

        [XmlArray("plugins")]
        [XmlArrayItem("plugin")]
        public ChildItemCollection<IcebotChannelConfiguration, IcebotChannelPluginConfiguration> Plugins { get; set; }
    }

    [XmlType("IcebotChannelPluginConfiguration")]
    public class IcebotChannelPluginConfiguration : IChildItem<IcebotChannelConfiguration>
    {
        internal IcebotChannelPluginConfiguration()
        {
            Enable = false;
        }

        [XmlIgnore]
        public IcebotChannelConfiguration Parent { get; set; }

        [XmlAttribute("enable")]
        public bool Enable { get; set; }

        [XmlAttribute("name")]
        public string PluginName { get; set; }
    }

    [XmlType("IcebotServerPluginConfiguration")]
    public class IcebotServerPluginConfiguration : IChildItem<IcebotServerConfiguration>
    {
        internal IcebotServerPluginConfiguration()
        {
            Enable = false;
        }

        [XmlIgnore]
        public IcebotServerConfiguration Parent { get; set; }

        [XmlAttribute("enable")]
        public bool Enable { get; set; }

        [XmlAttribute("name")]
        public string PluginName { get; set; }
    }
}
