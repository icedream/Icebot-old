using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Xml;

namespace Icebot
{
    class Program
    {
        static void Main(string[] args)
        {
            Icebot host = new Icebot();

            Console.WriteLine(Icebot._asm.GetName().Name);
            Console.WriteLine("\tVersion " + Icebot._asm.GetName().Version.ToString());
            Console.WriteLine();

            if (!File.Exists("Icebot.xml"))
            {
                Console.WriteLine("Creating configuration file...");

                IcebotConfiguration conf = new IcebotConfiguration();
                IcebotServerConfiguration server = new IcebotServerConfiguration();
                IcebotChannelConfiguration channel = new IcebotChannelConfiguration();
                IcebotChannelPluginConfiguration plugin1 = new IcebotChannelPluginConfiguration();

                plugin1.PluginName = "pluginlist";
                plugin1.Enable = true;

                channel.ChannelName = "#icebot";
                channel.Plugins.Add(plugin1);

                server.Nickname = "Icebot";
                server.Channels.Add(channel);

                conf.CommandPrefix = "!";
                conf.Servers.Add(server);

                /*XmlWriter w = XmlWriter.Create("Icebot.xml");
                w.WriteStartElement("configuration");
                w.WriteRaw(Properties.Resources.l4nStandardConfiguration);
                IcebotConfiguration.Serialize(w, conf);
                w.WriteEndElement();
                w.Close();*/

                conf.Save("Icebot.xml");

                Console.WriteLine("Config created, edit it to your needs and restart the bot!");
                return;
            }
            
            host.LoadConfig("Icebot.xml");
            host.Connect();

            System.Threading.Thread.Sleep(System.Threading.Timeout.Infinite);
        }
    }
}
