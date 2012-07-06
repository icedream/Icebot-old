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

#if PLUGIN_VERSION
namespace Icebot.InternalPlugins
{
    class Version : IcebotPlugin
    {
        public override void RegisterCommands()
        {
            PublicCommands.Add("version", "Prints the bot's version", new IcebotCommandDelegate(cmd_Version));
        }

        private void cmd_Version(IcebotCommand cmd)
        {

        }
    }
}
#endif