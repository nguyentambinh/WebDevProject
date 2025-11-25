using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace QLNSVATC.Models
{
    public class UserViewBagModel
    {
        public IDictionary<string, string> TranslateDict { get; set; }
        public string Lang { get; set; }
        public string Theme { get; set; }
        public string ThemeHex { get; set; }
        public bool DarkMode { get; set; }
        public string FontFamily { get; set; }
        public int FontSize { get; set; }
        public string LayoutCode { get; set; }
    }
}