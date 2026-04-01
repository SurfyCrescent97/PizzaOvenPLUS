using System;
using System.Windows.Media;
using System.Windows;

namespace PizzaOven
{
    // 1.0.5 Themes
    public class PLUSThemes
    {
        public static string rgb_to_hex(int r, int g, int b)
        {
            return $"#{r:X2}{g:X2}{b:X2}";
        }
        public static (byte r, byte g, byte b) hex_to_rgb(string hex)
        {
            hex = hex.Replace("#", "");

            byte r = Convert.ToByte(hex.Substring(0, 2), 16);
            byte g = Convert.ToByte(hex.Substring(2, 2), 16);
            byte b = Convert.ToByte(hex.Substring(4, 2), 16);

            return (r, g, b);
        }
        public static void Set_BrushColor(string brushname, string color)
        {
            var colordata = hex_to_rgb(color);
            Application.Current.Resources[brushname] =
                new SolidColorBrush(Color.FromRgb(colordata.r, colordata.g, colordata.b));
        }
    }
}
