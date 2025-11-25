using System;

namespace QLNSVATC.Helpers
{
    public static class UrlHelperExtensions
    {
        // Đảm bảo URL có http/https. Nếu thiếu sẽ tự thêm "https://".
        public static string EnsureHttp(this string url)
        {
            if (string.IsNullOrWhiteSpace(url)) return url;

            return url.StartsWith("http", StringComparison.OrdinalIgnoreCase)
                ? url
                : "https://" + url;
        }
    }
}
