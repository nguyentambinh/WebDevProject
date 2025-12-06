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
            try
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
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Work.Index error: " + ex);
                TempData["WorkError"] = "System error while loading work schedule.";

                // fallback: return empty current week so view can still render
                DateTime today = DateTime.Today;
                DateTime weekStart = GetWeekStart(today);
                DateTime weekEnd = weekStart.AddDays(6);

                var fallbackModel = new WorkWeekViewModel
                {
                    WeekStart = weekStart,
                    WeekEnd = weekEnd,
                    Days = Enumerable.Range(0, 7)
                        .Select(i => weekStart.AddDays(i))
                        .ToList(),
                    Employees = new List<WorkEmployeeRow>(),
                    CellMap = new Dictionary<string, WorkCellViewModel>()
                };

                return View("Index", fallbackModel);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult SaveDay(DateTime date, string employeeId, string content, string factor)
        {
            if (string.IsNullOrWhiteSpace(employeeId))
            {
                return Json(new { success = false, message = "Employee id is missing." });
            }

            try
            {
                employeeId = employeeId.Trim();
                content = (content ?? "").Trim();

                if (string.IsNullOrEmpty(content))
                {
                    return Json(new { success = false, message = "Please enter work content." });
                }

                // Normalize date and block past dates
                date = date.Date;
                DateTime today = DateTime.Today;
                if (date < today)
                {
                    return Json(new
                    {
                        success = false,
                        message = "Cannot add or edit work schedule for past dates."
                    });
                }

                var emp = db.NHANVIENs.FirstOrDefault(x => x.MANV == employeeId);
                if (emp == null)
                {
                    return Json(new { success = false, message = "Employee not found." });
                }

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

                return Json(new { success = true, message = "Work schedule has been saved." });
            }
            catch (Exception ex)
            {
                var errMsg = ex.InnerException != null
                    ? (ex.InnerException.InnerException != null
                        ? ex.InnerException.InnerException.Message
                        : ex.InnerException.Message)
                    : ex.Message;

                System.Diagnostics.Debug.WriteLine("Work.SaveDay error: " + ex);

                return Json(new
                {
                    success = false,
                    message = "System error while saving work schedule: " + errMsg
                });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteDay(DateTime date, string employeeId)
        {
            if (string.IsNullOrWhiteSpace(employeeId))
            {
                return Json(new { success = false, message = "Employee id is missing." });
            }

            try
            {
                date = date.Date;

                var record = db.LICHLAMVIECs
                    .FirstOrDefault(x => x.MANV == employeeId && x.NGAYLAMVIEC == date);

                if (record == null)
                {
                    return Json(new { success = false, message = "Work schedule not found." });
                }

                db.LICHLAMVIECs.Remove(record);
                db.SaveChanges();

                return Json(new { success = true, message = "Work schedule has been deleted." });
            }
            catch (Exception ex)
            {
                var errMsg = ex.InnerException != null
                    ? (ex.InnerException.InnerException != null
                        ? ex.InnerException.InnerException.Message
                        : ex.InnerException.Message)
                    : ex.Message;

                System.Diagnostics.Debug.WriteLine("Work.DeleteDay error: " + ex);

                return Json(new
                {
                    success = false,
                    message = "System error while deleting work schedule: " + errMsg
                });
            }
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
