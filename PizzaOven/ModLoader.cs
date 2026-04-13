using DiscordRPC;
using SharpCompress.Compressors.Xz;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography.X509Certificates;
using System.Security.Cryptography.Xml;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Markup;
using System.Windows.Media;


namespace PizzaOven
{
    // Copy over mod files in order of ModList
    public static class ModLoader
    {
        private static string version = null;
        // Restore all backups created from previous build
        public static bool Restart()
        {
            // Restore all backups
            RestoreDirectory(Global.config.ModsFolder);
            // Delete all banks that aren't vanilla
            var banks = new List<string> (new string[] { "master.bank", "master.strings.bank", "music.bank", "sfx.bank" });
            foreach (var file in Directory.GetFiles($"{Global.config.ModsFolder}{Global.s}sound{Global.s}Desktop", "*", SearchOption.AllDirectories))
                if (!banks.Contains(Path.GetFileName(file).ToLowerInvariant()))
                    try {
                        File.Delete(file);
                    }
                    catch (Exception e)
                    {
                        if (e is System.UnauthorizedAccessException)
                            Global.logger.WriteLine($"Access denied when trying to delete {file}. Try reinstalling Pizza Tower to a folder you have access to or running Pizza Oven+ in administrator mode", LoggerType.Error);
                        else
                            throw;
                        return false;
                    }
            // Delete all dlls that aren't vanilla
            var dlls = new List<string>(new string[] { "fmod.dll", "fmod-gamemaker.dll", "fmodstudio.dll", "gameframe_x64.dll", "steam_api.dll",
            "steam_api64.dll", "steamworks_x64.dll"});
            // Also delete mp4 files
            foreach (var file in Directory.GetFiles($"{Global.config.ModsFolder}", "*", SearchOption.TopDirectoryOnly))
                if ((Path.GetExtension(file).ToLowerInvariant() == ".dll" && !dlls.Contains(Path.GetFileName(file).ToLowerInvariant()))
                    || Path.GetExtension(file).ToLowerInvariant() == ".mp4")
                        try {
                            File.Delete(file);
                        }
                        catch (Exception e)
                        {
                            if (e is System.UnauthorizedAccessException)
                                Global.logger.WriteLine($"Access denied when trying to delete {file}. Try reinstalling Pizza Tower to a folder you have access to or running Pizza Oven+ in administrator mode", LoggerType.Error);
                            else
                                throw;
                            return false;
                        }
            // Delete empty folders
            foreach (var directory in Directory.GetDirectories($"{Global.config.ModsFolder}{Global.s}sound{Global.s}Desktop"))
                    try {
                        if (Directory.GetFiles(directory).Length == 0 && Directory.GetDirectories(directory).Length == 0)
                            Directory.Delete(directory, false);
                    }
                    catch (Exception e)
                    {
                        if (e is System.UnauthorizedAccessException)
                            Global.logger.WriteLine($"Access denied when trying to delete {directory}. Try reinstalling Pizza Tower to a folder you have access to or running Pizza Oven+ in administrator mode", LoggerType.Error);
                        else
                            throw;
                        return false;
                    }
            // Delete .win from older version of Pizza Oven
            if (File.Exists($"{Global.config.ModsFolder}{Global.s}PizzaOven.win"))
                try {
                    File.Delete($"{Global.config.ModsFolder}{Global.s}PizzaOven.win");
                }
                catch (Exception e)
                {
                    if (e is System.UnauthorizedAccessException)
                        Global.logger.WriteLine($"Access denied when trying to delete {Global.config.ModsFolder}{Global.s}PizzaOven.win. Try reinstalling Pizza Tower to a folder you have access to or running Pizza Oven in administrator mode", LoggerType.Error);
                    else
                        throw;
                    return false;
                }
            return true;
        }
		
		
		public static bool Downgrade(string path)
        {
			var failed = true;
			Global.logger.WriteLine($"Attempting to patch {Path.GetFileName(path)} with data.win...", LoggerType.Info);
			try
            {
				var xdelta = $"{Global.assemblyLocation}{Global.s}Dependencies{Global.s}xdelta.exe";
				var source = $"{Global.config.ModsFolder}{Global.s}data.win";
				var downgradefile = path;
				File.Copy(source, $"{source}.downgradepo", true);
				if (!File.Exists($"{source}.po"))
					File.Copy(source, $"{source}.po", true);
				Patch(source,downgradefile,$"{source}.temp",xdelta);
				File.Move($"{source}.temp", source, true);				
			}
			catch (Exception e)
            {
				failed = false;
				
			}
			return failed;
		}

      

        private static string AFOMfilepath()
        {
            var modsfolder = $@"{Global.assemblyLocation}{Global.s}Mods";
            const string AFOMHomepage = "https://gamebanana.com/mods/466970";

            if (!Directory.Exists(modsfolder))
                return "";

            foreach (var modDir in Directory.GetDirectories(modsfolder))
            {
                var modJsonPath = Path.Combine(modDir, "mod.json");

                if (!File.Exists(modJsonPath))
                    continue;

                try
                {
                    string json = File.ReadAllText(modJsonPath);
                    using JsonDocument doc = JsonDocument.Parse(json);

                    if (doc.RootElement.TryGetProperty("homepage", out var homepage) &&
                        homepage.GetString() == AFOMHomepage)
                    {
                        return modDir; 
                    }
                }
                catch
                {
                    continue;
                }
            }

            return ""; 
        }


