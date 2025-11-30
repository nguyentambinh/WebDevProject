using System;
using System.Drawing;
using System.Text.RegularExpressions;

namespace QLNSVATC.Helpers 
{
    public static class ColorHelper
    {
        public static string HexToRgba(string hexColor, double alpha)
        {
            if (alpha < 0.0) alpha = 0.0;
            if (alpha > 1.0) alpha = 1.0;

            string cleanHex = hexColor.StartsWith("#") ? hexColor.Substring(1) : hexColor;

            if (cleanHex.Length != 6 && cleanHex.Length != 3)
            {
                return $"rgba(0, 0, 0, {alpha.ToString("F2")})";
            }

            try
            {
                Color color;
                if (cleanHex.Length == 3)
                {
                    cleanHex = "" + cleanHex[0] + cleanHex[0] + cleanHex[1] + cleanHex[1] + cleanHex[2] + cleanHex[2];
                }

                color = ColorTranslator.FromHtml("#" + cleanHex);

                return $"rgba({color.R}, {color.G}, {color.B}, {alpha.ToString("F2")})";
            }
            catch
            {
                return $"rgba(0, 0, 0, {alpha.ToString("F2")})";
            }
        }
    }
}