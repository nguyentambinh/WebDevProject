namespace QLNSVATC.Areas.Admin.Data
{
    public class ActivityViewModel
    {
        public int LogId { get; set; }
        public string ActionCode { get; set; }
        public System.DateTime? ActionTime { get; set; }
        public string PerformedBy { get; set; }
        public string Description { get; set; }
    }
}
