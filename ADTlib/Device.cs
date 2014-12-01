namespace GiacomoFurlan.ADTlib
{
    public class Device
    {
        public Build Build { get; set; }
        public string SerialNumber { get; set; }
        public string State { get; set; }

        public string Model
        {
            get { return Build.GetPropOrDefault("ro.product.model"); }
        }
    }
}
