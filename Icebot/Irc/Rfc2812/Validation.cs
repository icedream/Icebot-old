using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

#if CLIENTSIDE_VALIDATION
namespace Icebot.Irc.Rfc2812
{
    [Serializable]
    public class ValidationException : Exception
    {
        public ValidationException() { }
        public ValidationException(string message) : base(message) { }
        public ValidationException(string message, Exception inner) : base(message, inner) { }
        protected ValidationException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context)
            : base(info, context) { }
    }

    class Validation
    {
        public bool ValidateChanString(string chanstring)
        {
        }

        public bool ValidateSingleTarget(string target)
        {
        }

        public bool ValidateTargets(string targets)
        {
        }

        public bool ValidateChannel(string channel)
        {
        }

        public bool ValidateKey(string key)
        {
        }

        public bool ValidateLetter(char letter)
        {
        }

        public bool ValidateDigit(char digit)
        {
        }

        public bool ValidateHex(char digit)
        {
        }
    }
}
#endif
