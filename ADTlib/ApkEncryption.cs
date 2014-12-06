using System;

namespace GiacomoFurlan.ADTlib
{
    public class ApkEncryption
    {
        public string Algorithm { get; set; }
        public string Key { get; set; }
        public string IV { get; set; }

        public bool IsComplete
        {
            get { return !(String.IsNullOrEmpty(Algorithm) && String.IsNullOrEmpty(Key) && String.IsNullOrEmpty(IV)); }
        }
    }
}
