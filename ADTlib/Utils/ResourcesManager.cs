using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;

namespace GiacomoFurlan.ADTlib.Utils
{
    class ResourcesManager
    {
        private static ResourcesManager _instance;

        public const string AdbExe = "adb.exe";
        public const string FastbootExe = "fastboot.exe";

        private const string ResourcesPath = "GiacomoFurlan.APTlib.Tools";
        private static readonly string[] AptFolder = new[] { "GiacomoFurlan", "APTlib" };

        private string _execPath;

        public static ResourcesManager Instance
        {
            get { return _instance ?? (_instance = new ResourcesManager()); }
        }

        private ResourcesManager()
        {
            // Setup the exec path
            GetExecPath();

            // If the files do not exist, write them
            if (!CheckFilesExistence()) WriteResourceFiles();
        }

        public string GetExecPath()
        {
            if (!String.IsNullOrEmpty(_execPath)) return _execPath;

            var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            return (_execPath = Path.Combine(appData, Path.Combine(AptFolder)));
        }

        public bool CheckFilesExistence()
        {
            return Directory.Exists(_execPath) &&
                   File.Exists(Path.Combine(_execPath, AdbExe)) &&
                   File.Exists(Path.Combine(_execPath, FastbootExe));
        }

        public bool WriteResourceFiles()
        {
            var path = GetExecPath();
            try
            {
                if (!Directory.Exists(_execPath)) Directory.CreateDirectory(_execPath);
                else
                {
                    var files = Directory.GetFiles(_execPath);
                    foreach (var file in files)
                    {
                        File.Delete(file);
                    }
                }

                // Write the resources
                var resources =
                    Assembly.GetExecutingAssembly()
                        .GetManifestResourceNames()
                        .Where(name => name.StartsWith(ResourcesPath));

                foreach (var resource in resources)
                {
                    var readStream = Assembly.GetExecutingAssembly().GetManifestResourceStream(resource);
                    if (readStream == null) throw new Exception("Invalid resource (" + resource + ")");

                    var splittedResourcePath = resource.Split('.');
                    var fileName = String.Join(".", splittedResourcePath.Skip(Math.Max(0, splittedResourcePath.Count() - 2)).Take(2));

                    using (var reader = new BinaryReader(readStream))
                    {
                        using (var writer = File.OpenWrite(Path.Combine(_execPath, fileName)))
                        {
                            int read;
                            var buffer = new byte[65536]; // 64 MB

                            while ((read = reader.Read(buffer, 0, buffer.Length)) > 0)
                            {
                                writer.Write(buffer, 0, read);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.Write(ex);
                return false;
            }
            return true;
        }
    }
}
