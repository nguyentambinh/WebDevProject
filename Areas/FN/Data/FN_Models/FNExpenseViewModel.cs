using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace QLNSVATC.Areas.FN.Data.FN_Models
{
    public class RecentExpenseVM
    {
        public string Type { get; set; }
        public string Description { get; set; }
        public DateTime? Date { get; set; }
        public decimal Amount { get; set; }
        public string ProjectCode { get; set; }
    }

    public class FNExpenseViewModel
    {
        public int PendingApprovals { get; set; }
        public int PendingMaterial { get; set; }
        public int PendingTransport { get; set; }

        public decimal TotalProjectCost { get; set; }
        public decimal TotalMaterialCost { get; set; }
        public decimal TotalTransportCost { get; set; }

        public List<RecentExpenseVM> RecentExpenses { get; set; } = new List<RecentExpenseVM>();

        public List<TeamSpendingVM> TeamSpendings { get; set; }
            = new List<TeamSpendingVM>();

        public List<CategorySpendingVM> CategorySpendings { get; set; }
            = new List<CategorySpendingVM>();
    }

    public class MaterialPurchaseRowVM
    {
        public string RequestCode { get; set; }
        public string MaterialName { get; set; }
        public decimal Amount { get; set; }
        public DateTime? Date { get; set; }
        public string ProjectCode { get; set; }
        public string ProjectName { get; set; }
        public string ProjectStatusCode { get; set; }
        public decimal? ChangeFactor { get; set; }
    }


    public class MaterialPurchaseListVM
    {
        public List<MaterialPurchaseRowVM> Items { get; set; } = new List<MaterialPurchaseRowVM>();
    }
    public class TeamSpendingVM
    {
        public string TeamName { get; set; }
        public decimal Amount { get; set; }
        public int HeightPercent { get; set; }
    }

    public class CategorySpendingVM
    {
        public string CategoryCode { get; set; }
        public string Label { get; set; }
        public decimal Amount { get; set; }
        public int HeightPercent { get; set; }
    }
}