namespace GiacomoFurlan.ADTlib.Utils
{
    public class ExeResponse
    {
        public int ExitCode { get; set; }
        public string StdOutput { get; set; }
        public string StdError { get; set; }

        public static bool IsNullOrAbnormalExit(ExeResponse er)
        {
            return er == null || er.ExitCode != 0;
        }
    }
}