        public static bool BuildAFOM(string mod)
        {
            if (AFOMfilepath() == "")
            {
                Global.logger.WriteLine($"You must have AFOM installed to access this", LoggerType.Error);
                return false;
            }
            else
            {
                string sourceDir = "";
                foreach (var dir in Directory.GetDirectories(mod, "*", SearchOption.AllDirectories))
                {
                    if (Directory.Exists(Path.Combine(dir, "levels")))
                    {
                        sourceDir = dir;
                        break;
                    }
                }

                if (sourceDir == "")
                    return false;
                string towersPath = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "PizzaTower_GM2", "towers");

                if (!Directory.Exists(towersPath))
                    return false;

                string modName = Path.GetFileName(mod);
                string baseName = $"{modName}";
                string destDir = Path.Combine(towersPath, baseName);

                var result = MessageBox.Show($"The folder \"{Path.GetFileName(destDir)}\" already exists.\n\nReplace it?", "AFOM Tower already exists in your folder", MessageBoxButton.YesNo, MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    Directory.Delete(destDir, true);
                }
                else
                {
                    var index = 0;
                    while (Directory.Exists(destDir))
                    {
                        index++;
                        destDir = Path.Combine(towersPath, $"{baseName} ({index})");
                    }
                }

                Directory.CreateDirectory(destDir);

                foreach (var dir in Directory.GetDirectories(sourceDir, "*", SearchOption.AllDirectories))
                {
                    Directory.CreateDirectory(dir.Replace(sourceDir, destDir));
                }

                foreach (var file in Directory.GetFiles(sourceDir, "*", SearchOption.AllDirectories))
                {
                    File.Copy(file, file.Replace(sourceDir, destDir), true);
                }
                Global.logger.WriteLine($"Moved AFOM files successfully", LoggerType.Info);
                if (!Build(AFOMfilepath()))
                {
                    Global.logger.WriteLine($"Failed to build AFOM...", LoggerType.Error);
                    return false;
                }
            }
            return true;
        }

        public static void RevertGMLoader(List<string> copiedFiles, Dictionary<string, string> movedFiles, string tempRoot)
        {
            foreach (var file in copiedFiles)
            {
                try
                {
                    if (File.Exists(file))
                        File.Delete(file);
                    else if (Directory.Exists(file))
                        Directory.Delete(file, true);
                }
                catch { }
            }

            foreach (var pair in movedFiles)
            {
                try
                {
                    string originalPath = pair.Key;
                    string backupPath = pair.Value;
                    if (File.Exists(backupPath))
                    {
                        Directory.CreateDirectory(Path.GetDirectoryName(originalPath)!);
                        File.Move(backupPath, originalPath, true);
                    }
                    else if (Directory.Exists(backupPath))
                    {
                        Directory.CreateDirectory(Path.GetDirectoryName(originalPath)!);
                        Directory.Move(backupPath, originalPath);
                    }
                }
                catch { }
            }

            try { if (Directory.Exists(tempRoot)) Directory.Delete(tempRoot, true); } catch { }
        }

