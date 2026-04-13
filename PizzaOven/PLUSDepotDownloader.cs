using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

namespace PizzaOven
{
    public class PLUSDepotDownloader
    {
        public static string GetSteamUsername()
        {
            using var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Valve\Steam");
            if (key?.GetValue("AutoLoginUser") is string value && !string.IsNullOrEmpty(value))
                return value;
            return "";
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
