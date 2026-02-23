using System;
using System.IO;

namespace PizzaOven
{
    public class PLUSSavesystem
    {
        private static readonly string folderPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "PizzaOvenPLUS");

        private static readonly string IniPath = Path.Combine(folderPath, "settings.ini");

        public static void write_ini(string section, string key, string value)
        {
            PLUSINI.write_ini(IniPath, section, key, value);
        }

        public static string read_ini(string section, string key, string defaultValue = "")
        {
            return PLUSINI.read_ini(IniPath, section, key, defaultValue);
        }

        public static string[,] read_ini_section(string section)
        {
            return PLUSINI.read_ini_section(IniPath, section);
        }
        public static bool toggle_ini_bool(string section, string key, bool defaultValue)
        {
            return PLUSINI.toggle_ini_bool(IniPath, section, key, defaultValue);
        }
        public static void delete_ini(string section, string key)
        {
            PLUSINI.delete_ini(IniPath, section, key);
        }

        public static void delete_ini_section(string section)
        {
            PLUSINI.delete_ini_section(IniPath, section);
        }
    }
}
