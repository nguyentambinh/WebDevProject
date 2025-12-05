using System;
using System.Linq;
using System.Globalization;
using System.IO;
using System.Text;
using System.Web;

namespace QLNSVATC.Helpers
{
    public static class FileHelper
    {
        public static string GetFileExtension(this string fileName)
        {
            return Path.GetExtension(fileName)?.ToLower() ?? string.Empty;
        }

        public static string GetFileNameOnly(this string filePath)
        {
            return Path.GetFileNameWithoutExtension(filePath);
        }

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

        public static string BuildCandidateFolderName(string candidateName, DateTime time)
        {
            string safeName = candidateName.ToSafeName();
            return $"{time:yyyyMMddHH}_{safeName}";
        }

        public static string GetReportTypeCode(string areaName)
        {
            if (string.IsNullOrWhiteSpace(areaName))
                return "BCCM";

            areaName = areaName.Trim().ToUpperInvariant();

            if (areaName == "FN") return "BCTC";
            if (areaName == "HR") return "BCNS";

            return "BCCM";
        }

        public static string BuildReportFileName(string areaName, DateTime? time = null, string extension = ".xlsx")
        {
            var ts = (time ?? DateTime.Now).ToString("yyyyMMddHHmmss");
            var typeCode = GetReportTypeCode(areaName);

            if (!extension.StartsWith("."))
                extension = "." + extension;

            return $"{ts}_{typeCode}{extension}";
        }

        public static string EnsureFolder(string virtualPath)
        {
            if (HttpContext.Current == null)
                throw new InvalidOperationException("HttpContext.Current is null. Ensure this is called in web context.");

            string physical = HttpContext.Current.Server.MapPath(virtualPath);
            if (!Directory.Exists(physical))
                Directory.CreateDirectory(physical);

            return physical;
        }

        public static string SaveReportBytes(byte[] bytes, string virtualFolder, string fileName)
        {
            string folder = EnsureFolder(virtualFolder);
            string fullPath = Path.Combine(folder, fileName);
            File.WriteAllBytes(fullPath, bytes);
            return fullPath;
        }
    }
}
