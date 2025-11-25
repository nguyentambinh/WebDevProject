using System;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace QLNSVATC.Helpers
{
    public static class SlugHelper
    {
        //Hàm chuẩn hóa chuỗi thành slug
        public static string ToSlug(this string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return string.Empty;
            text = text.ToLowerInvariant();
            string normalized = text.Normalize(NormalizationForm.FormD);
            var sb = new StringBuilder();

            foreach (var c in normalized)
            {
                var uc = CharUnicodeInfo.GetUnicodeCategory(c);

                if (uc != UnicodeCategory.NonSpacingMark) 
                {
                    char ch = c;
                    if (ch == 'đ') ch = 'd';
                    if (ch == ' ' || ch == '_')
                        ch = '-';

                    sb.Append(ch);
                }
            }

            var slug = sb.ToString().Normalize(NormalizationForm.FormC);

            slug = Regex.Replace(slug, @"[^a-z0-9\-]", "-");
            slug = Regex.Replace(slug, @"-+", "-");
            slug = slug.Trim('-');

            return slug;
        }

    }
}
