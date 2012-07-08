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
using Icebot.Api;

#if PLUGIN_PLUGINLIST
namespace Icebot.InternalPlugins
{
    public class PluginManager : ChannelPlugin
    {
        public PluginManager()
        {
            this.PluginInfo = new PluginInfo("Plugin list");
        }

        public override void OnRegisterCommands()
        {
            Commands.Add(new PluginCommand(
                Irc.MessageType.Public,
                "ls",
                "Lists all loaded server and channel plugins.",
                new CommandDelegate(public_ls)
                ));
            Commands.Add(new PluginCommand(
                Irc.MessageType.Public,
                "lsc",
                "Lists all loaded channel plugins.",
                new CommandDelegate(public_lsc)
                ));
            Commands.Add(new PluginCommand(
                Irc.MessageType.Public,
                "lsc",
                "Lists all loaded server plugins.",
                new CommandDelegate(public_lss)
                ));

            Commands.Add(new PluginCommand(
                Irc.MessageType.All,
                "load",
                "Loads a plugin.",
                new CommandDelegate(public_lsc),
                new[] {
                    new { Name = "pluginname", Value = typeof(String) }
                }
                ));
            Commands.Add(new PluginCommand(
                Irc.MessageType.All,
                "unload",
                "Unlads a plugin.",
                new CommandDelegate(public_lsc),
                new[] {
                    new { Name = "pluginname", Value = typeof(String) }
                }
                ));
        }

        public void public_ls(IcebotCommand cmd)
        {
            List<string> allNames = new List<string>();

            if (Channel.Server.Plugins.Length != 0)
                allNames.AddRange(Channel.Server.PluginNames);
            if (Channel.Plugins.Length != 0)
                allNames.AddRange(Channel.PluginNames);

            if (allNames.Count > 0)
                Channel.SendMessage("Available plugins on this channel: "
                    + string.Join("; ", allNames)
                    + "."
                    );
            else
                Channel.SendMessage("No plugins loaded");
        }

        public void public_lsc(IcebotCommand cmd)
        {
            if (Channel.Plugins.Length != 0)
                Channel.SendMessage("Available channel plugins on this channel: "
                    + string.Join("; ", Channel.PluginNames)
                    + "."
                    );
            else
                Channel.SendMessage("No channel plugins loaded.");
        }

        public void public_lss(IcebotCommand cmd)
        {
            if (Channel.Server.Plugins.Length != 0)
                Channel.SendMessage("Available server plugins on this channel: "
                    + string.Join("; ", Channel.Server.PluginNames)
                    + "."
                    );
            else
                Channel.SendMessage("No channel plugins loaded.");
        }
    }
}
#endif