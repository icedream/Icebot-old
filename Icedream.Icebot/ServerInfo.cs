using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Icedream.Icebot
{
    /// <summary>
    /// Complete ISUPPORT implementation for server
    /// information.
    /// </summary>
    public class ServerInfo
    {
        /// <summary>
        /// The raw table with all ISUPPORTs of the server.
        /// </summary>
        public Dictionary<string, string> ISupportList = new Dictionary<string, string>();

        internal void Reset()
        {
            ISupportList.Clear();
        }
        internal void ParseISupportLine(IrcNumericMessageEventArgs reply)
        {
            if (reply.Numeric == IrcNumericMethod.RPL_ISUPPORT)
            {
                foreach (string sl in reply.Arguments)
                {
                    if (sl == reply.Arguments.Last())
                        break; // ":is supported by the server", useless trash
                    string name = sl;
                    string value = "True";
                    if (sl.Contains('='))
                    {
                        name = sl.Substring(0, sl.IndexOf('='));
                        value = sl.Substring(name.Length + 1);
                    }
                    ISupportList[name.ToUpper()] = value;
                }
            }
            else if (reply.Numeric == IrcNumericMethod.RPL_MYINFO)
            {
                ServerName = reply.Arguments[0];
                ServerVersion = reply.Arguments[1];
            }
        }
        internal bool IsISupport(IrcNumericReplyEventArgs reply)
        {
            return reply.Numeric == IrcNumericMethod.RPL_ISUPPORT;
        }

        // PREFIX=(<usermodes>)<userprefixes>
        public char[] SupportedChannelUserPrefixes
        { get { return ISupportList["PREFIX"].Substring(1).Split(')')[0].ToCharArray(); } }
        public char[] SupportedChannelUserModes
        { get { return ISupportList["PREFIX"].Substring(1).Split(')')[1].ToCharArray(); } }

        // CHANTYPES=<chanprefixes>
        public char[] SupportedChannelPrefixes
        { get { return ISupportList["CHANTYPES"].ToCharArray(); } }

        // CHANMODES=<A Modes>,<B Modes>,<C Modes>,<D Modes>
        public char[] SupportedATypeChannelModes
        { get { return ISupportList["CHANMODES"].Split(',')[0].ToCharArray(); } }
        public char[] SupportedBTypeChannelModes
        { get { return ISupportList["CHANMODES"].Split(',')[1].ToCharArray(); } }
        public char[] SupportedCTypeChannelModes
        { get { return ISupportList["CHANMODES"].Split(',')[2].ToCharArray(); } }
        public char[] SupportedDTypeChannelModes
        { get { return ISupportList["CHANMODES"].Split(',')[3].ToCharArray(); } }

        // MODES=<n>
        public uint AllowedMaxChannelModesWithParameter
        { get { return uint.Parse(ISupportList["MODES"]); } }

        // MAXCHANNELS=<n>
        public uint AllowedMaxJoinedChannels
        { get { return uint.Parse(ISupportList["MAXCHANNELS"]); } }

        // CHANLIMIT=<pfx>:<num> [ { ,<pfx>:<num> } ]
        public Dictionary<char, uint> AllowedMaxJoinedChannelsPerChannelPrefix
        {
            get
            {
                Dictionary<char, uint> u = new Dictionary<char, uint>();
                foreach (string w in ISupportList["CHANLIMIT"].Split(','))
                {
                    string[] s = w.Split(':');
                    char[] p = s[0].ToCharArray();
                    uint n = uint.Parse(s[1]);
                    foreach (char c in p)
                        u.Add(c, n);
                }
                return u;
            }
        }

        // NICKLEN=<n>
        public uint AllowedMaxNicknameLength
        { get { return uint.Parse(ISupportList["NICKLEN"]); } }

        // MAXBANS=<n>
        public uint AllowedMaxBansPerChannel
        { get { return uint.Parse(ISupportList["MAXBANS"]); } }

        // MAXLIST=<modes>:<num> [ { ,<modes>:<num> } ]
        public Dictionary<char, uint> AllowedMaxEntriesPerMode
        {
            get
            {
                Dictionary<char, uint> u = new Dictionary<char, uint>();
                foreach (string w in ISupportList["MAXLIST"].Split(','))
                {
                    string[] s = w.Split(':');
                    char[] p = s[0].ToCharArray();
                    uint n = uint.Parse(s[1]);
                    foreach (char c in p)
                        u.Add(c, n);
                }
                return u;
            }
        }

        // NETWORK=<name>
        public string NetworkName
        { get { return ISupportList["NETWORK"]; } }

        // EXCEPTS=<mode>
        public bool SupportsBanExceptions
        { get { return ISupportList.ContainsKey("EXCEPTS"); } }
        public char BanExceptionsMode
        { get { return ISupportList["EXCEPTS"][0]; } }

        // INVEX=<mode>
        public bool SupportsInviteExceptions
        { get { return ISupportList.ContainsKey("INVEX"); } }
        public char InviteExceptionsMode
        { get { return ISupportList["INVEX"][0]; } }

        // WALLCHOPS
        public bool SupportsWallChannelOperators
        { get { return ISupportList.ContainsKey("WALLCHOPS"); } }

        // WALLVOICES
        public bool SupportsWallVoicedUsers
        { get { return ISupportList.ContainsKey("WALLVOICES"); } }

        // STATUSMSG=<prefixes which receive status msg>
        // TODO: Better names?
        public bool SupportsPrefixLimitedChannelUserMessaging
        { get { return ISupportList.ContainsKey("STATUSMSG"); } }
        public char[] PrefixesForLimitedChannelUserMessaging
        { get { return ISupportList["STATUSMSG"].ToCharArray(); } }

        // CASEMAPPING
        public string CaseMappingName
        { get { return ISupportList["CASEMAPPING"]; } }
        public ServerCaseMapping CaseMapping
        {
            get
            {
                string cm = CaseMappingName.ToLower();
                switch (cm)
                {
                    case "ascii":
                        return ServerCaseMapping.ASCII;

                    case "rfc1459":
                        return ServerCaseMapping.RFC1459_Traditional;

                    case "strict-rfc1459":
                        return ServerCaseMapping.RFC1459_Strict;

                    default:
                        return ServerCaseMapping.Custom;
                }
            }
        }

        // ELIST
        public bool SupportsExtendedList
        { get { return ISupportList.ContainsKey("ELIST"); } }
        public char[] ExtendedListTokens
        { get { return ISupportList["ELIST"].ToCharArray(); } }

        // TOPICLEN=<n>
        public uint AllowedMaxTopicLength
        { get { return uint.Parse(ISupportList["TOPICLEN"]); } }

        // KICKLEN=<n>
        public uint AllowedMaxKickCommentLength
        { get { return uint.Parse(ISupportList["KICKLEN"]); } }

        // CHIDLEN=<n>
        [Obsolete("Replaced by IDCHAN (AllowedMaxChannelIdLengthPerPrefix)")]
        public uint AllowedMaxChannelIdLength
        { get { return uint.Parse(ISupportList["CHIDLEN"]); } }

        // IDCHAN=<n>
        public Dictionary<char, uint> AllowedMaxChannelIdLengthPerPrefix
        {
            get
            {
                Dictionary<char, uint> u = new Dictionary<char, uint>();
                foreach (string w in ISupportList["CHANLIMIT"].Split(','))
                {
                    string[] s = w.Split(':');
                    char[] p = s[0].ToCharArray();
                    uint n = uint.Parse(s[1]);
                    foreach (char c in p)
                        u.Add(c, n);
                }
                return u;
            }
        }

        // STD=<id>
        // Somehow not used by any server.
        public string UsingStandard
        { get { return ISupportList["STD"]; } }

        // SILENCE=<n>
        public bool SupportsSilenceCommand
        { get { return ISupportList.ContainsKey("SILENCE"); } }
        public uint AllowedMaxSilenceEntries
        { get { return uint.Parse(ISupportList["SILENCE"]); } }

        // RFC2812
        public bool SupportsRFC2812Features
        { get { return ISupportList.ContainsKey("RFC2812"); } }

        // PENALTY
        public bool HasExtraPenalties
        { get { return ISupportList.ContainsKey("PENALTY"); } }

        // FNC
        public bool HasForcedNickChanges
        { get { return ISupportList.ContainsKey("FNC"); } }

        // SAFELIST
        public bool HasSafeListReply
        { get { return ISupportList.ContainsKey("SAFELIST"); } }

        // AWAYLEN=<n>
        public uint AllowedMaxAwayCommentLength
        { get { return uint.Parse(ISupportList["AWAYLEN"]); } }

        // CPRIVMSG
        public bool HasCPRIVMSGCommand
        { get { return ISupportList.ContainsKey("CPRIVMSG"); } }

        // CNOTICE
        public bool HasCNOTICECommand
        { get { return ISupportList.ContainsKey("CNOTICE"); } }

        // NOQUIT, seems to be a server feature
        public bool NoQuit
        { get { return ISupportList.ContainsKey("NOQUIT"); } }

        // USERIP
        public bool HasUSERIPCommand
        { get { return ISupportList.ContainsKey("USERIP"); } }

        // MAXNICKLEN
        // TODO: Implement ISUPPORT->MAXNICKLEN, definition unsure.

        // MAXTARGETS
        public uint AllowedMaxSimultaneousTargets
        { get { return uint.Parse(ISupportList["MAXTARGETS"]); } }

        // KNOCK
        public bool HasKnockCommand
        { get { return ISupportList.ContainsKey("KNOCK"); } }

        // CALLERID[=<mode>]
        public bool AcceptsServerSideIgnoreMode
        {
            get
            {
                return
                    ISupportList.ContainsKey("CALLERID")
                    || ISupportList.ContainsKey("ACCEPT")
                    ;
            }
        }

        // VCHANS
        public bool SupportsVirtualChannels
        { get { return ISupportList.ContainsKey("VCHANS"); } }

        // WATCH=<n>
        public bool AllowedMaxWATCHes
        { get { return ISupportList.ContainsKey("WATCH"); } }

        // WHOX
        public bool WhoViaWhoxProtocol
        { get { return ISupportList.ContainsKey("WHOX"); } }

        // LANGUAGE=<count>
        public bool SupportsLanguageCommand
        { get { return ISupportList.ContainsKey("LANGUAGE"); } }
        public string[] SupportedLanguages
        {
            get
            {
                string[] s = ISupportList["LANGUAGE"].Split(',');
                return s.Skip(1).ToArray();
            }
        }

        // from RPL_MYINFO
        public string ServerName { get; internal set; }
        public string ServerVersion { get; internal set; }
        public char[] AvailableUserModes { get; internal set; }
        public char[] AvailableChannelModes { get; internal set; }

        // from MOTD replies, supports START reply, so no sync function needed here
        internal List<string> _motdLines = new List<string>();
        public string[] MessageOfTheDayLines { get { return _motdLines.ToArray(); } }
        public string MessageOfTheDay { get { return string.Join(Environment.NewLine, MessageOfTheDayLines); } }

        // from RPL_INFO
        internal List<string> _infoLines = new List<string>();
        public string[] InfoLines { get; private set; }
        public string Info { get; private set; }
        internal void _syncInfo()
        {
            InfoLines = _infoLines.ToArray();
            _infoLines.Clear();
            Info = string.Join(Environment.NewLine, InfoLines);
        }
    }

    public enum ServerCaseMapping
    {
        ASCII = 0,
        RFC1459_Traditional = 1,
        RFC1459_Strict = 2,
        Custom = 3
    }
}
