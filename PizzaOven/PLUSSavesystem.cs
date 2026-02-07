using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;

namespace PizzaOven
{
    public class PLUSSavesystem
    {
        private static readonly string folderPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),"PizzaOvenPLUS");

        private static readonly string IniPath = Path.Combine(folderPath, "settings.ini");

        public static void write_ini(string section, string key, string value)
        {
            if (!Directory.Exists(folderPath))
                Directory.CreateDirectory(folderPath);

            var data = read_all();

            if (!data.ContainsKey(section))
                data[section] = new Dictionary<string, string>();

            data[section][key] = value;

            save_all(data);
        }

        public static string read_ini(string section, string key, string defaultValue = "")
        {
            var data = read_all();

            if (data.TryGetValue(section, out var sectionData) &&
                sectionData.TryGetValue(key, out var value))
            {
                return value;
            }

            return defaultValue;
        }

        public static string[,] read_ini_section(string section)
        {
            var data = read_all();

            if (!data.TryGetValue(section, out var sectionData) || sectionData.Count == 0)
                return new string[0, 0];

            var keys = sectionData.Keys.ToArray();
            var values = sectionData.Values.ToArray();

            var result = new string[sectionData.Count, 2];

            for (int i = 0; i < sectionData.Count; i++)
            {
                result[i, 0] = keys[i];
                result[i, 1] = values[i];
            }

            return result;
        }

        public static void delete_ini(string section, string key)
        {
            if (string.IsNullOrWhiteSpace(section) || string.IsNullOrWhiteSpace(key))
                return;

            var data = read_all();

            if (!data.TryGetValue(section, out var sectionData))
                return;

            if (sectionData.Remove(key))
            {
                if (sectionData.Count == 0)
                    data.Remove(section);

                if (data.Count == 0)
                {
                    try
                    {
                        if (File.Exists(IniPath))
                            File.Delete(IniPath);
                    }
                    catch
                    {
                    }
                }
                else
                {
                    save_all(data);
                }
            }
        }

        public static void delete_ini_section(string section)
        {
            if (string.IsNullOrWhiteSpace(section))
                return;

            var data = read_all();

            if (!data.Remove(section))
                return;

            if (data.Count == 0)
            {
                try
                {
                    if (File.Exists(IniPath))
                        File.Delete(IniPath);
                }
                catch
                {
                }
            }
            else
            {
                save_all(data);
            }
        }

        private static Dictionary<string, Dictionary<string, string>> read_all()
        {
            var result = new Dictionary<string, Dictionary<string, string>>(StringComparer.OrdinalIgnoreCase);
            if (!File.Exists(IniPath))
                return result;

            string currentSection = "";

            foreach (var line in File.ReadAllLines(IniPath))
            {
                var trimmed = line.Trim();

                if (string.IsNullOrEmpty(trimmed) || trimmed.StartsWith(";"))
                    continue;

                if (trimmed.StartsWith("[") && trimmed.EndsWith("]"))
                {
                    currentSection = trimmed[1..^1];
                    if (!result.ContainsKey(currentSection))
                        result[currentSection] = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                }
                else
                {
                    int idx = trimmed.IndexOf('=');
                    if (idx <= 0 || string.IsNullOrEmpty(currentSection))
                        continue;

                    string key = trimmed[..idx].Trim();
                    string value = trimmed[(idx + 1)..].Trim();

                    result[currentSection][key] = value;
                }
            }

            return result;
        }

        private static void save_all(Dictionary<string, Dictionary<string, string>> data)
        {
            if (!Directory.Exists(folderPath))
                Directory.CreateDirectory(folderPath);

            using var writer = new StreamWriter(IniPath, false);

            foreach (var section in data)
            {
                writer.WriteLine($"[{section.Key}]");

                foreach (var kv in section.Value)
                    writer.WriteLine($"{kv.Key}={kv.Value}");

                writer.WriteLine();
            }
        }
    }
}

