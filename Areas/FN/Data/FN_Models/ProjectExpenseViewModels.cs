using System;
using System.Collections.Generic;

namespace QLNSVATC.Areas.FN.Data.FN_Models
{
    public class ProjectExpenseRowVM
    {
        public string ProjectCode { get; set; }
        public string ProjectName { get; set; }
        public string DetailLabel { get; set; }
        public string ExpenseType { get; set; }
        public string ExpenseCode { get; set; }
        public DateTime? ExpenseDate { get; set; }
        public decimal Amount { get; set; }
        public string StatusCode { get; set; }
    }

    public class ProjectExpenseListViewModel
    {
        public IList<ProjectExpenseRowVM> Items { get; set; } = new List<ProjectExpenseRowVM>();
        public int TotalCount { get; set; }
        public decimal TotalAmount { get; set; }
        public int PendingCount { get; set; }
        public int ApprovedCount { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalPages { get; set; }
    }
    

}
