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

        internal const string CommandBackup = "backup";
        internal const string CommandEmulator = "emu";
        internal const string CommandInstall = "install";
        internal const string CommandPull = "pull";
        internal const string CommandPush = "push";
        internal const string CommandReboot = "reboot";
        internal const string CommandRestore = "restore";
        internal const string CommandShell = "shell";
        internal const string CommandUninstall = "unistall";

        public const string RebootBootloader = "bootloader";
        public const string RebootRecovery = "recovery";

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
            var output = Exe.Adb(null, new[] {"devices"});

            if (ExeResponse.IsNullOrAbnormalExit(output)) return null;

            var cursor = output.StdOutput.Split(new[] {Environment.NewLine}, StringSplitOptions.None).GetEnumerator();

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
            var exec = Execute(device, "get-state");
            return ExeResponse.IsNullOrAbnormalExit(exec) ? StateUnknown : exec.StdOutput;
        }

        /// <summary>
        /// Executes a generic ADB command.
        /// </summary>
        /// <param name="device">Optional if there is only one device attached</param>
        /// <param name="command"></param>
        /// <param name="arguments"></param>
        /// <returns></returns>
        public ExeResponse Execute(Device device, string command, params string[] arguments)
        {
            if (String.IsNullOrEmpty(command)) return null;
            if (arguments == null) arguments = new string[] { };

            arguments = new[] { command }.Concat(arguments).ToArray();

            return Exe.Adb(device, arguments);
        }

        /// <summary>
        /// Executes a shell command.
        /// </summary>
        /// <param name="device">The device to execute the command on, if null the first one will be used.</param>
        /// <param name="parameters">The command and its arguments</param>
        /// <returns></returns>
        public ExeResponse Shell(Device device, params string[] parameters)
        {
            return Execute(device, CommandShell, parameters);
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

            if (ExeResponse.IsNullOrAbnormalExit(mkdir) || mkdir.StdOutput.Contains("failed"))
            {
                Debug.WriteLine("Unable to create directory " + destDirectory);
                return false;
            }

            // setup the arguments
            var arguments = new[] {filePath, destDirectory + "/" + destFileName};

            // execute
            var push = Execute(device, CommandPush, arguments);
            return !ExeResponse.IsNullOrAbnormalExit(push) && !push.StdOutput.Contains("failed");
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

            var execute = Execute(device, CommandPull, sourcePath,
                Path.Combine(new[] {destDirectory, destFileName}).WrapInQuotes());

            return !ExeResponse.IsNullOrAbnormalExit(execute) && !execute.StdOutput.Contains("not exist");
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

            return !ExeResponse.IsNullOrAbnormalExit(execute) && String.IsNullOrEmpty(execute.StdOutput);
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
            if (!File.Exists(pathToApk)) return false;

            if (forwardLock) parameters.Add("-l");
            if (reinstall) parameters.Add("-r");
            if (onSdCard) parameters.Add("-s");
            if (encryption != null && encryption.IsComplete)
            {
                parameters.AddRange(new[]
                {
                    "--algo", encryption.Algorithm,
                    "--key", encryption.Key,
                    "--iv", encryption.IV
                });
            }
            parameters.Add(pathToApk.WrapInQuotes());

            var execute = Execute(device, CommandInstall, parameters.ToArray());

            return !ExeResponse.IsNullOrAbnormalExit(execute) &&
                   execute.StdOutput.IndexOf("success", StringComparison.CurrentCultureIgnoreCase) > 0;
        }

        /// <summary>
        /// Uninstall a package from the device
        /// </summary>
        /// <param name="device">If null, the first device in the list will be used.</param>
        /// <param name="packageName">The package name of the application to uninstall</param>
        /// <param name="keepData">If true, the process will maintain the user's data</param>
        /// <returns></returns>
        public bool Uninstall(Device device, string packageName, bool keepData)
        {
            var execute = keepData
                ? Execute(device, CommandUninstall, "-k")
                : Execute(device, CommandUninstall);

            return !ExeResponse.IsNullOrAbnormalExit(execute) &&
                   execute.StdOutput.IndexOf("success", StringComparison.CurrentCultureIgnoreCase) > 0;
        }


        /// <summary>
        /// Backup the user's (and system's) applications and the relative data.
        /// It may throw an exception if you don't have the permission to write the backup file.
        /// Note: it may take a while. It is highly suggested to be run in a separated thread.
        /// </summary>
        /// <param name="device">If null, the first device in the list will be used.</param>
        /// <param name="pathToBackupFile">Where to save the backup (suggested extension: .ab)</param>
        /// <param name="apk">If you want to include the APK files, too</param>
        /// <param name="obb">If you want to include the APK extension files, too (the ones downloaded separately)</param>
        /// <param name="shared">If you want to include the shared partition (/sdcard)</param>
        /// <param name="all">If you want to do a backup of all the applications</param>
        /// <param name="includeSystem">(Only if all is true) if you want to include the system applications in the complete backup</param>
        /// <param name="packages">(Only if all is false) the packages you want to do the backup</param>
        public ExeResponse Backup(Device device, string pathToBackupFile, bool apk, bool obb, bool shared, bool all, bool includeSystem, params string[] packages)
        {
            var directory = Path.GetDirectoryName(pathToBackupFile);
            directory = directory ?? String.Empty;
            if (!Directory.Exists(pathToBackupFile)) Directory.CreateDirectory(directory);
            packages = packages ?? new string[] { };

            var parameters = new List<string>
            {
                "-f", pathToBackupFile.WrapInQuotes(),
                apk ? "-apk" : "-noapk",
                obb ? "-obb" : "-noobb",
                shared ? "-shared" : "-noshared"
            };

            if (all)
            {
                parameters.Add("-all");
                if (includeSystem) parameters.Add("-system");
            }
            else
            {
                parameters.AddRange(packages);
            }

            return Execute(device, CommandBackup, parameters.ToArray());
        }

        /// <summary>
        /// Restores an Android backup (done via <see cref="Backup"/> or in any case an ADB backup file)
        /// NOTE: you have to "manually" wait the process to end, as adb immediately exits
        /// </summary>
        /// <param name="device">If null, the first device in the list will be used.</param>
        /// <param name="pathToBackupFile">The restore file path</param>
        public ExeResponse Restore(Device device, string pathToBackupFile)
        {
            return Execute(device, CommandRestore, pathToBackupFile.WrapInQuotes());
        }

        /// <summary>
        /// Reboot the device. If mode is specified, reboots the device in bootloader or recovery.
        /// </summary>
        /// <param name="device">If null, the first device in the list will be used.</param>
        /// <param name="mode">null, Adb.RebootBootloader or Adb.RebootRecovery</param>
        public ExeResponse Reboot(Device device, string mode)
        {
            return String.IsNullOrEmpty(mode)
                ? Execute(device, CommandReboot)
                : Execute(device, CommandReboot, mode);
        }
    }
}
