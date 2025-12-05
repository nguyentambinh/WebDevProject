using System;
using System.Collections.Generic;

namespace QLNSVATC.Areas.FN.Data.FN_Models
{
    public class ProfitTrendPointVM
    {
        public string PeriodLabel { get; set; }
        public decimal Revenue { get; set; }
        public decimal Cost { get; set; }
        public decimal Profit { get; set; }
    }

    public class ProfitOverviewViewModel
    {
        public decimal TotalRevenue { get; set; }
        public decimal TotalCost { get; set; }
        public decimal TotalProfit { get; set; }
        public decimal ProfitMargin { get; set; }

        public decimal ProjectRevenue { get; set; }
        public decimal ServiceRevenue { get; set; }
        public decimal ProjectCost { get; set; }
        public decimal MaterialCost { get; set; }
        public decimal TransportCost { get; set; }
        public decimal ProjectProfit { get; set; }
        public decimal ServiceProfit { get; set; }

        public int ProjectCount { get; set; }
        public int CompletedProjectCount { get; set; }
        public int ServiceOrderCount { get; set; }

        public IList<ProfitTrendPointVM> Trend { get; set; } = new List<ProfitTrendPointVM>();
        public IList<ProjectHighlightVM> TopProfitProjects { get; set; } = new List<ProjectHighlightVM>();
    }
}
