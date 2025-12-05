using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using QLNSVATC.Models;

namespace QLNSVATC.Areas.HR.Data
{
    public class ProjectInformationViewModel
    {
        public IList<ProjectSectionViewModel> Sections { get; set; }
        public IList<DTTHEOKV> Areas { get; set; }

        public IList<DTTHEOLHCT> Types { get; set; }  
        public IList<KHACHHANG> Customers { get; set; } 

        public ProjectInformationViewModel()
        {
            Sections = new List<ProjectSectionViewModel>();
            Areas = new List<DTTHEOKV>();
            Types = new List<DTTHEOLHCT>();
            Customers = new List<KHACHHANG>();
        }
    }

    public class EmployeeInProjectItem
    {
        public string EmployeeId { get; set; }
        public string FullName { get; set; }
    }

    public class EmployeeOptionItem
    {
        public string EmployeeId { get; set; }
        public string FullName { get; set; }
    }

    public class ProjectDetailsViewModel
    {
        public string ProjectKey { get; set; }
        public string ProjectCode { get; set; }
        public string ContractCode { get; set; }
        public string ProjectName { get; set; }
        public string CustomerName { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public string StatusCode { get; set; }
        public string StatusLabel { get; set; }

        public List<EmployeeInProjectItem> CurrentEmployees { get; set; }
        public List<EmployeeOptionItem> AvailableEmployees { get; set; }

        // Nếu cần Types/Customers cho trang Details thì giữ lại, không sao
        public List<DTTHEOLHCT> Types { get; set; }
        public List<KHACHHANG> Customers { get; set; }
    }
}
