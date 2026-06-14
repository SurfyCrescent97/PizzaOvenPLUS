using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Text.Json;
using Microsoft.Win32;
using System.Diagnostics.Eventing.Reader;

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
            var isbase = System.Windows.Forms.MessageBox.Show("Please ensure your data.win.po(most likely true) or if that doesn't exist your data.win is base Pizza Tower\n\nPress yes if you are sure\nPress no if you are unsure or no (this will open you to choose the correct one)", "Confirmation", System.Windows.Forms.MessageBoxButtons.YesNo, System.Windows.Forms.MessageBoxIcon.Question);
            string ogWinFile = "";


            if (isbase == System.Windows.Forms.DialogResult.Yes)
            {
                if (File.Exists($@"{Global.config.ModsFolder}{Global.s}data.win.po"))
                {
                    ogWinFile = $@"{Global.config.ModsFolder}{Global.s}data.win.po";
                }
                else if (File.Exists($@"{Global.config.ModsFolder}{Global.s}data.win"))
                {
                    ogWinFile = $@"{Global.config.ModsFolder}{Global.s}data.win";
                }
                else
                {
                    MessageBox.Show("No data.win or data.win.po file found in the application directory try again but press no.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
            }
            else
            {
                var ogWinFileDialog = new OpenFileDialog();
                ogWinFileDialog.Filter = "Source (*.win)|*.win";

                if (File.Exists($@"{Global.config.ModsFolder}"))
                {
                    ogWinFileDialog.InitialDirectory = $@"{Global.config.ModsFolder}";
                }


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
            }

            var ptVersions = JsonSerializer.Deserialize<List<PTversion>>(File.ReadAllText($@"{Global.assemblyLocation}{Global.s}Dependencies{Global.s}ptversions.json"));

            string selectedVersion = mainWindow.DowngradeDownloadCombo.SelectedItem as string;
            if (string.IsNullOrEmpty(selectedVersion))
            {
                System.Windows.Forms.MessageBox.Show("Please select a version.", "Error", System.Windows.Forms.MessageBoxButtons.OK, System.Windows.Forms.MessageBoxIcon.Error);
                return;
            }

            string versionsDir = $@"{Global.assemblyLocation}{Global.s}Downgrades";
            string tempDir = $@"{Global.assemblyLocation}{Global.s}Downgrades{Global.s}temp";
            Directory.CreateDirectory(versionsDir);
            Directory.CreateDirectory(tempDir);

            string steamUser = GetSteamUsername();
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

                string tempSource = $"{outputDir}{Global.s}source.win";
                string tempTarget = $"{outputDir}{Global.s}data.win";
                string patchFile = $"{Global.assemblyLocation}{Global.s}Downgrades{Global.s}{version}.xdelta";
                string xdeltaPath = $"{Global.assemblyLocation}{Global.s}Dependencies{Global.s}xdelta.exe";

                File.Copy(ogWinFile, tempSource, true);

                if (File.Exists(tempTarget))
                {
                    ModLoader.CreatePatch(tempSource, tempTarget, patchFile, xdeltaPath);
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

                System.Windows.Forms.MessageBox.Show($"Version {version} downloaded! Select and Use Patch Version when patching to use it!", "Success", System.Windows.Forms.MessageBoxButtons.OK, System.Windows.Forms.MessageBoxIcon.Information);

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
