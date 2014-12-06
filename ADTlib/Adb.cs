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

        public const string StateBootloader = "bootloader";
        public const string StateDevice = "device";
        public const string StateOffline = "offline";
        public const string StateUnauthorized = "unauthorized";
        public const string StateUnknown = "unknown";

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
                    SerialNumber = matches.Groups["serial"].Value
                });
            }

            return devices;
        }

        /// <summary>
        /// Executes adb and returns the current state of the device (<see cref="StateUnknown"/> if not found or on error)
        /// </summary>
        /// <param name="device"></param>
        /// <returns></returns>
        public string GetDeviceState(Device device)
        {
            return Execute(device, "get-state", true).TrimEnd() ?? StateUnknown;
        }

        /// <summary>
        /// Executes a generic ADB command. If returnValue is true, it will output the command output (if any)
        /// </summary>
        /// <param name="device"></param>
        /// <param name="command"></param>
        /// <param name="returnValue"></param>
        /// <param name="arguments"></param>
        /// <returns></returns>
        public string ExecuteNoParametric(Device device, string command, bool returnValue, IEnumerable<string> arguments)
        {
            if (String.IsNullOrEmpty(command)) return null;
            if (arguments == null) arguments = new string[] { };

            arguments = new string[] { command }.Concat(arguments).ToArray();

            if (returnValue) return Exe.AdbReturnString(device, arguments);

            Exe.Adb(device, arguments);
            return null;
        }

        /// <summary>
        /// Executes a generic ADB command. If returnValue is true, it will output the command output (if any)
        /// </summary>
        /// <param name="device">Optional if there is only one device attached</param>
        /// <param name="command"></param>
        /// <param name="arguments"></param>
        /// <param name="returnValue"></param>
        /// <returns></returns>
        public string Execute(Device device, string command, bool returnValue, params string[] arguments)
        {
            return ExecuteNoParametric(device, command, returnValue, arguments);
        }

        /// <summary>
        /// Executes a shell command, returning the eventual result.
        /// </summary>
        /// <param name="device">The device to execute the command on, if null the first one will be used.</param>
        /// <param name="parameters">The command and its arguments</param>
        /// <returns></returns>
        public string ShellNoParametric(Device device, IEnumerable<string> parameters)
        {
            return ExecuteNoParametric(device, CommandShell, true, parameters).TrimEnd();
        }

        /// <summary>
        /// Executes a shell command, returning the eventual result.
        /// </summary>
        /// <param name="device">The device to execute the command on, if null the first one will be used.</param>
        /// <param name="parameters">The command and its arguments</param>
        /// <returns></returns>
        public string Shell(Device device, params string[] parameters)
        {
            return Execute(device, CommandShell, true, parameters).TrimEnd();
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
            var mkdir = Shell(device, "mkdir -p", destDirectory);
            if (mkdir.Contains("failed"))
            {
                Debug.WriteLine("Unable to create directory " + destDirectory);
                return false;
            }

            // setup the arguments
            var arguments = new string[]{ filePath, destDirectory + "/" + destFileName };

            // execute
            var push = Execute(device, CommandPush, true, arguments);
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

            var execute = Execute(device, CommandPull, true,
                new string[] { sourcePath, Path.Combine(new string[] { destDirectory, destFileName }).WrapInQuotes() });

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
            var execute = Shell(device, "rm -rf", pathToFileOrDirectory);

            return (String.IsNullOrEmpty(execute));
        }

        /// <summary>
        /// Install a local APK on the device
        /// </summary>
        /// <param name="device">If null, the first device in the list will be used.</param>
        /// <param name="pathToApk">Path to the APK file to install</param>
        /// <param name="forwardLock">(Deprecated by Google!) installs the application on a read-only space</param>
        /// <param name="reinstall">Reinstall instead of installing as new, preserving the user's data</param>
        /// <param name="onSdCard">Try to install the application on the sdcard instead of the internal storage</param>
        /// <param name="encryption">If not null, it allows to setup the encryption details, if the apk is encrypted</param>
        /// <returns></returns>
        public bool Install(Device device, string pathToApk, bool forwardLock, bool reinstall, bool onSdCard, ApkEncryption encryption)
        {
            var parameters = new List<string>();

            if (forwardLock) parameters.Add("-l");
            if (reinstall) parameters.Add("-r");
            if (onSdCard) parameters.Add("-s");
            if (encryption != null && encryption.IsComplete)
            {
                parameters.AddRange(new string[]
                {
                    "--algo", encryption.Algorithm,
                    "--key", encryption.Key,
                    "--iv", encryption.IV
                });
            }
            var execute = ExecuteNoParametric(device, CommandInstall, true, parameters);

            return execute.IndexOf("success", StringComparison.CurrentCultureIgnoreCase) > 0;
        }

        public bool Uninstall(Device device, string packageName, bool keepData)
        {
            var execute = keepData
                ? Execute(device, CommandUninstall, true, "-k")
                : Execute(device, CommandUninstall, true);
            return execute.IndexOf("success", StringComparison.CurrentCultureIgnoreCase) > 0;
        }
    }
}