        public static void CloneDirectory(string sourceDir, string targetDir)
        {
            Directory.CreateDirectory(targetDir);

            foreach (string file in Directory.GetFiles(sourceDir))
            {
                string fileName = Path.GetFileName(file);
                string destFile = Path.Combine(targetDir, fileName);
                File.Copy(file, destFile, true); 
            }

            foreach (string dir in Directory.GetDirectories(sourceDir))
            {
                string dirName = Path.GetFileName(dir);
                string destDir = Path.Combine(targetDir, dirName);
                CloneDirectory(dir, destDir);
            }
        }
        public static bool BuildGMLoader(string mod)
        {
            string sourceFolder = $"{Global.assemblyLocation}{Global.s}GMLoader";
            string destinationFolder = $"{Global.config.ModsFolder}{Global.s}";

            List<string> copiedFiles = new List<string>();
            Dictionary<string, string> movedFiles = new Dictionary<string, string>();

            string tempRoot = Path.Combine(destinationFolder, "__gmloader_backup__");
            if (Directory.Exists(tempRoot))
            {
                string parentDir = Directory.GetParent(tempRoot)!.FullName;

                foreach (var entry in Directory.EnumerateFileSystemEntries(tempRoot))
                {
                    string destPath = Path.Combine(parentDir, Path.GetFileName(entry));

                    if (Directory.Exists(entry))
                        Directory.Move(entry, destPath);
                    else
                        File.Move(entry, destPath, true);
                }

                Directory.Delete(tempRoot, true);
            }

            try
            {
                var folders = Directory.GetDirectories(sourceFolder, "*", SearchOption.AllDirectories);
                var files = Directory.GetFiles(sourceFolder, "*", SearchOption.AllDirectories);

                Directory.CreateDirectory(tempRoot);

                foreach (var folder in folders)
                {
                    string relativeFolder = Path.GetRelativePath(sourceFolder, folder);
                    string destinationFolderPath = Path.Combine(destinationFolder, relativeFolder);

                    if (!Directory.Exists(destinationFolderPath))
                    {
                        Directory.CreateDirectory(destinationFolderPath);
                        copiedFiles.Add(destinationFolderPath);
                    }
                }

                foreach (var file in files)
                {
                    try
                    {
                        string relativePath = Path.GetRelativePath(sourceFolder, file);
                        string destinationPath = Path.Combine(destinationFolder, relativePath);
                        Directory.CreateDirectory(Path.GetDirectoryName(destinationPath)!);

                        if (File.Exists(destinationPath))
                        {
                            string backupPath = Path.Combine(tempRoot, relativePath);
                            Directory.CreateDirectory(Path.GetDirectoryName(backupPath)!);
                            File.Move(destinationPath, backupPath, true);
                            movedFiles[destinationPath] = backupPath;
                        }
                        else
                        {
                            copiedFiles.Add(destinationPath);
                        }

                        File.Copy(file, destinationPath, true);
                        Global.logger.WriteLine($"[GMLoader] Copied: {relativePath}", LoggerType.Info);
                    }
                    catch (Exception exFile)
                    {
                        Global.logger.WriteLine($"[GMLoader] Error copying {file}: {exFile.Message}, reverting...", LoggerType.Warning);
                        RevertGMLoader(copiedFiles, movedFiles, tempRoot);
                        return false;
                    }
                }
                string modsReadPath = Path.Combine(mod);
                Global.logger.WriteLine($"[GMLoader] Searching for mod files in: {modsReadPath}", LoggerType.Info);     
                string[] foldersToCopy = new[] { "audio", "code", "config", "csx", "lib", "room", "shader", "textures", "xdelta" };

                string currentPath = modsReadPath;

                bool found = false;

                while (true)
                {
                    if (foldersToCopy.Any(f => Directory.Exists(Path.Combine(currentPath, f))))
                    {
                        found = true;
                        break; 
                    }

                    var subdirs = Directory.GetDirectories(currentPath);
                    if (subdirs.Length == 0)
                    {
                        break;
                    }

                    currentPath = subdirs[0];
                }

                string modsDestination = Path.Combine(destinationFolder, "mods");

                Directory.CreateDirectory(modsDestination);
                CloneDirectory(modsReadPath, modsDestination);

                if (!found)
                {
                    Global.logger.WriteLine($"No folders to copy were found in {modsReadPath}", LoggerType.Error);
                    return false;
                }

                Global.logger.WriteLine($"Found folders to copy in: {currentPath}", LoggerType.Info);
                

                string gmLoaderExe = Path.Combine(destinationFolder, "GMLoader.exe");

                if (!File.Exists(gmLoaderExe))
                {
                    Global.logger.WriteLine($"GMLoader.exe not found at {gmLoaderExe}", LoggerType.Error);
                    return false;
                }

                ProcessStartInfo startInfo = new ProcessStartInfo
                {
                    FileName = gmLoaderExe,
                    WorkingDirectory = Path.GetDirectoryName(gmLoaderExe),
                    UseShellExecute = false,  
                    CreateNoWindow = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                };

                using (Process process = new Process())
                {
                    process.StartInfo = startInfo;

                    process.OutputDataReceived += (sender, args) =>
                    {
                        if (args.Data == "Press any key to close...")
                            process.Kill();
                        else if (!string.IsNullOrEmpty(args.Data))
                            Global.logger.WriteLine($"[GMLoader] {args.Data}", LoggerType.Info);
                    };

                    process.ErrorDataReceived += (sender, args) =>
                    {
                        if (!string.IsNullOrEmpty(args.Data))
                            Global.logger.WriteLine($"[GMLoader ERROR] {args.Data}", LoggerType.Error);
                    };



                    process.Start();
                    process.BeginOutputReadLine();
                    process.BeginErrorReadLine();
                    process.WaitForExit();
                }


                RevertGMLoader(copiedFiles, movedFiles, tempRoot);
                return true;
            }
            catch (Exception ex)
            {
                Global.logger.WriteLine($"[GMLoader] Fatal error: {ex.Message}, reverting changes...", LoggerType.Error);
                RevertGMLoader(copiedFiles, movedFiles, tempRoot);
                return false;
            }
        }


