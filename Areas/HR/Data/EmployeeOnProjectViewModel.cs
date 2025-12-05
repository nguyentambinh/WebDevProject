using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace QLNSVATC.Areas.HR.Data
{
    public class EmployeeOnProjectViewModel
    {
        public string EmployeeId { get; set; } 
        public string FullName { get; set; }
        public string DepartmentName { get; set; }
        public string PositionName { get; set; }
        public float? TotalHours { get; set; }  
    }
}