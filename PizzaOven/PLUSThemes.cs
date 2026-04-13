using System;
using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Effects;
using System.Windows.Media.Imaging;

namespace PizzaOven
{
    public class PLUSThemes
    {
        public static string rgb_to_hex(byte r, byte g, byte b)
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
        public static Color rgb_as_color(byte r, byte g, byte b)
        {
            return Color.FromArgb(255, r, g, b);
        }
        public static (byte r, byte g, byte b) color_as_rgb(Color color)
        {
            return (color.R, color.G, color.B);
        }
        public static void Set_BrushColor(string brushname, string color)
        {
            var colordata = hex_to_rgb(color);
            Application.Current.Resources[brushname] =
                new SolidColorBrush(Color.FromRgb(colordata.r, colordata.g, colordata.b));
        }
        public static bool validhex(string hex)
        {
            if (hex.StartsWith("#"))
                hex = hex.Substring(1);
            return hex.Length == 6 && int.TryParse(hex, System.Globalization.NumberStyles.HexNumber, null, out _);
        }
        public static Color Get_BrushColor(string brushname)
        {
            var app = Application.Current;
            if (app == null)
                return Colors.Transparent;

            var res = app.TryFindResource(brushname);
            if (res == null)
                return Colors.Transparent;

            if (res is SolidColorBrush scb)
                return scb.Color;

            if (res is Color c)
                return c;

            if (res is string s && validhex(s))
            {
                var rgb = hex_to_rgb(s);
                return Color.FromRgb(rgb.r, rgb.g, rgb.b);
            }

            return Colors.Transparent;
        }
        public static string Get_BrushColorAsHex(string brushname)
        {
            var color = Get_BrushColor(brushname);
            var (r, g, b) = color_as_rgb(color);
            return rgb_to_hex(r, g, b);
        }
        public static string Base64_SaveFile(string path)
        {
            if (!File.Exists(path))
                throw new FileNotFoundException("Image not found", path);

            byte[] bytes = File.ReadAllBytes(path);
            return Convert.ToBase64String(bytes);
        }

        public static void Base64_LoadFile(string base64, string path)
        {
            byte[] bytes = Convert.FromBase64String(base64);
            File.WriteAllBytes(path, bytes);
        }
        public static bool IsBase64String(string base64)
        {
            Span<byte> buffer = new Span<byte>(new byte[base64.Length]);
            return Convert.TryFromBase64String(base64, buffer, out int bytesParsed);
        }

    }
}
