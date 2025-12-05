using System;
using System.Linq;
using System.Globalization;
using System.IO;
using System.Text;

namespace QLNSVATC.Helpers
{
    public static class FileHelper
    {
        // Lấy phần mở rộng của file
        public static string GetFileExtension(this string fileName)
        {
            return Path.GetExtension(fileName)?.ToLower() ?? string.Empty;
        }
        // Lấy tên file không kèm phần mở rộng.
        public static string GetFileNameOnly(this string filePath)
        {
            return Path.GetFileNameWithoutExtension(filePath);
        }

        // Hàm bỏ dấu Unicode khỏi chuỗi
        public static string RemoveDiacritics(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return text;
            var normalized = text.Normalize(NormalizationForm.FormD);
            var sb = new StringBuilder();

            foreach (var ch in normalized)
            {
                var uc = CharUnicodeInfo.GetUnicodeCategory(ch);
                if (uc != UnicodeCategory.NonSpacingMark)
                {
                    sb.Append(ch);
                }
            }
            return sb.ToString().Normalize(NormalizationForm.FormC);
        }


        public static string NormalizeSimpleName(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return "unknown";

            string noDiacritics = RemoveDiacritics(text);

            var sb = new StringBuilder();
            foreach (char c in noDiacritics)
            {
                if (char.IsLetterOrDigit(c))
                    sb.Append(c);
            }

            return sb.ToString();
        }

        public static string BuildNormalizedFileName(string fullName, string fileType, DateTime time, string extension)
        {
            string cleanName = NormalizeSimpleName(fullName);
            string timestamp = time.ToString("yyyyMMddHHmmss");

            if (!extension.StartsWith("."))
                extension = "." + extension;

            return $"{cleanName}_{fileType}_{timestamp}{extension}".ToLower();
        }
        public static string ToSafeName(this string raw)
        {
            if (string.IsNullOrWhiteSpace(raw))
                return "Unknown";
            raw = RemoveDiacritics(raw);  

            var invalidChars = Path.GetInvalidFileNameChars();

            string cleaned = raw.Trim();
            while (cleaned.Contains("  "))
                cleaned = cleaned.Replace("  ", " ");
            cleaned = new string(cleaned
                .Select(ch =>
                {
                    if (invalidChars.Contains(ch)) return '_';
                    if (ch == ' ') return '_';
                    return ch;
                })
                .ToArray());

            return cleaned;
        }

        // Tạo tên folder: yyyyMMddHH_Ho_ten_ung_vien 
        public static string BuildCandidateFolderName(string fullName, DateTime time)
        {
            string name = fullName ?? "";
            name = RemoveDiacritics(name).Trim();
            name = name.Replace(" ", string.Empty);

            string prefix = time.ToString("yyyyMMddHHmmss");
            return $"{prefix}_{name}";
        }
    }
}
