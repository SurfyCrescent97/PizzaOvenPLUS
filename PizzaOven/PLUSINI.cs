using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;

namespace PizzaOven
{
    public class PLUSINI
    {
        public static void write_ini(string filePath, string section, string key, string value)
        {
            EnsureDirectory(filePath);

            var data = read_all(filePath);

            if (!data.ContainsKey(section))
                data[section] = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            data[section][key] = value;

            save_all(filePath, data);
        }
		

        public static string read_ini(string filePath, string section, string key, string defaultValue = "")
        {
            var data = read_all(filePath);

            if (data.TryGetValue(section, out var sectionData) &&
                sectionData.TryGetValue(key, out var value))
            {
                return value;
            }

            return defaultValue;
        }

        public static string[,] read_ini_section(string filePath, string section)
        {
            var data = read_all(filePath);

            if (!data.TryGetValue(section, out var sectionData) || sectionData.Count == 0)
                return new string[0, 0];

            var result = new string[sectionData.Count, 2];
            int i = 0;

            foreach (var kv in sectionData)
            {
                result[i, 0] = kv.Key;
                result[i, 1] = kv.Value;
                i++;
            }

            return result;
        }
        public static bool read_ini_bool(string filePath, string section, string key, bool defaultValue)
        {
            return read_ini(filePath, section, key, defaultValue.ToString().ToLowerInvariant()) == "true";
        }
        public static bool toggle_ini_bool(string filePath, string section, string key, bool defaultValue)
		{
			bool toggle = read_ini(filePath, section, key, defaultValue.ToString().ToLowerInvariant()) != "true";
			write_ini(filePath, section, key, toggle.ToString().ToLowerInvariant());
			return toggle;
		}
        public static void delete_ini_value(string filePath, string section, string key)
        {
            if (string.IsNullOrWhiteSpace(section) || string.IsNullOrWhiteSpace(key))
                return;

            var data = read_all(filePath);

            if (!data.TryGetValue(section, out var sectionData))
                return;

            if (sectionData.Remove(key))
            {
                if (sectionData.Count == 0)
                    data.Remove(section);

                if (data.Count == 0)
                {
                    if (File.Exists(filePath))
                        File.Delete(filePath);
                }
                else
                {
                    save_all(filePath, data);
                }
            }
        }
        public static void delete_ini_section(string filePath, string section)
        {
            if (string.IsNullOrWhiteSpace(section))
                return;

            var data = read_all(filePath);

            if (!data.Remove(section))
                return;

            if (data.Count == 0)
            {
                if (File.Exists(filePath))
                    File.Delete(filePath);
            }
            else
            {
                save_all(filePath, data);
            }
        }
        private static Dictionary<string, Dictionary<string, string>> read_all(string filePath)
        {
            var result = new Dictionary<string, Dictionary<string, string>>(StringComparer.OrdinalIgnoreCase);

            if (!File.Exists(filePath))
                return result;

            string currentSection = "";

            foreach (var line in File.ReadAllLines(filePath))
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

        private static void save_all(string filePath, Dictionary<string, Dictionary<string, string>> data)
        {
            EnsureDirectory(filePath);

            using var writer = new StreamWriter(filePath, false);

            foreach (var section in data)
            {
                writer.WriteLine($"[{section.Key}]");

                foreach (var kv in section.Value)
                    writer.WriteLine($"{kv.Key}={kv.Value}");

                writer.WriteLine();
            }
        }

        private static void EnsureDirectory(string filePath)
        {
            string? dir = Path.GetDirectoryName(filePath);

            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                Directory.CreateDirectory(dir);
        }
    }
}
