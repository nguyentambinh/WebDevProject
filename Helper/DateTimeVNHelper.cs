using System;

namespace QLNSVATC.Helpers
{
    public static class DateTimeHelper
    {
        // Định dạng ngày kiểu Việt Nam dd/MM/yyyy.
        public static string ToVNDate(this DateTime date)
        {
            return date.ToString("dd/MM/yyyy");
        }
        // Hiển thị dạng "x phút trước", "x giờ trước", "x ngày trước" hoặc ngày cụ thể.
        public static string TimeAgo(this DateTime date)
        {
            var ts = DateTime.Now - date;

            if (ts.TotalMinutes < 1) return "Vừa xong";
            if (ts.TotalHours < 1) return $"{(int)ts.TotalMinutes} phút trước";
            if (ts.TotalDays < 1) return $"{(int)ts.TotalHours} giờ trước";
            if (ts.TotalDays < 7) return $"{(int)ts.TotalDays} ngày trước";

            return date.ToString("dd/MM/yyyy");
        }
    }
}
