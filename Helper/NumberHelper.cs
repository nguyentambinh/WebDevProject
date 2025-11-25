using System;
using System.Globalization;

namespace QLNSVATC.Helpers
{
    public static class NumberHelper
    {
        // Định dạng tiền Việt Nam, ví dụ: 5000000 -> "5.000.000 ₫"
        public static string ToCurrency(this decimal number)
        {
            return string.Format(new CultureInfo("vi-VN"), "{0:N0} ₫", number);
        }

        // Định dạng số có dấu phân tách, không kèm đơn vị tiền.
        public static string FormatNumber(this decimal number)
        {
            return number.ToString("N0", CultureInfo.InvariantCulture);
        }
    }
}
