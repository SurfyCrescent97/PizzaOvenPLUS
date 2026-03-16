using Microsoft.Win32;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace PizzaOven
{
    public static class RegistryConfig
    {
        public static bool InstallGBHandler()
        {
            string AppPath = $"{Global.assemblyLocation}{Global.s}PizzaOven.exe";
            string protocolName = $"pizzaovenplus";
            try
            {
                var reg = Registry.CurrentUser.CreateSubKey(@"Software\Classes\PizzaOvenPLUS");
                reg.SetValue("", $"URL:{protocolName}");
                reg.SetValue("URL Protocol", "");
                reg = reg.CreateSubKey(@"shell\open\command");
                reg.SetValue("", $"\"{AppPath}\" -download \"%1\"");
                reg.Close();
                return true;
            }
            catch
            {
                return false;
            }
        }
        public static async Task<bool> InstallPairHandler(string secretkey, string memberID)
        {
            ModDownloader.RemoteInstallPairPolling();
            var requestUrl = $"https://api.gamebanana.com/Core/Item/Data?itemtype=Member&itemid={memberID}&fields=name";
            try
            {
                using (var httpClient = new HttpClient())
                {
                    var responseMessage = await httpClient.GetAsync(requestUrl);
                    var responseString = await responseMessage.Content.ReadAsStringAsync();
                }
            }
            catch (HttpRequestException ex)
            {
                var errormsg = "";
                switch (Regex.Match(ex.Message, @"\d+").Value)
                {
                    case "443":
                        errormsg = "Your internet connection is down.";
                        break;
                    case "500":
                    case "503":
                    case "504":
                        errormsg = "GameBanana's servers are down.";
                        break;
                    default:
                        errormsg = ex.Message;
                        break;
                }
                MessageBox.Show($"{errormsg}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false; 
            }

            try
            { 
                var memberName = "";
                var failurepoint = 0;
                try
                {
                    using (var httpClient = new HttpClient())
                    {
                        failurepoint = 1;
                        var response = await httpClient.GetAsync(requestUrl);

                        var jsonString = await response.Content.ReadAsStringAsync();

                        var data = JsonSerializer.Deserialize<List<string>>(jsonString);

                        memberName = data[0];
                    }
                    failurepoint = 0;
                    var reg = Registry.CurrentUser.OpenSubKey(@"Software\Classes\PizzaOvenPLUS", true);

                    reg.SetValue("secretkey", secretkey.ToString(), RegistryValueKind.String);
                    reg.SetValue("memberid", memberID.ToString(), RegistryValueKind.String);

                    reg.Close();

                    ModDownloader.RemoteInstallPairPolling();
                    MessageBox.Show($"Successfully paired with GameBanana: {memberName}", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                catch
                {
                    if (failurepoint == 1)
                        MessageBox.Show($"Member Not Found", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    else
                        MessageBox.Show($"Failed to pair with GameBanana", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return false;
                }
                
                return true;
            }
            catch
            {
                return false;
            }
        }
        public static string GetReg(string register)
        {
            using (var reg = Registry.CurrentUser.OpenSubKey(@"Software\Classes\PizzaOvenPLUS"))
            {
                return reg?.GetValue(register) as string ?? "";
            }
        }
    }
}
