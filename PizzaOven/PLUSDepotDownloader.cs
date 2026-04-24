using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Text.Json;
using Microsoft.Win32;

namespace PizzaOven
{
    public class PTversion
    {
        public string manifestID { get; set; }
        public string version { get; set; }
        public string type { get; set; }
    }

    public class PLUSDepotDownloader
    {
        public static string GetSteamUsername()
        {
            using var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Valve\Steam");
            if (key?.GetValue("AutoLoginUser") is string value && !string.IsNullOrEmpty(value))
                return value;
            return "";
        }
        public static async Task DowngradeDownload(MainWindow mainWindow)
        {
            string ogWinFile = "";
            var ogWinFileDialog = new OpenFileDialog();
            ogWinFileDialog.Filter = "Source (*.win)|*.win";


            if (ogWinFileDialog.ShowDialog() == true)
            {
                ogWinFile = ogWinFileDialog.FileName;
            }
            else
            {
                MessageBox.Show("No file selected.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            if (string.IsNullOrEmpty(ogWinFile))
            {
                System.Windows.Forms.MessageBox.Show("Please select a .win file first.", "Error", System.Windows.Forms.MessageBoxButtons.OK, System.Windows.Forms.MessageBoxIcon.Error);
                return;
            }

            var ptVersions = JsonSerializer.Deserialize<List<PTversion>>(File.ReadAllText($@"Dependencies{Global.s}ptversions.json"));

            string selectedVersion = mainWindow.DowngradeDownloadCombo.SelectedItem as string;
            if (string.IsNullOrEmpty(selectedVersion))
            {
                System.Windows.Forms.MessageBox.Show("Please select a version.", "Error", System.Windows.Forms.MessageBoxButtons.OK, System.Windows.Forms.MessageBoxIcon.Error);
                return;
            }

            string versionsDir = Path.Combine(Global.assemblyLocation, "Downgrades");
            string tempDir = Path.Combine(versionsDir, "temp");
            Directory.CreateDirectory(versionsDir);
            Directory.CreateDirectory(tempDir);

            string steamUser = PLUSDepotDownloader.GetSteamUsername();
            foreach (var v in ptVersions)
            {
                if (v.version != selectedVersion)
                    continue;

                if (v.type == "depot")
                {
                    bool success = await DownloadDowngradeAsync("2231450", "2231451", v.manifestID, steamUser, tempDir, ogWinFile, v.version);

                    if (!success)
                    {
                        Console.WriteLine($"Failed to process version {v.version}");
                        continue;
                    }

                    try
                    {
                        string sourceFile = Path.Combine(tempDir, "data.win");
                        string destFile = Path.Combine(versionsDir, $"{v.version}.win");
                        if (File.Exists(sourceFile))
                            File.Move(sourceFile, destFile, true);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error moving file for version {v.version}: {ex.Message}");
                    }

                    Console.WriteLine($"Version {v.version} processed successfully.");
                    break;
                }
            }

            try
            {
                if (Directory.Exists(tempDir))
                    Directory.Delete(tempDir, true);
            }
            catch { }
        }
        public static void CreatePatch(string sourceFile, string targetFile, string patchFile, string xdelta)
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = xdelta,
                Arguments = $@"-e -s ""{sourceFile}"" ""{targetFile}"" ""{patchFile}""",
                WorkingDirectory = Path.GetDirectoryName(xdelta),
                UseShellExecute = false,
                CreateNoWindow = true,
                WindowStyle = ProcessWindowStyle.Hidden
            };

            using var process = new Process { StartInfo = startInfo };
            process.Start();
            process.WaitForExit();
        }

        public static async Task<bool> DownloadDowngradeAsync(string appID, string depotID, string manifestID, string username, string outputDir, string ogWinFile, string version)
        {
            string depotDownloaderPath = $@"{Global.assemblyLocation}{Global.s}Dependencies{Global.s}DepotDownloader{Global.s}DepotDownloader.exe";
            if (!File.Exists(depotDownloaderPath))
            {
                System.Windows.Forms.MessageBox.Show($"{depotDownloaderPath} not found.", "Error", System.Windows.Forms.MessageBoxButtons.OK, System.Windows.Forms.MessageBoxIcon.Error);
                return false;
            }

            string args = $@"-app {appID} -depot {depotID} -manifest {manifestID} -remember-password -username ""{username}"" -dir ""{outputDir}""";

            var startInfo = new ProcessStartInfo
            {
                FileName = depotDownloaderPath,
                Arguments = args,
                WorkingDirectory = Path.GetDirectoryName(depotDownloaderPath),
                UseShellExecute = true,
                CreateNoWindow = false,
                WindowStyle = ProcessWindowStyle.Normal
            };

            try
            {
                using var process = new Process { StartInfo = startInfo };
                process.Start();
                await process.WaitForExitAsync();

                if (process.ExitCode != 0)
                {
                    System.Windows.Forms.MessageBox.Show($"Could not download depot {manifestID}", "Error", System.Windows.Forms.MessageBoxButtons.OK, System.Windows.Forms.MessageBoxIcon.Error);
                    return false;
                }

                string tempSource = Path.Combine(outputDir, "source.win");
                string tempTarget = Path.Combine(outputDir, "data.win");
                string patchFile = Path.Combine(Global.assemblyLocation, "Downgrades", $"{version}.xdelta");
                string xdeltaPath = Path.Combine(Global.assemblyLocation, "Dependencies", "xdelta.exe");

                File.Copy(ogWinFile, tempSource, true);

                if (File.Exists(tempTarget))
                {
                    CreatePatch(tempSource, tempTarget, patchFile, xdeltaPath);
                    File.Delete(tempTarget);
                }
                else Console.WriteLine($"Warning: {tempTarget} not found.");

                try
                {
                    string tempDepotDir = $@"{outputDir}.DepotDownloader{Global.s}";
                    if (Directory.Exists(tempDepotDir))
                        Directory.Delete(tempDepotDir, true);
                }
                catch { }

                return true;
            }
            catch (Exception ex)
            {
                System.Windows.Forms.MessageBox.Show($"Error running DepotDownloader:\n{ex.Message}", "Error", System.Windows.Forms.MessageBoxButtons.OK, System.Windows.Forms.MessageBoxIcon.Error);
                return false;
            }
        }
    }
}
