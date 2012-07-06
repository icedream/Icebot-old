using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Icebot
{

    /// <summary>
    /// Represents a command sent to the Icebot.
    /// </summary>
    public class IcebotCommand
    {
        /// <summary>
        /// The name of the command (mostly the first word in the message)
        /// </summary>
        public string Command
        { get; internal set; }

        /// <summary>
        /// The arguments of the command
        /// </summary>
        public string[] Arguments
        { get; internal set; }

        /// <summary>
        /// From where does the command come from?
        /// </summary>
        public IcebotCommandSourceType SourceType
        { get; internal set; }

        /// <summary>
        /// Specific sender, may be an IrcUser or IrcChannel.
        /// </summary>
        public string Source
        { get; internal set; }

        /// <summary>
        /// Specific targets, mainly IrcChannels.
        /// </summary>
        public string Targets
        { get; internal set; }

        /// <summary>
        /// Returns where the bot will write the response.
        /// Use it in combination with SendMessage/SendNotice.
        /// </summary>
        public string ResponseTarget
        {
            get
            {
                if (IsPublic())
                    return Targets;
                else
                    return Source;
            }
        }

        public bool IsPublic()
        {
            return (IcebotCommandSourceType.Public & SourceType) == SourceType;
        }
    }

    /// <summary>
    /// Represents the source from which the command came.
    /// </summary>
    [Flags]
    public enum IcebotCommandSourceType
    {
        PrivateMessage = 0,
        PrivateAction = 1,
        PrivateNotice = 2,
        PublicNotice = 3,
        PublicMessage = 4,

        Public = PublicMessage | PublicNotice,
        Private = PrivateMessage | PrivateAction | PrivateNotice
    }
}
