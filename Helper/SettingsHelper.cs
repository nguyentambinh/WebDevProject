using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using QLNSVATC.Models;

namespace QLNSVATC.Helpers
{
    public static class SettingsHelper
    {
        //Cá nhân hóa theo từng user
        public static UserViewBagModel BuildViewBagData(QLNSVATCEntities db, string userId)
        {
            USER_SETTINGS st = null;

            if (!string.IsNullOrEmpty(userId))
                st = db.USER_SETTINGS.FirstOrDefault(x => x.UserId == userId);

            if (st == null)
            {
                st = new USER_SETTINGS
                {
                    UserId = userId ?? "GUEST",
                    ThemeCode = "black",
                    DarkMode = true,
                    LanguageCode = "vi_VN",
                    FontCode = "inter",
                    FontSize = 18,
                    LayoutCode = "default"
                };
            }

            // Lấy Theme và Font
            string themeHexColor = null;
            string fontName = null;

            if (!string.IsNullOrEmpty(st.ThemeCode))
            {
                if (st.ThemeCode.StartsWith("#"))          
                {
                    themeHexColor = st.ThemeCode;
                }
                else
                {
                    var t = db.THEMEs.FirstOrDefault(x => x.ThemeCode == st.ThemeCode);
                    if (t != null)
                        themeHexColor = t.HexColor;
                }
            }

            if (!string.IsNullOrEmpty(st.FontCode))
            {
                var f = db.FONTS.FirstOrDefault(x => x.FontCode == st.FontCode);
                if (f != null)
                    fontName = f.FontName;
            }

            // Lấy ngôn ngữ
            string lang = st.LanguageCode ?? "vi-VN";
            string langCol = lang.Replace("-", "_");     // vi-VN -> vi_VN

            var trans = db.PHIENDICHes.ToList();
            var dict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            foreach (var t in trans)
            {
                PropertyInfo prop = typeof(PHIENDICH).GetProperty(langCol);

                string text = prop?.GetValue(t) as string;

                if (string.IsNullOrWhiteSpace(text))
                {
                    var pVi = typeof(PHIENDICH).GetProperty("vi_VN");
                    text = pVi?.GetValue(t) as string ?? t.TranslateKey;
                }

                dict[t.TranslateKey] = text;
            }

            return new UserViewBagModel
            {
                TranslateDict = dict,
                Lang = lang,
                Theme = st.ThemeCode,
                ThemeHex = themeHexColor,
                DarkMode = st.DarkMode,            
                FontFamily = fontName ?? st.FontCode,
                FontSize = st.FontSize ?? 14,
                LayoutCode = st.LayoutCode
            };

        }
    }

    
}
