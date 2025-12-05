using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace QLNSVATC.Areas.Admin.Data
{
    public class MenuEditViewModel
    {
        public int? MenuId { get; set; }
        public string MenuName { get; set; }
        public string MenuLink { get; set; }
        public int? ParentId { get; set; }
        public int OrderNumber { get; set; }
        public string Role { get; set; }
        public string TranslateKey { get; set; }

        public string vi_VN { get; set; }
        public string en_US { get; set; }
        public string jp_JP { get; set; }
        public string kr_KR { get; set; }
        public string cn_CN { get; set; }
    }

    public class MenuTranslationViewModel
    {
        public string TranslateKey { get; set; }
        public string Role { get; set; }
        public string vi_VN { get; set; }
        public string en_US { get; set; }
        public string jp_JP { get; set; }
        public string kr_KR { get; set; }
        public string cn_CN { get; set; }
    }


    public class LANG_TRANSLATE
    {
        public string TranslateKey { get; set; }
        public string vi_VN { get; set; }
        public string en_US { get; set; }
        public string jp_JP { get; set; }
        public string kr_KR { get; set; }
        public string cn_CN { get; set; }
    }
}