using System;
using System.Collections.Generic;

namespace QLNSVATC.Areas.FN.Data.FN_Models
{
    public class TransportExpenseRowVM
    {
        public string TransportCode { get; set; }
        public DateTime? TransportDate { get; set; }
        public string MaterialName { get; set; }
        public string ProjectCode { get; set; }
        public decimal Cost { get; set; }
        public string StatusCode { get; set; }
    }

    public class TransportExpenseViewModel
    {
        public IList<TransportExpenseRowVM> Items { get; set; } = new List<TransportExpenseRowVM>();

        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalCount { get; set; }

        public decimal TotalCost { get; set; }
        public string StatusFilter { get; set; }
        public string Search { get; set; }
        public DateTime? From { get; set; }
        public DateTime? To { get; set; }
    }
}
