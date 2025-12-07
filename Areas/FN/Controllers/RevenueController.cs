using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using QLNSVATC.Models;
using QLNSVATC.Helpers;
using System.Data.Entity;
using System.Text;
using QLNSVATC.Areas.FN.Data.FN_Models;
using System.IO;
using ClosedXML.Excel;
using QLNSVATC.Helper;

namespace QLNSVATC.Areas.FN.Controllers
{
    public class RevenueController : Controller
    {
        private readonly QLNSVATCEntities db = new QLNSVATCEntities();

        public ActionResult Project(DateTime? from, DateTime? to)
        {
            if (!CheckAccess.Role("FN"))
            {
                Session.Clear();
                return RedirectToAction("Login", "Account", new { area = "" });
            }
            string userId = Session["UserId"] as string;
            var st = SettingsHelper.BuildViewBagData(db, userId);
            ViewBag.Settings = st;
            ViewBag.CurrentLang = st.Lang;

            DateTime today = DateTime.Today;
            DateTime toDate = to ?? today;
            DateTime fromDate = from ?? new DateTime(toDate.AddMonths(-11).Year,
                                                     toDate.AddMonths(-11).Month,
                                                     1);

            var projQuery = from dt in db.DTDUANs
                            join dahd in db.DUANTHEOHOPDONGs
                                on dt.MADA equals dahd.MADA
                            where dt.TIENNGHIEMTHU_TONG != null
                                  && dahd.NGAYKT != null
                                  && dahd.NGAYKT >= fromDate
                                  && dahd.NGAYKT <= toDate
                            select new { dt, dahd };

            decimal totalRevenue = projQuery.Sum(x => (decimal?)x.dt.TIENNGHIEMTHU_TONG) ?? 0m;
            int completedProjects = projQuery.Select(x => x.dt.MADA).Distinct().Count();
            decimal avgPerProject = completedProjects > 0
                ? totalRevenue / completedProjects
                : 0m;

            var projByMonth = projQuery
                .GroupBy(x => new { x.dahd.NGAYKT.Value.Year, x.dahd.NGAYKT.Value.Month })
                .Select(g => new RevenueMonthVM
                {
                    Year = g.Key.Year,
                    Month = g.Key.Month,
                    Revenue = g.Sum(x => (decimal)x.dt.TIENNGHIEMTHU_TONG)
                })
                .ToList();

            var market = db.BIENDONGTHITRUONGs
                .Where(x => x.THOIGIAN >= fromDate && x.THOIGIAN <= toDate)
                .GroupBy(x => new { x.THOIGIAN.Year, x.THOIGIAN.Month })
                .Select(g => new
                {
                    g.Key.Year,
                    g.Key.Month,
                    Index = g.Average(x => x.HSTHITRUONG)
                }).ToList();

            var map = projByMonth.ToDictionary(
                x => (x.Year, x.Month),
                x => x);

            foreach (var mk in market)
            {
                var key = (mk.Year, mk.Month);
                if (map.TryGetValue(key, out var monthVm))
                {
                    monthVm.MarketIndex = mk.Index;
                }
                else
                {
                    map[key] = new RevenueMonthVM
                    {
                        Year = mk.Year,
                        Month = mk.Month,
                        MarketIndex = mk.Index,
                        Revenue = 0
                    };
                }
            }

            var revenueByMonth = map.Values
                .OrderBy(x => x.Year)
                .ThenBy(x => x.Month)
                .ToList();

            var revenueByRegion = db.DTDUANs
                .Where(x => x.TIENNGHIEMTHU_TONG != null && x.DTTHEOKV != null)
                .GroupBy(x => new { x.MAKV, x.DTTHEOKV.TENTINH })
                .Select(g => new ProjectRegionVM
                {
                    RegionCode = g.Key.MAKV,
                    RegionName = g.Key.TENTINH,
                    Revenue = g.Sum(x => (decimal)x.TIENNGHIEMTHU_TONG)
                })
                .OrderByDescending(x => x.Revenue)
                .Take(6)
                .ToList();

            var topProjects = db.DTDUANs
                .Where(x => x.TIENNGHIEMTHU_TONG != null)
                .OrderByDescending(x => x.TIENNGHIEMTHU_TONG)
                .Take(10)
                .Select(x => new TopProjectVM
                {
                    ProjectCode = x.MADA,
                    ProjectName = x.DUAN.DUANTHEOHOPDONGs
                                      .Select(d => d.HOPDONG.TENHD)
                                      .FirstOrDefault() ?? x.MADA,

                    Revenue = (decimal)x.TIENNGHIEMTHU_TONG,
                    RegionName = x.DTTHEOKV != null ? x.DTTHEOKV.TENTINH : null,
                    TypeName = x.DTTHEOLHCT != null ? x.DTTHEOLHCT.TENLH : null
                })
                .ToList();


            var vm = new ProjectRevenueViewModel
            {
                TotalRevenue = totalRevenue,
                CompletedProjects = completedProjects,
                AveragePerProject = avgPerProject,
                RevenueByMonth = revenueByMonth,
                RevenueByRegion = revenueByRegion,
                TopProjects = topProjects,
                FromDate = fromDate,
                ToDate = toDate
            };

            return View(vm);
        }

