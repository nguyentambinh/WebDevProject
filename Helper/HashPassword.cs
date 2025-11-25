using System;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace QLNSVATC.Helpers
{
    public static class SecurityHelper
    {
        // Hàm bảo mật mật khẩu bằng SHA256
        public static string HashPassword(this string password)
        {
            if (string.IsNullOrEmpty(password))
                return string.Empty;

            byte[] bytes = Encoding.UTF8.GetBytes(password);

            using (SHA256 sha = SHA256.Create())
            {
                byte[] hashBytes = sha.ComputeHash(bytes);
                StringBuilder sb = new StringBuilder();

                foreach (byte b in hashBytes)
                {
                    sb.Append(b.ToString("x2"));
                }

                return sb.ToString();
            }
        }
        // Hàm kiểm tra độ mạnh của mật khẩu
        public static bool IsStrongPassword(this string password)
        {
            if (string.IsNullOrWhiteSpace(password)) return false;

            return Regex.IsMatch(password,
                @"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[@$!%*?&]).{8,}$");
        }
    }
}
