/* later expanded
using Microsoft.Win32;
using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Forms;

public class NotifierConfig
{
    public DateTime? LastUpdateDate { get; set; }
    public bool Close { get; set; } = false;
}

static class Program
{
    [STAThread]
    static void Main()
    {
        string exePath = Environment.ProcessPath;

        using RegistryKey key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run", true);

        key.SetValue("PizzaOven+ Notifier", exePath);

        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);

        Application.Run(new TrayApplicationContext());
    }
}

class TrayApplicationContext : ApplicationContext
{
    private readonly NotifyIcon _tray;
    private readonly ContextMenuStrip _menu;

    private readonly string url = "https://api.gamebanana.com/Core/Item/Data?itemtype=Tool&itemid=21866&fields=Updates().bSubmissionHasUpdates(),Updates().aGetLatestUpdates()&return_keys=1";

    private readonly HttpClient _http = new HttpClient();

    private System.Windows.Forms.Timer _updateTimer;
    private System.Windows.Forms.Timer _shutdownTimer;

    private NotifierConfig config;

    public TrayApplicationContext()
    {
        _menu = new ContextMenuStrip();
        _menu.Items.Add("Stop Notifications", null, OnExit);
        config = LoadConfig();

        _tray = new NotifyIcon
        {
            Icon = new Icon("PizzaOvenPLUSIcon.ico"),
            Text = "PizzaOven+ Notifier",
            ContextMenuStrip = _menu,
            Visible = true
        };
        StartShutdownCheck();

        if (config.Close)
        {
            _tray.Visible = false;
            _tray.Dispose();
            _menu.Dispose();
            _http.Dispose();

            ExitThread();
            return;
        }


        _tray.ShowBalloonTip(2000, "PizzaOven+", "You’ll get update notifications even when the app is closed. You can disable this anytime by temporarily closing it in your tray icon or in PizzaOven+ App Settings.", ToolTipIcon.None);

        StartLoop();
       
    }

    private async void StartLoop()
    {
        while (true)
        {
            await CheckUpdates();
            await Task.Delay(5000);
        }
    }

    private void StartShutdownCheck()
    {
        _shutdownTimer = new System.Windows.Forms.Timer();
        _shutdownTimer.Interval = 50;

        _shutdownTimer.Tick += (s, e) =>
        {
            config = LoadConfig();

            if (config.Close)
            {
                _tray.Visible = false;
                _tray.Dispose();
                _menu.Dispose();
                _http.Dispose();

                ExitThread();
            }
        };

        _shutdownTimer.Start();
    }

    private async Task CheckUpdates()
    {
        try
        {
            var json = await _http.GetStringAsync(url);

            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            var updates = root
                .GetProperty("Updates().aGetLatestUpdates()")
                .EnumerateArray()
                .Select(u => new
                {
                    Title = u.GetProperty("_sTitle").GetString(),
                    Date = FromUnix(u.GetProperty("_tsDateAdded").GetInt64())
                })
                .OrderByDescending(x => x.Date)
                .FirstOrDefault();

            if (updates == null)
                return;

            config = LoadConfig();

            if (updates.Date > config.LastUpdateDate || config.LastUpdateDate == null)
            {
                _tray.ShowBalloonTip(3000, "PizzaOven+ Update", $"New update: {updates.Title}", ToolTipIcon.None);

                config.LastUpdateDate = updates.Date;
                SaveConfig(config);
            }
        }
        catch { }
    }

    private static DateTime FromUnix(long unix)
    {
        return DateTimeOffset.FromUnixTimeSeconds(unix).DateTime;
    }

    private string ConfigPath => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "PizzaOvenPlus", "notifierconfig.json");

    private void SaveConfig(NotifierConfig config)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(ConfigPath)!);

        File.WriteAllText(ConfigPath, JsonSerializer.Serialize(config, new JsonSerializerOptions
        {
            WriteIndented = true
        }));
    }

    private NotifierConfig LoadConfig()
    {
        try
        {
            if (!File.Exists(ConfigPath))
                return new NotifierConfig();

            return JsonSerializer.Deserialize<NotifierConfig>(File.ReadAllText(ConfigPath)) ?? new NotifierConfig();
        } catch { return new NotifierConfig(); }
    }

    private void OnExit(object sender, EventArgs e)
    {
        config.Close = true;
        SaveConfig(config);

        _tray.Visible = false;
        _tray.Dispose();
        _menu.Dispose();

        ExitThread();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _tray?.Dispose();
            _menu?.Dispose();
            _http?.Dispose();
            _shutdownTimer?.Dispose();
        }

        base.Dispose(disposing);
    }
}
*/