        public ActionResult Service(DateTime? from, DateTime? to)
        {
            string userId = Session["UserId"] as string;
            var st = SettingsHelper.BuildViewBagData(db, userId);
            ViewBag.Settings = st;
            ViewBag.CurrentLang = st.Lang;

            DateTime today = DateTime.Today;
            DateTime toDate = to ?? today;
            DateTime fromDate = from ?? new DateTime(toDate.AddMonths(-11).Year,
                                                     toDate.AddMonths(-11).Month,
                                                     1);

            var svcQuery = db.DTDICHVUs
                .Where(x => x.NGAYSDDV != null
                            && x.NGAYSDDV >= fromDate
                            && x.NGAYSDDV <= toDate
                            && x.GIACATONG != null);

            decimal totalRevenue = svcQuery.Sum(x => (decimal?)x.GIACATONG) ?? 0m;
            int orders = svcQuery.Count();
            decimal avgOrder = orders > 0 ? totalRevenue / orders : 0m;

            var revenueByMonth = svcQuery
                .GroupBy(x => new { x.NGAYSDDV.Value.Year, x.NGAYSDDV.Value.Month })
                .Select(g => new RevenueMonthVM
                {
                    Year = g.Key.Year,
                    Month = g.Key.Month,
                    Revenue = g.Sum(x => (decimal)x.GIACATONG)
                })
                .OrderBy(x => x.Year).ThenBy(x => x.Month)
                .ToList();

            var topServices = svcQuery
                .GroupBy(x => x.LHDICHVU)
                .Select(g => new TopServiceVM
                {
                    ServiceType = g.Key,
                    Revenue = g.Sum(x => (decimal)x.GIACATONG),
                    Count = g.Count()
                })
                .OrderByDescending(x => x.Revenue)
                .Take(10)
                .ToList();

            var vm = new ServiceRevenueViewModel
            {
                TotalRevenue = totalRevenue,
                ServiceOrders = orders,
                AverageOrderValue = avgOrder,
                RevenueByMonth = revenueByMonth,
                TopServices = topServices,
                FromDate = fromDate,
                ToDate = toDate
            };
            return View(vm);
        }

        #region EXPORT HELPER

