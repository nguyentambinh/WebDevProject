using System;
using System.Collections.Generic;

namespace QLNSVATC.Models.FN_Models
{
    // Dùng chung cho cả 2 màn
    public class RevenueMonthVM
    {
        public int Year { get; set; }
        public int Month { get; set; }
        public decimal Revenue { get; set; }
        public double? MarketIndex { get; set; }   // chỉ dùng bên Project
    }

    // ============= PROJECT REVENUE =============
    public class ProjectRevenueViewModel
    {
        public decimal TotalRevenue { get; set; }
        public int CompletedProjects { get; set; }
        public decimal AveragePerProject { get; set; }

        public List<RevenueMonthVM> RevenueByMonth { get; set; }
        public List<ProjectRegionVM> RevenueByRegion { get; set; }
        public List<TopProjectVM> TopProjects { get; set; }

        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }

        public ProjectRevenueViewModel()
        {
            RevenueByMonth = new List<RevenueMonthVM>();
            RevenueByRegion = new List<ProjectRegionVM>();
            TopProjects = new List<TopProjectVM>();
        }
    }

    public class ProjectRegionVM
    {
        public string RegionCode { get; set; }
        public string RegionName { get; set; }
        public decimal Revenue { get; set; }
    }

    public class TopProjectVM
    {
        public string ProjectCode { get; set; }
        public string ProjectName { get; set; }
        public string RegionName { get; set; }
        public string TypeName { get; set; }
        public decimal Revenue { get; set; }
    }

    // ============= SERVICE REVENUE =============
    public class ServiceRevenueViewModel
    {
        public decimal TotalRevenue { get; set; }
        public int ServiceOrders { get; set; }
        public decimal AverageOrderValue { get; set; }

        public List<RevenueMonthVM> RevenueByMonth { get; set; }
        public List<TopServiceVM> TopServices { get; set; }

        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }

        public ServiceRevenueViewModel()
        {
            RevenueByMonth = new List<RevenueMonthVM>();
            TopServices = new List<TopServiceVM>();
        }
    }

    public class TopServiceVM
    {
        public string ServiceType { get; set; } // LHDICHVU
        public decimal Revenue { get; set; }
        public int Count { get; set; }
    }
}
