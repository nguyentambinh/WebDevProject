namespace QLNSVATC.Areas.FN.Data.FN_Models
{
    public class RevenueReportRowVM
    {
        public System.DateTime? Date { get; set; }
        public string Name { get; set; }
        public string PersonInCharge { get; set; }
        public decimal ContractValue { get; set; }
        public decimal Tax { get; set; }
        public decimal Deposit { get; set; }
        public decimal Remaining { get; set; }
        public decimal TotalAmount { get; set; }
        public string Status { get; set; }
    }
}
