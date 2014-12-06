using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace GiacomoFurlan.ADTlib.Utils
{
    public class Exe
    {

        private static ProcessStartInfo RunPrepare(string executable, Device device, IEnumerable<string> parameters)
        {
            var mgr = ResourcesManager.Instance;
            executable = Path.Combine(mgr.GetExecPath(), executable);

            if (!File.Exists(executable)) throw new FileNotFoundException(executable + " was not found");

            var startInfo = new ProcessStartInfo
            {
                CreateNoWindow = true,
                ErrorDialog = false,
                FileName = executable,
                UseShellExecute = true,
                WindowStyle = ProcessWindowStyle.Hidden,
                Arguments = (device != null && !String.IsNullOrEmpty(device.SerialNumber)
                    ? "-s " + device.SerialNumber
                    : "") + " " + String.Join(" ", parameters.Select(x => x.Replace("\"", "\\\"")))
            };

            return startInfo;
        }

        /// <summary>
        /// Executes adb or fastboot from the executing folder (%AppData%)
        /// </summary>
        /// <param name="executable">ResourcesManager.AdbExe or ResourcesManager.FastbootExe</param>
        /// <param name="device">The device to execute the command on (required attribute: SerialNumber)</param>
        /// <param name="parameters">the list of parameters passed to the executable</param>
        /// <returns>the output, null in case executable is not a file</returns>
        private static ExeResponse Run(string executable, Device device, IEnumerable<string> parameters)
        {
            try
            {
                var startInfo = RunPrepare(executable, device, parameters);
                startInfo.UseShellExecute = false;
                startInfo.RedirectStandardError = true;
                startInfo.RedirectStandardOutput = true;

                // unescape escaped quotes
                startInfo.FileName = startInfo.FileName.Replace("\\\"", "\"");
                startInfo.Arguments = startInfo.Arguments.Replace("\\\"", "\"");

                Debug.WriteLine("{0} {1}", startInfo.FileName, startInfo.Arguments);

                var proc = new Process { StartInfo = startInfo };
                if (!proc.Start()) throw new Exception("Unable to start process " + executable + " " + startInfo.Arguments);
                proc.WaitForExit();

                var error = proc.StandardError.ReadToEnd();

                Debug.WriteIf(String.IsNullOrEmpty(error), error);

                return new ExeResponse
                {
                    ExitCode = proc.ExitCode,
                    StdError = proc.StandardError.ReadToEnd().TrimEnd(),
                    StdOutput = proc.StandardOutput.ReadToEnd().TrimEnd()
                };
            }
            catch (Exception ex)
            {
                Debug.Write(ex);
                return null;
            }
        }

        public static ExeResponse Adb(Device device, IEnumerable<string> parameters)
        {
            return Run(ResourcesManager.AdbExe, device, parameters);
        }
    }
}
