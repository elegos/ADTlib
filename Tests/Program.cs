using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using GiacomoFurlan.ADTlib;

namespace Tests
{
    class Program
    {
        static void Main()
        {
            var adb = Adb.Instance;

            const string testTmpFolder = "/sdcard/adtlibtests";

            try
            {
                #region Class Exe
                //Console.WriteLine("Executing adb devices (void)");
                //Exe.Adb(null, new[] {"devices"});

                //Console.WriteLine("Executing adb devices (string)");
                //Console.Write(Exe.AdbReturnString(null, new[] {"devices"}));
                #endregion

                #region Class Adb
                Console.WriteLine("Starting the adb server");
                adb.StartServer();

                Console.WriteLine("Waiting for device(s) (10 seconds)...");
                adb.WaitForDevice(null, 10000);
                Console.WriteLine("Executing Adb.GetDevicesList()");
                var devices = adb.GetDevicesList();
                if (devices == null || !devices.Any())
                {
                    Console.WriteLine("No devices found.");
                    goto EndOfTests;
                }
                foreach (var device in devices)
                {
                    Console.WriteLine(device.Model + " - " + device.SerialNumber + " (" + device.State + ")");
                }

                var testDevice = devices.FirstOrDefault();
                if (testDevice == null)
                {
                    Console.WriteLine("No devices connected.");
                    goto EndOfTests;
                }

                Console.WriteLine("Executing Adb.GetState(device)");
                var state = adb.GetDeviceState(testDevice);
                Console.WriteLine("State: " + state);

                if (state != Adb.StateDevice)
                {
                    Console.WriteLine("Device connected, but status not 'device' ({0})", state);
                    goto EndOfTests;
                }

                Console.WriteLine("Executing Adb.Push");
                var pushed = adb.Push(testDevice, "TestFiles\\LoremIpsum.txt", testTmpFolder + "/");
                Console.WriteLine("Command: " + (pushed ? "success" : "fail"));

                Console.WriteLine("Executing Adb.Pull");
                var pulled = adb.Pull(testDevice, testTmpFolder + "/LoremIpsum.txt", ".\\");
                Console.WriteLine("Command: " + (pulled ? "success" : "fail"));

                Console.WriteLine("Executing Adb.Shell");
                Console.WriteLine("List of files in " + testTmpFolder + ": " + adb.Shell(testDevice, "ls", "-l", testTmpFolder));

                Console.WriteLine("Executing Adb.Delete");
                var deleted = adb.Delete(testDevice, testTmpFolder);
                Console.WriteLine("Command: " + (deleted ? "success" : "fail"));

                var tempBackup = Path.Combine(Environment.CurrentDirectory, "AndroidBackup.ab");
                Console.WriteLine("Executing Adb.Backup (backup: Facebook app)");
                adb.Backup(testDevice, tempBackup, false, false, false, false, false, "com.facebook.katana");

                Console.WriteLine("Executing Adb.Restore");
                adb.Restore(testDevice, tempBackup);
                Console.WriteLine("Press any key when the restore process is completed.");
                Console.ReadKey();
                Console.WriteLine();
                if (File.Exists(tempBackup)) File.Delete(tempBackup);

                Console.WriteLine("Trying to start the server as root (needs a rooted device)");
                Console.WriteLine(adb.StartAsRoot(testDevice) ? "Success" : "Failure");

                Console.WriteLine("Trying to remount /system partition as rw (needs adb in root mode)");
                Console.WriteLine(adb.RemountSystem(testDevice) ? "Success (warning: /system is now rw untill reboot)" : "Failure");
                #endregion
            }
            catch (Exception ex)
            {
                Debug.Write(ex);
            }

            // end of tests
            EndOfTests:
            Console.WriteLine(Environment.NewLine + "Killing the adb server");
            adb.KillServer();
            Console.WriteLine("Press any key to exit.");
            Console.ReadKey();
        }
    }
}
