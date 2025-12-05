// Models/FN_Models/NewExpenseViewModel.cs
using System;
using System.Collections.Generic;
using System.Web.Mvc;

namespace QLNSVATC.Areas.FN.Data.FN_Models
{
    public class NewExpenseViewModel
    {
        public string ProjectCode { get; set; }
        public string TypeCode { get; set; }
        public decimal? Amount { get; set; }
        public DateTime? Date { get; set; } 

        public string MaterialName { get; set; }
        public decimal? ChangeFactor { get; set; }

        public IList<SelectListItem> ProjectOptions { get; set; }
        public IList<SelectListItem> TypeOptions { get; set; }

        public NewExpenseViewModel()
        {
            ProjectOptions = new List<SelectListItem>();
            TypeOptions = new List<SelectListItem>();
            TypeCode = "Project";
            Date = DateTime.Today;
        }
    }
}
