using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.Serialization;

namespace Icebot.Api
{
    public class PluginSettings
    {
        [XmlAttribute("name")]
        public string Name { get; set; }

        [XmlArray("settings")]
        [XmlArrayItem("setting")]
        public List<PluginSetting> Configuration { get; set; }
    }

    public class PluginSetting
    {
        [XmlAttribute("name")]
        public string Name { get; set; }

        [XmlAttribute("value")]
        public string Value { get; set; }
    }
}
