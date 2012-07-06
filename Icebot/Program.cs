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
using System.Xml;

namespace Icebot
{
    class Program
    {
        static void Main(string[] args)
        {
            if (args.Contains("license", StringComparer.OrdinalIgnoreCase))
            {
                License();
                return;
            }

            Icebot host = new Icebot();

            Console.WriteLine(Icebot._asm.GetName().Name);
            Console.WriteLine("\tVersion " + Icebot._asm.GetName().Version.ToString());
            Console.WriteLine("\tCopyright (C) 2012 Carl Kittelberger");
            Console.WriteLine();

            Console.WriteLine("This program comes with ABSOLUTELY NO WARRANTY.");
            Console.WriteLine("This is free software, and you are welcome to redistribute it under certain conditions.");
            Console.WriteLine("To read the whole license, start this program with the 'license' parameter.");
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

        static void License()
        {
            Console.WriteLine(string.Join("; ", Icebot._asm.GetManifestResourceNames()));
        }
    }
}
