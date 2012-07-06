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
