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

        // Hàm bỏ dấu Unicode (kể cả tiếng Việt) khỏi chuỗi
        public static string RemoveDiacritics(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return text;

            // Chuẩn hoá về dạng tách dấu
            var normalized = text.Normalize(NormalizationForm.FormD);
            var sb = new StringBuilder();

            foreach (var ch in normalized)
            {
                var uc = CharUnicodeInfo.GetUnicodeCategory(ch);
                // Bỏ các ký tự dấu (NonSpacingMark)
                if (uc != UnicodeCategory.NonSpacingMark)
                {
                    sb.Append(ch);
                }
            }

            // Trả về dạng đã bỏ dấu
            return sb.ToString().Normalize(NormalizationForm.FormC);
        }


        public static string NormalizeSimpleName(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return "unknown";

            // Bỏ dấu trước
            string noDiacritics = RemoveDiacritics(text);

            // Chỉ giữ ký tự chữ + số
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

            // extension phải bao gồm dấu .
            if (!extension.StartsWith("."))
                extension = "." + extension;

            return $"{cleanName}_{fileType}_{timestamp}{extension}".ToLower();
        }
        public static string ToSafeName(this string raw)
        {
            if (string.IsNullOrWhiteSpace(raw))
                return "Unknown";

            // CHANGED: bỏ dấu trước khi xử lý ký tự cấm + space
            raw = RemoveDiacritics(raw);   // CHANGED

            var invalidChars = Path.GetInvalidFileNameChars();

            // Trim, gom nhiều space về 1 space
            string cleaned = raw.Trim();
            while (cleaned.Contains("  "))
                cleaned = cleaned.Replace("  ", " ");

            // Thay space = '_' và loại ký tự cấm => '_' luôn
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

        // Tạo tên folder: yyyyMMddHH_Ho_ten_ung_vien (không dấu, không space)
        public static string BuildCandidateFolderName(string candidateName, DateTime time)
        {
            // CHANGED: dùng ToSafeName đã bỏ dấu + thay space bằng '_'
            string safeName = candidateName.ToSafeName();   // CHANGED
            return $"{time:yyyyMMddHH}_{safeName}";
        }
    }
}
