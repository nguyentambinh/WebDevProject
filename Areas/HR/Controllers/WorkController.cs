using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using QLNSVATC.Models;
using QLNSVATC.Helpers;
using QLNSVATC.Areas.HR.Data;
using QLNSVATC.Helper;

namespace QLNSVATC.Areas.HR.Controllers
{
    public class WorkController : Controller
    {
        private readonly QLNSVATCEntities db = new QLNSVATCEntities();

        public ActionResult Index(DateTime? anchor)
        {
            if (!CheckAccess.Role("HR"))
            {
                Session.Clear();
                return RedirectToAction("Login", "Account", new { area = "" });
            }
            string userId = Session["UserId"] as string;
            var st = SettingsHelper.BuildViewBagData(db, userId);
            ViewBag.Settings = st;
            ViewBag.CurrentLang = st.Lang;

            DateTime today = DateTime.Today;
            DateTime baseDate = (anchor ?? today).Date;

            DateTime weekStart = GetWeekStart(baseDate);
            DateTime weekEnd = weekStart.AddDays(6);

            var employees = (from nv in db.NHANVIENs
                             join cv in db.VITRICONGVIECs
                                 on nv.MACV equals cv.MACV into cvJoin
                             from cv in cvJoin.DefaultIfEmpty()
                             orderby nv.HOLOT, nv.TENNV
                             select new WorkEmployeeRow
                             {
                                 EmployeeId = nv.MANV,
                                 FullName = (nv.HOLOT ?? "") + " " + (nv.TENNV ?? ""),
                                 PositionName = cv != null ? cv.TENCV : "",
                                 AvatarPath = nv.ANH
                             }).ToList();
            var schedules = db.LICHLAMVIECs
                .Where(x => x.NGAYLAMVIEC >= weekStart && x.NGAYLAMVIEC <= weekEnd)
                .ToList();

            var holidays = db.DSNGAYLEs
                .Where(x => x.NGAYLE >= weekStart && x.NGAYLE <= weekEnd)
                .ToList();

            var days = Enumerable.Range(0, 7)
                .Select(i => weekStart.AddDays(i))
                .ToList();

            var cellMap = new Dictionary<string, WorkCellViewModel>();

            foreach (var row in schedules)
            {
                var key = BuildCellKey(row.MANV, row.NGAYLAMVIEC);

                bool isSunday = row.NGAYLAMVIEC.DayOfWeek == DayOfWeek.Sunday;
                if (row.CHUNHAT.HasValue)
                {
                    isSunday = row.CHUNHAT.Value != 0;
                }

                bool isHoliday = row.NGAYLE.HasValue && row.NGAYLE.Value != 0;
                string holidayName = null;

                if (!isHoliday)
                {
                    var h = holidays.FirstOrDefault(hh => hh.NGAYLE == row.NGAYLAMVIEC);
                    if (h != null)
                    {
                        isHoliday = true;
                        holidayName = h.NOIDUNG;
                    }
                }

                cellMap[key] = new WorkCellViewModel
                {
                    EmployeeId = row.MANV,
                    Date = row.NGAYLAMVIEC,
                    Content = row.NOIDUNGLAMVIEC,
                    Factor = row.HESOLUONG,
                    IsSunday = isSunday,
                    IsHoliday = isHoliday,
                    HolidayName = holidayName
                };
            }

            var model = new WorkWeekViewModel
            {
                WeekStart = weekStart,
                WeekEnd = weekEnd,
                Days = days,
                Employees = employees,
                CellMap = cellMap
            };

            return View("Index", model);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult SaveDay(DateTime date, string employeeId, string content, string factor)
        {
            if (string.IsNullOrWhiteSpace(employeeId))
            {
                return Json(new { success = false, message = "Thiếu mã nhân viên." });
            }

            employeeId = employeeId.Trim();
            content = (content ?? "").Trim();

            if (string.IsNullOrEmpty(content))
            {
                return Json(new { success = false, message = "Vui lòng nhập nội dung công việc." });
            }

            var emp = db.NHANVIENs.FirstOrDefault(x => x.MANV == employeeId);
            if (emp == null)
            {
                return Json(new { success = false, message = "Không tìm thấy nhân viên." });
            }

            date = date.Date;

            float? parsedFactor = null;
            if (!string.IsNullOrWhiteSpace(factor))
            {
                float tmp;
                if (float.TryParse(
                        factor.Replace(",", "."),
                        System.Globalization.NumberStyles.Any,
                        System.Globalization.CultureInfo.InvariantCulture,
                        out tmp))
                {
                    parsedFactor = tmp;
                }
            }

            var record = db.LICHLAMVIECs
                .FirstOrDefault(x => x.MANV == employeeId && x.NGAYLAMVIEC == date);

            bool isNew = false;
            if (record == null)
            {
                record = new LICHLAMVIEC
                {
                    MANV = employeeId,
                    NGAYLAMVIEC = date
                };
                isNew = true;
            }

            bool isSunday = date.DayOfWeek == DayOfWeek.Sunday;
            bool isHoliday = db.DSNGAYLEs.Any(x => x.NGAYLE == date);

            record.CHUNHAT = (byte)(isSunday ? 1 : 0);
            record.NGAYLE = (byte)(isHoliday ? 1 : 0);
            record.HESOLUONG = parsedFactor;
            record.NOIDUNGLAMVIEC = content;

            if (isNew)
            {
                db.LICHLAMVIECs.Add(record);
            }

            db.SaveChanges();

            return Json(new { success = true, message = "Đã lưu lịch làm việc." });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteDay(DateTime date, string employeeId)
        {
            if (string.IsNullOrWhiteSpace(employeeId))
            {
                return Json(new { success = false, message = "Thiếu mã nhân viên." });
            }

            date = date.Date;

            var record = db.LICHLAMVIECs
                .FirstOrDefault(x => x.MANV == employeeId && x.NGAYLAMVIEC == date);

            if (record == null)
            {
                return Json(new { success = false, message = "Không tìm thấy lịch làm việc." });
            }

            db.LICHLAMVIECs.Remove(record);
            db.SaveChanges();

            return Json(new { success = true, message = "Đã xoá lịch làm việc." });
        }

        private DateTime GetWeekStart(DateTime anchor)
        {
            int diff = (7 + (int)anchor.DayOfWeek - (int)DayOfWeek.Monday) % 7;
            return anchor.AddDays(-diff).Date;
        }

        private string BuildCellKey(string employeeId, DateTime date)
        {
            return (employeeId ?? "").Trim() + "|" + date.ToString("yyyy-MM-dd");
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
