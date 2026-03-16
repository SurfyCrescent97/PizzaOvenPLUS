using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

// Future Use
namespace PizzaOven
{
    public static class PLUSLanguage
    {
        private static readonly Assembly Assembly = Assembly.GetExecutingAssembly();
        private const string ResourceFolder = ".Language.";

        public static readonly Dictionary<string, string> Languages =
            Assembly.GetManifestResourceNames()
            .Where(r => r.Contains(ResourceFolder) && r.EndsWith(".ini"))
            .ToDictionary(
                r =>
                {
                    int start = r.IndexOf(ResourceFolder, StringComparison.Ordinal) + ResourceFolder.Length;
                    int end = r.LastIndexOf(".ini", StringComparison.Ordinal);
                    return r.Substring(start, end - start);
                },
                r => r,
                StringComparer.OrdinalIgnoreCase
            );

        public static List<string> language_filenames() => Languages.Keys.ToList();

        public static bool language_exist(string lang) => Languages.ContainsKey(lang);

        public static string read_language(string lang, string value, string defaultvalue)
        {
            if (!language_exist(lang))
            {
                if (lang.Equals("english", StringComparison.OrdinalIgnoreCase))
                    return "ENGLISH NOT FOUND";

                return read_language("english", value, defaultvalue);
            }
            using Stream resourceStream = Assembly.GetManifestResourceStream(Languages[lang])
                ?? throw new FileNotFoundException($"Embedded language resource not found: {lang}");

            string tempPath = Path.GetTempFileName();
            try
            {
                using (var fileStream = new FileStream(tempPath, FileMode.Create, FileAccess.Write, FileShare.None))
                {
                    resourceStream.CopyTo(fileStream);
                }
                if (lang.Equals("english", StringComparison.OrdinalIgnoreCase))
                    return PLUSINI.read_ini(tempPath, "Language", value, defaultvalue);
                return PLUSINI.read_ini(tempPath, "Language", value, read_language("english",value,defaultvalue));
            }
            finally
            {
                if (File.Exists(tempPath))
                    File.Delete(tempPath);
            }
        }
        public static string read_language_metadata(string lang, string value)
        {
            if (!language_exist(lang))
            {
                return "NOT FOUND METADATA";
            }
            using Stream resourceStream = Assembly.GetManifestResourceStream(Languages[lang])
                ?? throw new FileNotFoundException($"Embedded language resource not found: {lang}");

            string tempPath = Path.GetTempFileName();
            try
            {
                using (var fileStream = new FileStream(tempPath, FileMode.Create, FileAccess.Write, FileShare.None))
                {
                    resourceStream.CopyTo(fileStream);
                }

                return PLUSINI.read_ini(tempPath, "Metadata", value, "");
            }
            finally
            {
                if (File.Exists(tempPath))
                    File.Delete(tempPath);
            }
        }
    }
}