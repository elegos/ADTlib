using GiacomoFurlan.ADTlib.Utils;

namespace GiacomoFurlan.ADTlib
{
    public class Device
    {
        public Build Build { get; set; }
        public string SerialNumber { get; set; }

        public string State
        {
            get { return Adb.Instance.GetDeviceState(this); }
        }

        public string Model
        {
            get { return Build.GetPropOrDefault("ro.product.model"); }
        }
    }
}
