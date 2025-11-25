using System;
using System.Globalization;
using System.Text.RegularExpressions;

namespace QLNSVATC.Helpers
{
    public static class StringHelper
    {
        // Kiểm tra định dạng email hợp lệ
        public static bool IsEmail(this string input)
        {
            if (string.IsNullOrWhiteSpace(input)) return false;

            return Regex.IsMatch(input,
                @"^[^@\s]+@[^@\s]+\.[^@\s]+$",
                RegexOptions.IgnoreCase);
        }
        // Chuyển chuỗi thành chữ hoa mỗi từ
        public static string ToTitleCase(this string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return string.Empty;

            return CultureInfo.CurrentCulture.TextInfo.ToTitleCase(text.ToLower());
        }
        // Cắt chuỗi nếu dài hơn maxLength và thêm "..."
        public static string Ellipsis(this string text, int maxLength)
        {
            if (string.IsNullOrEmpty(text)) return text;

            return text.Length <= maxLength
                ? text
                : text.Substring(0, maxLength) + "...";
        }
    }
}
