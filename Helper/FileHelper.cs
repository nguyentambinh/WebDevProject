using System;
using System.IO;

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
    }
}
