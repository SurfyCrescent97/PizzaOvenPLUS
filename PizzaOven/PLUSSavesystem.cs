using System;
using System.IO;

namespace PizzaOven
{
   
    public class PLUSSavesystem
    {
        private static readonly string folderPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "PizzaOvenPLUS");
        private static readonly string IniPath = Path.Combine(folderPath, "settings.ini");
        private static FileSystemWatcher watcher;
        public static event Action IniEdited;
        private static System.Timers.Timer debounceTimer;

        public static void StartWatcher()
        {
            if (watcher != null)
                return;

            watcher = new FileSystemWatcher(folderPath, "settings.ini");
            watcher.NotifyFilter = NotifyFilters.LastWrite;

            debounceTimer = new System.Timers.Timer(100);
            debounceTimer.AutoReset = false;
            debounceTimer.Elapsed += (s, e) =>
            {
                IniEdited?.Invoke();
            };

            watcher.Changed += (s, e) =>
            {
                debounceTimer.Stop();
                debounceTimer.Start();
            };

            watcher.EnableRaisingEvents = true;
        }
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
        public static bool read_ini_bool(string section, string key, bool defaultValue)
        {
            return PLUSINI.read_ini_bool(IniPath, section, key, defaultValue);
        }
        public static bool toggle_ini_bool(string section, string key, bool defaultValue)
        {
            return PLUSINI.toggle_ini_bool(IniPath, section, key, defaultValue);
        }
        public static void delete_ini_value(string section, string key)
        {
            PLUSINI.delete_ini_value(IniPath, section, key);
        }
        public static void delete_ini_section(string section)
        {
            PLUSINI.delete_ini_section(IniPath, section);
        }
    }
}
