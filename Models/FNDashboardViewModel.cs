using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace QLNSVATC.Models
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
        public int RevenueInvoiceCount { get; set; }
        public int TransactionCount { get; set; }
        public decimal Balance => TotalEarning - TotalSpending;
        public List<FNChartPointVM> RevenueTrend { get; set; } = new List<FNChartPointVM>();
        public List<ProjectHighlightVM> TopRevenueProjects { get; set; } = new List<ProjectHighlightVM>();
        public List<ProjectHighlightVM> TopProfitProjects { get; set; } = new List<ProjectHighlightVM>();
        public List<ProjectHighlightVM> TopEmployeeProjects { get; set; } = new List<ProjectHighlightVM>();

    }
    public class ProjectHighlightVM
    {
        public string MaDa { get; set; }
        public string ProjectName { get; set; }   // tạm dùng MADA, sau này bạn map sang tên dự án cũng được
        public decimal Revenue { get; set; }
        public decimal Cost { get; set; }
        public decimal Profit { get; set; }
        public decimal? ProfitPercent { get; set; }
        public int EmployeeCount { get; set; }    // tạm 0, sau gắn bảng phân công nhân viên
    }

    public class FNChartPointVM
    {
        public string Label { get; set; }
        public decimal Value { get; set; }
    }

}