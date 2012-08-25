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
            Server.RegisterCommand(new IcebotCommandDeclaration(
                Plugin: this,
                Name: "ls",
                MessageType: Irc.IrcMessageType.Public,
                Description: "Lists all plugins enabled on this channel.",
                Callback: new IcebotCommandDelegate(public_ls)
            ));
            Server.RegisterCommand(new IcebotCommandDeclaration(
                Plugin: this,
                Name: "lss",
                MessageType: Irc.IrcMessageType.Public,
                Description: "Lists all plugins which are available on this server.",
                Callback: new IcebotCommandDelegate(public_lss)
            ));
            Server.RegisterCommand(new IcebotCommandDeclaration(
                Plugin: this,
                Name: "enable",
                MessageType: Irc.IrcMessageType.Public,
                Description: "Enables a plugin on a channel.",
                Callback: new IcebotCommandDelegate(public_enable)
            ));
            Server.RegisterCommand(new IcebotCommandDeclaration(
                Plugin: this,
                Name: "disable",
                MessageType: Irc.IrcMessageType.Public,
                Description: "Disables a plugin on a channel.",
                Callback: new IcebotCommandDelegate(public_disable)
            ));

            base.Run();
        }

        // TODO: Make this globally available or something like this
        public string GetTargetName(IcebotCommand cmd)
        {
            return cmd.Target is Bot.IrcListener ? cmd.SenderNickname : Channel.Settings.Name;
        }

        public void public_ls(IcebotCommand cmd)
        {
            // Channel plugins

            var allNames =
                (from p in Channel._attachedPlugins select p.GetType().Name);

            if (allNames.Count() > 0)
                Server.Irc.SendMessage(GetTargetName(cmd), "Enabled plugins on this channels: "
                    + string.Join("; ", allNames)
                    + "."
                    );
            else
                Server.Irc.SendMessage(GetTargetName(cmd), "No plugins enabled on this channel.");
        }

        public void public_lss(IcebotCommand cmd)
        {
            // Server plugins

            if (Channel.Server.Plugins.Count > 0)
                Server.Irc.SendMessage(GetTargetName(cmd), "Available plugins on this server: "
                    + string.Join("; ", from p in Channel.Server.Plugins select p.GetType().Name)
                    + "."
                    );
            else
                Server.Irc.SendMessage(GetTargetName(cmd), "No plugins available.");
        }
    }
}