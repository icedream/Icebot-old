using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.Serialization;
using Icebot.Api;

namespace Icebot.Bot
{
    public class IcebotChannelSettings
    {
        [XmlAttribute("name")]
        public string Name { get; set; }

        [XmlAttribute("key")]
        public string Key { get; set; }

        [XmlAttribute("forced-modes")]
        public string ForcedModes { get; set; }

        [XmlArray("plugins")]
        [XmlArrayItem("plugin")]
        public List<Api.IcebotPluginSettings> PluginSettings { get; set; }

        [XmlAttribute("prefix")]
        public string Prefix { get; set; }

        public IcebotChannelSettings()
        {
            PluginSettings = new List<IcebotPluginSettings>();
        }
    }
}
