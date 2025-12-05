using System;
using System.Collections.Generic;

namespace QLNSVATC.Areas.HR.Data
{
    public class WorkEmployeeRow
    {
        public string EmployeeId { get; set; }
        public string FullName { get; set; }
        public string PositionName { get; set; }
        public string AvatarPath { get; set; }
    }

    public class WorkCellViewModel
    {
        public string EmployeeId { get; set; }
        public DateTime Date { get; set; }
        public string Content { get; set; }
        public float? Factor { get; set; }
        public bool IsSunday { get; set; }
        public bool IsHoliday { get; set; }
        public string HolidayName { get; set; }
    }

    public class WorkWeekViewModel
    {
        public DateTime WeekStart { get; set; }
        public DateTime WeekEnd { get; set; }

        public List<DateTime> Days { get; set; }

        public IList<WorkEmployeeRow> Employees { get; set; }

        public Dictionary<string, WorkCellViewModel> CellMap { get; set; }
    }
}
