using System;
using System.IO;

namespace GiacomoFurlan.ADTlib.Utils
{
    public static class Extensions
    {
        public static string WindowsToUnixPath(this String str)
        {
            return str.Replace("\\", "/");
        }

        public static string WrapInQuotes(this String str)
        {
            return '"' + str + '"';
        }

        public static string EscapeSpacesUnixStyle(this String str)
        {
            return str.Replace(" ", "\\ ");
        }
    }
}
