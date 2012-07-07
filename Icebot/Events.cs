﻿/**
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
using Icebot.Irc;

namespace Icebot
{
    public delegate void OnNumericReplyHandler(IcebotServer server, NumericReply numeric);
    public delegate void OnRawHandler(IcebotServer server, string line);
    public delegate void OnPublicBotCommandHandler(IcebotChannel channel, IcebotCommand cmd);
    public delegate void OnPrivateBotCommandHandler(IcebotServer server, IcebotCommand cmd);
    public delegate void OnReceivedServerSupport(IcebotServer server, string name, string value);
    public delegate void OnMessageHandler(Message message);

    public delegate void OnChannelUserHandler(ChannelUser user);
    public delegate void OnServerUserHandler(User user);
    public delegate void OnNickChange(RenamedUser user);
}