        public static bool Build(string mod)
        {
            var langapply = PLUSSavesystem.read_ini("Files", "POLanguage", "true") == "true";
            var errors = 0;
            var successes = 0;
            var FilesToPatch = Directory.GetFiles($"{Global.config.ModsFolder}{Global.s}sound{Global.s}Desktop").ToList();
            FilesToPatch.Insert(0, $"{Global.config.ModsFolder}{Global.s}data.win");
            FilesToPatch.Insert(1, $"{Global.config.ModsFolder}{Global.s}PizzaTower.exe");
            var xdelta = $"{Global.assemblyLocation}{Global.s}Dependencies{Global.s}xdelta.exe";
            if (!File.Exists(xdelta))
            {

                Global.logger.WriteLine($"{xdelta} is not found. Please try redownloading Pizza Oven+", LoggerType.Error);
                return false;
            }

            foreach (var modFile in Directory.GetFiles(mod, "*", SearchOption.AllDirectories))
            {
                var extension = Path.GetExtension(modFile);
                try
                {
                    // xdelta patches
                    if (extension.Equals(".xdelta", StringComparison.InvariantCultureIgnoreCase))
                    {
                        // Attempt to checksum each xdelta patch
                        WindowChecksum(modFile, xdelta);
                        var success = false;
                        var gotAccessDeniedError = false;
                        foreach (var file in FilesToPatch)
                        {
                            if (!File.Exists(file))
                            {
                                Global.logger.WriteLine($"{file} does not exist", LoggerType.Error);
                                continue;
                            }
                            try
                            {
                                // Attempt to patch file
                                Global.logger.WriteLine($"Attempting to patch {Path.GetFileName(file)} with {Path.GetFileName(modFile)}...", LoggerType.Info);
                                Patch(file, modFile, $"{Path.GetDirectoryName(file)}{Global.s}temp", xdelta);
                                // Only make backup if it doesn't already exist
                                if (!File.Exists($"{file}.po"))
                                    File.Copy(file, $"{file}.po", true);
                                File.Move($"{Path.GetDirectoryName(file)}{Global.s}temp", file, true);
                                Global.logger.WriteLine($"Applied {Path.GetFileName(modFile)} to {Path.GetFileName(file)}.", LoggerType.Info);
                                successes++;
                                if (Path.GetFileName(modFile).ToLowerInvariant().Contains("yyc") && File.Exists($"{Global.config.ModsFolder}{Global.s}Steamworks_x64.dll"))
                                    File.Move($"{Global.config.ModsFolder}{Global.s}Steamworks_x64.dll", $"{Global.config.ModsFolder}{Global.s}Steamworks_x64.dll.po", true);
                            }
                            catch 
                            {
                                try
                                {
                                    // Attempt to patch file with temp
                                    PathFixPatch(file, modFile, $"{Path.GetDirectoryName(file)}{Global.s}temp", xdelta);
                                    File.Move($"{Path.GetDirectoryName(file)}{Global.s}temp", file, true);
                                    Global.logger.WriteLine($"Applied {Path.GetFileName(modFile)} to {Path.GetFileName(file)}.", LoggerType.Info);
                                    successes++;
                                    if (Path.GetFileName(modFile).ToLowerInvariant().Contains("yyc") && File.Exists($"{Global.config.ModsFolder}{Global.s}Steamworks_x64.dll"))
                                        File.Move($"{Global.config.ModsFolder}{Global.s}Steamworks_x64.dll", $"{Global.config.ModsFolder}{Global.s}Steamworks_x64.dll.po", true);
                                }
                                catch (Exception e)
                                {
                                    if (e is System.UnauthorizedAccessException) {
                                        Global.logger.WriteLine($"Access denied when trying to patch {Path.GetFileName(file)} with {Path.GetFileName(modFile)}", LoggerType.Warning);
                                        gotAccessDeniedError = true;
                                        break;
                                    }
                                    Global.logger.WriteLine($"Unable to patch {Path.GetFileName(file)} with {Path.GetFileName(modFile)}", LoggerType.Warning);
                                    continue;
                                }
                            }
                            // Stop trying to patch if it was successful
                            success = true;
                            break;
                        }
                        if (!success)
                        {
                            if (gotAccessDeniedError)
                            {
                                Global.logger.WriteLine($"{Path.GetFileName(modFile)} got an access denied error while patch a file. Try reinstalling Pizza Tower to a folder you have access to or running Pizza Oven+ in administrator mode", LoggerType.Error);
                            }
                            else
                            {
                                Global.logger.WriteLine($"{Path.GetFileName(modFile)} wasn't able to patch any file. Ensure that either the mod or your game version is up to date. {Path.GetFileName(modFile)} is intended for {version}. " +
                                    $"If this version number matches with your current game version go to {Global.config.ModsFolder} and delete data.win.po and anything else with a .po extension(or use the provided clean PO. files button) then verify integrity of game files and try again.", LoggerType.Error);
                            }
                            errors++;
                        }
                    }
                    // Language .txt files
                    else if (extension.Equals(".txt", StringComparison.InvariantCultureIgnoreCase))
                    {
                        // Verify .txt file is for language
                        var basename = Path.GetFileNameWithoutExtension(modFile);
                        if (File.ReadAllText(modFile).Contains("lang = ", StringComparison.InvariantCultureIgnoreCase))
                        {
                            // Copy over file to lang folder

                            var file = $"{Global.config.ModsFolder}{Global.s}lang{Global.s}{Path.GetFileName(modFile)}";
                            if (langapply)
                            {
                                if (File.Exists(file))
                                {
                                    File.Copy(file, $"{file}.po", true);
                                }
                                else
                                {
                                    File.WriteAllText($"{file}.custompo", string.Empty);
                                }
                            }

                            File.Copy(modFile, $"{Global.config.ModsFolder}{Global.s}lang{Global.s}{Path.GetFileName(modFile)}", true);
                            Global.logger.WriteLine($"Copied over {Path.GetFileName(modFile)} to language folder", LoggerType.Info);
                            successes++;
                        }
                        //checks if contains the words credits 
                        else if (basename.Contains("credits", StringComparison.InvariantCultureIgnoreCase))
                        {
                            // Copy over file to game folder
                            var file = $"{Global.config.ModsFolder}{Global.s}{Path.GetFileName(modFile)}";

                            if (File.Exists(file))
                            {
                                File.Copy(file, $"{file}.po", true);
                            }
                            else
                            {
                                File.WriteAllText($"{file}.custompo", string.Empty);
                            }

                            File.Copy(modFile, $"{Global.config.ModsFolder}{Global.s}{Path.GetFileName(modFile)}", true);
                            Global.logger.WriteLine($"Copied over {Path.GetFileName(modFile)} to game folder since it seems to be a credits file", LoggerType.Info);
                            successes++;
                        }
                    }

                    // Font .png files [PIZZA OVEN ORIGINAL CODE]
                    /*
                    else if (extension.Equals(".png", StringComparison.InvariantCultureIgnoreCase))
                    {
                        // Check if png is in fonts folder
                        if (modFile.Contains("fonts", StringComparison.InvariantCultureIgnoreCase))
                        {
                            // Create fonts folder
                            Directory.CreateDirectory($"{Global.config.ModsFolder}{Global.s}lang{Global.s}fonts");
                            // Copy over file to fonts folder
                            File.Copy(modFile, $"{Global.config.ModsFolder}{Global.s}lang{Global.s}fonts{Global.s}{Path.GetFileName(modFile)}", true);
                            Global.logger.WriteLine($"Copied over {Path.GetFileName(modFile)} to fonts folder", LoggerType.Info);
                            successes++;
                        }
                    }
					*/

                    // Copy over .win file in case modder provides entire file instead of .xdelta patch
                    else if (extension.Equals(".win", StringComparison.InvariantCultureIgnoreCase))
                    {
                        var dataWin = $"{Global.config.ModsFolder}{Global.s}data.win";
                        // Only make backup if it doesn't already exist
                        if (!File.Exists($"{dataWin}.po"))
                            File.Copy(dataWin, $"{dataWin}.po", true);
                        File.Copy(modFile, dataWin, true);
                        Global.logger.WriteLine($"Copied over {Path.GetFileName(modFile)} to use instead of data.win", LoggerType.Info);
                        successes++;
                    }
                    // Copy over .bank file in case modder provides entire file instead of .xdelta patch
                    else if (extension.Equals(".bank", StringComparison.InvariantCultureIgnoreCase))
                    {
                        var FileToReplace = $"{Global.config.ModsFolder}{Global.s}sound{Global.s}Desktop{Global.s}{Path.GetFileName(modFile)}";
                        if (File.Exists(FileToReplace))
                        {
                            // Only make backup if it doesn't already exist
                            if (!File.Exists($"{FileToReplace}.po"))
                                File.Copy(FileToReplace, $"{FileToReplace}.po", true);
                            File.Copy(modFile, FileToReplace, true);
                            Global.logger.WriteLine($"Copied over {Path.GetFileName(modFile)} to use in sound folder", LoggerType.Info);
                        }
                        // Copy the file over if its not vanilla
                        else
                        {
                            var FileToAdd = $"{Global.config.ModsFolder}{Global.s}sound{Global.s}Desktop{Global.s}{Path.GetFileName(modFile)}";
                            // Add subdirectory name if it's not the same name as the mod folder

                            if (!string.Equals(Path.GetFileName(Path.GetDirectoryName(modFile)),"Desktop",StringComparison.InvariantCultureIgnoreCase))
                            {
                                if (!Path.GetFileName(Path.GetDirectoryName(modFile)).Equals(Path.GetFileName(mod), StringComparison.InvariantCultureIgnoreCase))
                                    FileToAdd = $"{Global.config.ModsFolder}{Global.s}sound{Global.s}Desktop{Global.s}{Path.GetFileName(Path.GetDirectoryName(modFile))}{Global.s}{Path.GetFileName(modFile)}"; 
                            }
                            Directory.CreateDirectory(Path.GetDirectoryName(FileToAdd));
                            File.Copy(modFile, FileToAdd, true);

                        }
                        successes++;
                    }
                    // Extension .dll files
                    else if (extension.Equals(".dll", StringComparison.InvariantCultureIgnoreCase))
                    {
                        // Copy over file to game folder
                        File.Copy(modFile, $"{Global.config.ModsFolder}{Global.s}{Path.GetFileName(modFile)}", true);
                        Global.logger.WriteLine($"Copied over {Path.GetFileName(modFile)} to game folder", LoggerType.Info);
                        successes++;
                    }
                    // Video Files
                    else if (extension.Equals(".mp4", StringComparison.InvariantCultureIgnoreCase))
                    {
                        // Copy over file to game folder
                        File.Copy(modFile, $"{Global.config.ModsFolder}{Global.s}{Path.GetFileName(modFile)}", true);
                        Global.logger.WriteLine($"Copied over {Path.GetFileName(modFile)} to game folder", LoggerType.Info);
                        successes++;
                    }
                }
                catch (Exception e)
                {
                    if (e is System.UnauthorizedAccessException)
                        Global.logger.WriteLine($"Access denied when trying to apply {Path.GetFileName(modFile)}. Try reinstalling Pizza Tower to a folder you have access to or running Pizza Oven+ in administrator mode", LoggerType.Error);
                    else
                        throw;
                }
            }
            var langfolder = $"{Global.config.ModsFolder}{Global.s}lang{Global.s}";
            var langFiles = Directory
                .GetFiles(langfolder, "*.txt", SearchOption.TopDirectoryOnly)
                .OrderBy(f => f) 
                .ToList();

            List<string> langlist = new();
            List<string> langlistfile = new();

            foreach (var file in langFiles)
            {
                string text = File.ReadAllText(file);

                Match match = Regex.Match(
                    text,
                    @"lang\s*=\s*""([^""]+)""",
                    RegexOptions.IgnoreCase
                );

                if (match.Success)
                {
                    langlist.Add(match.Groups[1].Value);
                    langlistfile.Add(Path.GetFileNameWithoutExtension(file));
                }
            }


            // PIZZAOVEN+ check 2
            foreach (var modFile in Directory.GetFiles(mod, "*", SearchOption.AllDirectories))
            {
                var extension = Path.GetExtension(modFile);
                var basename = Path.GetFileNameWithoutExtension(modFile);
                try
                {
                    if (extension.Equals(".ttf", StringComparison.InvariantCultureIgnoreCase) || extension.Equals(".otf", StringComparison.InvariantCultureIgnoreCase))
                    {
                        // Copy over file to fonts folder
                        var file = $"{Global.config.ModsFolder}{Global.s}lang{Global.s}fonts{Global.s}{Path.GetFileName(modFile)}";
                        if (langapply)
                        {
                            if (File.Exists(file))
                            {
                                File.Copy(file, $"{file}.po", true);
                            }
                            else
                            {
                                File.WriteAllText($"{file}.custompo", string.Empty);
                            }
                        }

                        File.Copy(modFile, $"{Global.config.ModsFolder}{Global.s}lang{Global.s}fonts{Global.s}{Path.GetFileName(modFile)}", true);
                        Global.logger.WriteLine($"Copied over {Path.GetFileName(modFile)} to fonts folder", LoggerType.Info);
                        successes++;
                    }
                    else if (extension.Equals(".def", StringComparison.InvariantCultureIgnoreCase))
                    {
                        // Copy over file to lang folder
                        var file = $"{Global.config.ModsFolder}{Global.s}lang{Global.s}{Path.GetFileName(modFile)}";
                        if (langapply)
                        {
                            if (File.Exists(file))
                            {
                                File.Copy(file, $"{file}.po", true);
                            }
                            else
                            {
                                File.WriteAllText($"{file}.custompo", string.Empty);
                            }
                        }

                        File.Copy(modFile, $"{Global.config.ModsFolder}{Global.s}lang{Global.s}{Path.GetFileName(modFile)}", true);
                        Global.logger.WriteLine($"Copied over {Path.GetFileName(modFile)} to language folder", LoggerType.Info);
                        successes++;
                    }
                    // wow updated png code!!
                    else if (extension.Equals(".png", StringComparison.InvariantCultureIgnoreCase))
                    {
                        basename = Regex.Replace(basename, @"^\d+", "");

                        string match = langlist
                            .FirstOrDefault(x => basename.StartsWith(x, StringComparison.OrdinalIgnoreCase));

                        if (match == null)
                        {
                            match = langlistfile
                                .FirstOrDefault(x => basename.StartsWith(x, StringComparison.OrdinalIgnoreCase));
                        }

                        if (match != null)
                        {
                            basename = match;
                        }
                        else
                        {
                            basename = Regex.Replace(basename, @"\d+$", "");
                        }

                        bool pngcopied = false;
                        List<string> fontList = new List<string>
                        {
                            "bigfont",
                            "captionfont",
                            "credits",
                            "tutorial"
                        };
                        if ((langlist.Contains(basename) || langlistfile.Contains(basename) || langlist.Any(x => x.StartsWith(basename, StringComparison.OrdinalIgnoreCase))) && !fontList.Any(x => x.StartsWith(basename, StringComparison.OrdinalIgnoreCase)))
                        {
                            // Copy over file to graphics folder
                            var file = $"{Global.config.ModsFolder}{Global.s}lang{Global.s}graphics{Global.s}{Path.GetFileName(modFile)}";
                            if (langapply)
                            {
                                if (File.Exists(file))
                                {
                                    File.Copy(file, $"{file}.po", true);
                                }
                                else
                                {
                                    File.WriteAllText($"{file}.custompo", string.Empty);
                                }
                            }

                            File.Copy(modFile, $"{Global.config.ModsFolder}{Global.s}lang{Global.s}graphics{Global.s}{Path.GetFileName(modFile)}", true);
                            Global.logger.WriteLine($"Copied over {Path.GetFileName(modFile)} to graphics folder", LoggerType.Info);
                            pngcopied = true;
                            successes++;
                        }
                        else
                        {
                            for (int i = 0; i < langlist.Count; i++)
                            {
                                if (!pngcopied && (fontList.Contains(basename) || basename.EndsWith($"_{langlist[i]}") || basename.EndsWith($"_{langlistfile[i]}")))
                                {
                                    // Copy over file to fonts folder
                                    var file = $"{Global.config.ModsFolder}{Global.s}lang{Global.s}fonts{Global.s}{Path.GetFileName(modFile)}";
                                    if (langapply)
                                    {
                                        if (File.Exists(file))
                                        {
                                            File.Copy(file, $"{file}.po", true);
                                        }
                                        else
                                        {
                                            File.WriteAllText($"{file}.custompo", string.Empty);
                                        }
                                    }

                                    File.Copy(modFile,$"{Global.config.ModsFolder}{Global.s}lang{Global.s}fonts{Global.s}{Path.GetFileName(modFile)}",true);
                                    Global.logger.WriteLine( $"Copied over {Path.GetFileName(modFile)} to fonts folder",LoggerType.Info);
                                    successes++;
                                    pngcopied = true;
                                    break; 
                                }
                            }

                        }
                        if (!pngcopied)
                        {
                            Global.logger.WriteLine($"Found {Path.GetFileName(modFile)} but doesn't seem to have an attached language file so skipping", LoggerType.Warning);
                        }
                    }
                    else if (extension.Equals(".json", StringComparison.InvariantCultureIgnoreCase))
                    {
                        if (langlist.Contains(basename) || langlistfile.Contains(basename))
                        {
                            // Copy over file to graphics folder
                            var file = $"{Global.config.ModsFolder}{Global.s}lang{Global.s}graphics{Global.s}{Path.GetFileName(modFile)}";
                            if (langapply)
                            {
                                if (File.Exists(file))
                                {
                                    File.Copy(file, $"{file}.po", true);
                                }
                                else
                                {
                                    File.WriteAllText($"{file}.custompo", string.Empty);
                                }
                            }
                            File.Copy(modFile, $"{Global.config.ModsFolder}{Global.s}lang{Global.s}graphics{Global.s}{Path.GetFileName(modFile)}", true);
                            Global.logger.WriteLine($"Copied over {Path.GetFileName(modFile)} to graphics folder", LoggerType.Info);
                            successes++;
                        }
                        else
                        {
                            Global.logger.WriteLine($"Found {Path.GetFileName(modFile)} but doesn't seem to have an attached language file so skipping", LoggerType.Warning);
                        }
                    }
                }
                catch (Exception e)
                {
                    if (e is System.UnauthorizedAccessException)
                        Global.logger.WriteLine($"Access denied when trying to apply {Path.GetFileName(modFile)}. Try reinstalling Pizza Tower to a folder you have access to or running Pizza Oven+ in administrator mode", LoggerType.Error);
                    else
                        throw;
                }
            }
            if (successes == 0)
                Global.logger.WriteLine($"No file was used from the current mod", LoggerType.Error);
			if (File.Exists($"{Global.config.ModsFolder}{Global.s}data.win.downgradepo"))
			{
                if (errors != 0 || successes < 0)
                {
                    File.Move($"{Global.config.ModsFolder}{Global.s}data.win.downgradepo", $"{Global.config.ModsFolder}{Global.s}data.win", true);
                    Global.logger.WriteLine("Undowngrading the patch", LoggerType.Warning);
                }
                else
                    File.Delete($"{Global.config.ModsFolder}{Global.s}data.win.downgradepo");
			}
            return errors == 0 && successes > 0;
        }

