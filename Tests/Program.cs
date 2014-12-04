using System;
using System.Diagnostics;
using System.Linq;
using GiacomoFurlan.ADTlib;

namespace Tests
{
    class Program
    {
        static void Main(string[] args)
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
                Console.WriteLine("Executing Adb.GetDevicesList()");
                var devices = adb.GetDevicesList();
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

                Console.WriteLine("Executing Adb.Push(device, source, dest)");
                var pushed = adb.Push(testDevice, "TestFiles\\LoremIpsum.txt", testTmpFolder + "/");
                Console.WriteLine("Command: " + (pushed ? "success" : "fail"));

                Console.WriteLine("Executing Adb.Pull(device, source, dest)");
                var pulled = adb.Pull(testDevice, testTmpFolder + "/LoremIpsum.txt", ".\\");
                Console.WriteLine("Command: " + (pulled ? "success" : "fail"));

                Console.WriteLine("Executing Adb.Shell(device, params)");
                Console.WriteLine("List of files in " + testTmpFolder + ": " + adb.Shell(testDevice, "ls", "-l", testTmpFolder));

                Console.WriteLine("Executing Adb.Delete(device, pathToFileOrDirectory)");
                var deleted = adb.Delete(testDevice, testTmpFolder);
                Console.WriteLine("Command: " + (deleted ? "success" : "fail"));
                #endregion
            }
            catch (Exception ex)
            {
                Debug.Write(ex);
            }

            // end of tests
            EndOfTests:
            Console.ReadKey();
        }
    }
}
