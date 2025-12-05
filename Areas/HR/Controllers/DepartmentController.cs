using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web.Mvc;
using QLNSVATC.Models;
using QLNSVATC.Helpers;
using QLNSVATC.Helper;

namespace QLNSVATC.Areas.HR.Controllers
{
    public class DepartmentController : Controller
    {
        private readonly QLNSVATCEntities db = new QLNSVATCEntities();

        public ActionResult Information(int page = 1)
        {
            if (!CheckAccess.Role("HR"))
            {
                Session.Clear();
                return RedirectToAction("Login", "Account", new { area = "" });
            }
            var userId = Session["UserId"] as string;
            var st = SettingsHelper.BuildViewBagData(db, userId);
            ViewBag.Settings = st;
            const int pageSize = 19;

            var all = db.PHONGBANs
                .OrderBy(p => p.MAPB)
                .ToList();

            int totalDepartments = all.Count;
            int totalPages = (int)Math.Ceiling(totalDepartments / (double)pageSize);
            if (totalPages == 0) totalPages = 1;

            if (page < 1) page = 1;
            if (page > totalPages) page = totalPages;

            var data = all
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = totalPages;
            ViewBag.TotalDepartments = totalDepartments;

            var empCountByDept = db.NHANVIENs
                .Where(nv => nv.MAPB != null)
                .GroupBy(nv => nv.MAPB)
                .ToDictionary(g => g.Key, g => g.Count());

            var areaList = db.DTTHEOKVs.ToList();
            var areaByCode = areaList.ToDictionary(k => k.MAKV, k => k.TENTINH);
            var headNameByDept = new Dictionary<string, string>();
            var areaNameByDept = new Dictionary<string, string>();

            var headIdsForPage = data
                .Where(p => !string.IsNullOrEmpty(p.MATRG_PHG))
                .Select(p => p.MATRG_PHG)
                .Distinct()
                .ToList();

            var headEmployees = db.NHANVIENs
                .Where(nv => headIdsForPage.Contains(nv.MANV))
                .ToList();

            foreach (var pb in data)
            {
                if (pb.DIADIEM.HasValue)
                {
                    string code = pb.DIADIEM.Value.ToString();
                    string areaName;
                    if (areaByCode.TryGetValue(code, out areaName))
                    {
                        areaNameByDept[pb.MAPB] = areaName;
                    }
                }

                if (!string.IsNullOrEmpty(pb.MATRG_PHG))
                {
                    var nv = headEmployees.FirstOrDefault(x => x.MANV == pb.MATRG_PHG);
                    if (nv != null)
                    {
                        headNameByDept[pb.MAPB] = (nv.HOLOT + " " + nv.TENNV).Trim();
                    }
                }
            }

            ViewBag.EmployeeCountByDept = empCountByDept;
            ViewBag.HeadNameByDept = headNameByDept;
            ViewBag.AreaNameByDept = areaNameByDept;
            ViewBag.AreaList = areaList.OrderBy(x => x.TENTINH).ToList();

            return View(data);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public JsonResult Delete(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
                return Json(new { success = false, message = "Department ID is invalid." });

            var pb = db.PHONGBANs.Find(id);
            if (pb == null)
                return Json(new { success = false, message = "Department not found." });

            bool hasEmployees = db.NHANVIENs.Any(nv => nv.MAPB == id);
            if (hasEmployees)
            {
                return Json(new
                {
                    success = false,
                    message = "Cannot delete this department because it still has employees."
                });
            }

            try
            {
                db.PHONGBANs.Remove(pb);
                db.SaveChanges();
                return Json(new { success = true, message = "Department has been deleted." });
            }
            catch (Exception ex)
            {
                return Json(new
                {
                    success = false,
                    message = "Error while deleting department: " + ex.Message
                });
            }
        }

        public ActionResult Details(string id)
        {
            var userId = Session["UserId"] as string;
            var st = SettingsHelper.BuildViewBagData(db, userId);
            ViewBag.Settings = st;

            if (string.IsNullOrWhiteSpace(id))
            {
                return RedirectToAction("Information");
            }

            var pb = db.PHONGBANs.Find(id);
            if (pb == null)
            {
                return HttpNotFound();
            }

            var headIds = db.PHONGBANs
                .Where(x => x.MATRG_PHG != null)
                .Select(x => x.MATRG_PHG)
                .ToList();

            var inDept = db.NHANVIENs
                .Where(nv => nv.MAPB == id)
                .OrderBy(nv => nv.MANV)
                .ToList();

            var outDept = db.NHANVIENs
                .Where(nv => nv.MAPB != id || nv.MAPB == null)
                .Where(nv => !headIds.Contains(nv.MANV))
                .OrderBy(nv => nv.MANV)
                .ToList();

            var model = new DepartmentDetailsViewModel
            {
                PhongBan = pb,
                EmployeesInDepartment = inDept,
                EmployeesOutsideDepartment = outDept
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public JsonResult AddEmployee(string mapb, string manv)
        {
            if (string.IsNullOrWhiteSpace(mapb) || string.IsNullOrWhiteSpace(manv))
            {
                return Json(new { success = false, message = "Invalid data." });
            }

            var pb = db.PHONGBANs.Find(mapb);
            if (pb == null)
                return Json(new { success = false, message = "Department not found." });

            var nv = db.NHANVIENs.Find(manv);
            if (nv == null)
                return Json(new { success = false, message = "Employee not found." });

            bool isHeadSomewhere = db.PHONGBANs.Any(p => p.MATRG_PHG == nv.MANV);
            if (isHeadSomewhere)
            {
                return Json(new
                {
                    success = false,
                    message = "This employee is currently a Head of Department and cannot be moved."
                });
            }

            nv.MAPB = mapb;
            db.SaveChanges();

            return Json(new { success = true, message = "Employee has been added to this department." });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public JsonResult RemoveEmployee(string mapb, string manv)
        {
            if (string.IsNullOrWhiteSpace(mapb) || string.IsNullOrWhiteSpace(manv))
            {
                return Json(new { success = false, message = "Invalid data." });
            }

            var pb = db.PHONGBANs.Find(mapb);
            if (pb == null)
                return Json(new { success = false, message = "Department not found." });

            var nv = db.NHANVIENs.Find(manv);
            if (nv == null)
                return Json(new { success = false, message = "Employee not found." });

            if (!string.IsNullOrEmpty(pb.MATRG_PHG) && pb.MATRG_PHG == nv.MANV)
            {
                return Json(new
                {
                    success = false,
                    message = "Cannot remove Head of Department from this department."
                });
            }

            if (nv.MAPB != mapb)
            {
                return Json(new
                {
                    success = false,
                    message = "Employee does not belong to this department."
                });
            }

            nv.MAPB = null;
            db.SaveChanges();

            return Json(new { success = true, message = "Employee has been removed from this department." });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public JsonResult PromoteToHead(string mapb, string manv)
        {
            if (string.IsNullOrWhiteSpace(mapb) || string.IsNullOrWhiteSpace(manv))
            {
                return Json(new { success = false, message = "Invalid data." });
            }

            var pb = db.PHONGBANs.Find(mapb);
            if (pb == null)
            {
                return Json(new { success = false, message = "Department not found." });
            }

            var nv = db.NHANVIENs.Find(manv);
            if (nv == null)
            {
                return Json(new { success = false, message = "Employee not found." });
            }

            if (nv.MAPB != mapb)
            {
                return Json(new
                {
                    success = false,
                    message = "Employee does not belong to this department and cannot be promoted."
                });
            }

            pb.MATRG_PHG = manv;
            db.SaveChanges();

            return Json(new { success = true, message = "Employee has been promoted to Head of Department successfully." });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public JsonResult CreateInline(string tenpb, string diadiemCode)
        {
            if (string.IsNullOrWhiteSpace(tenpb) || string.IsNullOrWhiteSpace(diadiemCode))
            {
                return Json(new { success = false, message = "Please fill in all required information." });
            }

            tenpb = tenpb.Trim();

            byte diadiemByte;
            if (!byte.TryParse(diadiemCode, out diadiemByte))
            {
                return Json(new { success = false, message = "Invalid area code." });
            }

            string newCode = GenerateDeptCode();

            var pb = new PHONGBAN
            {
                MAPB = newCode,
                TENPB = tenpb,
                DIADIEM = diadiemByte,
                MATRG_PHG = null
            };

            db.PHONGBANs.Add(pb);
            db.SaveChanges();

            return Json(new
            {
                success = true,
                message = "New department has been created.",
                mapb = newCode
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public JsonResult UpdateInfo(string mapb, string tenpb, string diadiemCode)
        {
            if (string.IsNullOrWhiteSpace(mapb))
            {
                return Json(new { success = false, message = "Department ID is invalid." });
            }

            var pb = db.PHONGBANs.Find(mapb);
            if (pb == null)
            {
                return Json(new { success = false, message = "Department not found." });
            }

            if (!string.IsNullOrWhiteSpace(tenpb))
            {
                pb.TENPB = tenpb.Trim();
            }

            if (!string.IsNullOrWhiteSpace(diadiemCode))
            {
                byte diadiemByte;
                if (!byte.TryParse(diadiemCode, out diadiemByte))
                {
                    return Json(new { success = false, message = "Invalid area code." });
                }
                pb.DIADIEM = diadiemByte;
            }

            db.SaveChanges();

            return Json(new { success = true, message = "Department information has been updated." });
        }

        private string GenerateDeptCode()
        {
            var rand = new Random();
            const string letters = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";

            while (true)
            {
                var sb = new StringBuilder();
                for (int i = 0; i < 3; i++)
                {
                    sb.Append(letters[rand.Next(letters.Length)]);
                }
                sb.Append(rand.Next(0, 10));
                sb.Append(rand.Next(0, 10));

                string code = sb.ToString();
                bool exists = db.PHONGBANs.Any(p => p.MAPB == code);
                if (!exists)
                    return code;
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) db.Dispose();
            base.Dispose(disposing);
        }
    }
}