        public static void Patch(string file, string patch, string output, string xdelta)
        {
            ProcessStartInfo startInfo = new ProcessStartInfo();
            startInfo.CreateNoWindow = true;
            startInfo.UseShellExecute = false;
            startInfo.FileName = xdelta;
            startInfo.WindowStyle = ProcessWindowStyle.Hidden;
            startInfo.WorkingDirectory = Path.GetDirectoryName(xdelta);
            startInfo.Arguments = $@"-d -s ""{file}"" ""{patch}"" ""{output}""";
            using (Process process = new Process())
            {
                process.StartInfo = startInfo;
                process.Start();
                process.WaitForExit();
            }
        }
        public static void PathFixPatch(string file, string patch, string outputFileName, string xdelta)
        {
            string baseDir = Path.Combine(Path.GetDirectoryName(outputFileName)!, "PizzaOvenPlusPatching");
            string workingDir = baseDir;
            int count = 1;

            while (Directory.Exists(workingDir))
            {
                workingDir = baseDir + count;
                count++;
            }

            Directory.CreateDirectory(workingDir);

            try
            {
                string xdeltaName = Path.GetFileName(xdelta);
                string tempXdeltaPath = Path.Combine(workingDir, xdeltaName);
                File.Copy(xdelta, tempXdeltaPath, true);

                string tempFile = Path.Combine(workingDir, Path.GetFileName(file));
                string tempPatch = Path.Combine(workingDir, Path.GetFileName(patch));

                File.Copy(file, tempFile, true);
                File.Copy(patch, tempPatch, true);

                string tempOutput = Path.Combine(workingDir, Path.GetFileName(outputFileName));

                ProcessStartInfo startInfo = new ProcessStartInfo
                {
                    CreateNoWindow = true,
                    UseShellExecute = false,
                    FileName = tempXdeltaPath,
                    WorkingDirectory = workingDir,
                    WindowStyle = ProcessWindowStyle.Hidden,
                    Arguments = $@"-d -s ""{tempFile}"" ""{tempPatch}"" ""{tempOutput}"""
                };

                using (Process process = new Process())
                {
                    process.StartInfo = startInfo;
                    process.Start();
                    process.WaitForExit();
                }

                string finalPath = Path.Combine(Path.GetDirectoryName(outputFileName)!, Path.GetFileName(outputFileName));
                File.Copy(tempOutput, finalPath, true);
            }
            finally
            {
                if (Directory.Exists(workingDir))
                    Directory.Delete(workingDir, true);
            }
        }

