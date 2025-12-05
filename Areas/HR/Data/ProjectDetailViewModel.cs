using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace QLNSVATC.Areas.HR.Data
{
    public class ProjectDetailViewModel
    {
        public string ProjectKey { get; set; }
        public string ProjectCode { get; set; }
        public string ProjectName { get; set; }
        public string ContractCode { get; set; }
        public string ContractTypeName { get; set; } 
        public string CustomerName { get; set; }
        public string CustomerAddress { get; set; }
        public string AreaName { get; set; }
        public string AreaCode { get; set; }
        public string TypeName { get; set; }     
        public DateTime? StartDate { get; set; }
        public DateTime? PlannedEndDate { get; set; }
        public DateTime? ActualEndDate { get; set; }
        public string StatusLabel { get; set; }
        public string StatusCode { get; set; }

        public IList<EmployeeOnProjectViewModel> AssignedEmployees { get; set; }
        public IList<EmployeeOnProjectViewModel> AvailableEmployees { get; set; }
    }
}