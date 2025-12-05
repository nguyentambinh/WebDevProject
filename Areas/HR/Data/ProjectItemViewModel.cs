using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace QLNSVATC.Areas.HR.Data
{
    public class ProjectItemViewModel
    {
        public string ProjectKey { get; set; }   
        public string ProjectCode { get; set; }   
        public string ProjectName { get; set; }  
        public string ContractCode { get; set; } 
        public string CustomerName { get; set; }  
        public string AreaCode { get; set; }      
        public string AreaName { get; set; }    
        public DateTime? StartDate { get; set; } 
        public DateTime? EndDate { get; set; }   
        public string StatusCode { get; set; }    
        public string StatusLabel { get; set; }   
        public int EmployeeCount { get; set; }  
    }
}