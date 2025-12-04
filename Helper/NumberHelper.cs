using System;
using System.Globalization;

namespace QLNSVATC.Helpers
{
    public static class NumberHelper
    {
        public static string ToCurrency(this decimal number)
        {
            return string.Format(new CultureInfo("vi-VN"), "{0:N0} ₫", number);
        }

        public static string ToCurrency(this decimal number, string langCode)
        {
            var culture = ResolveCulture(langCode);
            return number.ToString("C0", culture);
        }

        public static string FormatNumber(this decimal number)
        {
            return number.ToString("N0", CultureInfo.InvariantCulture);
        }

        public static string FormatNumber(this decimal number, string langCode)
        {
            var culture = ResolveCulture(langCode);
            return number.ToString("N0", culture);
        }
        public static string ToShortCurrency(this decimal number, string langCode)
        {
            var culture = ResolveCulture(langCode);
            decimal abs = Math.Abs(number);
            decimal value = number;
            string suffix = "";

            if (abs >= 1_000_000_000m)
            {
                value = number / 1_000_000_000m;
                suffix = "B";
            }
            else if (abs >= 1_000_000m)
            {
                value = number / 1_000_000m;
                suffix = "M";
            }
            else if (abs >= 1_000m)
            {
                value = number / 1_000m;
                suffix = "K";
            }
            else
            {
                return number.ToCurrency(langCode);
            }

            string formatted = value.ToString("0.#", culture);

            if (culture.Name.StartsWith("vi"))
            {
                return $"{formatted}{suffix} ₫";
            }
            else if (culture.Name.StartsWith("en"))
            {
                string symbol = culture.NumberFormat.CurrencySymbol ?? "$";
                return $"{symbol}{formatted}{suffix}";
            }
            else
            {
                string symbol = culture.NumberFormat.CurrencySymbol;
                if (!string.IsNullOrEmpty(symbol))
                    return $"{formatted}{suffix} {symbol}";

                return $"{formatted}{suffix}";
            }
        }


        public static string ToCurrency(this double number, string langCode)
            => ((decimal)number).ToCurrency(langCode);

        public static string ToCurrency(this long number, string langCode)
            => ((decimal)number).ToCurrency(langCode);

        public static string ToShortCurrency(this double number, string langCode)
            => ((decimal)number).ToShortCurrency(langCode);

        public static string ToShortCurrency(this long number, string langCode)
            => ((decimal)number).ToShortCurrency(langCode);

        public static string FormatNumber(this double number, string langCode)
            => ((decimal)number).FormatNumber(langCode);

        public static string FormatNumber(this long number, string langCode)
            => ((decimal)number).FormatNumber(langCode);

        private static CultureInfo ResolveCulture(string langCode)
        {
            if (string.IsNullOrWhiteSpace(langCode))
                langCode = "vi_VN";

            langCode = langCode.Replace("_", "-");

            try
            {
                return CultureInfo.GetCultureInfo(langCode);
            }
            catch
            {
                return CultureInfo.GetCultureInfo("vi-VN");
            }
        }
        public static string FormatNumber(this int number) 
            => ((decimal)number).FormatNumber();

        public static string FormatNumber(this int number, string langCode)
            => ((decimal)number).FormatNumber(langCode);

        public static string ToCurrency(this int number, string langCode)
            => ((decimal)number).ToCurrency(langCode);

        public static string ToShortCurrency(this int number, string langCode)
            => ((decimal)number).ToShortCurrency(langCode);
    }
}
