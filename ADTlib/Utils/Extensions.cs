using System;
using System.Diagnostics;
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

        public static void WaitForFileLock(this String str)
        {
            if (!File.Exists(str)) return;

            while (true)
            {
                try
                {
                    using (var fs = new FileStream(str,
                    FileMode.Open, FileAccess.ReadWrite,
                    FileShare.None, 100))
                    {
                        fs.ReadByte();

                        // If we got this far the file is ready
                        break;
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("File in use: " + str);
                    System.Threading.Thread.Sleep(500);
                }
            }
        }
    }
}
