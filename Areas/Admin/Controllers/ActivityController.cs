using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Web.Mvc;
using ClosedXML.Excel;
using QLNSVATC.Helpers;
using QLNSVATC.Models;
using QLNSVATC.Areas.Admin.Data;
using QLNSVATC.Helper;

namespace QLNSVATC.Areas.Admin.Controllers
{
    public class ActivityController : Controller
    {
        private readonly QLNSVATCEntities db = new QLNSVATCEntities();

        // GET: Admin/Activity
        public ActionResult Index(string actionCode, string user, string from, string to)
        {
            if (!CheckAccess.Role("AD"))
            {
                Session.Clear();
                return RedirectToAction("Login", "Account", new { area = "" });
            }
            string userId = Session["UserId"] as string;
            var st = SettingsHelper.BuildViewBagData(db, userId);
            ViewBag.Settings = st;
            ViewBag.CurrentLang = st.Lang;

            DateTime? fromDate = null;
            DateTime? toDate = null;

            if (!string.IsNullOrEmpty(from))
            {
                if (DateTime.TryParseExact(from, "yyyy-MM-dd",
                        CultureInfo.InvariantCulture, DateTimeStyles.None, out var d))
                {
                    fromDate = d.Date;
                }
            }

            if (!string.IsNullOrEmpty(to))
            {
                if (DateTime.TryParseExact(to, "yyyy-MM-dd",
                        CultureInfo.InvariantCulture, DateTimeStyles.None, out var d2))
                {
                    toDate = d2.Date.AddDays(1).AddTicks(-1);
                }
            }

            var query = db.ACTIVITY_LOG.AsQueryable();

            if (!string.IsNullOrEmpty(actionCode))
                query = query.Where(x => x.ActionCode == actionCode);

            if (!string.IsNullOrEmpty(user))
                query = query.Where(x => x.PerformedBy == user);

            if (fromDate.HasValue)
                query = query.Where(x => x.AcTionTime >= fromDate.Value);

            if (toDate.HasValue)
                query = query.Where(x => x.AcTionTime <= toDate.Value);

            var list = query
                .OrderByDescending(x => x.AcTionTime)
                .Select(x => new ActivityViewModel
                {
                    LogId = x.LogID,
                    ActionCode = x.ActionCode,
                    ActionTime = x.AcTionTime,
                    PerformedBy = x.PerformedBy,
                    Description = x.Description
                })
                .ToList();

            return View(list);
        }

        // GET: Admin/Activity/ExportExcel
        public ActionResult ExportExcel(string actionCode, string user, string from, string to)
        {
            DateTime? fromDate = null;
            DateTime? toDate = null;

            if (!string.IsNullOrEmpty(from))
            {
                if (DateTime.TryParseExact(from, "yyyy-MM-dd",
                        CultureInfo.InvariantCulture, DateTimeStyles.None, out var d))
                {
                    fromDate = d.Date;
                }
            }

            if (!string.IsNullOrEmpty(to))
            {
                if (DateTime.TryParseExact(to, "yyyy-MM-dd",
                        CultureInfo.InvariantCulture, DateTimeStyles.None, out var d2))
                {
                    toDate = d2.Date.AddDays(1).AddTicks(-1);
                }
            }

            var query = db.ACTIVITY_LOG.AsQueryable();

            if (!string.IsNullOrEmpty(actionCode))
                query = query.Where(x => x.ActionCode == actionCode);

            if (!string.IsNullOrEmpty(user))
                query = query.Where(x => x.PerformedBy == user);

            if (fromDate.HasValue)
                query = query.Where(x => x.AcTionTime >= fromDate.Value);

            if (toDate.HasValue)
                query = query.Where(x => x.AcTionTime <= toDate.Value);

            var data = query
                .OrderByDescending(x => x.AcTionTime)
                .ToList();

            // Tạo file Excel
            using (var wb = new XLWorkbook())
            {
                var ws = wb.Worksheets.Add("ActivityLogs");

                // Header
                ws.Cell(1, 1).Value = "Log ID";
                ws.Cell(1, 2).Value = "Action";
                ws.Cell(1, 3).Value = "Time";
                ws.Cell(1, 4).Value = "User";
                ws.Cell(1, 5).Value = "Description";

                ws.Row(1).Style.Font.Bold = true;

                int row = 2;
                foreach (var item in data)
                {
                    ws.Cell(row, 1).Value = item.LogID;
                    ws.Cell(row, 2).Value = item.ActionCode;
                    ws.Cell(row, 3).Value = item.AcTionTime.HasValue
                        ? item.AcTionTime.Value.ToString("dd/MM/yyyy HH:mm:ss")
                        : "";
                    ws.Cell(row, 4).Value = item.PerformedBy;
                    ws.Cell(row, 5).Value = item.Description;
                    row++;
                }

                ws.Columns().AdjustToContents();

                using (var stream = new MemoryStream())
                {
                    wb.SaveAs(stream);
                    stream.Position = 0;

                    string fileName = "ActivityLogs_" +
                                      DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".xlsx";

                    return File(stream.ToArray(),
                        "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                        fileName);
                }
            }
        }
    }
}
