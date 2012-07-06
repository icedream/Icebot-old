using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Icebot.Irc
{
    // TODO: Make RfcCommandsDefinition better. Any plans?
    class RfcCommandsDefinition
    {
        const string CommandNamePattern = "{0}_{1}";

        private Dictionary<string, string> Commands = new Dictionary<string, string>();

        protected void Add(string commandandargumentpattern)
        {
            string command = null;
            if (commandandargumentpattern.IndexOf(' ') >= 0)
                command = commandandargumentpattern.Substring(0, commandandargumentpattern.IndexOf(' '));
            else
                command = commandandargumentpattern;
            string argumentpattern = null;
            if(command.Trim().Length != commandandargumentpattern.Trim().Length)
                argumentpattern = commandandargumentpattern.Substring(command.Length + 1);
            Add(command, argumentpattern);
        }
        // TODO: Implement CLEAN method to include endless amount of parameters
        protected void AddIncremental(string command, int minimum, int maximum)
        {
            string argp = "";
            for (int i = 0; i < minimum; i++)
                argp += " {" + i + "}";
            for (int i = minimum; i < maximum; i++)
            {
                Add(command, argp);
                argp += " {" + i + "}";
            }
        }
        // Private since wrong usage will confuse the output.
        private void Add(string command, string argumentpattern)
        {
            string[] argumentpatterns = { };
            if(!string.IsNullOrWhiteSpace(argumentpattern) && !string.IsNullOrEmpty(argumentpattern))
                argumentpatterns = argumentpattern.Split(' ');
            Add(command, argumentpatterns);
        }
        protected void Add(string command, params string[] argumentpatterns)
        {
            Commands.Add(string.Format(CommandNamePattern, command.ToLower(), argumentpatterns.Length), command.ToUpper() + " " + string.Join(" ", argumentpatterns));
        }
        private string GetID(string command, int argc)
        {
            return string.Format(CommandNamePattern, command.ToLower(), argc);
        }
        private string Get(string command, int argc)
        {
            string id = GetID(command, argc);
            return (!Commands.ContainsKey(id)) ? null : Commands[id];
        }
        internal string Format(string command, params object[] arguments)
        {
            string r = null;
            r = Get(command, arguments.Length);
            if (r == null)
                return null;
            else
                return String.Format(r, arguments);
        }
    }
}
