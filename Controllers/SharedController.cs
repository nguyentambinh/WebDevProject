using QLNSVATC.Helpers;
using QLNSVATC.Models;
using System.Web.Mvc;
using System.Linq;
using System.Diagnostics;
using System;
using System.Data.Entity;
using System.Data.Entity.Validation;
namespace QLNSVATC.Controllers
{
    public class SharedController : Controller
    {
        private QLNSVATCEntities db = new QLNSVATCEntities();

        //Load Personalized Navigation Menu
        [ChildActionOnly]
        public ActionResult _Nav(string code)
        {
            var vb = SettingsHelper.BuildViewBagData(db, code);

            ViewBag.TranslateDict = vb.TranslateDict;
            ViewBag.CurrentLang = vb.Lang;
            ViewBag.ThemeColor = vb.ThemeHex;
            ViewBag.DarkMode = vb.DarkMode;
            ViewBag.FontFamily = vb.FontFamily;
            ViewBag.FontSize = vb.FontSize;
            ViewBag.LayoutCode = vb.LayoutCode;

            var menus = db.MENUs.ToList();

            Debug.WriteLine($"UserId      = {code}");
            Debug.WriteLine($"Lang        = {vb.Lang}");
            Debug.WriteLine($"ThemeCode   = {vb.Theme}");
            Debug.WriteLine($"ThemeHex    = {vb.ThemeHex}");
            Debug.WriteLine($"DarkMode    = {vb.DarkMode}");
            Debug.WriteLine($"FontFamily  = {vb.FontFamily}");
            Debug.WriteLine($"FontSize    = {vb.FontSize}");
            Debug.WriteLine($"LayoutCode  = {vb.LayoutCode}");
            Debug.WriteLine($"DictCount   = {vb.TranslateDict?.Count ?? 0}");

            if (vb.TranslateDict != null)
            {
                foreach (var m in menus)
                {
                    if (!string.IsNullOrEmpty(m.TranslateKey)
                        && vb.TranslateDict.TryGetValue(m.TranslateKey, out var text)
                        && !string.IsNullOrWhiteSpace(text))
                    {
                        m.MenuName = text;
                    }
                }
            }

            return PartialView("_Nav", menus);
        }
        

        //Load Personalized Footer
        [ChildActionOnly]
        public ActionResult _Footer(string userId)
        {
            ViewBag.UserId = userId;
            var vb = SettingsHelper.BuildViewBagData(db, userId);

            ViewBag.CurrentLang = vb.Lang;
            ViewBag.ThemeColor = vb.ThemeHex;
            ViewBag.DarkMode = vb.DarkMode;
            ViewBag.FontFamily = vb.FontFamily;
            ViewBag.FontSize = vb.FontSize;
            ViewBag.LayoutCode = vb.LayoutCode;

            return PartialView("_Footer");
        }
        public ActionResult _Header(string userId)
        {
            ViewBag.UserId = userId;
            return PartialView("_Header");
        }
        //Saving User Settings
        [HttpPost]
        public ActionResult SaveSettings(string ThemeColor, string DarkMode,
                                     string LanguageCode, string FontCode,
                                     int FontSize)
        {
            string userId = Convert.ToString(Session["UserId"]);
            Debug.WriteLine("SaveSetting");
            Debug.WriteLine($"userId       = {userId}");
            Debug.WriteLine($"ThemeColor   = {ThemeColor}");
            Debug.WriteLine($"DarkMode(raw)= {DarkMode}");
            Debug.WriteLine($"LanguageCode = {LanguageCode}");
            Debug.WriteLine($"FontCode     = {FontCode}");
            Debug.WriteLine($"FontSize     = {FontSize}");

            TempData["DebugSettings"] =
                $"userId={userId}; Theme={ThemeColor}; Dark={DarkMode}; Lang={LanguageCode}; Font={FontCode}; Size={FontSize}";

            if (string.IsNullOrEmpty(userId))
            {
                TempData["DebugSettings"] += " | userId NULL -> chưa login";
                return Redirect("/?status=error");
            }

            var st = db.USER_SETTINGS.FirstOrDefault(x => x.UserId == userId);

            bool dark = DarkMode == "true";

            if (st == null)
            {
                Debug.WriteLine("-> Chưa có bản ghi, tạo mới USER_SETTINGS");
                st = new USER_SETTINGS
                {
                    UserId = userId
                };
                db.USER_SETTINGS.Add(st);
            }
            else
            {
                Debug.WriteLine($"-> Update bản ghi SettingId = {st.SettingId}");
            }

            st.ThemeCode = ThemeColor;
            st.DarkMode = dark;
            st.LanguageCode = LanguageCode;
            st.FontCode = FontCode;
            st.FontSize = FontSize;
            st.UpdatedAt = DateTime.Now;
            Session["DarkMode"] = dark;
            Session["ThemeColor"] = ThemeColor;
            Session["LanguageCode"] = LanguageCode;
            Session["FontCode"] = FontCode;
            Session["FontSize"] = FontSize;
            db.SaveChanges();

            Debug.WriteLine("-> SaveChanges() xong OK");
            TempData["DebugSettings"] += " | Save OK";

            return Redirect("/?status=success");
        }
    }
}

