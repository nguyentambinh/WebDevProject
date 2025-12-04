using System;
using System.Collections.Generic;

namespace QLNSVATC.Models
{
    public class HRDashboardViewModel
    {
        // Tổng quan
        public int TotalEmployees { get; set; }
        public int TotalDepartments { get; set; }
        public int TotalMale { get; set; }
        public int TotalFemale { get; set; }
        public int EmployeesWithInsurance { get; set; }
        public int EmployeesWithHealthInfo { get; set; }
        public decimal MaxBaseSalary { get; set; }
        public decimal MaxBonus { get; set; }
        public decimal MaxPenalty { get; set; }
        public double MaxOvertime { get; set; }

        // Top lương / thưởng / phạt
        public List<EmployeeRankingItem> TopSalaries { get; set; }
        public List<EmployeeRankingItem> TopBonuses { get; set; }
        public List<EmployeeRankingItem> TopPenalties { get; set; }

        // Tổng hợp phòng ban
        public List<DepartmentSummaryItem> DepartmentSummaries { get; set; }

        // Chấm công / tăng ca
        public List<AttendanceSummaryItem> TopOvertime { get; set; }

        // Log hoạt động gần đây
        public List<ACTIVITY_LOG> RecentLogs { get; set; }

        public HRDashboardViewModel()
        {
            TopSalaries = new List<EmployeeRankingItem>();
            TopBonuses = new List<EmployeeRankingItem>();
            TopPenalties = new List<EmployeeRankingItem>();
            DepartmentSummaries = new List<DepartmentSummaryItem>();
            TopOvertime = new List<AttendanceSummaryItem>();
            RecentLogs = new List<ACTIVITY_LOG>();
        }
    }

    public class EmployeeRankingItem
    {
        public string MaNV { get; set; }
        public string HoTen { get; set; }
        public string TenPhongBan { get; set; }

        // Giá trị chính dùng để xếp hạng (lương / thưởng / phạt)
        public decimal Value { get; set; }

        // Đơn vị hiển thị (VND, giờ,...)
        public string Unit { get; set; }

        // Nhãn mô tả (Lương cơ bản, Tổng thưởng,...)
        public string Label { get; set; }
    }

    public class DepartmentSummaryItem
    {
        public string MaPB { get; set; }
        public string TenPB { get; set; }
        public int SoNhanVien { get; set; }
        public int SoNam { get; set; }
        public int SoNu { get; set; }
    }

    public class AttendanceSummaryItem
    {
        public string MaNV { get; set; }
        public string HoTen { get; set; }
        public string TenPhongBan { get; set; }
        public double TongCa { get; set; } // tổng ca làm việc (TONGCA)
    }
}
