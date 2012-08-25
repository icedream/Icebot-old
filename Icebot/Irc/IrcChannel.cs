using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Icebot.Irc
{
    public class IrcChannel
    {
        internal List<IrcMaskedUser> _temporaryBanList = new List<IrcMaskedUser>();
        internal List<IrcMaskedUser> _temporaryInviteList = new List<IrcMaskedUser>();
        internal List<IrcMaskedUser> _temporaryExceptList = new List<IrcMaskedUser>();
        public bool IsJoined { get; internal set; }
        public string Name { get; internal set; }
        public string Topic { get; internal set; }
        public int UserCount { get; internal set; }
        public List<string> Modes { get; internal set; }
        public List<IrcChannelUser> Users { get; internal set; }
        public IrcMaskedUser[] BanList { get; internal set; }
        public IrcMaskedUser[] InviteList { get; internal set; }
        public IrcMaskedUser[] ExceptList { get; internal set; }

        internal void _syncBanList()
        {
            BanList = _temporaryBanList.ToArray();
            _temporaryBanList.Clear();
        }

        internal void _syncInviteList()
        {
            InviteList = _temporaryInviteList.ToArray();
            _temporaryInviteList.Clear();
        }

        internal void _syncExceptList()
        {
            ExceptList = _temporaryExceptList.ToArray();
            _temporaryExceptList.Clear();
        }

        public IrcChannel()
        {
            IsJoined = false;
            Topic = "";
            Users = new List<IrcChannelUser>();
            UserCount = 0;
            BanList = new IrcMaskedUser[0];
            InviteList = new IrcMaskedUser[0];
            ExceptList = new IrcMaskedUser[0];
            Modes = new List<string>();
        }
    }
}
