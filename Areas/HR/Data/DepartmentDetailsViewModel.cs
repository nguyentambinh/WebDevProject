using System.Collections.Generic;

namespace QLNSVATC.Models
{
    public class DepartmentDetailsViewModel
    {
        public IEnumerable<PHONGBAN> Departments { get; set; }
        public int CurrentPage { get; set; }
        public int TotalPages { get; set; }
        public int TotalDepartments { get; set; }

        public PHONGBAN PhongBan { get; set; }
        public List<NHANVIEN> EmployeesInDepartment { get; set; }
        public List<NHANVIEN> EmployeesOutsideDepartment { get; set; }
    }
}
