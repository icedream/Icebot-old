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

namespace Icebot.InternalPlugins
{
    public class PluginManager : IcebotServerPlugin // usable as server and channel plugin, since IcebotServerPlugin base includes IcebotChannelPlugin base
    {
        public PluginManager()
        {
            this.Title = "Plugin Manager";
            this.Author = "Icedream";
            this.Description = "Plugin help & management";
        }

        public override void Run()
        {
            RegisterCommand(new CommandDeclaration(
                Name: "ls",
                MessageType: Irc.IrcMessageType.Public,
                Description: "Lists all plugins enabled on this channel.",
                Callback: new EventHandler<IcebotCommandEventArgs>(public_ls)
            ));
            base.Run();
        }

        public void public_ls(object sender, IcebotCommandEventArgs cmd)
        {
            // Channel plugins
            var allNames =
                (from p in cmd.Declaration.Host.GetEnabledPluginsOnChannel(cmd.Channel) select p.GetType().Name);

            if (allNames.Count() > 0)
                cmd.Command.Sender.SendNotice("Enabled plugins on this channels: "
                    + string.Join("; ", allNames)
                    + "."
                    );
            else
                cmd.Command.Sender.SendNotice("No plugins enabled on this channel.");
        }

    }
}