        public static void RestoreDirectory(string path)
        {
            if (Directory.Exists(path))
            {
                foreach (var file in Directory.GetFiles(path, "*.po", SearchOption.AllDirectories)) {
                    try
                    {
                        File.Move(file, Path.ChangeExtension(file, String.Empty), true);
                    }
                    catch (Exception e)
                    {
                        if (e is System.UnauthorizedAccessException)
                            Global.logger.WriteLine($"Access denied when trying to restore {Path.GetFileName(file)}. Try reinstalling Pizza Tower to a folder you have access to or running Pizza Oven+ in administrator mode", LoggerType.Error);
                        else
                            throw;
                    }
                }
                foreach (var file in Directory.GetFiles(path, "*.downgradepo", SearchOption.AllDirectories))
                {
                    try
                    {
                        File.Delete(file);
                    }
                    catch (Exception e)
                    {
                        if (e is System.UnauthorizedAccessException)
                            Global.logger.WriteLine($"Access denied when trying to restore {Path.GetFileName(file)}. Try reinstalling Pizza Tower to a folder you have access to or running Pizza Oven+ in administrator mode", LoggerType.Error);
                        else
                            throw;
                    }
                }
                foreach (var file in Directory.GetFiles(path, "*.custompo", SearchOption.AllDirectories))
                {
                    try
                    {
                        var newPath = Path.ChangeExtension(file, null);
                        File.Move(file, newPath, true);
                        File.Delete(newPath);
                    }
                    catch (Exception e)
                    {
                        if (e is System.UnauthorizedAccessException)
                            Global.logger.WriteLine($"Access denied when trying to restore {Path.GetFileName(file)}. Try reinstalling Pizza Tower to a folder you have access to or running Pizza Oven+ in administrator mode", LoggerType.Error);
                        else
                            throw;
                    }
                }
            }
        }

