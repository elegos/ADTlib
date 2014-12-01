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

                Console.WriteLine("Executing Adb.Push(device, source, dest)");
                var pushed = adb.Push(devices.FirstOrDefault(), "TestFiles\\LoremIpsum.txt", "/sdcard/test/abc/");
                Console.WriteLine("Command: " + (pushed ? "success" : "fail"));

                Console.WriteLine("Executing Adb.Pull(device, source, dest)");
                var pulled = adb.Pull(devices.FirstOrDefault(), "/sdcard/test/abc/LoremIpsum.txt", ".\\");
                Console.WriteLine("Command: " + (pulled ? "success" : "fail"));

                Console.WriteLine("Executing Adb.Delete(device, pathToFileOrDirectory)");
                var deleted = adb.Delete(devices.FirstOrDefault(), "/sdcard/test/");
                Console.WriteLine("Command: " + (deleted ? "success" : "fail"));

                #endregion
            }
            catch (Exception ex)
            {
                Debug.Write(ex);
            }

            // end of tests
            Console.ReadKey();
        }
    }
}
