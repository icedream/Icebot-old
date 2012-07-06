using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Icebot.Irc
{
    public class NumericReply
    {
        internal NumericReply(string line)
        {
            int num = -1;

            // Syntax: :sender NUM yournick :text
            string[] spl = line.Split(' ');

            if (
                spl.Length < 3
                || !int.TryParse(spl[1], out num)
                )
                throw new FormatException("Raw line is not a valid numeric reply.");

            Sender = spl[0];
            Numeric = (Numeric)num; // int.Parse(spl[1]);
            Target = spl[2];
            if (spl[3].StartsWith(":"))
                Data = string.Join(" ", spl.Skip(3)).Substring(1);
            else
                Data = spl[3];

            // Generate splitted data array
            List<string> s = new List<string>();
            string[] s1 = Data.Split(new string[] { " :" }, StringSplitOptions.RemoveEmptyEntries);
            string lastParam = s1.Last();
            s.AddRange(s1.Split(' '));
            s.Add(lastParam);
            DataSplit = s.ToArray();
        }

        public string Sender { get; internal set; }
        public Numeric Numeric { get; internal set; }
        public string Target { get; internal set; }
        public string Data { get; internal set; }
        public string[] DataSplit { get; internal set; }
    }
}
