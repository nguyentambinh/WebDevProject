using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using QLNSVATC.Helpers;
using QLNSVATC.Models;
using QLNSVATC.Areas.Admin.Data;
using QLNSVATC.Helper;

namespace QLNSVATC.Areas.Admin.Controllers
{
    public class MenuController : Controller
    {
        private readonly QLNSVATCEntities db = new QLNSVATCEntities();

        public ActionResult Index(string role = null)
        {
            try
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
                ViewBag.ThemeColor = st.ThemeHex;
                ViewBag.DarkMode = st.DarkMode;
                ViewBag.FontFamily = st.FontFamily;
                ViewBag.FontSize = st.FontSize;

                var roles = db.MENUs
                    .Select(m => m.Role)
                    .Distinct()
                    .Where(r => r != null && r != "")
                    .OrderBy(r => r)
                    .ToList();

                ViewBag.Roles = roles;
                ViewBag.RoleFilter = role;

                IQueryable<MENU> query = db.MENUs;

                if (!string.IsNullOrEmpty(role))
                {
                    query = query.Where(m => m.Role == role);
                }

                var menus = query
                    .OrderBy(m => m.ParentId)
                    .ThenBy(m => m.OrderNumber)
                    .ThenBy(m => m.MenuId)
                    .ToList();

                var keys = menus
                    .Where(m => !string.IsNullOrEmpty(m.TranslateKey))
                    .Select(m => m.TranslateKey)
                    .Distinct()
                    .ToList();

                var trans = db.PHIENDICHes
                    .Where(t => keys.Contains(t.TranslateKey))
                    .ToList();

                var dict = trans.ToDictionary(t => t.TranslateKey, t => t);
                ViewBag.Translations = dict;

                return View(menus);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Menu.Index error: " + ex);
                TempData["MenuError"] = "System error while loading menu list.";

                ViewBag.Roles = new List<string>();
                ViewBag.RoleFilter = role;
                ViewBag.Translations = new Dictionary<string, PHIENDICH>();

                return View(new List<MENU>());
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult SaveMenu(MenuEditViewModel model)
        {
            if (model == null)
            {
                TempData["MenuError"] = "Invalid data.";
                return RedirectToAction("Index");
            }

            if (!ModelState.IsValid)
            {
                TempData["MenuError"] = "Invalid data.";
                return RedirectToAction("Index", new { role = model.Role });
            }

            try
            {
                MENU menu;

                if (model.MenuId.HasValue && model.MenuId.Value > 0)
                {
                    menu = db.MENUs.Find(model.MenuId.Value);
                    if (menu == null)
                    {
                        TempData["MenuError"] = "Menu not found.";
                        return RedirectToAction("Index", new { role = model.Role });
                    }
                }
                else
                {
                    menu = new MENU();
                    db.MENUs.Add(menu);
                }

                menu.MenuName = model.MenuName;
                menu.MenuLink = string.IsNullOrWhiteSpace(model.MenuLink)
                    ? null
                    : model.MenuLink.Trim();

                menu.ParentId = model.ParentId;

                menu.Role = string.IsNullOrWhiteSpace(model.Role)
                    ? null
                    : model.Role.Trim();

                int desiredOrder = model.OrderNumber <= 0 ? 1 : model.OrderNumber;

                var siblingsQuery = db.MENUs.Where(m => m.ParentId == menu.ParentId);

                if (!string.IsNullOrEmpty(menu.Role))
                {
                    siblingsQuery = siblingsQuery.Where(m => m.Role == menu.Role);
                }

                if (menu.MenuId > 0)
                {
                    siblingsQuery = siblingsQuery.Where(m => m.MenuId != menu.MenuId);
                }

                var siblings = siblingsQuery
                    .OrderBy(m => m.OrderNumber)
                    .ThenBy(m => m.MenuId)
                    .ToList();

                if (desiredOrder > siblings.Count + 1)
                    desiredOrder = siblings.Count + 1;

                menu.OrderNumber = desiredOrder;

                int index = 1;
                foreach (var s in siblings)
                {
                    if (index == desiredOrder)
                    {
                        index++;
                    }
                    s.OrderNumber = index;
                    index++;
                }

                if (!string.IsNullOrWhiteSpace(model.TranslateKey))
                {
                    menu.TranslateKey = model.TranslateKey.Trim();
                }
                else if (string.IsNullOrEmpty(menu.TranslateKey))
                {
                    var rolePart = string.IsNullOrEmpty(menu.Role) ? "COMMON" : menu.Role.ToUpper();
                    var namePart = (model.MenuName ?? "ITEM")
                        .ToUpper()
                        .Replace(" ", "_");

                    menu.TranslateKey = $"MENU.{rolePart}.{namePart}";
                }

                if (!string.IsNullOrEmpty(menu.TranslateKey))
                {
                    var t = db.PHIENDICHes
                        .FirstOrDefault(x => x.TranslateKey == menu.TranslateKey);

                    if (t == null)
                    {
                        t = new PHIENDICH
                        {
                            TranslateKey = menu.TranslateKey
                        };
                        db.PHIENDICHes.Add(t);
                    }

                    if (model.vi_VN != null) t.vi_VN = model.vi_VN;
                    if (model.en_US != null) t.en_US = model.en_US;
                    if (model.jp_JP != null) t.jp_JP = model.jp_JP;
                    if (model.kr_KR != null) t.kr_KR = model.kr_KR;
                    if (model.cn_CN != null) t.cn_CN = model.cn_CN;
                }

                db.SaveChanges();

                TempData["MenuSuccess"] = model.MenuId.HasValue
                    ? "Menu updated successfully."
                    : "Menu created successfully.";

                return RedirectToAction("Index", new { role = menu.Role });
            }
            catch (Exception ex)
            {
                var errMsg = ex.InnerException != null
                    ? (ex.InnerException.InnerException != null
                        ? ex.InnerException.InnerException.Message
                        : ex.InnerException.Message)
                    : ex.Message;

                System.Diagnostics.Debug.WriteLine("Menu.SaveMenu error: " + ex);

                TempData["MenuError"] = "System error while saving menu: " + errMsg;
                return RedirectToAction("Index", new { role = model.Role });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Delete(int id, string role)
        {
            try
            {
                var menu = db.MENUs.Find(id);
                if (menu == null)
                {
                    TempData["MenuError"] = "Menu not found.";
                    return RedirectToAction("Index", new { role });
                }

                bool hasChild = db.MENUs.Any(m => m.ParentId == id);
                if (hasChild)
                {
                    TempData["MenuError"] = "Please delete or move all child menus first.";
                    return RedirectToAction("Index", new { role });
                }

                db.MENUs.Remove(menu);
                db.SaveChanges();

                TempData["MenuSuccess"] = "Menu deleted.";
                return RedirectToAction("Index", new { role = role ?? menu.Role });
            }
            catch (Exception ex)
            {
                var errMsg = ex.InnerException != null
                    ? (ex.InnerException.InnerException != null
                        ? ex.InnerException.InnerException.Message
                        : ex.InnerException.Message)
                    : ex.Message;

                System.Diagnostics.Debug.WriteLine("Menu.Delete error: " + ex);

                TempData["MenuError"] = "System error while deleting menu: " + errMsg;
                return RedirectToAction("Index", new { role });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult SaveTranslation(MenuTranslationViewModel model)
        {
            if (model == null || string.IsNullOrWhiteSpace(model.TranslateKey))
            {
                TempData["MenuError"] = "Missing TranslateKey.";
                return RedirectToAction("Index", new { role = model?.Role });
            }

            try
            {
                var key = model.TranslateKey.Trim();

                var t = db.PHIENDICHes
                    .FirstOrDefault(x => x.TranslateKey == key);

                if (t == null)
                {
                    t = new PHIENDICH
                    {
                        TranslateKey = key
                    };
                    db.PHIENDICHes.Add(t);
                }

                t.vi_VN = model.vi_VN;
                t.en_US = model.en_US;
                t.jp_JP = model.jp_JP;
                t.kr_KR = model.kr_KR;
                t.cn_CN = model.cn_CN;

                db.SaveChanges();

                TempData["MenuSuccess"] = "Translation saved.";
                return RedirectToAction("Index", new { role = model.Role });
            }
            catch (Exception ex)
            {
                var errMsg = ex.InnerException != null
                    ? (ex.InnerException.InnerException != null
                        ? ex.InnerException.InnerException.Message
                        : ex.InnerException.Message)
                    : ex.Message;

                System.Diagnostics.Debug.WriteLine("Menu.SaveTranslation error: " + ex);

                TempData["MenuError"] = "System error while saving translation: " + errMsg;
                return RedirectToAction("Index", new { role = model.Role });
            }
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
