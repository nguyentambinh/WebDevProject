using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using QLNSVATC.Helpers;
using QLNSVATC.Models;

namespace QLNSVATC.Areas.HR.Controllers
{
    public class SharedController : Controller
    {
        QLNSVATCEntities db = new QLNSVATCEntities();
        // GET: Admin/Shared
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
        public ActionResult _Header(string userId)
        {
            ViewBag.UserId = userId;
            return PartialView("_Header");
        }

        public ActionResult _Footer(string userId)
        {
            ViewBag.UserId = userId;
            return PartialView("_Footer");
        }
    }
}