        // xdelta print header
        private static void WindowChecksum(string patch, string xdelta)
        {
            int vcdiffCopyWindowLength = 0;
            ProcessStartInfo startInfo = new ProcessStartInfo();
            startInfo.CreateNoWindow = true;
            startInfo.UseShellExecute = false;
            startInfo.RedirectStandardOutput = true;
            startInfo.FileName = xdelta;
            startInfo.WindowStyle = ProcessWindowStyle.Hidden;
            startInfo.WorkingDirectory = Path.GetDirectoryName(xdelta);
            startInfo.Arguments = $@"printhdr ""{patch}""";

            // xdelta copy window length
            using (Process process = new Process())
            {
                process.StartInfo = startInfo;
                process.Start();

                // Find copy window length
                string line;
                while ((line = process.StandardOutput.ReadLine()) != null)
                {
                    if (line.Contains("VCDIFF copy window length:"))
                    {
                        // Write window length whole num
                        string[] header = line.Split(':');
                        if (header.Length >= 2 && int.TryParse(header[1].Trim(), out int length))
                        {
                            vcdiffCopyWindowLength = length;
                            Global.logger.WriteLine($"Checksum window length for {patch}: {vcdiffCopyWindowLength}", LoggerType.Info);
                        }
                        break;
                    }
                }

                process.WaitForExit();
            }

            try
            {
                // Read all in .txt file
                string[] checksumLines = null;
                using (Stream stream = Assembly.GetEntryAssembly().GetManifestResourceStream("PizzaOven.Dependencies.XDelta_Common_Checksum.txt"))
                using (StreamReader reader = new StreamReader(stream))
                {
                    checksumLines = EnumerateLines(reader).ToArray();
                }
                string prevLine = null;
                foreach (string checksumLine in checksumLines)
                {
                    // Checksum is specified length
                    if (!string.IsNullOrEmpty(checksumLine) && checksumLine.Length >= 8)
                    {
                        string checksumSubstring = checksumLine.Substring(0, 8);

                        if (int.TryParse(checksumSubstring, out int checksum))
                        {
                            // Compare .txt and window length checksum
                            if (checksum == vcdiffCopyWindowLength)
                            {
                                Global.logger.WriteLine($"Match found checksum: {vcdiffCopyWindowLength}", LoggerType.Info);
                                // Version txt above matching checksum
                                if (!string.IsNullOrEmpty(prevLine))
                                {
                                    version = prevLine;
                                    Global.logger.WriteLine($"Patch applies to Pizza Tower: {version}", LoggerType.Info);
                                }
                                return;
                            }
                        }
                    }
                    prevLine = checksumLine;
                }
            }
            catch (Exception ex)
            {
                Global.logger.WriteLine($"Error while checking checksum file, {ex.Message}", LoggerType.Error);
            }
            version = null;
        }

