using System;
using System.Collections;
using System.Linq;
using System.Text.RegularExpressions;
using GiacomoFurlan.ADTlib.Utils;

namespace GiacomoFurlan.ADTlib
{
    public class Build
    {
        private Hashtable PropsList { get; set; }
        private const string PropRegex = "^(?<prop>[A-Z|a-z|0-9|\\.]+)=(?<value>[A-Z|a-z|0-9|\\.| ]+)";

        public Build()
        {
            PropsList = new Hashtable();
        }

        public Build(string serial)
        {
            PropsList = new Hashtable();
            Load(new Device{SerialNumber = serial});
        }

        public void Load(Device device)
        {
            if (PropsList.Count > 0) return;

            var raw = Exe.AdbReturnString(device, new[] {"shell", "cat /system/build.prop"});

            if (raw == null) return;
            var lines = raw.Split(new[] {Environment.NewLine}, StringSplitOptions.None);

            PropsList.Clear();
            foreach (var matches in lines.Select(line => Regex.Match(line, PropRegex, RegexOptions.IgnoreCase)).Where(matches => matches.Success))
            {
                PropsList.Add(matches.Groups["prop"].Value, matches.Groups["value"].Value);
            }
        }

        public string GetPropOrDefault(string prop)
        {
            return PropsList.ContainsKey(prop) ? PropsList[prop] as string : null;
        }
    }
}