        private FileResult ExportRevenueReportExcel(
            IEnumerable<RevenueReportRowVM> rows,
            string reportTitle,
            string downloadFileNamePrefix)
        {
            var list = rows?.ToList() ?? new List<RevenueReportRowVM>();

            using (var workbook = new XLWorkbook())
            {
                var ws = workbook.Worksheets.Add("BaoCao");

                int row = 1;

                // Tiêu đề
                var titleRange = ws.Range(row, 1, row, 9);
                titleRange.Merge();
                ws.Cell(row, 1).Value = reportTitle;
                ws.Cell(row, 1).Style.Font.Bold = true;
                ws.Cell(row, 1).Style.Font.FontSize = 14;
                ws.Cell(row, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Left;
                row += 2;

                // Header
                ws.Cell(row, 1).Value = "Ngày";
                ws.Cell(row, 2).Value = "Tên dịch vụ / dự án";
                ws.Cell(row, 3).Value = "Người phụ trách";
                ws.Cell(row, 4).Value = "Giá trị hợp đồng";
                ws.Cell(row, 5).Value = "Tiền thuế";
                ws.Cell(row, 6).Value = "Đã cọc";
                ws.Cell(row, 7).Value = "Còn thiếu";
                ws.Cell(row, 8).Value = "Thành tiền";
                ws.Cell(row, 9).Value = "Tình trạng";

                var headerRange = ws.Range(row, 1, row, 9);
                headerRange.Style.Font.Bold = true;
                headerRange.Style.Fill.BackgroundColor = XLColor.FromArgb(255, 255, 204);
                headerRange.Style.Border.BottomBorder = XLBorderStyleValues.Thin;

                row++;

                int dataStartRow = row;

                // Data
                foreach (var item in list)
                {
                    ws.Cell(row, 1).Value = item.Date;
                    ws.Cell(row, 1).Style.DateFormat.Format = "dd/MM/yyyy";

                    ws.Cell(row, 2).Value = item.Name;
                    ws.Cell(row, 3).Value = item.PersonInCharge ?? "";

                    ws.Cell(row, 4).Value = item.ContractValue;
                    ws.Cell(row, 5).Value = item.Tax;
                    ws.Cell(row, 6).Value = item.Deposit;
                    ws.Cell(row, 7).Value = item.Remaining;
                    ws.Cell(row, 8).Value = item.TotalAmount;
                    ws.Cell(row, 9).Value = item.Status;

                    ws.Range(row, 4, row, 8).Style.NumberFormat.Format = "#,##0";

                    row++;
                }

                // Tổng
                ws.Cell(row, 1).Value = "Tổng";
                var totalLabelRange = ws.Range(row, 1, row, 3);
                totalLabelRange.Merge();
                totalLabelRange.Style.Font.Bold = true;

                if (row > dataStartRow)
                {
                    ws.Cell(row, 4).FormulaA1 = $"SUM(D{dataStartRow}:D{row - 1})";
                    ws.Cell(row, 5).FormulaA1 = $"SUM(E{dataStartRow}:E{row - 1})";
                    ws.Cell(row, 6).FormulaA1 = $"SUM(F{dataStartRow}:F{row - 1})";
                    ws.Cell(row, 7).FormulaA1 = $"SUM(G{dataStartRow}:G{row - 1})";
                    ws.Cell(row, 8).FormulaA1 = $"SUM(H{dataStartRow}:H{row - 1})";

                    ws.Range(row, 4, row, 8).Style.NumberFormat.Format = "#,##0";
                    ws.Range(row, 4, row, 8).Style.Font.Bold = true;
                }

                var allRange = ws.Range(dataStartRow - 1, 1, row, 9);
                allRange.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                allRange.Style.Border.InsideBorder = XLBorderStyleValues.Thin;

                ws.Columns(1, 9).AdjustToContents();

                byte[] bytes;
                using (var stream = new MemoryStream())
                {
                    workbook.SaveAs(stream);
                    bytes = stream.ToArray();
                }

                string area = (string)(RouteData.DataTokens["area"] ?? "FN");
                string serverFileName = FileHelper.BuildReportFileName(area);
                FileHelper.SaveReportBytes(bytes, "~/Content/Uploads/BaoCao", serverFileName);

                string downloadFileName = $"{downloadFileNamePrefix}_{DateTime.Now:yyyyMMdd}.xlsx";

                return File(
                    bytes,
                    "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                    downloadFileName
                );
            }
        }

        #endregion

        [HttpGet]
        public ActionResult ExportServiceRevenue(DateTime? from, DateTime? to)
        {
            DateTime today = DateTime.Today;
            DateTime fromDate = from ?? new DateTime(today.Year, 1, 1);
            DateTime toDate = to ?? today;

            var query = db.DTDICHVUs
                .Where(x => x.NGAYSDDV.HasValue
                            && x.NGAYSDDV.Value >= fromDate
                            && x.NGAYSDDV.Value <= toDate)
                .ToList();

            var rows = query.Select(x =>
            {
                decimal contract = x.GIACATONG ?? 0m;

                decimal tax = Math.Round(contract * 0.10m, 0);

                decimal deposit = 0m;

                decimal total = contract + tax;
                decimal remaining = total - deposit;
                if (remaining < 0) remaining = 0;

                string status;
                if (remaining == 0 && total > 0)
                    status = "Đã thanh toán";
                else if (deposit > 0 && remaining > 0)
                    status = "Đã cọc";
                else
                    status = "Chưa cọc";

                return new RevenueReportRowVM
                {
                    Date = x.NGAYSDDV,
                    Name = x.LHDICHVU,
                    PersonInCharge = null,
                    ContractValue = contract,
                    Tax = tax,
                    Deposit = deposit,
                    Remaining = remaining,
                    TotalAmount = total,
                    Status = status
                };
            }).OrderBy(r => r.Date ?? DateTime.MinValue)
              .ToList();

            string title = "Mẫu báo cáo doanh thu dịch vụ";
            string prefix = "BaoCao_DichVu";

            return ExportRevenueReportExcel(rows, title, prefix);
        }

        [HttpGet]
        public ActionResult ExportProjectRevenue(DateTime? from, DateTime? to)
        {
            DateTime today = DateTime.Today;
            DateTime fromDate = from ?? new DateTime(today.Year, 1, 1);
            DateTime toDate = to ?? today;

            var personByDa = db.NVTHAMGIADAs
                .Select(x => new
                {
                    x.MADAHD,
                    x.MANV,
                    FullName = x.NHANVIEN.HOLOT + " " + x.NHANVIEN.TENNV
                })
                .ToList()
                .GroupBy(x => x.MADAHD)
                .ToDictionary(
                    g => g.Key,
                    g => string.Join(", ",
                            g.Select(z => z.FullName)
                             .Where(n => !string.IsNullOrWhiteSpace(n))
                             .Distinct())
                );

            var raw = (from dt in db.DTDUANs
                       join dahd in db.DUANTHEOHOPDONGs
                            on dt.MADA equals dahd.MADA
                       join da in db.DUANs
                            on dt.MADA equals da.MADA
                       join hd in db.HOPDONGs
                            on dahd.MAHD equals hd.MAHD
                       where dt.TIENNGHIEMTHU_TONG != null
                             && dahd.NGAYKT.HasValue
                             && dahd.NGAYKT.Value >= fromDate
                             && dahd.NGAYKT.Value <= toDate
                       select new
                       {
                           Date = dahd.NGAYKT,
                           ProjectCode = da.MADA,
                           ProjectName = hd.TENHD,
                           Deposit = da.TIENCOC,
                           Amount = dt.TIENNGHIEMTHU_TONG,
                           MADAHD = dahd.MADAHD
                       })
                       .ToList();

            var rows = raw
                .Select(x =>
                {
                    decimal contract = x.Amount ?? 0m;
                    decimal deposit = x.Deposit ?? 0m;
                    decimal tax = Math.Round(contract * 0.10m, 0);
                    decimal total = contract + tax;
                    decimal remaining = total - deposit;
                    if (remaining < 0) remaining = 0;

                    string status;
                    if (total == 0)
                        status = "Không phát sinh";
                    else if (remaining == 0)
                        status = "Đã thanh toán";
                    else if (deposit > 0 && remaining > 0)
                        status = "Đã cọc";
                    else
                        status = "Chưa cọc";

                    string personInCharge = null;
                    if (x.MADAHD != null && personByDa.TryGetValue(x.MADAHD, out var names))
                        personInCharge = names;

                    return new RevenueReportRowVM
                    {
                        Date = x.Date,
                        Name = string.IsNullOrWhiteSpace(x.ProjectName) ? x.ProjectCode : x.ProjectName,
                        PersonInCharge = personInCharge,
                        ContractValue = contract,
                        Tax = tax,
                        Deposit = deposit,
                        Remaining = remaining,
                        TotalAmount = total,
                        Status = status
                    };
                })
                .OrderBy(r => r.Date ?? DateTime.MinValue)
                .ToList();

            string title = "Mẫu báo cáo doanh thu dự án";
            string prefix = "BaoCao_DuAn";

            return ExportRevenueReportExcel(rows, title, prefix);
        }


        [HttpGet]
        public ActionResult ExportRevenueOverview(DateTime? from, DateTime? to)
        {
            DateTime today = DateTime.Today;
            DateTime fromDate = from ?? new DateTime(today.Year, 1, 1);
            DateTime toDate = to ?? today;

            var svcQuery = db.DTDICHVUs
                .Where(x => x.NGAYSDDV.HasValue
                            && x.NGAYSDDV.Value >= fromDate
                            && x.NGAYSDDV.Value <= toDate)
                .ToList();

            var svcRows = svcQuery.Select(x =>
            {
                decimal contract = x.GIACATONG ?? 0m;
                decimal tax = Math.Round(contract * 0.10m, 0);
                decimal deposit = 0m;
                decimal total = contract + tax;
                decimal remaining = total - deposit;
                if (remaining < 0) remaining = 0;

                string status;
                if (total == 0)
                    status = "Không phát sinh";
                else if (remaining == 0)
                    status = "Đã thanh toán";
                else if (deposit > 0 && remaining > 0)
                    status = "Đã cọc";
                else
                    status = "Chưa cọc";

                return new RevenueReportRowVM
                {
                    Date = x.NGAYSDDV,
                    Name = x.LHDICHVU,
                    PersonInCharge = null,
                    ContractValue = contract,
                    Tax = tax,
                    Deposit = deposit,
                    Remaining = remaining,
                    TotalAmount = total,
                    Status = "[DV] " + status
                };
            });

            var personByDa = db.NVTHAMGIADAs
                .Select(x => new
                {
                    x.MADAHD,
                    x.MANV,
                    FullName = x.NHANVIEN.HOLOT + " " + x.NHANVIEN.TENNV
                })
                .ToList()
                .GroupBy(x => x.MADAHD)
                .ToDictionary(
                    g => g.Key,
                    g => string.Join(", ",
                            g.Select(z => z.FullName)
                             .Where(n => !string.IsNullOrWhiteSpace(n))
                             .Distinct())
                );

            var rawProject = (from dt in db.DTDUANs
                              join dahd in db.DUANTHEOHOPDONGs
                                   on dt.MADA equals dahd.MADA
                              join da in db.DUANs
                                   on dt.MADA equals da.MADA
                              join hd in db.HOPDONGs
                                   on dahd.MAHD equals hd.MAHD
                              where dt.TIENNGHIEMTHU_TONG != null
                                    && dahd.NGAYKT.HasValue
                                    && dahd.NGAYKT.Value >= fromDate
                                    && dahd.NGAYKT.Value <= toDate
                              select new
                              {
                                  Date = dahd.NGAYKT,
                                  ProjectCode = da.MADA,
                                  ProjectName = hd.TENHD,
                                  Deposit = da.TIENCOC,
                                  Amount = dt.TIENNGHIEMTHU_TONG,
                                  MADAHD = dahd.MADAHD
                              })
                              .ToList();

            var prjRows = rawProject.Select(x =>
            {
                decimal contract = x.Amount ?? 0m;
                decimal deposit = x.Deposit ?? 0m;
                decimal tax = Math.Round(contract * 0.10m, 0);
                decimal total = contract + tax;
                decimal remaining = total - deposit;
                if (remaining < 0) remaining = 0;

                string status;
                if (total == 0)
                    status = "Không phát sinh";
                else if (remaining == 0)
                    status = "Đã thanh toán";
                else if (deposit > 0 && remaining > 0)
                    status = "Đã cọc";
                else
                    status = "Chưa cọc";

                string personInCharge = null;
                if (x.MADAHD != null && personByDa.TryGetValue(x.MADAHD, out var names))
                    personInCharge = names;

                return new RevenueReportRowVM
                {
                    Date = x.Date,
                    Name = string.IsNullOrWhiteSpace(x.ProjectName) ? x.ProjectCode : x.ProjectName,
                    PersonInCharge = personInCharge,
                    ContractValue = contract,
                    Tax = tax,
                    Deposit = deposit,
                    Remaining = remaining,
                    TotalAmount = total,
                    Status = "[DA] " + status
                };
            });

            var rows = svcRows.Concat(prjRows)
                              .OrderBy(r => r.Date ?? DateTime.MinValue)
                              .ToList();

            string title = "Mẫu báo cáo doanh thu tổng hợp";
            string prefix = "BaoCao_TongHop";

            return ExportRevenueReportExcel(rows, title, prefix);
        }

    }
}
