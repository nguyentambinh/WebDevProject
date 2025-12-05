using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace QLNSVATC.Areas.FN.Data.FN_Models
{
	public class FNDashboardViewModel
	{
        //Revenues
        public decimal TotalEarning { get; set; }
        public decimal RevenueFromProjects { get; set; }
        public decimal RevenueFromServices { get; set; }
        //Expenses
        public decimal TotalSpending { get; set; }
        public decimal CostForProjects { get; set; }
        public decimal CostForMaterials { get; set; }
        public decimal CostForTransport { get; set; }
        //Charts
        public decimal RevenueInvoiceCount { get; set; }
        public int TransactionCount { get; set; }
        public decimal Balance => TotalEarning - TotalSpending;
        public List<ProjectHighlightVM> TopRevenueProjects { get; set; } = new List<ProjectHighlightVM>();
        public List<ProjectHighlightVM> TopProfitProjects { get; set; } = new List<ProjectHighlightVM>();
        public List<ProjectHighlightVM> TopEmployeeProjects { get; set; } = new List<ProjectHighlightVM>();
        public IList<RevenuePointVM> RevenueTrend { get; set; }


    }
    public class ProjectHighlightVM
    {
        public string MaDa { get; set; }
        public string ProjectName { get; set; }
        public decimal Revenue { get; set; }
        public decimal Cost { get; set; }
        public decimal Profit { get; set; }
        public decimal? ProfitPercent { get; set; }
        public int EmployeeCount { get; set; }
    }
    public class RevenuePointVM
    {
        public string PeriodLabel { get; set; }
        public decimal Amount { get; set; }
    }
}