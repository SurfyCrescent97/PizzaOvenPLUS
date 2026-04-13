using Microsoft.Win32;
using Newtonsoft.Json.Linq;
using PizzaOven.UI;
using SharpCompress.Archives.SevenZip;
using SharpCompress.Common;
using SharpCompress.Readers;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.Eventing.Reader;
using System.DirectoryServices.ActiveDirectory;
using System.IO;
using System.Linq;
using System.Media;
using System.Net.Http;
using System.Reflection;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Media.Media3D;
using System.Windows.Shapes;
using System.Windows.Threading;
using System.Xml.Linq;
using static System.Collections.Specialized.BitVector32;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.Window;
using Path = System.IO.Path;


namespace PizzaOven
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    /// 
    public partial class MainWindow : Window
    {
        public class PTversion
        {
            public string manifestID { get; set; }
            public string version { get; set; }
            public string type { get; set; }
        }

        public static readonly string[] themebrushes = { "Primary", "Secondary", "Inner", "Loading", "Text" };
        public static readonly string[] themeimageExtensions = { ".png", ".jpg", ".jpeg", ".bmp" };

        class CreditItem
        {
            public string Name { get; set; }
            public string SmallText { get; set; }
        }

        Dictionary<string, List<CreditItem>> credits = new Dictionary<string, List<CreditItem>>()
        {
            {
                "Orginal PizzaOven",
                new List<CreditItem>
                {
                    new CreditItem { Name = "Tekka", SmallText = "All of the original PizzaOven Code and owner of PizzaOven" },
                    new CreditItem { Name = "Keku", SmallText = "Original PizzaOven Logo" },
                    new CreditItem { Name = "ARandomGuy231", SmallText = "Named the original PizzaOven" },
                    new CreditItem { Name = "Tom", SmallText = "Original Request and Owner of Gamebanana" },
                    new CreditItem { Name = "C1229", SmallText = "PizzaOven's Misc. Pull Requests" }
                }
            },
            {
                "PizzaOven+",
                new List<CreditItem>
                {
                    new CreditItem { Name = "SurfyCrescent97", SmallText = "Main Programmer and made PizzaOven+" },
                    new CreditItem { Name = "Mak n' Sauce", SmallText = "All Assets that aren't PizzaOven" },
                    new CreditItem { Name = "noodlecup", SmallText = "Made Tutorial Theme" },
                    new CreditItem { Name = "Joel Eastwood", SmallText = "Made Ronnie's Jingle" }
                }
            },
            {
                "Others",
                new List<CreditItem>
                {
                    new CreditItem { Name = "EmeraldMan", SmallText = "AFOM" },
                    new CreditItem { Name = "C1229", SmallText = "Depots List" },
                    new CreditItem { Name = "Senjay", SmallText = "GMLoader" },
                }
            }
        };
        public PLUSRonnieAnimate tutorialanimator;
        public PLUSRonnieAnimate introanimator;
        private PLUSRonnieAnimate launchanimator;
        private PLUSRonnieAnimate replayanimator;
        private PLUSRonnieAnimate settinganimator;
        public string version;
        public static string PizzaTowerVersion = "1.1.280";
        private Dictionary<string, string> defaultBrushHexes = new Dictionary<string, string>();
        // Separated from Global.config so that order is updated when datagrid is modified
        public List<string> exes;
        private FileSystemWatcher ModsWatcher;
        private List<FileSystemWatcher> PLUSWatchers = new List<FileSystemWatcher>();
        private MediaPlayer backgroundPlayer;
        private FlowDocument defaultFlow = new FlowDocument();
        private string defaultText = "No mod is currently selected. Pressing launch will start a vanilla Pizza Tower. \n\nyou can also go the PLUS' Settings to play on the older verisons that PLUS provides (if you wish you can even put your own downgrade patch in Downgrades folder.)\n\n" +
            "Start downloading and using mods in the Browse Mods tab on top. Only one mod can be selected at a time.";
        private string _currentFilter;

        public string[] transparentboxes = {
            "Logger",
            "ModDescription",
            "ModGrid"
        };

        public string CurrentFilter
        {
            get => _currentFilter;
            set
            {
                if (_currentFilter == value)
                    return;

                _currentFilter = value;
                UpdatePLUSfilter(_currentFilter);
            }
        }

        public MainWindow()
        {
            InitializeComponent();
            // Get Version Number
            var PizzaOvenVersion = Assembly.GetExecutingAssembly().GetName().Version.ToString();
            version = PizzaOvenVersion.Substring(0, PizzaOvenVersion.LastIndexOf('.'));

            var GMLoaderMergePath = $@"{Global.assemblyLocation}{Global.s}GMLoaderMergeTemp";
            if (Directory.Exists(GMLoaderMergePath))
                Directory.Delete(GMLoaderMergePath, true);

            foreach (var (sectionName, items) in credits)
            {
                var sectionText = new TextBlock
                {
                    Text = sectionName,
                    FontSize = 18,
                    FontWeight = FontWeights.Bold,
                    Margin = new Thickness(0, 10, 0, 5)
                };
                sectionText.SetResourceReference(TextBlock.ForegroundProperty, "TextBrush");

                CreditsPanel.Children.Add(sectionText);

                foreach (var item in items)
                {
                    var nameText = new TextBlock
                    {
                        Text = item.Name,
                        FontSize = 14
                    };
                    nameText.SetResourceReference(TextBlock.ForegroundProperty, "TextBrush");

                    var smallText = new TextBlock
                    {
                        Text = item.SmallText,
                        FontSize = 12,
                        Opacity = 0.7,
                        Margin = new Thickness(0, 0, 0, 5)
                    };
                    smallText.SetResourceReference(TextBlock.ForegroundProperty, "TextBrush");

                    CreditsPanel.Children.Add(nameText);
                    CreditsPanel.Children.Add(smallText);
                }
            }
            CurrentFilter = "";
            ApplyTransparentBoxes(true);
            Global.ronnietutorial = PLUSSavesystem.read_ini("Tutorial", "Finished", "false") != "true";
            try
            {
                if (PLUSSavesystem.read_ini("Init", "AssetsVer", "-1") != PizzaOvenVersion)
                {
                    string appDataPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "PizzaOvenPLUS", "CustomAssets");

                    Directory.CreateDirectory(appDataPath);

                    Assembly assembly = Assembly.GetExecutingAssembly();

                    string assetPrefix = "PizzaOven.CustomAssets.";

                    var resources = assembly
                        .GetManifestResourceNames()
                        .Where(r => r.StartsWith(assetPrefix));

                    foreach (string resourceName in resources)
                    {
                        string relativePath = resourceName
                            .Substring(assetPrefix.Length)
                            .Replace('.', Path.DirectorySeparatorChar);

                        int lastSeparator = relativePath.LastIndexOf(Path.DirectorySeparatorChar);
                        if (lastSeparator != -1)
                        {
                            relativePath =
                                relativePath[..lastSeparator] + "." +
                                relativePath[(lastSeparator + 1)..];
                        }

                        string outputPath = Path.Combine(appDataPath, relativePath);
                        Directory.CreateDirectory(Path.GetDirectoryName(outputPath)!);

                        if (File.Exists(outputPath))
                            continue;

                        using Stream resourceStream =
                            assembly.GetManifestResourceStream(resourceName)!;

                        using FileStream fileStream =
                            new FileStream(outputPath, FileMode.Create, FileAccess.Write);

                        resourceStream.CopyTo(fileStream);
                        PLUSMUSIC.InitializeAsync();
                    }
                    PLUSSavesystem.write_ini("Init", "AssetsVer", PizzaOvenVersion);
                }
                PLUSMUSIC.InitializeAsync();
                InitializeToggles();

                SoundVolume.Value = double.TryParse(PLUSSavesystem.read_ini("Audio", "SoundVolume", "100"), out double value) ? value : 100;
                PLUSMUSIC.ApplyCurrentVolume();
            }
            catch
            {
            }
            Global.logger = new Logger(ConsoleWindow);
            Global.config = new();

            PLUSrefresh();
            PLUSWatcher();

            Global.logger.WriteLine($"Launched PizzaOven+ Mod Manager v{version}!", LoggerType.Info);
            // Get Global.config if it exists
            if (File.Exists($@"{Global.assemblyLocation}{Global.s}Config.json"))
            {
                try
                {
                    var configString = File.ReadAllText($@"{Global.assemblyLocation}{Global.s}Config.json");
                    Global.config = JsonSerializer.Deserialize<Config>(configString);
                }
                catch (Exception e)
                {
                    Global.logger.WriteLine(e.Message, LoggerType.Error);
                }
            }

            // Last saved windows settings
            if (Global.config.Height != null && Global.config.Height >= MinHeight)
                Height = (double)Global.config.Height;
            if (Global.config.Width != null && Global.config.Width >= MinWidth)
                Width = (double)Global.config.Width;
            if (Global.config.Maximized)
                WindowState = WindowState.Maximized;
            if (Global.config.TopGridHeight != null)
                MainGrid.RowDefinitions[1].Height = new GridLength((double)Global.config.TopGridHeight, GridUnitType.Star);
            if (Global.config.BottomGridHeight != null)
                MainGrid.RowDefinitions[3].Height = new GridLength((double)Global.config.BottomGridHeight, GridUnitType.Star);
            if (Global.config.LeftGridWidth != null)
                MiddleGrid.ColumnDefinitions[0].Width = new GridLength((double)Global.config.LeftGridWidth, GridUnitType.Star);
            if (Global.config.RightGridWidth != null)
                MiddleGrid.ColumnDefinitions[2].Width = new GridLength((double)Global.config.RightGridWidth, GridUnitType.Star);

            if (!Directory.Exists($@"{Global.config.ModsFolder}"))
            {
                Global.config.ModsFolder = null;
            }

            if (Global.config.ModList == null)
                Global.config.ModList = new();

            Global.ModList = Global.config.ModList;

            for (int i = 0; i < Global.config.ModList.Count; i++)
            {
                Global.ModList[i].GMLoader = PLUSModType($@"{Global.assemblyLocation}{Global.s}Mods{Global.s}{Global.config.ModList[i].name}") == "GMLOADER";
            }

            Directory.CreateDirectory($@"{Global.assemblyLocation}{Global.s}Mods");

            // Watch mods folder to detect
            ModsWatcher = new FileSystemWatcher($@"{Global.assemblyLocation}{Global.s}Mods");
            ModsWatcher.Created += OnModified;
            ModsWatcher.Deleted += OnModified;
            ModsWatcher.Renamed += OnModified;

            Refresh();
            SelectItem();

            ModsWatcher.EnableRaisingEvents = true;

            defaultFlow.Blocks.Add(ConvertToFlowParagraph(defaultText));
            DescriptionWindow.Document = defaultFlow;
            var bitmap = new BitmapImage(new Uri("pack://application:,,,/PizzaOven;component/Assets/PizzaOvenPreview.png"));
            Preview.Source = bitmap;
            PreviewBG.Source = null;

            if (PLUSSavesystem.read_ini("LowEnd", "ModUpdate", "true") == "true")
            {
                Global.logger.WriteLine("Checking for updates...", LoggerType.Info);
                ModGrid.IsEnabled = false;
                ConfigButton.IsEnabled = false;
                LaunchButton.IsEnabled = false;
                ClearButton.IsEnabled = false;
                UpdateButton.IsEnabled = false;
                ModGridSearchButton.IsEnabled = false;
                App.Current.Dispatcher.Invoke(() =>
                {
                    ModUpdater.CheckForUpdates($"{Global.assemblyLocation}{Global.s}Mods", this);
                });
            }

            if (Global.config.ModsFolder == null)
            {
                // Setup on launch if not setup yet
                if (Setup.GameSetup())
                    LaunchButton.IsEnabled = true;
                else
                {
                    LaunchButton.IsEnabled = false;
                    Global.logger.WriteLine("Please click Setup before starting!", LoggerType.Warning);
                }
            }

            var announcementWindow = new PLUSAnnouncementWindow();
            announcementWindow.ShowDialog();
            ModDownloader.RemoteInstallPairPolling();

            if (Global.ronnietutorial)
            {
                PLUSTutorial.RunTutorial(this);
            }
            else
            {
                //hi to the people looking at the code!!! this how we check if you still have it lol
                TutorialButton.Visibility = Visibility.Visible;
                PLUSTutorial.RonnieVariables.KeptMod = File.Exists(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "PizzaTower_GM2", "RonnieTutorial.ini"));
                PLUSTutorial.RonnieVariables.ModDeleted = PLUSTutorial.TutorialModPath() == "";
                if (Directory.Exists(PLUSTutorial.TutorialModPath()))
                {
                    Directory.Delete(PLUSTutorial.TutorialModPath(), true);
                }
                if (!PLUSTutorial.RonnieVariables.ModDeleted || PLUSTutorial.RonnieVariables.KeptMod)
                {
                    introanimator = new PLUSRonnieAnimate();
                    introanimator.Initialize(this, this.Width / 2, 250, 1.5);

                    this.SizeChanged += (s, e) =>
                    {
                        try
                        {
                            if (introanimator != null && introanimator._overlayCanvas != null)
                            {
                                introanimator._overlayCanvas.Width = this.ActualWidth;
                                introanimator._overlayCanvas.Height = this.ActualHeight;
                            }
                        }
                        catch { }
                    };
                    PLUSTutorial.RunIntro(this);
                }
                else if (introanimator != null)
                {
                    introanimator.Destroy();
                }
            }

        }

        private void WindowLoaded(object sender, RoutedEventArgs e)
        {

        }
        private void OnModified(object sender, FileSystemEventArgs e)
        {
            Refresh();
            Global.UpdateConfig();
            // Bring window to front after download is done
            App.Current.Dispatcher.Invoke((Action)delegate
            {
                Activate();
            });
        }
        private async void SelectItem()
        {

            await Task.Run(() =>
            {
                App.Current.Dispatcher.Invoke((Action)delegate
                {
                    var index = Global.ModList.ToList().FindIndex(mod => mod.enabled == true);
                    if (index != -1)
                    {
                        ModGrid.SelectedItem = ModGrid.Items[index];
                        ModGrid.ScrollIntoView(ModGrid.Items[index]);
                    }
                    else
                    {
                        ModGrid.SelectedIndex = -1;
                        ShowMetadata(null);
                    }
                });
            });
        }

        private async void Refresh()
        {
            var currentModDirectory = $@"{Global.assemblyLocation}{Global.s}Mods";
            // Add new folders found in Mods to the ModList
            var PLUSfolder = PLUSSavesystem.read_ini_section("Folder");
            string PLUScurrentfolder;

            try
            {
                PLUScurrentfolder = (string)ModFolderCombo.SelectedItem;
                if (string.IsNullOrEmpty(PLUScurrentfolder))
                    PLUScurrentfolder = "All";
            }
            catch
            {
                PLUScurrentfolder = "All";
            }

            var PLUSskiplist = new List<string>();

            if (PLUScurrentfolder != "All")
            {
                for (int i = 0; i < PLUSfolder.GetLength(0); i++)
                {
                    if (PLUSfolder[i, 1] != PLUScurrentfolder)
                    {
                        if (!PLUSskiplist.Contains(PLUSfolder[i, 0]))
                        {
                            PLUSskiplist.Add(PLUSfolder[i, 0]);
                        }
                    }
                }
                foreach (var mod in Directory.GetDirectories(currentModDirectory))
                {
                    Mod m = new Mod();
                    m.name = Path.GetFileName(mod);
                    if (PLUSSavesystem.read_ini("Folder", m.name, "All") == "All")
                    {
                        PLUSskiplist.Add(m.name);
                    }
                }
            }

            foreach (var mod in Directory.GetDirectories(currentModDirectory))
            {
                if (Global.ModList.ToList().Where(x => x.name == Path.GetFileName(mod)).Count() == 0)
                {
                    Mod m = new Mod();
                    m.name = Path.GetFileName(mod);
                    m.enabled = false;

                    Thread.Sleep(1000);
                    if (File.Exists($"{mod}{Global.s}mod.json"))
                    {
                        FlowDocument descFlow = new FlowDocument();
                        var metadataString = File.ReadAllText($"{mod}{Global.s}mod.json");
                        Metadata metadata = JsonSerializer.Deserialize<Metadata>(metadataString);
                        m.preview = metadata.preview;
                    }
                    else
                        m.preview = new Uri("pack://application:,,,/PizzaOven;component/Assets/PizzaOvenLogo.png");
                    m.GMLoader = PLUSModType(mod) == "GMLOADER";

                    App.Current.Dispatcher.Invoke((Action)delegate
                    {
                        Global.ModList.Add(m);
                    });

                    if (!Global.ronnietutorial)
                        Global.logger.WriteLine($"Added {Path.GetFileName(mod)}", LoggerType.Info);
                }
            }



            // Remove deleted folders that are still in the ModList AS WELL AS FOLDER FILTERS
            foreach (var mod in Global.ModList.ToList())
            {
                if (!Directory.GetDirectories(currentModDirectory).ToList().Select(x => Path.GetFileName(x)).Contains(mod.name))
                {
                    App.Current.Dispatcher.Invoke((Action)delegate
                    {
                        Global.ModList.Remove(mod);
                    });
                    if (!Global.ronnietutorial)
                        Global.logger.WriteLine($"Deleted {mod.name}", LoggerType.Info);
                    continue;
                }
                if (PLUSskiplist.Contains(mod.name))
                {
                    App.Current.Dispatcher.Invoke((Action)delegate
                    {
                        Global.ModList.Remove(mod);
                    });
                    continue;
                }
                if (Global.ronnietutorial && mod.name != "Ronnie Oven Mod")
                {
                    App.Current.Dispatcher.Invoke((Action)delegate
                    {
                        Global.ModList.Remove(mod);
                    });
                    continue;
                }
            }

            await Task.Run(() =>
            {
                App.Current.Dispatcher.Invoke((Action)delegate
                {
                    ModGrid.ItemsSource = Global.ModList;
                    if (ModGrid.Items.Count == 0 && !Global.ronnietutorial)
                        DropBox.Visibility = Visibility.Visible;
                    else
                        DropBox.Visibility = Visibility.Collapsed;
                    Stats.Text = $"{Global.ModList.Count} mods • {Directory.GetFiles($@"{Global.assemblyLocation}{Global.s}Mods", "*", SearchOption.AllDirectories).Length.ToString("N0")} files • " +
                    $"{StringConverters.FormatSize(new DirectoryInfo($@"{Global.assemblyLocation}{Global.s}Mods").GetDirectorySize())} • v{version}";
                });
            });
            Global.config.ModList = Global.ModList;
            Global.logger.WriteLine("Refreshed!", LoggerType.Info);
        }

        private async void Setup_Click(object sender, RoutedEventArgs e)
        {
            if (Global.ronnietutorial && !PLUSTutorial.RonnieVariables.SetupAllow)
            {
                Global.logger.WriteLine("Not yet...", LoggerType.Tutorial);
                return;
            }
            await Task.Run(() =>
            {
                if (Setup.GameSetup())
                {
                    Dispatcher.Invoke(() =>
                    {
                        LaunchButton.IsEnabled = true;
                    });
                    if (Global.ronnietutorial)
                        PLUSTutorial.RonnieVariables.SetupSucessful = 1;

                }
                else if (Global.ronnietutorial)
                {
                    PLUSTutorial.RonnieVariables.SetupSucessful = 0;
                }
            });
        }
        private async void Launch_Click(object sender, RoutedEventArgs e)
        {
            // Build Mod Loadout
            if (Global.ronnietutorial && !PLUSTutorial.RonnieVariables.LauncherAllow)
            {
                Global.logger.WriteLine("Not yet...", LoggerType.Tutorial);
                return;
            }
            try
            {
                if (Global.ronnietutorial && Global.config.ModList.Where(x => x.enabled).ToList()[0].name != "Ronnie Oven Mod")
                {
                    Global.logger.WriteLine("Select the Ronnie Mod", LoggerType.Tutorial);
                    return;
                }
            }
            catch
            {
                Global.logger.WriteLine("Select the Ronnie Mod", LoggerType.Tutorial);
                return;
            }
            if (!Global.ronnietutorial)
            {
                Settings.IsEnabled = false;
            }
            if (Global.config.ModsFolder != null)
            {
                if (!Global.ronnietutorial)
                {
                    if (launchanimator != null)
                        launchanimator.Destroy();
                    launchanimator = new PLUSRonnieAnimate();
                    launchanimator.Initialize(this, this.ActualWidth, this.ActualHeight - 200, 1.5);

                    launchanimator.MoveTo(this.ActualWidth - 200, this.ActualHeight - 200);
                    launchanimator.ShakeVisual(5, 2);
                }
                ModGrid.IsEnabled = false;
                ConfigButton.IsEnabled = false;
                LaunchButton.IsEnabled = false;
                ClearButton.IsEnabled = false;
                UpdateButton.IsEnabled = false;
                ModGridSearchButton.IsEnabled = false;
                Refresh();
                Directory.CreateDirectory(Global.config.ModsFolder);
                Global.logger.WriteLine($"Cooking mods for Pizza Tower", LoggerType.Info);
                if (!await Build(Global.config.ModsFolder, DowngradeCombo.SelectedItem as string))
                {
                    Global.logger.WriteLine($"Pizza Oven+ failed to cook the selected mod and will not launch the game", LoggerType.Error);
                    ModGrid.IsEnabled = true;
                    ConfigButton.IsEnabled = true;
                    LaunchButton.IsEnabled = true;
                    ClearButton.IsEnabled = true;
                    UpdateButton.IsEnabled = true;
                    ModGridSearchButton.IsEnabled = true;
                    if (!Global.ronnietutorial)
                    {
                        Settings.IsEnabled = true;
                        launchanimator.GlideTo(this.ActualWidth, this.ActualHeight - 200, 20);
                        launchanimator.SetExpression("sad");
                    }
                    else
                    {
                        PLUSTutorial.RonnieVariables.FailedPatch = true;
                        if (File.Exists($"{Global.config.ModsFolder}{Global.s}data.win.po"))
                        {
                            File.Delete($"{Global.config.ModsFolder}{Global.s}data.win.po");
                        }
                    }
                    return;
                }
                else
                {
                    if (!Global.ronnietutorial)
                    {
                        launchanimator.GlideTo(this.ActualWidth, this.ActualHeight - 200, 20);
                        launchanimator.SetExpression("happy");
                    }
                }
                ModGrid.IsEnabled = true;
                ConfigButton.IsEnabled = true;
                LaunchButton.IsEnabled = true;
                ClearButton.IsEnabled = true;
                UpdateButton.IsEnabled = true;
                ModGridSearchButton.IsEnabled = true;
                if (!Global.ronnietutorial)
                {
                    Settings.IsEnabled = true;
                }
            }
            else
            {
                Global.logger.WriteLine("Please click Setup before starting!", LoggerType.Warning);
                return;
            }
            List<string> GMLoaderMods = new List<string>();

            for (int i = 0; i < Global.ModList.Count; i++)
            {
                if (Global.ModList[i].GMLoader_enabled)
                {
                    string modName = Global.ModList[i].name;

                    GMLoaderMods.Add($@"{Global.assemblyLocation}{Global.s}Mods{Global.s}{modName}");

                    Global.ModList[i].GMLoader_enabled = false;
                    Refresh();
                }
            }

            if (GMLoaderMods.Count > 0)
            {
                var runningGMLoaders = Process.GetProcessesByName("GMLoader");
                foreach (var proc in runningGMLoaders)
                {
                    try
                    {
                        proc.Kill();
                        proc.WaitForExit();
                    }
                    catch
                    {

                    }
                };
                Global.logger.WriteLine($"[GMLoader] GMLoader Mods Selected", LoggerType.Info);
                var GMLoaderMergePath = $@"{Global.assemblyLocation}{Global.s}GMLoaderMergeTemp";
                if (Directory.Exists(GMLoaderMergePath))
                    Directory.Delete(GMLoaderMergePath, true);
                await ModLoader.GMLoader_MergeMods(GMLoaderMods.ToArray(), GMLoaderMergePath);
                ConfigButton.IsEnabled = false;
                LaunchButton.IsEnabled = false;
                ClearButton.IsEnabled = false;
                UpdateButton.IsEnabled = false;
                ModGridSearchButton.IsEnabled = false;
                await DoGMLoader(GMLoaderMergePath);
                if (Directory.Exists(GMLoaderMergePath))
                    Directory.Delete(GMLoaderMergePath, true);
                ModGrid.IsEnabled = true;
                ConfigButton.IsEnabled = true;
                LaunchButton.IsEnabled = true;
                ClearButton.IsEnabled = true;
                UpdateButton.IsEnabled = true;
                ModGridSearchButton.IsEnabled = true;
            }
            if (!Global.ronnietutorial)
            {
                Settings.IsEnabled = true;
            }
            // Launch game
            if (Global.config.Launcher != null && File.Exists(Global.config.Launcher))
            {
                var path = Global.config.Launcher;
                try
                {
                    Global.UpdateConfig();
                    var mods = Global.config.ModList.Where(x => x.enabled).ToList();
                    var modtype = "Normal";
                    try
                    {
                        modtype = PLUSModType($@"{Global.assemblyLocation}{Global.s}Mods{Global.s}{mods[0].name}");
                    }
                    catch
                    {
                        modtype = "Normal";
                    }


                    Global.logger.WriteLine($"Launching {path}", LoggerType.Info);
                    var ps_extra = "";


                    if (PLUSSavesystem.read_ini("Launch", "Debug", "true") == "true" && !Global.ronnietutorial)
                    {
                        ps_extra = "-debug";
                    }


                    if (PLUSSavesystem.read_ini("Launch", "Steam", "false") == "true")
                    {
                        ProcessStartInfo psi = new ProcessStartInfo
                        {
                            FileName = "steam://rungameid/2231450",
                            UseShellExecute = true
                        };

                        Process.Start(psi);
                    }
                    else
                    {
                        var ps = new ProcessStartInfo(path)
                        {
                            WorkingDirectory = Path.GetDirectoryName(path),
                            Arguments = ps_extra,
                            UseShellExecute = true,
                            Verb = "open"
                        };
                        Process.Start(ps);
                    }



                }
                catch (Exception ex)
                {
                    Global.logger.WriteLine($"Couldn't launch {path} ({ex.Message})", LoggerType.Error);
                }
            }
            else
                Global.logger.WriteLine($"Please click Setup before starting!", LoggerType.Warning);

        }
        private void GameBanana_Click(object sender, RoutedEventArgs e)
        {
            var id = "7692";
            try
            {
                var ps = new ProcessStartInfo($"https://gamebanana.com/games/{id}")
                {
                    UseShellExecute = true,
                    Verb = "open"
                };
                Process.Start(ps);
            }
            catch (Exception ex)
            {
                Global.logger.WriteLine($"Couldn't open up GameBanana ({ex.Message})", LoggerType.Error);
            }
        }
        private void ScrollToBottom(object sender, TextChangedEventArgs args)
        {
            ConsoleWindow.ScrollToEnd();
        }

        private void ModGrid_ContextMenuOpening(object sender, ContextMenuEventArgs e)
        {
            FrameworkElement element = sender as FrameworkElement;
            if (element == null)
            {
                return;
            }

            if (ModGrid.SelectedItem == null)
                element.ContextMenu.Visibility = Visibility.Collapsed;
            else
                element.ContextMenu.Visibility = Visibility.Visible;
        }

        private async void DeleteItem_Click(object sender, RoutedEventArgs e)
        {
            if (Global.ronnietutorial)
            {
                MessageBox.Show("Deleting is not available during the tutorial.", "Unavailable Feature", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }
            var selectedMods = ModGrid.SelectedItems;
            var temp = new Mod[selectedMods.Count];
            selectedMods.CopyTo(temp, 0);
            foreach (var row in temp)
                if (row != null)
                {
                    var dialogResult = MessageBox.Show($@"Are you sure you want to delete {row.name}?" + Environment.NewLine + "This cannot be undone.", $@"Deleting {row.name}: Confirmation", MessageBoxButton.YesNo, MessageBoxImage.Warning);
                    if (dialogResult == MessageBoxResult.Yes)
                    {
                        try
                        {
                            await Task.Run(() => Directory.Delete($@"{Global.assemblyLocation}{Global.s}Mods{Global.s}{row.name}", true));
                            ModFolderCombo.SelectedItem = "All";
                            Global.logger.WriteLine($@"Deleting {row.name}.", LoggerType.Info);
                            ShowMetadata(null);
                        }
                        catch (Exception ex)
                        {
                            Global.logger.WriteLine($@"Couldn't delete {row.name} ({ex.Message})", LoggerType.Error);
                        }
                    }
                }
        }
        private void FolderName_Click(object sender, RoutedEventArgs e)
        {
            var selectedMods = ModGrid.SelectedItems;
            var temp = new Mod[selectedMods.Count];
            selectedMods.CopyTo(temp, 0);

            ModsWatcher.EnableRaisingEvents = false;
            foreach (var row in temp)
            {
                if (row != null)
                {
                    var ew = new PLUSFolderwindow(row.name, true);
                    ew.ShowDialog();
                }
            }
            ModsWatcher.EnableRaisingEvents = true;

            Global.UpdateConfig();
            Refresh();
            PLUSrefresh();
            ModGrid.Items.Refresh();
        }
        private async Task<bool> DoGMLoader(string path)
        {
            return await Task.Run(() => ModLoader.BuildGMLoader(path));
        }
        private async Task<bool> Build(string path, string downgradename)
        {
            return await Task.Run(async () =>
            {
                if (!ModLoader.Restart())
                    return false;
                string patchPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Downgrades", downgradename + ".xdelta");

                var mods = Global.config.ModList.Where(x => x.enabled).ToList();
                string modType = "";
                try
                {
                    modType = PLUSModType($@"{Global.assemblyLocation}{Global.s}Mods{Global.s}{mods[0].name}");
                }
                catch
                {
                    modType = "Normal";
                }
                var modTypeNormalized = (modType ?? string.Empty).Trim();

                bool isAFOM = string.Equals(modTypeNormalized, "AFOM", StringComparison.OrdinalIgnoreCase);
                bool isGMLOADER = string.Equals(modTypeNormalized, "GMLOADER", StringComparison.OrdinalIgnoreCase);
                var prenoisepatch = new List<string>
                {
                    "1.0.311",
                    "1.0.595",
                };

                if (!isAFOM && !isGMLOADER)
                {
                    if (PizzaTowerVersion != downgradename)
                    {
                        if (prenoisepatch.Contains(downgradename))
                        {
                            MessageBox.Show($"The selected version {downgradename} is pre-noise update please be wary you will have to clear out your lang folder for the timebeing and only have it contain your english.txt");
                            var folderName = $@"{Global.config.ModsFolder}{Global.s}lang";
                            if (Directory.Exists(folderName))
                            {
                                try
                                {
                                    Process process = Process.Start("explorer.exe", folderName);
                                    Global.logger.WriteLine($@"Opened {folderName}.", LoggerType.Info);
                                }
                                catch (Exception ex)
                                {
                                    Global.logger.WriteLine($@"Couldn't open {folderName}. ({ex.Message})", LoggerType.Error);
                                }
                            }
                            MessageBox.Show($"Please clear the lang folder, safely keep it somewhere and only include english.txt and continue");
                        }
                        if (!ModLoader.Downgrade(patchPath))
                        {
                            Global.logger.WriteLine($"Failed to Downgrade to {downgradename}", LoggerType.Error);
                            return false;
                        }
                        else
                        {
                            Global.logger.WriteLine($"Sucessfully Downgraded to {downgradename}", LoggerType.Info);
                        }
                    }
                }

                if (mods.Count == 0)
                    return true;

                if (mods.Count == 1)
                {
                    if (isGMLOADER)
                        return true;
                    else if (isAFOM)
                        return ModLoader.BuildAFOM($@"{Global.assemblyLocation}{Global.s}Mods{Global.s}{mods[0].name}");
                    else
                        return ModLoader.Build($@"{Global.assemblyLocation}{Global.s}Mods{Global.s}{mods[0].name}");
                }
                else if (mods.Count == 0)
                    return true;
                else
                    return false;
            });
        }

        private void Window_Closing(object sender, CancelEventArgs e)
        {
            if (WindowState == WindowState.Maximized)
            {
                Global.config.Height = RestoreBounds.Height;
                Global.config.Width = RestoreBounds.Width;
                Global.config.Maximized = true;
            }
            else
            {
                Global.config.Height = Height;
                Global.config.Width = Width;
                Global.config.Maximized = false;
            }
            Global.config.TopGridHeight = MainGrid.RowDefinitions[1].Height.Value;
            Global.config.BottomGridHeight = MainGrid.RowDefinitions[3].Height.Value;
            Global.config.LeftGridWidth = MiddleGrid.ColumnDefinitions[0].Width.Value;
            Global.config.RightGridWidth = MiddleGrid.ColumnDefinitions[2].Width.Value;
            Global.UpdateConfig();
            try
            {
                if (backgroundPlayer != null)
                {
                    backgroundPlayer.Stop();
                    backgroundPlayer.Close();
                    backgroundPlayer = null;
                }
            }
            catch { }
            Application.Current.Shutdown();
        }

        private void OpenItem_Click(object sender, RoutedEventArgs e)
        {
            var selectedMods = ModGrid.SelectedItems;
            var temp = new Mod[selectedMods.Count];
            selectedMods.CopyTo(temp, 0);
            foreach (var row in temp)
                if (row != null)
                {
                    var folderName = $@"{Global.assemblyLocation}{Global.s}Mods{Global.s}{row.name}";
                    if (Directory.Exists(folderName))
                    {
                        try
                        {
                            Process process = Process.Start("explorer.exe", folderName);
                            Global.logger.WriteLine($@"Opened {folderName}.", LoggerType.Info);
                        }
                        catch (Exception ex)
                        {
                            Global.logger.WriteLine($@"Couldn't open {folderName}. ({ex.Message})", LoggerType.Error);
                        }
                    }
                }
        }
        private void EditItem_Click(object sender, RoutedEventArgs e)
        {
            if (Global.ronnietutorial)
            {
                MessageBox.Show("Renaming is not available during the tutorial.", "Unavailable Feature", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }
            var selectedMods = ModGrid.SelectedItems;
            var temp = new Mod[selectedMods.Count];
            selectedMods.CopyTo(temp, 0);

            // Stop refreshing while renaming folders
            ModsWatcher.EnableRaisingEvents = false;
            foreach (var row in temp)
                if (row != null)
                {
                    EditWindow ew = new EditWindow(row.name, true);
                    ew.ShowDialog();
                }
            ModsWatcher.EnableRaisingEvents = true;
            Global.UpdateConfig();
            ModGrid.Items.Refresh();
        }
        private void FetchItem_Click(object sender, RoutedEventArgs e)
        {
            var selectedMods = ModGrid.SelectedItems;
            var temp = new Mod[selectedMods.Count];
            selectedMods.CopyTo(temp, 0);
            ModsWatcher.EnableRaisingEvents = false;
            foreach (var row in temp)
                if (row != null)
                {
                    FetchWindow fw = new FetchWindow(row);
                    fw.ShowDialog();
                    if (fw.success)
                    {
                        ShowMetadata(row.name);
                        ModGrid.Items.Refresh();
                    }
                }
            ModsWatcher.EnableRaisingEvents = true;
        }
        private void Add_Enter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                e.Handled = true;
                e.Effects = DragDropEffects.Move;
                DropBox.Visibility = Visibility.Visible;
            }
        }
        private void Add_Leave(object sender, DragEventArgs e)
        {
            e.Handled = true;
            DropBox.Visibility = Visibility.Collapsed;
        }
        private async void Add_Drop(object sender, DragEventArgs e)
        {
            e.Handled = true;
            var ModsFolder = $"{Global.assemblyLocation}{Global.s}Mods";
            // Ensure that mods folder exists
            Directory.CreateDirectory(ModsFolder);
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                string[] fileList = (string[])e.Data.GetData(DataFormats.FileDrop, false);
                await Task.Run(() => ExtractPackages(fileList));
            }
            DropBox.Visibility = Visibility.Collapsed;
        }
        private void ExtractPackages(string[] fileList)
        {
            var temp = $"{Global.assemblyLocation}{Global.s}temp";
            var ModsFolder = $"{Global.assemblyLocation}{Global.s}Mods";
            foreach (var file in fileList)
            {
                Directory.CreateDirectory(temp);
                // Move folder
                if (Directory.Exists(file))
                {
                    string path = $@"{temp}{Global.s}{Path.GetFileName(file)}";
                    int index = 2;
                    while (Directory.Exists(path))
                    {
                        path = $@"{temp}{Global.s}{Path.GetFileName(file)} ({index})";
                        index += 1;
                    }
                    MoveDirectory(file, path);
                }
                // Extract zip
                else if (Path.GetExtension(file).ToLower() == ".7z" || Path.GetExtension(file).ToLower() == ".rar" || Path.GetExtension(file).ToLower() == ".zip")
                {
                    string _ArchiveSource = file;
                    string _ArchiveType = Path.GetExtension(file);
                    if (File.Exists(_ArchiveSource))
                    {
                        try
                        {
                            if (Path.GetExtension(_ArchiveSource).Equals(".7z", StringComparison.InvariantCultureIgnoreCase))
                            {
                                using (var archive = SevenZipArchive.Open(_ArchiveSource))
                                {
                                    var reader = archive.ExtractAllEntries();
                                    while (reader.MoveToNextEntry())
                                    {
                                        if (!reader.Entry.IsDirectory)
                                            reader.WriteEntryToDirectory($"{temp}{Global.s}{Path.GetFileNameWithoutExtension(file)}", new ExtractionOptions()
                                            {
                                                ExtractFullPath = true,
                                                Overwrite = true
                                            });
                                    }
                                }
                            }
                            else
                            {
                                using (Stream stream = File.OpenRead(_ArchiveSource))
                                using (var reader = ReaderFactory.Open(stream))
                                {
                                    while (reader.MoveToNextEntry())
                                    {
                                        if (!reader.Entry.IsDirectory)
                                        {
                                            reader.WriteEntryToDirectory($"{temp}{Global.s}{Path.GetFileNameWithoutExtension(file)}", new ExtractionOptions()
                                            {
                                                ExtractFullPath = true,
                                                Overwrite = true
                                            });
                                        }
                                    }
                                }
                            }
                        }
                        catch (Exception e)
                        {
                            MessageBox.Show($"Couldn't extract {file}: {e.Message}", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                        }
                        File.Delete(_ArchiveSource);
                    }
                }
                foreach (var folder in Directory.GetDirectories(temp, "*", SearchOption.TopDirectoryOnly))
                {
                    string path = $@"{ModsFolder}{Global.s}{Path.GetFileName(folder)}";
                    int index = 2;
                    while (Directory.Exists(path))
                    {
                        path = $@"{ModsFolder}{Global.s}{Path.GetFileName(folder)} ({index})";
                        index += 1;
                    }
                    MoveDirectory(folder, path);
                }
                if (Directory.Exists(temp))
                    Directory.Delete(temp, true);
            }
        }
        private static void MoveDirectory(string sourcePath, string targetPath)
        {
            //Copy all the files & Replaces any files with the same name
            foreach (var path in Directory.GetFiles(sourcePath, "*.*", SearchOption.AllDirectories))
            {
                var newPath = path.Replace(sourcePath, targetPath);
                Directory.CreateDirectory(Path.GetDirectoryName(newPath));
                File.Copy(path, newPath, true);
            }
        }
        private void Clear_Click(object sender, RoutedEventArgs e)
        {
            var temp = Global.ModList.ToList();
            temp.ForEach(mod => mod.enabled = false);
            Global.ModList = new ObservableCollection<Mod>(temp);
            ShowMetadata(null);
            Global.UpdateConfig();
            ModGrid.SelectedIndex = -1;
        }
        private void Update_Click(object sender, RoutedEventArgs e)
        {
            Global.logger.WriteLine("Checking for updates...", LoggerType.Info);
            ModGrid.IsEnabled = false;
            ConfigButton.IsEnabled = false;
            LaunchButton.IsEnabled = false;
            ClearButton.IsEnabled = false;
            UpdateButton.IsEnabled = false;
            ModGridSearchButton.IsEnabled = false;
            App.Current.Dispatcher.Invoke(() =>
            {
                ModUpdater.CheckForUpdates($"{Global.assemblyLocation}{Global.s}Mods", this);
            });
        }
        private Paragraph ConvertToFlowParagraph(string text)
        {
            var flowDocument = new FlowDocument();

            var regex = new Regex(@"(https?:\/\/[^\s]+)", RegexOptions.Compiled | RegexOptions.IgnoreCase);
            var matches = regex.Matches(text).Cast<Match>().Select(m => m.Value).ToList();

            var paragraph = new Paragraph();
            flowDocument.Blocks.Add(paragraph);


            foreach (var segment in regex.Split(text))
            {
                if (matches.Contains(segment))
                {
                    var hyperlink = new Hyperlink(new Run(segment))
                    {
                        NavigateUri = new Uri(segment),
                    };

                    hyperlink.RequestNavigate += (sender, args) =>
                    {
                        var ps = new ProcessStartInfo(segment)
                        {
                            UseShellExecute = true,
                            Verb = "open"
                        };
                        Process.Start(ps);
                    };

                    paragraph.Inlines.Add(hyperlink);
                }
                else
                {
                    paragraph.Inlines.Add(new Run(segment));
                }
            }

            return paragraph;
        }

        private void ShowMetadata(string mod)
        {
            if (mod == null || !File.Exists($"{Global.assemblyLocation}{Global.s}Mods{Global.s}{mod}{Global.s}mod.json"))
            {
                DescriptionWindow.Document = defaultFlow;
                var bitmap = new BitmapImage(new Uri("pack://application:,,,/PizzaOven;component/Assets/PizzaOvenPreview.png"));
                Preview.Source = bitmap;
                PreviewBG.Source = null;
            }
            else
            {
                FlowDocument descFlow = new FlowDocument();
                var metadataString = File.ReadAllText($"{Global.assemblyLocation}{Global.s}Mods{Global.s}{mod}{Global.s}mod.json");
                Metadata metadata = JsonSerializer.Deserialize<Metadata>(metadataString);

                var para = new Paragraph();
                if (metadata.submitter != null)
                {
                    para.Inlines.Add($"Submitter: ");
                    if (metadata.avi != null && metadata.avi.ToString().Length > 0)
                    {
                        BitmapImage bm = new BitmapImage(metadata.avi);
                        Image image = new Image();
                        image.Source = bm;
                        image.Height = 35;
                        para.Inlines.Add(image);
                        para.Inlines.Add(" ");
                    }
                    if (metadata.upic != null && metadata.upic.ToString().Length > 0)
                    {
                        BitmapImage bm = new BitmapImage(metadata.upic);
                        Image image = new Image();
                        image.Source = bm;
                        image.Height = 25;
                        para.Inlines.Add(image);
                    }
                    else
                        para.Inlines.Add(metadata.submitter);
                    descFlow.Blocks.Add(para);
                }
                if (metadata.preview != null)
                {
                    var bitmap = new BitmapImage();
                    bitmap.BeginInit();
                    bitmap.UriSource = metadata.preview;
                    bitmap.EndInit();
                    Preview.Source = bitmap;
                    PreviewBG.Source = bitmap;
                }
                else
                {
                    var bitmap = new BitmapImage(new Uri("pack://application:,,,/PizzaOven;component/Assets/PizzaOvenPreview.png"));
                    Preview.Source = bitmap;
                    PreviewBG.Source = null;
                }
                para = new Paragraph();
                para.Inlines.Add("Category: ");
                if (metadata.caticon != null && metadata.caticon.ToString().Length > 0)
                {
                    BitmapImage bm = new BitmapImage(metadata.caticon);
                    Image image = new Image();
                    image.Source = bm;
                    image.Width = 20;
                    para.Inlines.Add(image);
                }
                para.Inlines.Add($" {metadata.cat}");
                descFlow.Blocks.Add(para);
                var text = "";
                if (!String.IsNullOrEmpty(metadata.description))
                    text += $"Description: {metadata.description}\n\n";
                if (!String.IsNullOrEmpty(metadata.filedescription))
                    text += $"File Description: {metadata.filedescription}\n\n";
                if (metadata.homepage != null && metadata.homepage.ToString().Length > 0)
                    text += $"Home Page: {metadata.homepage}";
                var init = ConvertToFlowParagraph(text);
                descFlow.Blocks.Add(init);
                DescriptionWindow.Document = descFlow;
                var descriptionText = new TextRange(DescriptionWindow.Document.ContentStart, DescriptionWindow.Document.ContentEnd);
                descriptionText.ApplyPropertyValue(Inline.BaselineAlignmentProperty, BaselineAlignment.Center);
            }
        }
        private void ModGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            Mod mod = (Mod)ModGrid.SelectedItem;
            if (mod != null)
                ShowMetadata(mod.name);
            var temp = Global.ModList.ToList();
            temp.ForEach(mod => mod.enabled = false);
            Global.ModList = new ObservableCollection<Mod>(temp);
            if (ModGrid.SelectedIndex == -1)
                ShowMetadata(null);
            else
                Global.ModList[ModGrid.SelectedIndex].enabled = true;
            Global.config.ModList = Global.ModList;
            Global.UpdateConfig();
        }

        private void Download_Click(object sender, RoutedEventArgs e)
        {
            if (Global.ronnietutorial && !PLUSTutorial.RonnieVariables.AllowDownloadMod)
            {
                return;
            }
            Button button = sender as Button;
            var item = button.DataContext as GameBananaRecord;
            new ModDownloader().BrowserDownload("Pizza Tower", item);
        }
        private void AltDownload_Click(object sender, RoutedEventArgs e)
        {
            if (Global.ronnietutorial)
            {
                MessageBox.Show("Alternate download links are not available during the tutorial.", "Unavailable Feature", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }
            Button button = sender as Button;
            var item = button.DataContext as GameBananaRecord;
            new AltLinkWindow(item.AlternateFileSources, item.Title,
                "Pizza Tower",
                item.Link.AbsoluteUri).ShowDialog();
        }
        private void Homepage_Click(object sender, RoutedEventArgs e)
        {
            if (Global.ronnietutorial)
            {
                MessageBox.Show("Homepage is not available during the tutorial.", "Unavailable Feature", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }
            Button button = sender as Button;
            var item = button.DataContext as GameBananaRecord;
            try
            {
                var ps = new ProcessStartInfo(item.Link.ToString())
                {
                    UseShellExecute = true,
                    Verb = "open"
                };
                Process.Start(ps);
            }
            catch (Exception ex)
            {
                Global.logger.WriteLine($"Couldn't open up {item.Link} ({ex.Message})", LoggerType.Error);
            }
        }
        private int imageCounter;
        private int imageCount;
        private FlowDocument ConvertToFlowDocument(string text)
        {
            var flowDocument = new FlowDocument();

            var regex = new Regex(@"(https?:\/\/[^\s]+)", RegexOptions.Compiled | RegexOptions.IgnoreCase);
            var matches = regex.Matches(text).Cast<Match>().Select(m => m.Value).ToList();

            var paragraph = new Paragraph();
            flowDocument.Blocks.Add(paragraph);


            foreach (var segment in regex.Split(text))
            {
                if (matches.Contains(segment))
                {
                    var hyperlink = new Hyperlink(new Run(segment))
                    {
                        NavigateUri = new Uri(segment),
                    };

                    hyperlink.RequestNavigate += (sender, args) => Process.Start(segment);

                    paragraph.Inlines.Add(hyperlink);
                }
                else
                {
                    paragraph.Inlines.Add(new Run(segment));
                }
            }

            return flowDocument;
        }
        private void MoreInfo_Click(object sender, RoutedEventArgs e)
        {
            HomepageButton.Content = $"{(TypeBox.SelectedValue as ComboBoxItem).Content.ToString().Trim().TrimEnd('s')} Page";
            Button button = sender as Button;
            var item = button.DataContext as GameBananaRecord;
            if (item.Compatible)
                DownloadButton.Visibility = Visibility.Visible;
            else
                DownloadButton.Visibility = Visibility.Collapsed;
            if (item.HasAltLinks)
                AltButton.Visibility = Visibility.Visible;
            else
                AltButton.Visibility = Visibility.Collapsed;
            DescPanel.DataContext = button.DataContext;
            MediaPanel.DataContext = button.DataContext;
            DescText.ScrollToHome();
            var text = "";
            text += item.ConvertedText;
            DescText.Document = ConvertToFlowDocument(text);
            ImageLeft.IsEnabled = true;
            ImageRight.IsEnabled = true;
            BigImageLeft.IsEnabled = true;
            BigImageRight.IsEnabled = true;
            imageCount = item.Media.Where(x => x.Type == "image").ToList().Count;
            imageCounter = 0;
            if (imageCount > 0)
            {
                Grid.SetColumnSpan(DescText, 1);
                ImagePanel.Visibility = Visibility.Visible;
                var image = new BitmapImage(new Uri($"{item.Media[imageCounter].Base}/{item.Media[imageCounter].File}"));
                Screenshot.Source = image;
                BigScreenshot.Source = image;
                CaptionText.Text = item.Media[imageCounter].Caption;
                BigCaptionText.Text = item.Media[imageCounter].Caption;
                if (!String.IsNullOrEmpty(CaptionText.Text))
                {
                    BigCaptionText.Visibility = Visibility.Visible;
                    CaptionText.Visibility = Visibility.Visible;
                }
                else
                {
                    BigCaptionText.Visibility = Visibility.Collapsed;
                    CaptionText.Visibility = Visibility.Collapsed;
                }
            }
            else
            {
                Grid.SetColumnSpan(DescText, 2);
                ImagePanel.Visibility = Visibility.Collapsed;
            }
            if (imageCount == 1)
            {
                ImageLeft.IsEnabled = false;
                ImageRight.IsEnabled = false;
                BigImageLeft.IsEnabled = false;
                BigImageRight.IsEnabled = false;
            }

            DescPanel.Visibility = Visibility.Visible;
        }
        private void CloseDesc_Click(object sender, RoutedEventArgs e)
        {
            DescPanel.Visibility = Visibility.Collapsed;
        }
        private void CloseMedia_Click(object sender, RoutedEventArgs e)
        {
            MediaPanel.Visibility = Visibility.Collapsed;
        }

        private void Image_Click(object sender, RoutedEventArgs e)
        {
            MediaPanel.Visibility = Visibility.Visible;
        }

        private void ImageLeft_Click(object sender, RoutedEventArgs e)
        {
            Button button = sender as Button;
            var item = button.DataContext as GameBananaRecord;
            if (--imageCounter == -1)
                imageCounter = imageCount - 1;
            var image = new BitmapImage(new Uri($"{item.Media[imageCounter].Base}/{item.Media[imageCounter].File}"));
            Screenshot.Source = image;
            CaptionText.Text = item.Media[imageCounter].Caption;
            BigScreenshot.Source = image;
            BigCaptionText.Text = item.Media[imageCounter].Caption;
            if (!String.IsNullOrEmpty(CaptionText.Text))
            {
                BigCaptionText.Visibility = Visibility.Visible;
                CaptionText.Visibility = Visibility.Visible;
            }
            else
            {
                BigCaptionText.Visibility = Visibility.Collapsed;
                CaptionText.Visibility = Visibility.Collapsed;
            }
        }

        private void ImageRight_Click(object sender, RoutedEventArgs e)
        {
            Button button = sender as Button;
            var item = button.DataContext as GameBananaRecord;
            if (++imageCounter == imageCount)
                imageCounter = 0;
            var image = new BitmapImage(new Uri($"{item.Media[imageCounter].Base}/{item.Media[imageCounter].File}"));
            Screenshot.Source = image;
            CaptionText.Text = item.Media[imageCounter].Caption;
            BigScreenshot.Source = image;
            BigCaptionText.Text = item.Media[imageCounter].Caption;
            if (!String.IsNullOrEmpty(CaptionText.Text))
            {
                BigCaptionText.Visibility = Visibility.Visible;
                CaptionText.Visibility = Visibility.Visible;
            }
            else
            {
                BigCaptionText.Visibility = Visibility.Collapsed;
                CaptionText.Visibility = Visibility.Collapsed;
            }
        }
        private static bool selected = false;

        private static Dictionary<TypeFilter, List<GameBananaCategory>> cats = new();

        private static readonly List<GameBananaCategory> All = new GameBananaCategory[]
        {
            new GameBananaCategory()
            {
                Name = "All",
                ID = null
            }
        }.ToList();
        private static readonly List<GameBananaCategory> None = new GameBananaCategory[]
        {
            new GameBananaCategory()
            {
                Name = "- - -",
                ID = null
            }
        }.ToList();
        private async void InitializeBrowser()
        {
            if (Global.ronnietutorial)
            {
                await FeedGenerator.GetFakeFeed(1, TypeFilter.Mods, FeedFilter.Recent, null, null, 20, false, null);
                FeedBox.ItemsSource = FeedGenerator.CurrentFeed.Records;
                LoadingBar.Visibility = Visibility.Collapsed;
                BrowserRefreshButton.Visibility = Visibility.Collapsed;
                return;
            }
            using (var httpClient = new HttpClient())
            {
                ErrorPanel.Visibility = Visibility.Collapsed;
                // Initialize categories and games
                var gameID = "7692";
                var types = new string[] { "Mod", "Wip", "Sound" };
                double totalPages = 0;
                var counter = 0;
                foreach (var type in types)
                {
                    var requestUrl = $"https://gamebanana.com/apiv4/{type}Category/ByGame?_aGameRowIds[]={gameID}&_sRecordSchema=Custom" +
                        "&_csvProperties=_idRow,_sName,_sProfileUrl,_sIconUrl,_idParentCategoryRow&_nPerpage=50";
                    string responseString = "";
                    try
                    {
                        var responseMessage = await httpClient.GetAsync(requestUrl);
                        responseString = await responseMessage.Content.ReadAsStringAsync();
                        responseString = Regex.Replace(responseString, @"""(\d+)""", @"$1");
                        var numRecords = responseMessage.GetHeader("X-GbApi-Metadata_nRecordCount");
                        if (numRecords != -1)
                        {
                            totalPages = Math.Ceiling(numRecords / 50);
                        }
                    }
                    catch (HttpRequestException ex)
                    {
                        LoadingBar.Visibility = Visibility.Collapsed;
                        ErrorPanel.Visibility = Visibility.Visible;
                        BrowserRefreshButton.Visibility = Visibility.Visible;
                        switch (Regex.Match(ex.Message, @"\d+").Value)
                        {
                            case "443":
                                BrowserMessage.Text = "Your internet connection is down.";
                                break;
                            case "500":
                            case "503":
                            case "504":
                                BrowserMessage.Text = "GameBanana's servers are down.";
                                break;
                            default:
                                BrowserMessage.Text = ex.Message;
                                break;
                        }
                        return;
                    }
                    catch (Exception ex)
                    {
                        LoadingBar.Visibility = Visibility.Collapsed;
                        ErrorPanel.Visibility = Visibility.Visible;
                        BrowserRefreshButton.Visibility = Visibility.Visible;
                        BrowserMessage.Text = ex.Message;
                        return;
                    }
                    List<GameBananaCategory> response = new();
                    try
                    {
                        response = JsonSerializer.Deserialize<List<GameBananaCategory>>(responseString);
                    }
                    catch (Exception)
                    {
                        LoadingBar.Visibility = Visibility.Collapsed;
                        ErrorPanel.Visibility = Visibility.Visible;
                        BrowserRefreshButton.Visibility = Visibility.Visible;
                        BrowserMessage.Text = "Uh oh! Something went wrong while deserializing the categories...";
                        return;
                    }
                    cats.Add((TypeFilter)counter, response);

                    // Make more requests if needed
                    if (totalPages > 1)
                    {
                        for (double i = 2; i <= totalPages; i++)
                        {
                            var requestUrlPage = $"{requestUrl}&_nPage={i}";
                            try
                            {
                                responseString = await httpClient.GetStringAsync(requestUrlPage);
                                responseString = Regex.Replace(responseString, @"""(\d+)""", @"$1");
                            }
                            catch (HttpRequestException ex)
                            {
                                LoadingBar.Visibility = Visibility.Collapsed;
                                ErrorPanel.Visibility = Visibility.Visible;
                                BrowserRefreshButton.Visibility = Visibility.Visible;
                                switch (Regex.Match(ex.Message, @"\d+").Value)
                                {
                                    case "443":
                                        BrowserMessage.Text = "Your internet connection is down.";
                                        break;
                                    case "500":
                                    case "503":
                                    case "504":
                                        BrowserMessage.Text = "GameBanana's servers are down.";
                                        break;
                                    default:
                                        BrowserMessage.Text = ex.Message;
                                        break;
                                }
                                return;
                            }
                            catch (Exception ex)
                            {
                                LoadingBar.Visibility = Visibility.Collapsed;
                                ErrorPanel.Visibility = Visibility.Visible;
                                BrowserRefreshButton.Visibility = Visibility.Visible;
                                BrowserMessage.Text = ex.Message;
                                return;
                            }
                            try
                            {
                                response = JsonSerializer.Deserialize<List<GameBananaCategory>>(responseString);
                            }
                            catch (Exception ex)
                            {
                                LoadingBar.Visibility = Visibility.Collapsed;
                                ErrorPanel.Visibility = Visibility.Visible;
                                BrowserRefreshButton.Visibility = Visibility.Visible;
                                BrowserMessage.Text = "Uh oh! Something went wrong while deserializing the categories...";
                                return;
                            }
                            cats[(TypeFilter)counter] = cats[(TypeFilter)counter].Concat(response).ToList();
                        }
                    }
                    counter++;
                }
            }
            filterSelect = true;
            FilterBox.ItemsSource = FilterBoxList;
            CatBox.ItemsSource = All.Concat(cats[(TypeFilter)TypeBox.SelectedIndex].Where(x => x.RootID == 0).OrderBy(y => y.ID));
            SubCatBox.ItemsSource = None;
            CatBox.SelectedIndex = 0;
            SubCatBox.SelectedIndex = 0;
            FilterBox.SelectedIndex = 1;
            filterSelect = false;
            RefreshFilter();
            selected = true;
        }
        private void ApplyTransparentBoxes(bool init = false)
        {
            var transparencysettings = new Dictionary<string, double>();

            foreach (var key in transparentboxes)
            {
                string value = PLUSSavesystem.read_ini("Themes", $"Transparency_{key}", "100");
                var slider = (Slider)this.FindName($"Transparency_{key}");

                if (double.TryParse(value, out double parsed))
                {
                    transparencysettings[key] = parsed;
                }
                else
                {
                    PLUSSavesystem.write_ini("Themes", key, "100");
                    transparencysettings[key] = 100;
                }
                if (init)
                {
                    slider.Value = transparencysettings[key];
                }
                transparencysettings[key] = transparencysettings[key] / 100.0;
            }

            if (init)
                return;

            var currentBorderBrush = (Brush)FindResource("InnerBrush") as SolidColorBrush;
            var currentBackground = (Brush)FindResource("InnerBrush") as SolidColorBrush;

            var newBorderBrush = currentBorderBrush?.Clone();
            var newBackground = currentBackground?.Clone();

            newBorderBrush.Opacity = 0.75;
            newBackground.Opacity = 0.75;

            ModBrowserBorder.BorderBrush = newBorderBrush;
            ModBrowserBorder.Background = newBackground;

            currentBorderBrush = DescriptionWindow.BorderBrush as SolidColorBrush;
            currentBackground = DescriptionWindow.Background as SolidColorBrush;

            newBorderBrush = currentBorderBrush?.Clone();
            newBackground = currentBackground?.Clone();

            newBorderBrush.Opacity = 1 * transparencysettings["ModDescription"];
            newBackground.Opacity = 1 * transparencysettings["ModDescription"];

            DescriptionWindow.BorderBrush = newBorderBrush;
            DescriptionWindow.Background = newBackground;

            currentBorderBrush = ConsoleWindow.BorderBrush as SolidColorBrush;
            currentBackground = ConsoleWindow.Background as SolidColorBrush;

            newBorderBrush = currentBorderBrush?.Clone();
            newBackground = currentBackground?.Clone();

            newBorderBrush.Opacity = 1 * transparencysettings["Logger"];
            newBackground.Opacity = 1 * transparencysettings["Logger"];

            ConsoleWindow.BorderBrush = newBorderBrush;
            ConsoleWindow.Background = newBackground;

            currentBorderBrush = (Brush)Application.Current.FindResource("InnerBrush") as SolidColorBrush;
            currentBackground = (Brush)Application.Current.FindResource("InnerBrush") as SolidColorBrush;

            newBorderBrush = currentBorderBrush?.Clone();
            newBackground = currentBackground?.Clone();

            newBorderBrush.Opacity = 0.75 * transparencysettings["ModGrid"];
            newBackground.Opacity = 0.75 * transparencysettings["ModGrid"];

            ModGrid_Border.BorderBrush = newBorderBrush;
            ModGrid_Border.Background = newBackground;
        }
        public void Transparent_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (sender is Slider slider)
            {
                string name = slider.Name;
                string value = slider.Value.ToString();

                PLUSSavesystem.write_ini("Themes", name, value);
                ApplyTransparentBoxes();
            }
        }
        private void OnBrowserTabSelected(object sender, RoutedEventArgs e)
        {
            if (!selected)
                InitializeBrowser();
            ApplyTransparentBoxes();
        }

        private static int page = 1;
        private void DecrementPage(object sender, RoutedEventArgs e)
        {
            --page;
            RefreshFilter();
        }
        private void IncrementPage(object sender, RoutedEventArgs e)
        {
            ++page;
            RefreshFilter();
        }
        private void BrowserRefresh(object sender, RoutedEventArgs e)
        {
            if (!selected)
                InitializeBrowser();
            else
                RefreshFilter();
        }
        private static bool filterSelect;
        private static bool searched = false;
        private bool modManagerRefreshed = false;

        private void OnModManagerSelected(object sender, RoutedEventArgs e)
        {
            if (!modManagerRefreshed)
            {
                Refresh();
                PLUSrefresh();
                modManagerRefreshed = true;
            }
            ApplyTransparentBoxes();
        }

        private void OnModManagerUnselected(object sender, RoutedEventArgs e)
        {
            modManagerRefreshed = false;
        }

        private void PLUSrefresh()
        {
            ApplyTransparentBoxes();
            string DowngradePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Downgrades");
            string ThemesPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Themes");
            if (Directory.Exists(DowngradePath))
            {
                string[] files = Directory.GetFiles(DowngradePath);

                var DowngradeSave = DowngradeCombo.SelectedItem as string;
                if (string.IsNullOrEmpty(DowngradeSave))
                    DowngradeSave = null;
                DowngradeCombo.Items.Clear();

                for (int i = 0; i < files.Length; i++)
                {
                    string fileName = Path.GetFileName(files[i]);

                    string nameWithoutExt = Path.GetFileNameWithoutExtension(fileName);

                    if (fileName.ToLower().Contains("xdelta"))
                        DowngradeCombo.Items.Add(nameWithoutExt);
                }
                DowngradeCombo.Items.Add(PizzaTowerVersion);
                if (string.IsNullOrEmpty(DowngradeCombo.SelectedItem as string))
                {
                    bool hasDowngradeSaved = false;
                    if (!string.IsNullOrEmpty(DowngradeSave))
                        hasDowngradeSaved = DowngradeCombo.Items.Cast<object>().Any(i => string.Equals(i?.ToString(), DowngradeSave, StringComparison.OrdinalIgnoreCase));

                    if (DowngradeSave == null || !hasDowngradeSaved)
                    {
                        DowngradeCombo.SelectedItem = PizzaTowerVersion;
                    }
                    else
                    {
                        var match = DowngradeCombo.Items.Cast<object>().FirstOrDefault(i => string.Equals(i?.ToString(), DowngradeSave, StringComparison.OrdinalIgnoreCase));
                        if (match != null)
                            DowngradeCombo.SelectedItem = match;
                    }
                }
            }
            if (Directory.Exists(ThemesPath))
            {
                string[] files = Directory.GetFiles(ThemesPath);

                var ThemeSave = ThemePresetsCombo.SelectedItem as string;

                if (string.IsNullOrEmpty(ThemeSave))
                    ThemeSave = null;

                ThemePresetsCombo.Items.Clear();

                for (int i = 0; i < files.Length; i++)
                {
                    string fileName = Path.GetFileName(files[i]);

                    string nameWithoutExt = Path.GetFileNameWithoutExtension(fileName);

                    if (fileName.ToLower().Contains("potheme"))
                    {
                        ThemePresetsCombo.Items.Add(nameWithoutExt);
                    }
                }
                if (string.IsNullOrEmpty(ThemePresetsCombo.SelectedItem as string))
                {
                    bool hasThemeSaved = false;
                    if (!string.IsNullOrEmpty(ThemeSave))
                        hasThemeSaved = ThemePresetsCombo.Items.Cast<object>().Any(i => string.Equals(i?.ToString(), ThemeSave, StringComparison.OrdinalIgnoreCase));

                    if (ThemeSave == null || !hasThemeSaved)
                    {
                        ThemePresetsCombo.SelectedIndex = 0;
                    }
                    else
                    {
                        var match = ThemePresetsCombo.Items.Cast<object>().FirstOrDefault(i => string.Equals(i?.ToString(), ThemeSave, StringComparison.OrdinalIgnoreCase));
                        if (match != null)
                            ThemePresetsCombo.SelectedItem = match;
                    }
                }
            }
            var ModFolderSave = ModFolderCombo.SelectedItem as string;
            if (string.IsNullOrEmpty(ModFolderSave))
                ModFolderSave = null;

            ModFolderCombo.Items.Clear();
            ModFolderCombo.Items.Add("All");
            var allfoldername = PLUSSavesystem.read_ini_section("Folder");
            if (allfoldername != null && allfoldername.GetLength(0) > 0)
            {
                for (int i = 0; i < allfoldername.GetLength(0); i++)
                {
                    string foldername = allfoldername[i, 0];
                    ModFolderCombo.Items.Add(foldername);
                }
            }

            bool hasModFolderSaved = false;
            if (!string.IsNullOrEmpty(ModFolderSave))
                hasModFolderSaved = ModFolderCombo.Items.Cast<object>().Any(i => string.Equals(i?.ToString(), ModFolderSave, StringComparison.OrdinalIgnoreCase));

            if (!hasModFolderSaved)
            {
                ModFolderCombo.SelectedItem = "All";
            }
            else
            {
                var match = ModFolderCombo.Items.Cast<object>().FirstOrDefault(i => string.Equals(i?.ToString(), ModFolderSave, StringComparison.OrdinalIgnoreCase));
                if (match != null)
                    ModFolderCombo.SelectedItem = match;
            }

            DowngradeDownloadCombo.Items.Clear();
            var ptversions = JsonSerializer.Deserialize<List<PTversion>>(File.ReadAllText($"{Global.assemblyLocation}{Global.s}Dependencies{Global.s}ptversions.json"));

            foreach (var v in ptversions)
            {
                DowngradeDownloadCombo.Items.Add(v.version);
            }

            if (string.IsNullOrEmpty(DowngradeDownloadCombo.SelectedItem as string))
            {
                DowngradeDownloadCombo.SelectedIndex = 0;
            }

            // Refresh the ModGrid UI so changes are visible immediately
            try
            {
                Dispatcher.Invoke(() =>
                {
                    ModGrid.ItemsSource = Global.ModList;
                    ModGrid.Items.Refresh();
                    if (ModGrid.Items.Count > 0)
                        ModGrid.ScrollIntoView(ModGrid.Items[0]);
                });
            }
            catch { }

            string bgImagePath = null;

            if (Directory.Exists(Global.customassetsfolder))
            {
                bgImagePath = Directory.GetFiles(Global.customassetsfolder)
                    .FirstOrDefault(f => Path.GetFileNameWithoutExtension(f).Equals("background", StringComparison.OrdinalIgnoreCase) && themeimageExtensions.Contains(Path.GetExtension(f), StringComparer.OrdinalIgnoreCase));
            }

            if (!string.IsNullOrEmpty(bgImagePath) && File.Exists(bgImagePath))
            {
                try
                {
                    byte[] imageData = File.ReadAllBytes(bgImagePath);

                    BitmapImage LoadBitmapFromBytes(byte[] data)
                    {
                        var bitmap = new BitmapImage();
                        using (var ms = new MemoryStream(data))
                        {
                            bitmap.BeginInit();
                            bitmap.CacheOption = BitmapCacheOption.OnLoad;
                            bitmap.StreamSource = ms;
                            bitmap.EndInit();
                            bitmap.Freeze();
                        }
                        return bitmap;
                    }

                    var bitmap = LoadBitmapFromBytes(imageData);
                    MainGrid.Background = new ImageBrush(bitmap)
                    {
                        Stretch = Stretch.UniformToFill
                    };
                    ModBrowserGrid.Background = new ImageBrush(bitmap)
                    {
                        Stretch = Stretch.UniformToFill
                    };
                    SettingsGrid.Background = new ImageBrush(bitmap)
                    {
                        Stretch = Stretch.UniformToFill
                    };
                    PatchNotesGrid.Background = new ImageBrush(bitmap)
                    {
                        Stretch = Stretch.UniformToFill
                    };

                }
                catch
                {
                    MainGrid.Background = (Brush)FindResource("PrimaryBrush");
                    ModBrowserGrid.Background = (Brush)FindResource("PrimaryBrush");
                    SettingsGrid.Background = (Brush)FindResource("PrimaryBrush");
                    PatchNotesGrid.Background = (Brush)FindResource("PrimaryBrush");
                }
            }
            else
            {
                MainGrid.Background = (Brush)FindResource("PrimaryBrush");
                ModBrowserGrid.Background = (Brush)FindResource("PrimaryBrush");
                SettingsGrid.Background = (Brush)FindResource("PrimaryBrush");
                PatchNotesGrid.Background = (Brush)FindResource("PrimaryBrush");
            }

        }
        private async void RefreshFilter()
        {
            NSFWCheckbox.IsEnabled = false;
            SearchBar.IsEnabled = false;
            SearchButton.IsEnabled = false;
            FilterBox.IsEnabled = false;
            TypeBox.IsEnabled = false;
            CatBox.IsEnabled = false;
            SubCatBox.IsEnabled = false;
            PageLeft.IsEnabled = false;
            PageRight.IsEnabled = false;
            PageBox.IsEnabled = false;
            PerPageBox.IsEnabled = false;
            ClearCacheButton.IsEnabled = false;
            ErrorPanel.Visibility = Visibility.Collapsed;
            filterSelect = true;
            PageBox.SelectedValue = page;
            filterSelect = false;
            Page.Text = $"Page {page}";
            LoadingBar.Visibility = Visibility.Visible;
            FeedBox.Visibility = Visibility.Collapsed;
            PageLeft.IsEnabled = false;
            PageRight.IsEnabled = false;
            var search = searched ? SearchBar.Text : null;
            await FeedGenerator.GetFeed(page, (TypeFilter)TypeBox.SelectedIndex, (FeedFilter)FilterBox.SelectedIndex, (GameBananaCategory)CatBox.SelectedItem,
                (GameBananaCategory)SubCatBox.SelectedItem, (PerPageBox.SelectedIndex + 1) * 10, (bool)NSFWCheckbox.IsChecked, search);
            FeedBox.ItemsSource = FeedGenerator.CurrentFeed.Records;
            if (FeedGenerator.error)
            {
                LoadingBar.Visibility = Visibility.Collapsed;
                ErrorPanel.Visibility = Visibility.Visible;
                BrowserRefreshButton.Visibility = Visibility.Visible;
                if (FeedGenerator.exception.Message.Contains("JSON tokens"))
                {
                    BrowserMessage.Text = "Uh oh! Pizza Oven failed to deserialize the GameBanana feed.";
                    return;
                }
                switch (Regex.Match(FeedGenerator.exception.Message, @"\d+").Value)
                {
                    case "443":
                        BrowserMessage.Text = "Your internet connection is down.";
                        break;
                    case "500":
                    case "503":
                    case "504":
                        BrowserMessage.Text = "GameBanana's servers are down.";
                        break;
                    default:
                        BrowserMessage.Text = FeedGenerator.exception.Message;
                        break;
                }
                return;
            }
            if (page < FeedGenerator.CurrentFeed.TotalPages)
                PageRight.IsEnabled = true;
            if (page != 1)
                PageLeft.IsEnabled = true;
            if (FeedBox.Items.Count > 0)
            {
                FeedBox.ScrollIntoView(FeedBox.Items[0]);
                FeedBox.Visibility = Visibility.Visible;
            }
            else
            {
                ErrorPanel.Visibility = Visibility.Visible;
                BrowserRefreshButton.Visibility = Visibility.Collapsed;
                BrowserMessage.Visibility = Visibility.Visible;
                BrowserMessage.Text = "Pizza Oven+ couldn't find any mods.";
            }
            PageBox.ItemsSource = Enumerable.Range(1, (int)(FeedGenerator.CurrentFeed.TotalPages));

            LoadingBar.Visibility = Visibility.Collapsed;
            CatBox.IsEnabled = true;
            SubCatBox.IsEnabled = true;
            TypeBox.IsEnabled = true;
            FilterBox.IsEnabled = true;
            PageBox.IsEnabled = true;
            PerPageBox.IsEnabled = true;
            SearchBar.IsEnabled = true;
            SearchButton.IsEnabled = true;
            NSFWCheckbox.IsEnabled = true;
            ClearCacheButton.IsEnabled = true;
        }

        private void FilterSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (IsLoaded && !filterSelect)
            {
                if (!searched || FilterBox.SelectedIndex != 3)
                {
                    filterSelect = true;
                    var temp = FilterBox.SelectedIndex;
                    FilterBox.ItemsSource = FilterBoxList;
                    FilterBox.SelectedIndex = temp;
                    filterSelect = false;
                }
                SearchBar.Clear();
                searched = false;
                page = 1;
                RefreshFilter();
            }
        }
        private void PerPageSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (IsLoaded && !filterSelect)
            {
                page = 1;
                RefreshFilter();
            }
        }
        private void TypeFilterSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (IsLoaded && !filterSelect)
            {
                SearchBar.Clear();
                searched = false;
                filterSelect = true;
                if (!searched)
                {
                    FilterBox.ItemsSource = FilterBoxList;
                    FilterBox.SelectedIndex = 1;
                }
                // Set categories
                if (cats[(TypeFilter)TypeBox.SelectedIndex].Any(x => x.RootID == 0))
                    CatBox.ItemsSource = All.Concat(cats[(TypeFilter)TypeBox.SelectedIndex].Where(x => x.RootID == 0).OrderBy(y => y.ID));
                else
                    CatBox.ItemsSource = None;
                CatBox.SelectedIndex = 0;
                var cat = (GameBananaCategory)CatBox.SelectedValue;
                if (cats[(TypeFilter)TypeBox.SelectedIndex].Any(x => x.RootID == cat.ID))
                    SubCatBox.ItemsSource = All.Concat(cats[(TypeFilter)TypeBox.SelectedIndex].Where(x => x.RootID == cat.ID).OrderBy(y => y.ID));
                else
                    SubCatBox.ItemsSource = None;
                SubCatBox.SelectedIndex = 0;
                filterSelect = false;
                page = 1;
                RefreshFilter();
            }
        }
        private void MainFilterSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (IsLoaded && !filterSelect)
            {
                SearchBar.Clear();
                searched = false;
                filterSelect = true;
                if (!searched)
                {
                    FilterBox.ItemsSource = FilterBoxList;
                    FilterBox.SelectedIndex = 1;
                }
                // Set Categories
                var cat = (GameBananaCategory)CatBox.SelectedValue;
                if (cats[(TypeFilter)TypeBox.SelectedIndex].Any(x => x.RootID == cat.ID))
                    SubCatBox.ItemsSource = All.Concat(cats[(TypeFilter)TypeBox.SelectedIndex].Where(x => x.RootID == cat.ID).OrderBy(y => y.ID));
                else
                    SubCatBox.ItemsSource = None;
                SubCatBox.SelectedIndex = 0;
                filterSelect = false;
                page = 1;
                RefreshFilter();
            }
        }
        private void SubFilterSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!filterSelect && IsLoaded)
            {
                SearchBar.Clear();
                searched = false;
                page = 1;
                RefreshFilter();
            }
        }
        private void UniformGrid_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            var grid = sender as UniformGrid;
            grid.Columns = (int)grid.ActualWidth / 400 + 1;
        }
        private void OnResize(object sender, RoutedEventArgs e)
        {
            BigScreenshot.MaxHeight = ActualHeight - 240;
        }

        private void PageBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!filterSelect && IsLoaded)
            {
                page = (int)PageBox.SelectedValue;
                RefreshFilter();
            }
        }
        private void NSFWCheckbox_Checked(object sender, RoutedEventArgs e)
        {
            if (!filterSelect && IsLoaded)
            {
                if (searched)
                {
                    filterSelect = true;
                    FilterBox.ItemsSource = FilterBoxList;
                    FilterBox.SelectedIndex = 1;
                    filterSelect = false;
                }
                SearchBar.Clear();
                searched = false;
                page = 1;
                RefreshFilter();
            }
        }
        private void ClearCache(object sender, RoutedEventArgs e)
        {
            FeedGenerator.ClearCache();
            RefreshFilter();
        }

        private void Search()
        {
            if (!filterSelect && IsLoaded && !String.IsNullOrWhiteSpace(SearchBar.Text))
            {
                filterSelect = true;
                FilterBox.ItemsSource = FilterBoxListWhenSearched;
                FilterBox.SelectedIndex = 3;
                NSFWCheckbox.IsChecked = true;
                // Set categories
                if (cats[(TypeFilter)TypeBox.SelectedIndex].Any(x => x.RootID == 0))
                    CatBox.ItemsSource = All.Concat(cats[(TypeFilter)TypeBox.SelectedIndex].Where(x => x.RootID == 0).OrderBy(y => y.ID));
                else
                    CatBox.ItemsSource = None;
                CatBox.SelectedIndex = 0;
                var cat = (GameBananaCategory)CatBox.SelectedValue;
                if (cats[(TypeFilter)TypeBox.SelectedIndex].Any(x => x.RootID == cat.ID))
                    SubCatBox.ItemsSource = All.Concat(cats[(TypeFilter)TypeBox.SelectedIndex].Where(x => x.RootID == cat.ID).OrderBy(y => y.ID));
                else
                    SubCatBox.ItemsSource = None;
                SubCatBox.SelectedIndex = 0;
                filterSelect = false;
                searched = true;
                page = 1;
                RefreshFilter();
            }
        }
        private void SearchBar_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
                Search();
        }
        private static readonly List<string> FilterBoxList = new string[] { "Featured", "Recent", "Popular" }.ToList();
        private static readonly List<string> FilterBoxListWhenSearched = new string[] { "Featured", "Recent", "Popular", "- - -" }.ToList();

        private void SearchButton_Click(object sender, RoutedEventArgs e)
        {
            Search();
        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (IsLoaded && ModGridSearchButton.IsEnabled)
                if (e.KeyboardDevice.IsKeyDown(Key.LeftCtrl) || e.KeyboardDevice.IsKeyDown(Key.RightCtrl))
                {
                    switch (e.Key)
                    {
                        case Key.F:
                            ModGrid_SearchBar.Focus();
                            break;
                    }
                }
        }

        private void ModGrid_SearchBar_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyboardDevice.IsKeyDown(Key.Enter))
                ModGridSearch();
        }
        private void ModGridSearch()
        {
            if (!String.IsNullOrEmpty(ModGrid_SearchBar.Text) && ModGridSearchButton.IsEnabled && Global.ModList.Count > 0)
            {
                string text = ModGrid_SearchBar.Text;
                Global.ModList = new ObservableCollection<Mod>(Global.ModList.Where(mod => mod.name.Contains(text, StringComparison.InvariantCultureIgnoreCase))
                    .Concat(Global.ModList.Where(mod => !mod.name.Contains(text, StringComparison.InvariantCultureIgnoreCase))));

                Refresh();
                ModGrid.ScrollIntoView(ModGrid.Items[0]);
            }
        }

        private void ModGridSearchButton_Click(object sender, RoutedEventArgs e)
        {
            ModGridSearch();
        }

        private void Clear_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            ModGrid_SearchBar.Clear();
        }

        public void PLUSWatcher()
        {
            string[] foldersToWatch = new string[]
            {
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Downgrades"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "PizzaOvenPLUS", "CustomAssets")
            };

            foreach (var folder in foldersToWatch)
            {
                if (Directory.Exists(folder))
                {
                    var watcher = new FileSystemWatcher();
                    watcher.Path = folder;
                    watcher.Filter = "*.*";
                    watcher.NotifyFilter = NotifyFilters.FileName | NotifyFilters.LastWrite;

                    watcher.Created += PLUSWatcher_Changed;
                    watcher.Deleted += PLUSWatcher_Changed;
                    watcher.Changed += PLUSWatcher_Changed;
                    watcher.Renamed += PLUSWatcher_Renamed;

                    watcher.EnableRaisingEvents = true;

                    PLUSWatchers.Add(watcher);
                }
            }
        }

        private void PLUSWatcher_Changed(object sender, FileSystemEventArgs e)
        {
            Dispatcher.BeginInvoke((Action)(() => PLUSrefresh()));
        }

        private void PLUSWatcher_Renamed(object sender, RenamedEventArgs e)
        {
            Dispatcher.BeginInvoke((Action)(() => PLUSrefresh()));
        }


        private string PLUSModType(string path)
        {
            if (!File.Exists(path))
            {
                return "Normal";
            }
            var exts = Directory.EnumerateFiles(
                    path,
                    "*.*",
                    new EnumerationOptions
                    {
                        RecurseSubdirectories = true,
                        IgnoreInaccessible = true
                    })
                .Where(file => !string.Equals(
                    Path.GetFileName(file),
                    "mod.json",
                    StringComparison.OrdinalIgnoreCase))
                .Select(Path.GetExtension)
                .Where(ext => !string.IsNullOrEmpty(ext))
                .Select(ext => ext.TrimStart('.').ToLowerInvariant())
                .Distinct()
                .ToArray();

            bool hasLevelsDir = Directory.EnumerateDirectories(path, "*", SearchOption.AllDirectories)
            .Any(dir => string.Equals(
                Path.GetFileName(dir),
                "levels",
                StringComparison.OrdinalIgnoreCase
            ));

            string[] GMLoaderFolder = { "audio", "code", "lib", "config", "csx", "room", "shader", "texture", "xdelta" };

            bool potentialGMLoader = Directory.EnumerateDirectories(path, "*", SearchOption.AllDirectories).Any(d => GMLoaderFolder.Contains(Path.GetFileName(d), StringComparer.OrdinalIgnoreCase));

            bool xdeltainfolder = false;

            if (Directory.Exists(path))
            {
                var xdeltaFolders = Directory.EnumerateDirectories(path, "xdelta", SearchOption.AllDirectories);

                xdeltainfolder = xdeltaFolders.Any(xdeltaFolder => Directory.EnumerateFileSystemEntries(xdeltaFolder).Any(entry => Path.GetFileName(entry).Equals("xdelta", StringComparison.OrdinalIgnoreCase)));
            }

            Metadata metadata;

            var jsonPath = $"{path}{Global.s}mod.json";

            if (File.Exists(jsonPath))
            {
                try
                {
                    string json = File.ReadAllText(jsonPath);
                    metadata = JsonSerializer.Deserialize<Metadata>(json) ?? new Metadata();
                }
                catch
                {
                    metadata = new Metadata();
                }
            }
            else
            {
                metadata = new Metadata();
            }

            if (metadata.cat == "GMLoader")
            {
                return "GMLOADER";
            }
            else if (metadata.cat == "CYOP/AFOM")
            {
                return "AFOM";
            }
            else if (exts.Contains("xdelta") && !xdeltainfolder)
            {
                return "Normal";
            }
            else if (potentialGMLoader)
            {
                return "GMLOADER";
            }
            else if (hasLevelsDir && exts.Contains("json") && exts.Contains("ini"))
            {
                return "AFOM";
            }
            return "Normal";
        }
        private static IEnumerable<T> FindVisualChildren<T>(DependencyObject depObj) where T : DependencyObject
        {
            if (depObj == null) yield break;
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(depObj); i++)
            {
                var child = VisualTreeHelper.GetChild(depObj, i);
                if (child is T t) yield return t;
                foreach (var childOfChild in FindVisualChildren<T>(child))
                    yield return childOfChild;
            }
        }

        private static IEnumerable<T> FindLogicalChildren<T>(DependencyObject root) where T : DependencyObject
        {
            if (root == null) yield break;
            foreach (var child in LogicalTreeHelper.GetChildren(root))
            {
                if (child is DependencyObject dep)
                {
                    if (child is T t) yield return t;
                    foreach (var descendant in FindLogicalChildren<T>(dep))
                        yield return descendant;
                }
            }
        }
        private void UpdatePLUSfilter(string filter)
        {
            filter = (filter ?? string.Empty).Trim();

            var settingsContent = Settings?.Content as DependencyObject;
            if (settingsContent == null)
                return;

            var visual = FindVisualChildren<StackPanel>(settingsContent);
            var logical = FindLogicalChildren<StackPanel>(settingsContent);
            var stackPanels = visual.Concat(logical).Distinct().ToList();
            if (!stackPanels.Any())
                return;

            foreach (var panel in stackPanels)
            {
                string searchable = panel.Tag?.ToString() ?? string.Empty;

                if (string.IsNullOrWhiteSpace(searchable))
                {
                    DependencyObject parent = VisualTreeHelper.GetParent(panel);

                    while (parent != null)
                    {
                        if (parent is StackPanel parentPanel && parentPanel.Tag != null)
                        {
                            searchable = parentPanel.Tag.ToString();
                            break;
                        }

                        parent = VisualTreeHelper.GetParent(parent);
                    }
                }

                if (string.IsNullOrWhiteSpace(searchable))
                {
                    continue;
                }

                
                bool match = string.Equals(searchable.Trim(),filter?.Trim(),StringComparison.OrdinalIgnoreCase);
                panel.Visibility = match ? Visibility.Visible : Visibility.Collapsed;
            }
            TutorialButton.Visibility = Visibility.Visible; 
            SettingOptions.Visibility = string.IsNullOrEmpty(filter) ? Visibility.Visible : Visibility.Collapsed;

            if (Global.ronnietutorial)
            {
                TutorialPanel.Visibility = Visibility.Collapsed;
                TutorialButton.Visibility = Visibility.Collapsed;   
            }
        }
        private void FilterButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn)
            {
                CurrentFilter = btn.Content?.ToString();
            }
        }
        private void FilterButton_HoverOn(object sender, RoutedEventArgs e)
        {
            if (introanimator != null)
                introanimator.Destroy();
            if (launchanimator != null)
                launchanimator.Destroy();
            if (replayanimator != null)
                replayanimator.Destroy();
            if (Global.ronnietutorial && PLUSTutorial.RonnieVariables.RonnieExplainSettings != 1)
            {
                if (Global.ronnietutorial && PLUSTutorial.RonnieVariables.RonnieExplainSettings == 0)
                {
                    PLUSTutorial.RonnieVariables.RonnieExplainSettings = 1;
                }
                return;
            }
            settinganimator = new PLUSRonnieAnimate();
            settinganimator.Initialize(this, 0, this.Height - 300, 1);
            settinganimator.SetExpression("thinking");
            if (sender is Button btn)
            {
                var messages = new Dictionary<string, string>
                {
                    ["Tutorial"] = "You can use this to replay my tutorial",
                    ["Links"] = "Wanna suggest something? there's a form in there where you can suggest features, maybe join our discord or check out other social links",
                    ["App Settings"] = "Settings mainly to do with the application such as display on discord so people can see my cute little face on it or startup on opening your device",
                    ["Launch Settings"] = "Settings mainly to do with the launch like applying downgrades or customising what happens on launch",
                    ["Mod Settings"] = "Settings mainly to do with mods such as PO. Files, Adding folder to categorise your mods or saving current Pizza Tower folder as mod",
                    ["App Customization"] = "Settings mainly to do with the looks of the app or the sounds of the app",
                    ["GMLoader"] = "Settings to mainly convert your xdelta mods into GMLoader mods(recommended for mods that have smaller additions)",
                    ["Credits"] = "Contributions to PizzaOven+ and the original PizzaOven too"
                };

                if (btn.Content?.ToString() is string key && messages.TryGetValue(key, out var message))
                {
                    if (Global.ronnietutorial && tutorialanimator != null)
                    {
                        PLUSTutorial.RonnieVariables.publictextbox = tutorialanimator.MakeTextbox(tutorialanimator.GetX() + 110, tutorialanimator.GetY() + 25, message);
                        if (settinganimator != null)
                        {
                            settinganimator.Destroy();
                        }
                    }
                    else 
                    {
                        settinganimator.MakeTextbox(settinganimator.GetX() + 110, settinganimator.GetY() + 25, message);
                        if (key == "Tutorial")
                        {
                            settinganimator.SetExpression("happy");
                        }
                    }
                }
            }
        }
        private void FilterButton_HoverOff(object sender, RoutedEventArgs e)
        {
            if (Global.ronnietutorial && tutorialanimator != null)
            {
                tutorialanimator.DestroyTextbox(PLUSTutorial.RonnieVariables.publictextbox);
            }
            if (settinganimator != null)
            { 
                settinganimator.Destroy();
            }

        }
        private void FilterButtonBack_Click(object sender, RoutedEventArgs e)
        {
            CurrentFilter = "";
        }
        private void CleanPO_click(object sender, RoutedEventArgs e)
        {
            var path = Global.config.ModsFolder;
            ModLoader.RestoreDirectory(Global.config.ModsFolder);
        }

        private void DeleteModFolder_Click(object sender, RoutedEventArgs e)
        {
            if (ModFolderCombo.SelectedItem as string == "All")
            {
                MessageBoxResult allresult = MessageBox.Show("Do you want to delete ALL folders?", "Confirm Delete", MessageBoxButton.YesNo, MessageBoxImage.Question);
                if (allresult == MessageBoxResult.No)
                    return;
                PLUSSavesystem.delete_ini_section("Folder");
                return;
            }
            MessageBoxResult result = MessageBox.Show("Do you want to delete this folder?", "Confirm Delete", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (result == MessageBoxResult.No)
                return;
            var saves = PLUSSavesystem.read_ini_section("Folder");
            for (int i = 0; i < saves.GetLength(0); i++)
            {
                if (saves[i, 0] == ModFolderCombo.SelectedItem as string)
                {
                    PLUSSavesystem.delete_ini_value("Folder", saves[i, 0]);
                }
            }
            ModFolderCombo.SelectedItem = "All";
        }
        private void OpenLink_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn)
            {
                string url = btn.Name switch
                {
                    "OpenSuggestForm" => "https://docs.google.com/forms/d/e/1FAIpQLScI-8L6-ATpE6_ip3gzESXAWi4B_0pwHiHI5g83fb3SlLTM_A/viewform?usp=dialog",
                    "OpenEmail" => "https://mail.google.com/mail/u/0/#inbox?compose=GTvVlcSGKZhCvzvPvWzHvQZTnWMgDSzDHWTFDjnfWdjQscBHkRtBhmJPRKjjJbkNqlGRbtHlWzDWW",
                    "OpenTwitterX" => "https://x.com/SurfyCrescent97",
                    "OpenDiscord" => "https://discord.gg/snv7CrRQzx",
                    _ => null
                };

                if (!string.IsNullOrEmpty(url))
                {
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = url,
                        UseShellExecute = true
                    });
                }
            }
        }
        private void AssetsFolder_Click(object sender, RoutedEventArgs e)
        {
            Process process = Process.Start("explorer.exe", Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "PizzaOvenPLUS", "CustomAssets"));
        }

        private void RestoreMissingAssets_Click(object sender, RoutedEventArgs e)
        {
            string assetPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "PizzaOvenPLUS", "CustomAssets");

            Directory.CreateDirectory(assetPath);

            Assembly assembly = Assembly.GetExecutingAssembly();

            string assetPrefix = "PizzaOven.CustomAssets.";

            var resources = assembly
                .GetManifestResourceNames()
                .Where(r => r.StartsWith(assetPrefix));

            foreach (string resourceName in resources)
            {
                string relativePath = resourceName
                    .Substring(assetPrefix.Length)
                    .Replace('.', Path.DirectorySeparatorChar);

                int lastSeparator = relativePath.LastIndexOf(Path.DirectorySeparatorChar);
                if (lastSeparator != -1)
                {
                    relativePath =
                        relativePath[..lastSeparator] + "." +
                        relativePath[(lastSeparator + 1)..];
                }

                string outputPath = Path.Combine(assetPath, relativePath);
                Directory.CreateDirectory(Path.GetDirectoryName(outputPath)!);

                if (File.Exists(outputPath))
                    continue;

                using Stream resourceStream =
                    assembly.GetManifestResourceStream(resourceName)!;

                using FileStream fileStream =
                    new FileStream(outputPath, FileMode.Create, FileAccess.Write);

                resourceStream.CopyTo(fileStream);
                PLUSMUSIC.InitializeAsync();
            }
        }

        private void RestoreALLAssets_Click(object sender, RoutedEventArgs e)
        {
            string folderPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "PizzaOvenPLUS", "CustomAssets");
            if (Directory.Exists(folderPath))
            {
                foreach (string file in Directory.GetFiles(folderPath, "*", SearchOption.AllDirectories))
                {
                    try
                    {
                        File.Delete(file);
                    }
                    catch
                    {
                    }
                }
                foreach (string dir in Directory.GetDirectories(folderPath, "*", SearchOption.AllDirectories))
                {
                    try
                    {
                        if (Directory.GetFiles(dir).Length == 0 && Directory.GetDirectories(dir).Length == 0)
                            Directory.Delete(dir);
                    }
                    catch
                    {
                    }
                }
            }

            Directory.CreateDirectory(folderPath);
            RestoreMissingAssets_Click(sender, e);
            PLUSMUSIC.InitializeAsync();
        }
        private void HandlePLUStoggle(string section, string key, bool defaultValue, string toggleName)
        {
            bool enabled = PLUSSavesystem.toggle_ini_bool(section, key, defaultValue);
            InitPLUSToggle(toggleName, enabled);
        }
        private void POLanguage_Click(object sender, RoutedEventArgs e)
        {
            HandlePLUStoggle("Audio", "Mute", false, "POLanguage");
        }
        private void StartupToggle_Click(object sender, RoutedEventArgs e)
        {
            RegistryConfig.ToggleStartup();
            InitPLUSToggle("Startup", RegistryConfig.GetStartupStatus() == "Enabled");
        }
        private void Mute_Click(object sender, RoutedEventArgs e)
        {
            HandlePLUStoggle("Audio", "Mute", false, "Mute");
        }
        private void UnfocusedMute_Click(object sender, RoutedEventArgs e)
        {
            HandlePLUStoggle("Audio", "UnfocusedMute", true, "UnfocusedMute");
        }
        private void RPCtoggle_Click(object sender, RoutedEventArgs e)
        {
            HandlePLUStoggle("Discord", "RPC", true, "RPC");
        }
        private void DebugToggle_Click(object sender, RoutedEventArgs e)
        {
            HandlePLUStoggle("Launch", "Debug", true, "Debug");
        }
        private void SteamLaunch_Click(object sender, RoutedEventArgs e)
        {
            HandlePLUStoggle("Launch", "Steam", true, "SteamLaunch");
        }
        private void MODUPDATERtoggle_Click(object sender, RoutedEventArgs e)
        {
            HandlePLUStoggle("LowEnd", "ModUpdate", true, "ModUpdater");
        }
        public void InitPLUSToggle(string name, bool enabled)
        {
            Button? button = null;
            string? OnText = "";
            string? OffText = "";

            switch (name)
            {
                case "Mute":
                    PLUSMUSIC.MuteEnabled = enabled;
                    PLUSMUSIC.ApplyCurrentVolume();
                    button = MuteButton;
                    OnText = "Disable Mute? [IT'S ON]";
                    OffText = "Enable Mute? [IT'S OFF]";
                    break;
                case "UnfocusedMute":
                    PLUSMUSIC.unfocusedMuteEnabled = enabled;
                    PLUSMUSIC.ApplyCurrentVolume();
                    button = UnfocusedMuteButton;
                    OnText = "Disable Unfocused Mute? [IT'S ON]";
                    OffText = "Enable Unfocused Mute? [IT'S OFF]";
                    break;
                case "RPC":
                    try
                    {
                        if (enabled)
                            PLUSRPC.DiscordPresenceService.Initialize();
                        else
                            PLUSRPC.DiscordPresenceService.Shutdown();
                    }
                    catch { }
                    button = RPCtoggle;
                    OnText = "Enable RPC? [IT'S ON]";
                    OffText = "Disable RPC? [IT'S OFF]";
                    break;
                case "Debug":
                    button = DebugToggle;
                    OnText = "Enable Debug? [IT'S ON]";
                    OffText = "Disable Debug? [IT'S OFF]";
                    break;
                case "SteamLaunch":
                    button = SteamLaunchToggle;
                    OnText = "Don't use Steam? [IT'S ON]";
                    OffText = "Use Steam? [IT'S OFF]";
                    break;
                case "ModUpdater":
                    button = MODUPDATERtoggle;
                    OnText = "Disable Check for Mod Updates? [IT'S ON]";
                    OffText = "Enable Check for Mod Updates? [IT'S OFF]";
                    break;
                case "POLanguage":
                    if (!enabled)
                    {
                        var ptfolder = $"{Global.config.ModsFolder}";
                        var langPath = Path.Combine(ptfolder, "lang");
                        var extensions = new[] { ".po", ".custompo", ".downgradepo" };

                        if (Directory.Exists(langPath))
                        {
                            foreach (var file in Directory.GetFiles(langPath, "*.*", SearchOption.AllDirectories)
                                                          .Where(f => extensions.Contains(Path.GetExtension(f))))
                            {
                                File.Delete(file);
                            }
                        }
                    }
                    button = POLanguage;
                    OnText = "Do not Apply to Language Files? [IT'S ON]";
                    OffText = "Do Apply to Language Files? [IT'S OFF]";
                    break;
                case "Startup":
                    button = StartupToggle;
                    OnText = "Do not open on Startup? [IT'S ON]";
                    OffText = "Do open on Startup? [IT'S OFF]";
                    break;
            }
            if (button != null && OnText != null && OffText != null)
            {
                button.Content = enabled ? OnText : OffText;
            }
        }
        private void InitializeToggles()
        {
            InitPLUSToggle("Mute", PLUSSavesystem.read_ini("Audio", "Mute", "false") == "true");
            InitPLUSToggle("UnfocusedMute", PLUSSavesystem.read_ini("Audio", "UnfocusedMute", "true") == "true");
            InitPLUSToggle("RPC", PLUSSavesystem.read_ini("Discord", "RPC", "true") == "true");
            InitPLUSToggle("Debug", PLUSSavesystem.read_ini("Launch", "Debug", "true") == "true");
            InitPLUSToggle("SteamLaunch", PLUSSavesystem.read_ini("Launch", "Steam", "false") == "true");
            InitPLUSToggle("ModUpdater", PLUSSavesystem.read_ini("LowEnd", "ModUpdate", "true") == "true");
            InitPLUSToggle("POLanguage", PLUSSavesystem.read_ini("Files", "POLanguage", "true") == "true");
            InitPLUSToggle("Startup", RegistryConfig.GetStartupStatus() == "Enabled");

            string[] brushNames = { "Primary", "Secondary", "Inner", "Loading", "Text" };

            foreach (var name in brushNames)
            {
                string key = name;

                if (!defaultBrushHexes.ContainsKey(key))
                {
                    Themes_Defaults(name, PLUSThemes.Get_BrushColorAsHex($"{name}Brush"));
                }
                Theme_Update(name, true);
            }
        }

        private void SoundVolume_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            int volume = (int)SoundVolume.Value;
            PLUSSavesystem.write_ini("Audio", "SoundVolume", volume.ToString());
            PLUSMUSIC.ApplyCurrentVolume();
        }

        public void POOverwriteBackup_Click(object sender, RoutedEventArgs e)
        {
            var targetfile = $"{Global.config.ModsFolder}{Global.s}data.win.po";
            var targetcopy = $"{Global.config.ModsFolder}{Global.s}data.win";
            MessageBoxResult result = MessageBoxResult.Yes;
            if (File.Exists(targetfile))
            {
                result = MessageBox.Show("Do you want to overwrite current one?", "Confirm Overwrite", MessageBoxButton.YesNo, MessageBoxImage.Question);
            }
            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    if (File.Exists(targetcopy))
                    {
                        File.Copy(targetcopy, targetfile, true);
                        MessageBox.Show("Backup successful!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    else
                    {
                        MessageBox.Show($"Failed to backup: can't find data.win", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Failed to backup: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        public async void GMLoaderConvert_Click(object sender, RoutedEventArgs e)
        {
            if (Global.ronnietutorial)
            {
                return;
            }
            string GMLoaderFolder = $"{AppDomain.CurrentDomain.BaseDirectory}GMLoader";
            string[] foldersToDelete = {
            $"{GMLoaderFolder}{Global.s}Export",
            $"{GMLoaderFolder}{Global.s}vanilla_export",
            $"{GMLoaderFolder}{Global.s}modded_export",
            $"{GMLoaderFolder}{Global.s}converted_output",
            $"{GMLoaderFolder}{Global.s}GMLoader.txt"
            };

            var runningGMLoaders = Process.GetProcessesByName("GMLoader");
            foreach (var proc in runningGMLoaders)
            {
                try
                {
                    proc.Kill();
                    proc.WaitForExit();
                }
                catch
                {

                }
            }

            foreach (var folder in foldersToDelete)
            {
                if (Directory.Exists(folder))
                {
                    Directory.Delete(folder, recursive: true);
                }
            }
            ModManager.IsSelected = true;
            MessageBox.Show("Please select the base data.win file. After that, select the modded file, which can be either a .xdelta or a .win. Please do not close the tool while processing, as it may take a long time.", "GMLoader Convert", MessageBoxButton.OK, MessageBoxImage.Information);
            var sourceDialog = new OpenFileDialog();
            sourceDialog.Filter = "Source (*.win)|*.win";

            string source = "";

            if (sourceDialog.ShowDialog() == true)
            {
                source = sourceDialog.FileName;
            }
            else
            {
                MessageBox.Show("No file selected.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }


            var moddedDialog = new OpenFileDialog();
            moddedDialog.Filter = "Modded (*.xdelta;*.win)|*.xdelta;*.win";

            string modded = "";

            if (moddedDialog.ShowDialog() == true)
            {
                modded = moddedDialog.FileName;
            }
            else
            {
                MessageBox.Show("No file selected.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            File.Copy(source, $"{source}.GMLoader", true);
            File.Copy(modded, $"{modded}.GMLoader", true);

            File.Move($"{source}.GMLoader", $"{GMLoaderFolder}{Global.s}vanilla.win", true);
            File.Move($"{modded}.GMLoader", $"{GMLoaderFolder}{Global.s}modded{Path.GetExtension(modded)}", true);
            string newsource = $"{GMLoaderFolder}{Global.s}vanilla.win";
            string newmodded = $"{GMLoaderFolder}{Global.s}modded{Path.GetExtension(modded)}";
            var xdelta = $"{Global.assemblyLocation}{Global.s}Dependencies{Global.s}xdelta.exe";
            if (File.Exists($"{GMLoaderFolder}{Global.s}modded.xdelta"))
            {
                try
                {
                    Global.logger.WriteLine("Attempting to Patch Modded to Source", LoggerType.Info);
                    ModLoader.PathFixPatch(newsource, newmodded, $"{newmodded}.temp", xdelta);
                    File.Move($"{newmodded}.temp", $"{GMLoaderFolder}{Global.s}modded.win", true);
                    File.Delete($"{GMLoaderFolder}{Global.s}modded.xdelta");
                }
                catch
                {
                    File.Delete($"{newsource}");
                    File.Delete($"{newmodded}");
                    Global.logger.WriteLine("Failed to Patch Modded to Source", LoggerType.Error);
                    return;
                }
            }
            Global.logger.WriteLine("Sucessfully Patch Modded to Source", LoggerType.Info);

            string gmLoaderExe = Path.Combine(GMLoaderFolder, "GMLoader.exe");

            ProcessStartInfo startInfo = new ProcessStartInfo
            {
                FileName = gmLoaderExe,
                Arguments = "-convert",
                WorkingDirectory = Path.GetDirectoryName(gmLoaderExe)!,
                UseShellExecute = false,
                RedirectStandardOutput = false,
                RedirectStandardError = false,
                CreateNoWindow = false
            };

            using (Process process = new Process())
            {
                process.StartInfo = startInfo;

                process.Start();
                string outputFolder = Path.Combine(GMLoaderFolder, "converted_output");

                string[] GMLoaderFOLDERS = { "audio", "code", "lib", "csx", "room", "shader", "texture", "xdelta" };
                bool anyExist = false;
                bool noGMLoaderFolders = true;
                bool emptydir = true;

                while (true)
                {
                    if (process.HasExited)
                        break;
                    emptydir = false;
                    string convertedOutput = Path.Combine(GMLoaderFolder, "converted_output");

                    if (Directory.Exists(convertedOutput))
                    {
                        foreach (var folder in GMLoaderFOLDERS)
                        {
                            string path = Path.Combine(convertedOutput, folder);
                            if (Directory.Exists(path))
                            {
                                noGMLoaderFolders = false;
                            }
                        }

                        foreach (var subFolder in Directory.GetDirectories(convertedOutput))
                        {
                            var entries = Directory.EnumerateFileSystemEntries(subFolder).ToArray();

                            if (entries.Length <= 0)
                            {
                                emptydir = true;
                            }
                        }
                        if (!emptydir)
                        {
                            if (!noGMLoaderFolders)
                            {
                                Thread.Sleep(1000);
                                anyExist = true;
                                Thread.Sleep(1000);
                                break;
                            }
                        }
                    }



                    if (anyExist)
                        break;

                    Thread.Sleep(1000);
                }



                if (!anyExist)
                {
                    MessageBox.Show("GMLoader exited before producing output. Please be patient if you closed it", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
                else
                {
                    process.Kill();
                    string basePath = $"{Global.assemblyLocation}{Global.s}Mods{Global.s}{Path.GetFileName(modded)} - GMLoader";
                    string finalPath = basePath;

                    int i = 1;
                    while (Directory.Exists(finalPath))
                    {
                        finalPath = $"{basePath} ({i++})";
                    }

                    Directory.Move(outputFolder, finalPath);
                    foreach (var folder in foldersToDelete)
                    {
                        if (Directory.Exists(folder))
                        {
                            Directory.Delete(folder, recursive: true);
                        }
                    }
                    if (File.Exists($"{newsource}"))
                        File.Delete($"{newsource}");
                    if (File.Exists($"{newmodded}"))
                        File.Delete($"{newmodded}");
                }

            }
        }
        public async void ReplayTutorial_Click(object sender, RoutedEventArgs e)
        {
            if (replayanimator != null || Global.ronnietutorial)
            {
                return;
            }
            PLUSSavesystem.write_ini("Tutorial", "BrokenModSkip", "false");
            PLUSSavesystem.write_ini("Tutorial", "SettingsSection", "false");
            MessageBoxResult result = MessageBox.Show("Do you want to replay the tutorial?", "Confirm Replay", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (result == MessageBoxResult.Yes)
            {
                PLUSSavesystem.write_ini("Tutorial", "Replay", "true");
                PLUSSavesystem.write_ini("Tutorial", "ForcedReplay", "false");
                PLUSSavesystem.write_ini("Tutorial", "Finished", "false");
                replayanimator = new PLUSRonnieAnimate();
                replayanimator.Initialize(this, this.Width / 6, -100, 1.5);
                replayanimator.SetExpression("happy");

                replayanimator.GlideTo(this.Width / 6, 250, 40);

                await PLUSWait.WaitUntil(() => replayanimator.GetY() >= 250);
                var curtextbox = replayanimator.MakeTextbox(replayanimator.GetX() + 110, replayanimator.GetY() + 25, "TAKE IT FROM THE TOP");
                await PLUSWait.WaitSeconds(5);
                PLUSTutorial.TutorialState("false");
            }
            else
            {
                PLUSTutorial.RonnieVariables.DeclineReplay += 1;
                replayanimator = new PLUSRonnieAnimate();
                replayanimator.Initialize(this, this.Width / 6, -100, 1.5);
                var curtextbox = replayanimator.MakeTextbox(replayanimator.GetX() + 110, replayanimator.GetY() + 25, "");
                replayanimator.DestroyTextbox(curtextbox);
                replayanimator.GlideTo(this.Width / 6, 250, 40);
                replayanimator.SetExpression("sad");
                await PLUSWait.WaitUntil(() => replayanimator.GetY() >= 250);
                if (PLUSTutorial.RonnieVariables.DeclineReplay == 3)
                {
                    replayanimator.DestroyTextbox(curtextbox);
                    curtextbox = replayanimator.MakeTextbox(replayanimator.GetX() + 110, replayanimator.GetY() + 25, "Stop it! or else");
                    await PLUSWait.WaitSeconds(3);
                    replayanimator.DestroyTextbox(curtextbox);
                }
                else if (PLUSTutorial.RonnieVariables.DeclineReplay > 3)
                {
                    PLUSSavesystem.write_ini("Tutorial", "Replay", "false");
                    PLUSSavesystem.write_ini("Tutorial", "ForcedReplay", "true");
                    PLUSSavesystem.write_ini("Tutorial", "Finished", "false");
                    replayanimator.DestroyTextbox(curtextbox);
                    curtextbox = replayanimator.MakeTextbox(replayanimator.GetX() + 110, replayanimator.GetY() + 25, "You asked for this");
                    await PLUSWait.WaitSeconds(3);
                    replayanimator.DestroyTextbox(curtextbox);
                    PLUSTutorial.TutorialState("false");
                }
                replayanimator.GlideTo(this.Width / 6, -250, 40);
                await PLUSWait.WaitUntil(() => replayanimator.GetY() <= -250);
                replayanimator.Destroy();
                replayanimator = null;
            }
        }
        public void OpenPTFolder_Click(object sender, RoutedEventArgs e)
        {
            Process process = Process.Start("explorer.exe", Global.config.ModsFolder);
        }
        public void ChooseNewPTFolder_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Pressing Setup will reset it to finding your default steam folder, launch without setup if need to");
            string defaultPath = String.Empty;
            OpenFileDialog dialog = new OpenFileDialog();
            dialog.DefaultExt = ".exe";
            dialog.Filter = $"Executable File (PizzaTower.exe)|PizzaTower.exe";
            dialog.Title = $"Select PizzaTower.exe from your Steam Install folder";
            dialog.Multiselect = false;
            dialog.InitialDirectory = Global.assemblyLocation;
            dialog.ShowDialog();
            if (!String.IsNullOrEmpty(dialog.FileName)
                && Path.GetFileName(dialog.FileName).Equals("PizzaTower.exe", StringComparison.InvariantCultureIgnoreCase))
                defaultPath = dialog.FileName;
            else if (!String.IsNullOrEmpty(dialog.FileName))
            {
                Global.logger.WriteLine($"PizzaTower.exe not found", LoggerType.Error);
                return;
            }
            Global.config.ModsFolder = Path.GetDirectoryName(defaultPath);
            Global.config.Launcher = defaultPath;
            Global.UpdateConfig();
        }
        public void KillGMLoader_Click(object sender, RoutedEventArgs e)
        {
            var runningGMLoaders = Process.GetProcessesByName("GMLoader");
            foreach (var proc in runningGMLoaders)
            {
                try
                {
                    proc.Kill();
                    proc.WaitForExit();
                }
                catch
                {

                }
            }
        }

        public async void DowngradeDownload_Click(object sender, RoutedEventArgs e)
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

            string selectedVersion = DowngradeDownloadCombo.SelectedItem as string;
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
                    bool success = await PLUSDepotDownloader.DownloadDowngradeAsync("2231450", "2231451", v.manifestID, steamUser, tempDir, ogWinFile, v.version);

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

        public void SavePTFolderMod_Click(object sender, RoutedEventArgs e)
        {
            Dictionary<string, bool> includedmodfiles = new Dictionary<string, bool>()
            {
                { "Data.win", true },
                { "Language Files", false },
                { "Banks", false },
                { "DLLs", false },
                { "Credits", false }
            };
            Window win = new Window
            {
                Title = "Create Mod",
                Width = 320,
                Height = 360,
                WindowStartupLocation = WindowStartupLocation.CenterScreen,
                Background = (Brush)Application.Current.Resources["PrimaryBrush"]
            };

            StackPanel panel = new StackPanel
            {
                Margin = new Thickness(10)
            };

            TextBox nameBox = new TextBox
            {
                Height = 30,
                Margin = new Thickness(0, 0, 0, 10),
                Background = (Brush)Application.Current.Resources["PrimaryBrush"],
                Foreground = (Brush)Application.Current.Resources["TextBrush"],
                BorderBrush = (Brush)Application.Current.Resources["TextBrush"],
                CaretBrush = (Brush)Application.Current.Resources["TextBrush"],
                Text = "Enter Mod Name..."
            };

            panel.Children.Add(nameBox);

            void AddOption(string key, bool isEnabled)
            {
                CheckBox cb = new CheckBox
                {
                    Content = key,
                    IsChecked = includedmodfiles.ContainsKey(key) && includedmodfiles[key],
                    IsEnabled = isEnabled,
                    Foreground = (Brush)Application.Current.Resources["TextBrush"],
                    Margin = new Thickness(0, 5, 0, 5)
                };

                cb.Checked += (s, ev) => includedmodfiles[key] = true;
                cb.Unchecked += (s, ev) => includedmodfiles[key] = false;

                panel.Children.Add(cb);
            }

            AddOption("Data.win", false);
            AddOption("Language Files", true);
            AddOption("Banks", true);
            AddOption("DLLs", true);
            AddOption("Credits", true);

            TextBlock errorText = new TextBlock
            {
                Foreground = Brushes.Red,
                Margin = new Thickness(0, 10, 0, 0)
            };

            panel.Children.Add(errorText);

            Button ok = new Button
            {
                Content = "OK",
                Height = 30,
                Margin = new Thickness(0, 10, 0, 0),
                Background = (Brush)Application.Current.Resources["PrimaryBrush"],
                Foreground = (Brush)Application.Current.Resources["TextBrush"],
                BorderBrush = (Brush)Application.Current.Resources["TextBrush"]
            };

            ok.Click += (s, ev) =>
            {
                string modName = nameBox.Text.Trim();

                if (string.IsNullOrWhiteSpace(modName) || modName == "Enter Mod Name...")
                {
                    errorText.Text = "Mod name is required.";
                    return;
                }

                string path = System.IO.Path.Combine($"{Global.assemblyLocation}{Global.s}Mods", modName);

                if (System.IO.Directory.Exists(path))
                {
                    errorText.Text = "Mod folder already exists.";
                    return;
                }

                Directory.CreateDirectory(path);
                win.Close();
                if (includedmodfiles["Data.win"])
                {
                    File.Copy(
                        $"{Global.config.ModsFolder}{Global.s}data.win",
                        $"{path}{Global.s}data.win",
                        true
                    );
                }

                if (includedmodfiles["Language Files"])
                {
                    string langSource = $"{Global.config.ModsFolder}{Global.s}lang";
                    string langDest = $"{path}{Global.s}lang";

                    if (Directory.Exists(langSource))
                    {
                        Directory.CreateDirectory(langDest);

                        foreach (var file in Directory.GetFiles(langSource, "*.*", SearchOption.AllDirectories))
                        {
                            var destFile = file.Replace(langSource, langDest);
                            Directory.CreateDirectory(Path.GetDirectoryName(destFile)!);
                            File.Copy(file, destFile, true);
                        }
                    }
                }

                if (includedmodfiles["Banks"])
                {
                    string banksSource = $"{Global.config.ModsFolder}{Global.s}sound{Global.s}Desktop";
                    string banksDest = $"{path}{Global.s}sound{Global.s}Desktop";

                    if (Directory.Exists(banksSource))
                    {
                        Directory.CreateDirectory(banksDest);

                        foreach (var file in Directory.GetFiles(banksSource, "*.*", SearchOption.AllDirectories))
                        {
                            var destFile = file.Replace(banksSource, banksDest);
                            Directory.CreateDirectory(Path.GetDirectoryName(destFile)!);
                            File.Copy(file, destFile, true);
                        }
                    }
                }

                if (includedmodfiles["DLLs"])
                {
                    string dllSource = $"{Global.config.ModsFolder}";

                    if (Directory.Exists(dllSource))
                    {
                        foreach (var file in Directory.GetFiles(dllSource, "*.dll", SearchOption.TopDirectoryOnly))
                        {
                            var fileName = Path.GetFileName(file);
                            File.Copy(file, $"{path}{Global.s}{fileName}", true);
                        }
                    }
                }

                if (includedmodfiles["Credits"])
                {
                    string creditsSourceDir = $"{Global.config.ModsFolder}";

                    if (Directory.Exists(creditsSourceDir))
                    {
                        foreach (var file in Directory.GetFiles(creditsSourceDir, "*credits*.txt", SearchOption.AllDirectories))
                        {
                            var relativePath = file.Replace(creditsSourceDir, "");
                            var destFile = $"{path}{Global.s}{relativePath}";

                            Directory.CreateDirectory(Path.GetDirectoryName(destFile)!);
                            File.Copy(file, destFile, true);
                        }
                    }
                }
            };

            panel.Children.Add(ok);

            win.Content = panel;
            win.ShowDialog();
        }
        public System.Windows.Media.Color? Themes_GrabColor(string initialHex = "#FFFFFFFF")
        {
            System.Windows.Forms.ColorDialog colorDialog = new System.Windows.Forms.ColorDialog();

            colorDialog.Color = System.Drawing.ColorTranslator.FromHtml(initialHex);

            if (colorDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                System.Drawing.Color drawingColor = colorDialog.Color;

                System.Windows.Media.Color wpfColor = System.Windows.Media.Color.FromArgb(drawingColor.A, drawingColor.R, drawingColor.G, drawingColor.B);

                return wpfColor;
            }
            else
            {
                return null;
            }
        }
        public void Theme_Update(string brushname, bool skippicker = false)
        {
            if (skippicker)
            {
                var skippedcolor = PLUSSavesystem.read_ini("Themes", brushname, "");
                if (skippedcolor != "" && PLUSThemes.validhex(skippedcolor))
                    PLUSThemes.Set_BrushColor($"{brushname}Brush", skippedcolor);
                return;
            }
            var color = Themes_GrabColor(PLUSSavesystem.read_ini("Themes", brushname, "#FFFFFFFF")) ?? System.Windows.Media.Colors.Transparent;
            if (color != System.Windows.Media.Colors.Transparent)
            {
                var rgbcolor = PLUSThemes.color_as_rgb(color);
                var stringcolor = PLUSThemes.rgb_to_hex(rgbcolor.r, rgbcolor.g, rgbcolor.b);
                PLUSSavesystem.write_ini("Themes", brushname, stringcolor);
                PLUSThemes.Set_BrushColor($"{brushname}Brush", stringcolor);
            }
        }
        public void Themes_Defaults(string brushName, string hex)
        {
            if (string.IsNullOrWhiteSpace(brushName) || string.IsNullOrWhiteSpace(hex))
                return;

            defaultBrushHexes[brushName] = hex;
        }
        public void Themes_Reset(string brushname)
        {
            PLUSSavesystem.write_ini("Themes", brushname, "");
            PLUSSavesystem.delete_ini_value("Themes", brushname);
            PLUSThemes.Set_BrushColor($"{brushname}Brush", defaultBrushHexes[brushname]);
        }
        public void Theme_Click(object sender, RoutedEventArgs e)
        {
            var name = (sender as FrameworkElement)?.Name;
            if (string.IsNullOrEmpty(name))
                return;

            var key = name.Replace("Themes", "").Replace("Reset", "");

            Theme_Update(key);
        }
        public void ThemeReset_Click(object sender, RoutedEventArgs e)
        {
            var name = (sender as FrameworkElement)?.Name;
            if (string.IsNullOrEmpty(name)) 
                return;

            var key = name.Replace("Themes", "").Replace("Reset", "");

            Themes_Reset(key);
        }
        public void AddPatchNotes(string version, string[] topNotes, string[] notes, string[] catnotes, bool warnupdate, string timeago)
        {
            var localVersion = Assembly.GetExecutingAssembly().GetName().Version.ToString();

            var versionText = new TextBlock
            {
                Text = version,
                FontSize = 24,
                FontWeight = FontWeights.Bold,
                Margin = new Thickness(0, 10, 0, 5),
                Foreground = (Brush)FindResource("TextBrush")
            };

            var titlerowPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Margin = new Thickness(0, 2, 0, 2)
            };

            var timeagolabel = new Label
            {
                FontSize = 15,
                Background = new SolidColorBrush(Color.FromRgb(60, 64, 68)),
                Padding = new Thickness(5, 2, 5, 2),
                Margin = new Thickness(5, 2, 5, 2),
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Left,
                Foreground = Brushes.White
            };
            var timeagolabelcontentBinding = new Binding
            {
                Source = timeago,
                Mode = BindingMode.OneWay
            };
            timeagolabel.SetBinding(Label.ContentProperty, timeagolabelcontentBinding);

            titlerowPanel.Children.Add(versionText);
            titlerowPanel.Children.Add(timeagolabel);

            if (warnupdate)
            {
                var warnlabel = new Label
                {
                    FontSize = 15,
                    Background = new SolidColorBrush(Color.FromRgb(60, 64, 68)),
                    Padding = new Thickness(5, 2, 5, 2),
                    Margin = new Thickness(5, 2, 5, 2),
                    VerticalAlignment = VerticalAlignment.Center,
                    HorizontalAlignment = HorizontalAlignment.Left,
                    Foreground = Brushes.Red
                };
                var warncontentBinding = new Binding
                {
                    Source = $"v{localVersion.Substring(0, localVersion.LastIndexOf('.'))} is OUTDATED",
                    Mode = BindingMode.OneWay
                };
                warnlabel.SetBinding(Label.ContentProperty, warncontentBinding);
                titlerowPanel.Children.Add(warnlabel);
            }

            PatchNotesPanel.Children.Add(titlerowPanel);

            if (topNotes != null)
            {
                foreach (var topNote in topNotes)
                {
                    if (string.IsNullOrWhiteSpace(topNote))
                        continue;

                    var topNoteText = new TextBlock
                    {
                        Text = topNote,
                        FontSize = 15,
                        FontStyle = FontStyles.Italic,
                        Margin = new Thickness(5, 0, 0, 6),
                        Foreground = (Brush)FindResource("TextBrush")
                    };


                    PatchNotesPanel.Children.Add(topNoteText);
                }
            }
            if (notes != null)
            {
                for (int i = 0; i < notes.Length; i++)
                {
                    var noteText = new TextBlock
                    {
                        Text = "• " + notes[i],
                        FontSize = 14,
                        Margin = new Thickness(10, 2, 0, 2),
                        Foreground = (Brush)FindResource("TextBrush")
                    };

                    var catLabel = new Label
                    {
                        FontSize = 15,
                        Background = new SolidColorBrush(Color.FromRgb(60, 64, 68)),
                        Padding = new Thickness(5, 2, 5, 2),
                        Margin = new Thickness(5, 2, 5, 2),
                        VerticalAlignment = VerticalAlignment.Center,
                        HorizontalAlignment = HorizontalAlignment.Left
                    };

                    var contentBinding = new Binding
                    {
                        Source = catnotes[i],
                        Mode = BindingMode.OneWay
                    };
                    catLabel.SetBinding(Label.ContentProperty, contentBinding);

                    try
                    {
                        var converter = new CategoryColorConverter();

                        string category = catnotes[i];
                        var brush = (SolidColorBrush)converter.Convert(category, typeof(SolidColorBrush), null, System.Globalization.CultureInfo.InvariantCulture);

                        catLabel.Foreground = brush;
                    }
                    catch
                    {
                        catLabel.Foreground = Brushes.White;
                    }

                    var rowPanel = new StackPanel
                    {
                        Orientation = Orientation.Horizontal,
                        Margin = new Thickness(0, 2, 0, 2)
                    };

                    rowPanel.Children.Add(noteText);
                    rowPanel.Children.Add(catLabel);

                    PatchNotesPanel.Children.Add(rowPanel);
                }
            }
        }
        public async void CreatePatchNotes()
        {
            try
            { 
                PatchNotesPanel.Children.Clear();
                string url = "https://api.gamebanana.com/Core/Item/Data?itemtype=Tool&itemid=21866&fields=Updates().bSubmissionHasUpdates(),Updates().aGetLatestUpdates()&return_keys=1";

                using var client = new HttpClient();
                string jsonResponse = await client.GetStringAsync(url);

                using var doc = JsonDocument.Parse(jsonResponse);
                var root = doc.RootElement;

                if (!root.TryGetProperty("Updates().aGetLatestUpdates()", out var updatesArray))
                    return;

                var latest = updatesArray[0];

                string versionTitle = latest.GetProperty("_sTitle").GetString() ?? "";


                long ts = latest.GetProperty("_tsDateAdded").GetInt64();

                DateTimeOffset target = DateTimeOffset.FromUnixTimeSeconds(ts);
                TimeSpan diff = DateTimeOffset.UtcNow - target;


                var timeago = StringConverters.FormatTimeAgo(diff);

                var changelog = latest.GetProperty("_aChangeLog");
                string[] notes = new string[changelog.GetArrayLength()];
                string[] catnotes = new string[changelog.GetArrayLength()];
                int i = 0;
                foreach (var entry in changelog.EnumerateArray())
                {
                    string text = entry.GetProperty("text").GetString() ?? "";
                    string cat = entry.GetProperty("cat").GetString() ?? "";
                    catnotes[i] = cat;
                    notes[i++] = text;
                }

                string versionNumber = latest.GetProperty("_sVersion").GetString() ?? "";
                bool warnupdate = false;
                var localVersion = Assembly.GetExecutingAssembly().GetName().Version.ToString();
                Match onlineVersionMatch = Regex.Match(versionTitle, @"(?<version>([0-9]+\.?)+)[^a-zA-Z]");
                string onlineVersion = null;

                if (onlineVersionMatch.Success)
                {
                    onlineVersion = onlineVersionMatch.Value;
                    warnupdate = AutoUpdater.UpdateAvailable(onlineVersion, localVersion);
                }


                string[] topNotes = versionNumber switch
                {
                    "1.0.4" => new string[] { "Autoupdater should work from 1.0.3" },
                    "1.0.5" => new string[] { "Patch Notes Tab Introduction" },
                    _ => new string[] { "" }
                };

                AddPatchNotes(versionTitle, topNotes, notes, catnotes, warnupdate, timeago);
            }
            catch
            {
                AddPatchNotes("Failed to load", new string[] { "" }, new string[] { "Maybe Check your internet", "Maybe Gamebanana Servers are down"}, new string[] { "Addition", "Addition" }, false, "Failed to load");
            }
        }
        private void OnPatchNotesSelected(object sender, RoutedEventArgs e)
        {
            CreatePatchNotes();
        }

        public async void CheckLauncherUpdates_Click(object sender, RoutedEventArgs e)
        {
            var cts = new CancellationTokenSource();
            AutoUpdater.CheckForPizzaOvenUpdate(cts);
        }

        public void ThemesSave_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new Microsoft.Win32.SaveFileDialog
            {
                Title = "Save Theme",
                Filter = "PO Theme (*.potheme)|*.potheme",
                DefaultExt = ".potheme",
                AddExtension = true
            };

            bool? result = dialog.ShowDialog();

            if (result == true)
            {
                ThemesSaveFile(dialog.FileName);
            }

        }
        public void ThemesSaveFile(string themeFilePath)
        {
            var theme = new Dictionary<string, string>();

            theme["saveversion"] = Assembly.GetExecutingAssembly().GetName().Version.ToString();
            foreach (var brush in themebrushes)
            {
                theme[brush] = PLUSSavesystem.read_ini("Themes", brush, defaultBrushHexes[brush]);
            }

            theme["background"] = "";

            if (Directory.Exists(Global.customassetsfolder))
            {
                var bgImagePath = Directory.GetFiles(Global.customassetsfolder)
                    .FirstOrDefault(f => Path.GetFileNameWithoutExtension(f).Equals("background", StringComparison.OrdinalIgnoreCase) && themeimageExtensions.Contains(Path.GetExtension(f), StringComparer.OrdinalIgnoreCase));
                if (!string.IsNullOrEmpty(bgImagePath) && File.Exists(bgImagePath))
                {
                    theme["background"] = $"{Path.GetExtension(bgImagePath).TrimStart('.')};{PLUSThemes.Base64_SaveFile(bgImagePath)}";
                }
            }

            foreach (var transparent in transparentboxes)
            {
                var slider = (Slider)this.FindName($"Transparency_{transparent}");
                if (slider != null)
                    theme[$"Transparency_{transparent}"] = ((int)slider.Value).ToString();
            }

            string json = JsonSerializer.Serialize(theme, new JsonSerializerOptions
            {
                WriteIndented = true
            });
            File.WriteAllText(themeFilePath, json);
        }
        public void ThemesLoad_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new Microsoft.Win32.OpenFileDialog
            {
                Title = "Load Theme",
                Filter = "PO Theme (*.potheme)|*.potheme",
                DefaultExt = ".potheme",
                Multiselect = false
            };

            bool? result = dialog.ShowDialog();

            if (result == true)
            {
                ThemesFileLoad(dialog.FileName);
            }
        }
        public void ThemesFileLoad(string themeFilePath)
        {
            string json = System.IO.File.ReadAllText(themeFilePath);
            try
            {
                var theme = JsonSerializer.Deserialize<Dictionary<string, string>>(json);

                if (theme != null)
                {
                    try
                    {
                        foreach (var brush in themebrushes)
                        {
                            if (theme.ContainsKey(brush))
                            {
                                string value = theme[brush];
                                PLUSSavesystem.write_ini("Themes", brush, value);
                                Theme_Update(brush, true);
                            }
                        }
                        if (Directory.Exists(Global.customassetsfolder))
                        {
                            var bgImagePath = Directory.GetFiles(Global.customassetsfolder)
                                .FirstOrDefault(f => Path.GetFileNameWithoutExtension(f).Equals("background", StringComparison.OrdinalIgnoreCase) && themeimageExtensions.Contains(Path.GetExtension(f), StringComparer.OrdinalIgnoreCase));
                            if (!string.IsNullOrEmpty(bgImagePath) && File.Exists(bgImagePath))
                            {
                                System.IO.File.Delete(bgImagePath);
                            }
                        }

                        if (theme.ContainsKey("background"))
                        {
                            string[] backgroundata = theme["background"].Split(";");
                            if (backgroundata.Length == 2)
                            {
                                if (PLUSThemes.IsBase64String(backgroundata[1]))
                                {
                                    PLUSThemes.Base64_LoadFile(backgroundata[1], System.IO.Path.Combine(Global.customassetsfolder, $"background.{backgroundata[0]}"));
                                }
                            }
                        }
                        foreach (var transparent in transparentboxes)
                        {
                            if (theme.ContainsKey($"Transparency_{transparent}"))
                            {
                                PLUSSavesystem.write_ini("Themes", $"Transparency_{transparent}", theme[$"Transparency_{transparent}"]);
                            }
                            else
                            {
                                PLUSSavesystem.write_ini("Themes", $"Transparency_{transparent}", "100");
                            }
                            ApplyTransparentBoxes(true);
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Malformed Themes File: {ex}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
            catch { }
        }
        public void ThemesResetAll_Click(object sender, RoutedEventArgs e)
        {
            foreach (var brush in themebrushes)
            {
                Themes_Reset(brush);
            }
            if (Directory.Exists(Global.customassetsfolder))
            {
                var bgImagePath = Directory.GetFiles(Global.customassetsfolder)
                    .FirstOrDefault(f => Path.GetFileNameWithoutExtension(f).Equals("background", StringComparison.OrdinalIgnoreCase) && themeimageExtensions.Contains(Path.GetExtension(f), StringComparer.OrdinalIgnoreCase));
                if (!string.IsNullOrEmpty(bgImagePath) && File.Exists(bgImagePath))
                {
                    System.IO.File.Delete(bgImagePath);
                }
            }
            foreach (var transparent in transparentboxes)
            {
                PLUSSavesystem.write_ini("Themes", $"Transparency_{transparent}", "100");
                ApplyTransparentBoxes(true);
            }
        }
        public void ThemePresetsApply_Click(object sender, RoutedEventArgs e)
        {
            var theme = ThemePresetsCombo.SelectedItem as string;
            var filepath = $"{Global.assemblyLocation}{Global.s}Themes{Global.s}{theme}.potheme";
            if (File.Exists(filepath))
            {
                ThemesFileLoad(filepath);
            }
        }
        public void ThemesBackgroundUpload_Click(object sender, RoutedEventArgs e)
        {
            string filterExtensions = string.Join(";", themeimageExtensions.Select(ext => $"*{ext}"));
            string filter = $"Image Files ({filterExtensions})|{filterExtensions}";

            var dialog = new Microsoft.Win32.OpenFileDialog
            {
                Title = "Select Background",
                Filter = filter,
                DefaultExt = themeimageExtensions[0], 
                Multiselect = false 
            };

            bool? result = dialog.ShowDialog();

            if (result == true)
            {
                System.IO.File.Copy(dialog.FileName, System.IO.Path.Combine(Global.customassetsfolder, $"background{Path.GetExtension(dialog.FileName)}"), true);   
            }
        }
        public void ThemesBackgroundReset_Click(object sender, RoutedEventArgs e)
        {
            if (Directory.Exists(Global.customassetsfolder))
            {
                var bgImagePath = Directory.GetFiles(Global.customassetsfolder)
                    .FirstOrDefault(f => Path.GetFileNameWithoutExtension(f).Equals("background", StringComparison.OrdinalIgnoreCase) && themeimageExtensions.Contains(Path.GetExtension(f), StringComparer.OrdinalIgnoreCase));
                if (!string.IsNullOrEmpty(bgImagePath) && File.Exists(bgImagePath))
                {
                    System.IO.File.Delete(bgImagePath);
                }
            }
        }
    }
}

