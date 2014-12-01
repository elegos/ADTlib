using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using GiacomoFurlan.ADTlib.Utils;

namespace GiacomoFurlan.ADTlib
{
    public class Adb
    {

        public const string CommandBackup = "backup";
        public const string CommandEmulator = "emu";
        public const string CommandInstall = "install";
        public const string CommandPull = "pull";
        public const string CommandPush = "push";
        public const string CommandRestore = "restore";
        public const string CommandShell = "shell";
        public const string CommandUninstall = "unistall";

        private static Adb _instance;

        public static Adb Instance
        {
            get { return _instance ?? (_instance = new Adb()); }
        }

        private Adb(){}

        /// <summary>
        /// Retrieves a list of currently connected <see cref="Device"/>s (in debug mode).
        /// Drivers must be correctly installed to let ADB see the devices.
        /// </summary>
        /// <returns></returns>
        public List<Device> GetDevicesList()
        {
            var output = Exe.AdbReturnString(null, new[] {"devices"});

            if (output == null) return null;

            var cursor = output.Split(new[] {Environment.NewLine}, StringSplitOptions.None).GetEnumerator();

            var devices = new List<Device>();
            while (cursor.MoveNext())
            {
                var current = cursor.Current as string;
                if (String.IsNullOrEmpty(current)) continue;

                var matches = Regex.Match(current, "^(?<serial>[a-z|0-9]+)[\\s]+(?<state>[a-z|0-9]+)$");
                if (!matches.Success) continue;

                devices.Add(new Device
                {
                    Build = new Build(matches.Groups["serial"].Value),
                    SerialNumber = matches.Groups["serial"].Value,
                    State = matches.Groups["state"].Value
                });
            }

            return devices;
        }

        /// <summary>
        /// Executes a generic ADB command. If returnValue is true, it will output the command output (if any)
        /// </summary>
        /// <param name="device">Optional if there is only one device attached</param>
        /// <param name="command"></param>
        /// <param name="arguments"></param>
        /// <param name="returnValue"></param>
        /// <returns></returns>
        public string Execute(Device device, string command, List<string> arguments, bool returnValue)
        {
            if (String.IsNullOrEmpty(command) || arguments == null || !arguments.Any()) return null;
            arguments.Insert(0, command);

            if (returnValue) return Exe.AdbReturnString(device, arguments.ToArray());

            Exe.Adb(device, arguments.ToArray());
            return null;
        }

        /// <summary>
        /// Uploads a file to the device in the specified folder (or direct path)
        /// </summary>
        /// <param name="device">Optional if there is only one device attached</param>
        /// <param name="filePath"></param>
        /// <param name="destPath">May contain the file name. If it doesn't, the original file name will be used</param>
        /// <returns>True on success, false if was not able to create the destination directory or write the file</returns>
        public bool Push(Device device, string filePath, string destPath)
        {
            // check if local file exists
            if (!File.Exists(filePath)) return false;

            var destFileName = Path.GetFileName(destPath);
            var destDirectory = Path.GetDirectoryName(destPath).WindowsToUnixPath();
            if (String.IsNullOrEmpty(destFileName)) destFileName = Path.GetFileName(filePath);

            // create the destination path
            var mkdir = Execute(device, CommandShell, new List<string> {"mkdir -p", destDirectory}, true);
            if (mkdir.Contains("failed"))
            {
                Debug.WriteLine("Unable to create directory " + destDirectory);
                return false;
            }

            // setup the arguments
            var arguments = new List<string> { filePath, destDirectory + "/" + destFileName };

            // execute
            var push = Execute(device, CommandPush, arguments, true);
            return push == null || !push.Contains("failed");
        }

        /// <summary>
        /// Retrieves a file from the device
        /// </summary>
        /// <param name="device"></param>
        /// <param name="sourcePath"></param>
        /// <param name="destPath">May contain the file name. If it doesn't, the original file name will be used</param>
        /// <returns></returns>
        public bool Pull(Device device, string sourcePath, string destPath)
        {
            var destDirectory = Path.GetFullPath(destPath);
            var destFileName = Path.GetFileName(destPath);
            if (String.IsNullOrEmpty(destFileName)) destFileName = Path.GetFileName(sourcePath);

            var execute = Execute(device, CommandPull,
                new List<string> { sourcePath, Path.Combine(new string[] { destDirectory, destFileName }).WrapInQuotes() }, true);

            return execute == null || !execute.Contains("not exist");
        }

        /// <summary>
        /// Deletes a device's file or directory (recusively).
        /// </summary>
        /// <param name="device">If null, the first device in the list will be used.</param>
        /// <param name="pathToFileOrDirectory">Must be escaped UNIX style (<see cref="Extensions.EscapeSpacesUnixStyle"/>)</param>
        /// <returns></returns>
        public bool Delete(Device device, string pathToFileOrDirectory)
        {
            var execute = Execute(device, CommandShell, new List<string> { "rm -rf", pathToFileOrDirectory }, true);

            return (String.IsNullOrEmpty(execute));
        }
    }
}