        public static async Task GMLoader_MergeMods(string[] modpaths, string mergePath)
        {
            Directory.CreateDirectory(mergePath);
            string[] GMLoaderFolder = { "audio", "code", "lib", "config", "csx", "room", "shader", "texture", "xdelta" };

            for (int i = 0; i < modpaths.Length; i++)
            {
                string modPath = modpaths[i];
                string foundPath = null;

                foreach (var folder in GMLoaderFolder)
                {
                    if (Directory.Exists(Path.Combine(modPath, folder)))
                    {
                        foundPath = modPath;
                        break;
                    }
                }

                if (foundPath == null)
                {
                    var subdirs = Directory.GetDirectories(modPath, "*", SearchOption.AllDirectories);
                    foreach (var subdir in subdirs)
                    {
                        foreach (var folder in GMLoaderFolder)
                        {
                            if (Directory.Exists(Path.Combine(subdir, folder)))
                            {
                                foundPath = subdir;
                                break;
                            }
                        }
                        if (foundPath != null)
                            break;
                    }
                }

                modpaths[i] = foundPath;
            }

            modpaths = modpaths.Where(path => path != null).ToArray();

            Dictionary<string, List<string>> fileMap = new Dictionary<string, List<string>>();

            for (int i = 0; i < modpaths.Length; i++)
            {
                string modRoot = modpaths[i];

                var allFiles = Directory.GetFiles(modRoot, "*.*", SearchOption.AllDirectories);

                foreach (var file in allFiles)
                {
                    string relativePath = Path.GetRelativePath(modRoot, file);

                    if (!fileMap.ContainsKey(relativePath))
                        fileMap[relativePath] = new List<string>();

                    fileMap[relativePath].Add(file);
                }
            }

            foreach (var kvp in fileMap)
            {
                string relativePath = kvp.Key;
                List<string> paths = kvp.Value;

                string chosenPath;

                if (paths.Count > 1)
                {
                    chosenPath = AskConflictResolution(relativePath, paths);
                }
                else
                {
                    chosenPath = paths[0];
                }

                string destinationFile = Path.Combine(mergePath, relativePath);
                Directory.CreateDirectory(Path.GetDirectoryName(destinationFile));
                File.Copy(chosenPath, destinationFile, overwrite: true);
            }
        }

        public static string AskConflictResolution(string fileName, List<string> modPaths)
        {
            string selected = null;

            Window win = new Window
            {
                Title = "Conflict Detected",
                Width = 400,
                Height = 150,
                WindowStartupLocation = WindowStartupLocation.CenterScreen,
                Background = new SolidColorBrush(PLUSThemes.Get_BrushColor("PrimaryBrush"))
            };

            StackPanel panel = new StackPanel { Margin = new Thickness(10) };

            panel.Children.Add(new TextBlock
            {
                Text = $"Select mod to use for:\n{fileName}",
                Margin = new Thickness(0, 0, 0, 10),
                Foreground = Brushes.White
            });

            ComboBox combo = new ComboBox
            {
                ItemsSource = modPaths,
                SelectedIndex = 0,
                Background = new SolidColorBrush(PLUSThemes.Get_BrushColor("SecondaryBrush"))
            };
            panel.Children.Add(combo);


            System.Windows.Controls.Button ok = new System.Windows.Controls.Button
            {
                Content = "OK",
                Width = 80,
                HorizontalAlignment = HorizontalAlignment.Right,
                Margin = new Thickness(0, 10, 0, 0)
            };
            ok.Click += (s, e) =>
            {
                selected = combo.SelectedItem as string;
                win.Close();
            };
            panel.Children.Add(ok);

            win.Content = panel;
            win.ShowDialog();

            return selected;
        }


        private static IEnumerable<string> EnumerateLines(TextReader reader)
        {
            string line;
            while ((line = reader.ReadLine()) != null)
            {
                yield return line;
            }
        }
    }
}
