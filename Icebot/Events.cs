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
    public delegate void OnNickListUpdate(IcebotChannel channel, IcebotCommand cmd);
    public delegate void OnUserHandler(IcebotChannel channel, string nickname, string username, string hostname, string hostmask);
    public delegate void OnNickChange(IcebotServer server);
    public delegate void OnReceivedServerSupport(IcebotServer server, string name, string value);
}
