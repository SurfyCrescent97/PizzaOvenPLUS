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
using System.DirectoryServices.ActiveDirectory;
using System.IO;
using System.Linq;
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
        public class ModJSON
        {
            public string title { get; set; }
            public string preview { get; set; }
            public string submitter { get; set; }
            public string avi { get; set; }
            public string upic { get; set; }
            public string caticon { get; set; }
            public string cat { get; set; }
            public string description { get; set; }
            public string filedescription { get; set; }
            public string homepage { get; set; }
            public DateTime lastupdate { get; set; }
        }
        public async Task WaitForRonnieModClick()
        {
            while (true)
            {
                var first = Global.config.ModList.FirstOrDefault(x => x.enabled);

                if (first != null && first.name == "Ronnie Oven Mod")
                    return;

                await Task.Delay(100);
            }
        }
        public static async Task WaitUntil(Func<bool> condition, int checkDelayMs = 16)
        {
            while (!condition())
                await Task.Delay(checkDelayMs);
        }

        public static async Task WaitSeconds(double seconds)
        {
            int ms = (int)(seconds * 1000);
            await Task.Delay(ms);
        }

        public static async Task<bool> WaitUntilOrTimeout(Func<bool> condition, double timeoutSeconds, int checkDelayMs = 16)
        {
            int elapsedMs = 0;
            int timeoutMs = (int)(timeoutSeconds * 1000);

            while (!condition())
            {
                if (elapsedMs >= timeoutMs)
                    return false;

                await Task.Delay(checkDelayMs);
                elapsedMs += checkDelayMs;
            }

            return true;
        }


        public static class RonnieVariables
        {
            public static bool BrokenModSkip = false;
            public static int ModLaunchAmount = 0;
            public static bool LauncherAllow = false;
            public static bool SetupAllow = false;
            public static bool AllowDownloadMod = false;
            public static bool RonnieModSkip = false;
            public static int SetupSucessful = -1;
            public static bool FinishedLaunch = false;
            public static bool FailedPatch = false;
            public static bool KeptMod = false;
            public static bool ModDeleted = false;
            public static int DeclineReplay = 0;
        }


        bool tutorialskip = false;

        private async Task DanceAsync(PLUSRonnieAnimate animator, int times, int delayMs = 200)
        {
            if (animator == null)
                return;

            for (int i = 0; i < times; i++)
            {
                animator.SetExpression("happy");
                await Task.Delay(delayMs);

                animator.SetExpression("pointerup");
                await Task.Delay(delayMs);

                animator.SetExpression("happy2");
                await Task.Delay(delayMs);

                animator.SetExpression("pointerup");
                await Task.Delay(delayMs);

            }
        }




        public static async Task<bool> WaitUntilTutorialDownloaded(int checkDelayMs = 16)
        {
            string modPath = $"{Global.assemblyLocation}{Global.s}Mods{Global.s}Ronnie Oven Mod";
            string jsonFile = Path.Combine(modPath, "mod.json");

            while (true)
            {
                if (Directory.Exists(modPath) && File.Exists(jsonFile))
                {
                    try
                    {
                        string jsonText = await File.ReadAllTextAsync(jsonFile);
                        using var doc = JsonDocument.Parse(jsonText);
                        var root = doc.RootElement;

                        if (root.TryGetProperty("homepage", out JsonElement homepage))
                        {
                            return homepage.ValueKind == JsonValueKind.Null ||
                                   string.IsNullOrWhiteSpace(homepage.GetString());
                        }
                    }
                    catch
                    {
                    }
                }

                await Task.Delay(checkDelayMs);
            }
        }

        public static string TutorialModPath()
        {
            var currentModDirectory = $@"{Global.assemblyLocation}{Global.s}Mods";

            foreach (var mod in Directory.GetDirectories(currentModDirectory))
            {
                string jsonFile = Path.Combine(mod, "mod.json");

                if (!File.Exists(jsonFile))
                    continue;

                try
                {
                    string jsonText = File.ReadAllText(jsonFile);
                    using var doc = JsonDocument.Parse(jsonText);
                    var root = doc.RootElement;

                    string preview = root.TryGetProperty("preview", out var p) ? p.GetString() : null;
                    string avi = root.TryGetProperty("avi", out var a) ? a.GetString() : null;
                    string upic = root.TryGetProperty("upic", out var u) ? u.GetString() : null;

                    if (preview == "pack://application:,,,/PizzaOven;component/TutorialMod/mod.png" ||
                        avi == "pack://application:,,,/PizzaOven;component/TutorialMod/profile.png" ||
                        upic == "pack://application:,,,/PizzaOven;component/TutorialMod/upic.gif")
                    {
                        return mod;
                    }
                }
                catch
                {
                }
            }

            return "";
        }

        public static bool IsTutorialDownloaded()
        {
            string modPath = $"{Global.assemblyLocation}{Global.s}Mods{Global.s}Ronnie Oven Mod";
            string jsonFile = Path.Combine(modPath, "mod.json");

            if (Directory.Exists(modPath) && File.Exists(jsonFile))
            {
                try
                {
                    string jsonText = File.ReadAllText(jsonFile);
                    using var doc = JsonDocument.Parse(jsonText);
                    var root = doc.RootElement;

                    if (root.TryGetProperty("homepage", out JsonElement homepage))
                    {
                        return homepage.ValueKind == JsonValueKind.Null ||
                               string.IsNullOrWhiteSpace(homepage.GetString());
                    }
                }
                catch
                {
                }
            }

            return false;
        }
        private async Task RunIntro()
        {
            var curtextbox = introanimator.MakeTextbox(introanimator.GetX() + 110, introanimator.GetY() + 25, "");
            if (!RonnieVariables.ModDeleted)
            {
                introanimator.SetExpression("happy2");
                introanimator.DestroyTextbox(curtextbox);
                curtextbox = introanimator.MakeTextbox(introanimator.GetX() + 110, introanimator.GetY() + 25, "I saw that broken mod's thumbnail and deleted the mod for you! You are welcome");
                await WaitSeconds(5);
            }

            if (RonnieVariables.KeptMod)
            {
                introanimator.SetExpression("inspect");
                introanimator.DestroyTextbox(curtextbox);
                curtextbox = introanimator.MakeTextbox(introanimator.GetX() + 110, introanimator.GetY() + 25, "I also know you kept the mod and played it after the tutorial");
                await WaitSeconds(5);
            }
            introanimator.DestroyTextbox(curtextbox);
            introanimator.SetExpression("normal");
            curtextbox = introanimator.MakeTextbox(introanimator.GetX() + 110, introanimator.GetY() + 25, "Safe Travels");
            var _followtimer = new DispatcherTimer();
            _followtimer.Interval = TimeSpan.FromSeconds(0.01);
            _followtimer.Tick += (s, e) =>
            {
                Canvas.SetLeft(introanimator.GetTextbox(curtextbox), introanimator.GetX() + 110);
                Canvas.SetTop(introanimator.GetTextbox(curtextbox), introanimator.GetY() + 25);
            };
            _followtimer.Start();


            introanimator.GlideTo((this.Width / 2), -300, 5);
            await WaitUntil(() => introanimator.GetY() <= 0);
            _followtimer.Stop();
            introanimator.DestroyTextbox(curtextbox);
            await WaitUntil(() => introanimator.GetY() <= -300);

            introanimator.Destroy();
        }

        public class PLUSAnnouncement
        {
            public DateTime date { get; set; }
            public bool enabled { get; set; }
            public string message { get; set; }
            public string expression { get; set; }
            public bool shake { get; set; }
            public string url { get; set; }
        }
        public static async Task<PLUSAnnouncement> GetLatestAnnouncement()
        {
            string url = "https://raw.githubusercontent.com/SurfyCrescent97/PizzaOvenPLUS/main/announcements.json";

            using HttpClient client = new HttpClient();
            string json = await client.GetStringAsync(url);

            var announcement = JsonSerializer.Deserialize<PLUSAnnouncement>(json);

            return announcement;
        }


        private async Task RunTutorial()
        {
            if (File.Exists(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "PizzaTower_GM2", "RonnieTutorial.ini")))
            {
                File.Delete(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "PizzaTower_GM2", "RonnieTutorial.ini"));
            }

            TutorialPanel.Visibility = Visibility.Collapsed;

            tutorialanimator = new PLUSRonnieAnimate();
            tutorialanimator.Initialize(this, this.Width / 6, -100, 1.5);
            ModBrowser.IsEnabled = false;
            Settings.IsEnabled = false;

            this.SizeChanged += (s, e) =>
            {
                tutorialanimator._overlayCanvas.Width = this.ActualWidth;
                tutorialanimator._overlayCanvas.Height = this.ActualHeight;
            };


            tutorialanimator.GlideTo(this.Width / 6, 250, 30);

            await WaitUntil(() => tutorialanimator.GetY() >= 250);

            tutorialanimator.SetExpression("normal");
            PLUSMUSIC.Play_TutorialMusic();
            var curtextbox = tutorialanimator.MakeTextbox(tutorialanimator.GetX() + 110, tutorialanimator.GetY() + 25, "Hi!! Hello!! Hi!![click to proceed]");

            await tutorialanimator.WaitForClickOnImageAsync();

            tutorialanimator.SetExpression("dumb");
            tutorialanimator.DestroyTextbox(curtextbox);
            curtextbox = tutorialanimator.MakeTextbox(tutorialanimator.GetX() + 110, tutorialanimator.GetY() + 25, "I'm Ronnie, Ronnie the Oven!");
            RonnieVariables.RonnieModSkip = IsTutorialDownloaded();
            if (!RonnieVariables.RonnieModSkip)
            {
                PLUSSavesystem.write_ini("Tutorial", "BrokenModSkip", "false");        
                await tutorialanimator.WaitForClickOnImageAsync();
                tutorialanimator.SetExpression("happy2");
                tutorialanimator.DestroyTextbox(curtextbox);
                curtextbox = tutorialanimator.MakeTextbox(tutorialanimator.GetX() + 110, tutorialanimator.GetY() + 25, "Welcome to PizzaOVEN+ (Plus)");

                await tutorialanimator.WaitForClickOnImageAsync();
                tutorialanimator.SetExpression("inspect");
                tutorialanimator.DestroyTextbox(curtextbox);
                curtextbox = tutorialanimator.MakeTextbox(tutorialanimator.GetX() + 110, tutorialanimator.GetY() + 25, "Nice to meet you, Random Pizza Tower fan! or so I think you are");

                await tutorialanimator.WaitForClickOnImageAsync();
                tutorialanimator.SetExpression("happy2");
                tutorialanimator.DestroyTextbox(curtextbox);
                curtextbox = tutorialanimator.MakeTextbox(tutorialanimator.GetX() + 110, tutorialanimator.GetY() + 25, "I will be your personnel and annoying guide to understand this wacky and wonderful tool!\r\n");

                await tutorialanimator.WaitForClickOnImageAsync();
                tutorialanimator.SetExpression("normal");
                tutorialanimator.DestroyTextbox(curtextbox);
                curtextbox = tutorialanimator.MakeTextbox(tutorialanimator.GetX() + 110, tutorialanimator.GetY() + 25, "What's this for you ask? You are using a Pizza Oven extension! It's basically the same thing but with more stuff.");

                await tutorialanimator.WaitForClickOnImageAsync();
                tutorialanimator.SetExpression("normal");
                tutorialanimator.DestroyTextbox(curtextbox);
                curtextbox = tutorialanimator.MakeTextbox(tutorialanimator.GetX() + 110, tutorialanimator.GetY() + 25, "Allow me to give you a bunch of carefree tips to make your life easier! Consider me as a useful buddy.\r\n");

                await tutorialanimator.WaitForClickOnImageAsync();
                tutorialanimator.SetExpression("sad");
                tutorialanimator.DestroyTextbox(curtextbox);
                curtextbox = tutorialanimator.MakeTextbox(tutorialanimator.GetX() + 110, tutorialanimator.GetY() + 25, "You can choose to hate me but I don't really care because I don't make much friends anyway");

                await tutorialanimator.WaitForClickOnImageAsync();
                tutorialanimator.SetExpression("happy");
                tutorialanimator.DestroyTextbox(curtextbox);
                curtextbox = tutorialanimator.MakeTextbox(tutorialanimator.GetX() + 110, tutorialanimator.GetY() + 25, "but in case you don't, GREAT! Let me show you around...");

                await tutorialanimator.WaitForClickOnImageAsync();
                tutorialanimator.SetExpression("sad");
                tutorialanimator.DestroyTextbox(curtextbox);
                curtextbox = tutorialanimator.MakeTextbox(tutorialanimator.GetX() + 110, tutorialanimator.GetY() + 25, "If you'd like to skip the tutorial the button is there... heheh... or you can click me and listen to me");

                await tutorialanimator.MakeSkipButtonAsync(tutorialanimator._overlayCanvas, () =>
                {
                    tutorialskip = true;
                });


                tutorialanimator._overlayCanvas.IsHitTestVisible = false;


                if (tutorialskip)
                {
                    PLUSMUSIC.FadeOutTutorialMusic();
                    if (PLUSSavesystem.read_ini("Tutorial", "Replay", "false") == "true")
                    {
                        PLUSSavesystem.write_ini("Tutorial", "ReplaySkip", "true"); // now your doing it on purpose
                    }
                    else
                    {
                        PLUSSavesystem.write_ini("Tutorial", "Skip", "true"); // (?) Ronnie will remember that you
                    }
                    PLUSSavesystem.write_ini("Tutorial", "Finished", "true");
                    tutorialanimator.DestroyTextbox(curtextbox);
                    curtextbox = tutorialanimator.MakeTextbox(tutorialanimator.GetX() + 110, tutorialanimator.GetY() + 25, "Oh.. I guess I'll leave sigh");
                    tutorialanimator.GlideTo((this.Width / 6), 100, 1);
                    await WaitUntil(() => tutorialanimator.GetY() <= 100);

                    tutorialanimator.DestroyTextbox(curtextbox);
                    curtextbox = tutorialanimator.MakeTextbox(tutorialanimator.GetX() + 110, tutorialanimator.GetY() + 25, "Are you sure?");

                    await WaitSeconds(3);

                    tutorialanimator.DestroyTextbox(curtextbox);
                    curtextbox = tutorialanimator.MakeTextbox(tutorialanimator.GetX() + 110, tutorialanimator.GetY() + 25, "No no you're right bye...");
                    tutorialanimator.GlideTo(tutorialanimator.GetX(), -100, 1);

                    await WaitUntil(() => tutorialanimator.GetY() <= -100);

                    TutorialState();
                }
                else
                {
                    tutorialanimator.SetExpression("happy2");
                    tutorialanimator.DestroyTextbox(curtextbox);
                    curtextbox = tutorialanimator.MakeTextbox(tutorialanimator.GetX() + 110, tutorialanimator.GetY() + 25, "REALLY WOWOWOW");

                    await tutorialanimator.WaitForClickOnImageAsync();

                    tutorialanimator.SetExpression("happy2");
                    tutorialanimator.DestroyTextbox(curtextbox);
                    curtextbox = tutorialanimator.MakeTextbox(tutorialanimator.GetX() + 110, tutorialanimator.GetY() + 25, "THIS MAKES ME WANNA DANCE");
                    await DanceAsync(tutorialanimator, 10, 150);

                    tutorialanimator.SetExpression("thinking");
                    tutorialanimator.DestroyTextbox(curtextbox);
                    curtextbox = tutorialanimator.MakeTextbox(tutorialanimator.GetX() + 110, tutorialanimator.GetY() + 25, "Let's start off by looking for a mod to download.");

                    Point relativePoint = ModBrowser.TransformToAncestor(this)
                                    .Transform(new Point(0, 0));

                    double tabX = relativePoint.X + 100;
                    double tabY = relativePoint.Y;

                    tutorialanimator.SetExpression("pointerup");
                    tutorialanimator.MoveTo(tabX - 50, tabY + 50);
                    tutorialanimator.DestroyTextbox(curtextbox);
                    curtextbox = tutorialanimator.MakeTextbox(tutorialanimator.GetX() + 110, tutorialanimator.GetY() + 25, "Click on browse mods to see what kind of goofy shenaenaes fellow people have been up to!");


                    ModBrowser.IsEnabled = true;

                    await WaitUntil(() => ModBrowser.IsSelected);

                    ModManager.IsEnabled = false;

                    tutorialanimator.MoveTo(this.Width / 2, 200);
                    tutorialanimator.SetExpression("thinking");
                    tutorialanimator.DestroyTextbox(curtextbox);
                    curtextbox = tutorialanimator.MakeTextbox(tutorialanimator.GetX() + 110, tutorialanimator.GetY() + 25, "Hmmm let's see...");

                    await tutorialanimator.WaitForClickOnImageAsync();

                    tutorialanimator.SetExpression("happy");
                    tutorialanimator.DestroyTextbox(curtextbox);
                    curtextbox = tutorialanimator.MakeTextbox(tutorialanimator.GetX() + 110, tutorialanimator.GetY() + 25, "OH LOOK THERE I AM!");

                    await tutorialanimator.WaitForClickOnImageAsync();

                    tutorialanimator.SetExpression("sad");
                    tutorialanimator.DestroyTextbox(curtextbox);
                    curtextbox = tutorialanimator.MakeTextbox(tutorialanimator.GetX() + 110, tutorialanimator.GetY() + 25, "sniff... How heartwarming... someone made a mod for me! WOW!");

                    await tutorialanimator.WaitForClickOnImageAsync();
                    tutorialanimator.SetExpression("thinking");
                    tutorialanimator.DestroyTextbox(curtextbox);
                    curtextbox = tutorialanimator.MakeTextbox(tutorialanimator.GetX() + 110, tutorialanimator.GetY() + 25, "If you ever want a mod added to your collection, simply download it!");

                    await tutorialanimator.WaitForClickOnImageAsync();
                    tutorialanimator.SetExpression("inspect");
                    tutorialanimator.DestroyTextbox(curtextbox);
                    curtextbox = tutorialanimator.MakeTextbox(tutorialanimator.GetX() + 110, tutorialanimator.GetY() + 25, "Press on more info to check out the mod description!");

                    await tutorialanimator.WaitForClickOnImageAsync();
                    tutorialanimator.SetExpression("normal");
                    tutorialanimator.DestroyTextbox(curtextbox);
                    curtextbox = tutorialanimator.MakeTextbox(tutorialanimator.GetX() + 110, tutorialanimator.GetY() + 25, "Download this for me real quick, it really won't take long!");

                    RonnieVariables.AllowDownloadMod = true;

                    await WaitUntilTutorialDownloaded();

                    tutorialanimator.SetExpression("happy2");
                    tutorialanimator.DestroyTextbox(curtextbox);
                    curtextbox = tutorialanimator.MakeTextbox(tutorialanimator.GetX() + 110, tutorialanimator.GetY() + 25, "Yippee!!");

                    await tutorialanimator.WaitForClickOnImageAsync();

                    tutorialanimator.SetExpression("thinking");
                    tutorialanimator.DestroyTextbox(curtextbox);
                    curtextbox = tutorialanimator.MakeTextbox(tutorialanimator.GetX() + 110, tutorialanimator.GetY() + 25, "OKAY now that you have a cool mod to your collection, give it a lil swirl!");

                    await tutorialanimator.WaitForClickOnImageAsync();

                    Point relativePoint_2 = ModManager.TransformToAncestor(this)
                                  .Transform(new Point(0, 0));

                    double tabX_2 = relativePoint_2.X + 100;
                    double tabY_2 = relativePoint_2.Y;

                    tutorialanimator.SetExpression("pointerup");
                    tutorialanimator.MoveTo(tabX_2 - 50, tabY_2 + 50);
                    tutorialanimator.DestroyTextbox(curtextbox);
                    curtextbox = tutorialanimator.MakeTextbox(tutorialanimator.GetX() + 110, tutorialanimator.GetY() + 25, "Now Let's now click back");


                    ModManager.IsEnabled = true;

                    await WaitUntil(() => ModManager.IsSelected);

                    tutorialanimator.MoveTo(this.Width / 6, 250);
                    ModBrowser.IsEnabled = false;
                }
            }
            else
            {
                await tutorialanimator.WaitForClickOnImageAsync();
                tutorialanimator.SetExpression("thinking");
                tutorialanimator.DestroyTextbox(curtextbox);
                curtextbox = tutorialanimator.MakeTextbox(tutorialanimator.GetX() + 110, tutorialanimator.GetY() + 25, "Oh it seems like you have my mod installed");
                await tutorialanimator.WaitForClickOnImageAsync();
                tutorialanimator.SetExpression("pointerup");
                tutorialanimator.DestroyTextbox(curtextbox);
                curtextbox = tutorialanimator.MakeTextbox(tutorialanimator.GetX() + 110, tutorialanimator.GetY() + 25, "We can get to launching!!");
                
                if (PLUSSavesystem.read_ini("Tutorial","BrokenModSkip","false") == "true")
                {
                    await WaitSeconds(3);
                    tutorialanimator.SetExpression("thinking");
                    tutorialanimator.DestroyTextbox(curtextbox);
                    curtextbox = tutorialanimator.MakeTextbox(tutorialanimator.GetX() + 110, tutorialanimator.GetY() + 25, "Oh the mod might be broken uhm you can skip if you want lol to settings section or you can click me to try again");
                    await WaitSeconds(1);
                    await tutorialanimator.MakeSkipButtonAsync(tutorialanimator._overlayCanvas, () =>
                    {
                        RonnieVariables.BrokenModSkip = true;
                    });

                    tutorialanimator._overlayCanvas.IsHitTestVisible = false;
                    
                } 
                else
                {
                    await tutorialanimator.WaitForClickOnImageAsync();
                }
  
            }
            if (!RonnieVariables.BrokenModSkip)
            { 
                RonnieVariables.SetupAllow = true;
                tutorialanimator.SetExpression("thinking");
                tutorialanimator.DestroyTextbox(curtextbox);
                curtextbox = tutorialanimator.MakeTextbox(tutorialanimator.GetX() + 110, tutorialanimator.GetY() + 25, "Before we do this though, we need to make sure we have your files in check! Click on setup just to make sure...");


                DispatcherTimer setupTimer = new DispatcherTimer();
                setupTimer.Interval = TimeSpan.FromMilliseconds(16);

                setupTimer.Tick += async (s, e) =>
                {
                    if (RonnieVariables.SetupSucessful == 1)
                    {
                        setupTimer.Stop();
                        return;
                    }

                    if (RonnieVariables.SetupSucessful == 0)
                    {
                        await WaitSeconds(3);
                        if (RonnieVariables.SetupSucessful == 0)
                        {
                            tutorialanimator.SetExpression("sad");
                            tutorialanimator.DestroyTextbox(curtextbox);

                            curtextbox = tutorialanimator.MakeTextbox(tutorialanimator.GetX() + 110, tutorialanimator.GetY() + 25, "Looks like I wasn't able to find your Pizza Tower folder.. Could you click on it for me, pretty pleaaaase?");

                            RonnieVariables.SetupSucessful = -1;
                        }
                    }
                };


                setupTimer.Start();

                await WaitUntil(() => RonnieVariables.SetupSucessful == 1);

                setupTimer.Stop();

                tutorialanimator.SetExpression("happy");
                tutorialanimator.DestroyTextbox(curtextbox);
                curtextbox = tutorialanimator.MakeTextbox(tutorialanimator.GetX() + 110, tutorialanimator.GetY() + 25, "Alright! Now we're all fired up! First select the mod...");

                await WaitForRonnieModClick();

                tutorialanimator.SetExpression("normal");
                tutorialanimator.DestroyTextbox(curtextbox);
                curtextbox = tutorialanimator.MakeTextbox(tutorialanimator.GetX() + 110, tutorialanimator.GetY() + 25, "And then launch it!");

                if (!IsTutorialDownloaded())
                {
                    await WaitSeconds(3);
                    tutorialanimator.SetExpression("sad");
                    tutorialanimator.DestroyTextbox(curtextbox);
                    curtextbox = tutorialanimator.MakeTextbox(tutorialanimator.GetX() + 110, tutorialanimator.GetY() + 25, "Wait... You were not meant to delete the mod...");
                    await WaitSeconds(3);
                    TutorialState("false");
                }

                RonnieVariables.LauncherAllow = true;

                string path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "PizzaTower_GM2", "RonnieTutorial.ini");


                DispatcherTimer exetimer = new DispatcherTimer
                {
                    Interval = TimeSpan.FromMilliseconds(500)
                };

                exetimer.Tick += async (s, e) =>
                {
                    string processName = Path.GetFileNameWithoutExtension(Global.config.Launcher);

                    if (RonnieVariables.FailedPatch)
                    {
                        RonnieVariables.FailedPatch = false;
                        RonnieVariables.LauncherAllow = false;
                        tutorialanimator.SetExpression("thinking");
                        tutorialanimator.DestroyTextbox(curtextbox);
                        curtextbox = tutorialanimator.MakeTextbox(tutorialanimator.GetX() + 110, tutorialanimator.GetY() + 25, "Oh... that's weird... Uhmmmm why is it... not... working...");
                        await tutorialanimator.WaitForClickOnImageAsync();

                        tutorialanimator.SetExpression("sad");
                        tutorialanimator.DestroyTextbox(curtextbox);
                        curtextbox = tutorialanimator.MakeTextbox(tutorialanimator.GetX() + 110, tutorialanimator.GetY() + 25, "Oh god im such a useless failure aren't I... I can't do anything right!");
                        await tutorialanimator.WaitForClickOnImageAsync();

                        tutorialanimator.SetExpression("thinking");
                        tutorialanimator.DestroyTextbox(curtextbox);
                        curtextbox = tutorialanimator.MakeTextbox(tutorialanimator.GetX() + 110, tutorialanimator.GetY() + 25, "Hmm... oh I know! you should try messing around your steam settings! Trust me, it's super simple...");
                        await tutorialanimator.WaitForClickOnImageAsync();

                        tutorialanimator.DestroyTextbox(curtextbox);
                        curtextbox = tutorialanimator.MakeTextbox(tutorialanimator.GetX() + 110, tutorialanimator.GetY() + 25, "Then, going into your steam, go to Pizza Tower and click on properties.");
                        await tutorialanimator.WaitForClickOnImageAsync();

                        tutorialanimator.DestroyTextbox(curtextbox);
                        curtextbox = tutorialanimator.MakeTextbox(tutorialanimator.GetX() + 110, tutorialanimator.GetY() + 25, "You're gonna want to click on Installed files and then verify integrity Once you've done that and it says no files are missing");
                        await tutorialanimator.WaitForClickOnImageAsync();

                        tutorialanimator.SetExpression("happy");
                        tutorialanimator.DestroyTextbox(curtextbox);
                        curtextbox = tutorialanimator.MakeTextbox(tutorialanimator.GetX() + 110, tutorialanimator.GetY() + 25, "try launching the mod again after that. make sure this will overwrite your existing modifed pt files");
                        RonnieVariables.LauncherAllow = true;
                    }

                    if (File.Exists(path))
                    {
                        exetimer.Stop();
                    }
                    else
                    {
                        PLUSSavesystem.write_ini("Tutorial", "BrokenModSkip", "true");
                        RonnieVariables.ModLaunchAmount += 1;
                        await WaitUntil(() =>
                        Process.GetProcessesByName(processName).Length > 0);
                        PLUSMUSIC.SetTutorialMusicPaused(true);
                        await WaitUntil(() =>
                        Process.GetProcessesByName(processName).Length == 0);
                        PLUSMUSIC.SetTutorialMusicPaused(false);
                        if (!File.Exists(path) && !RonnieVariables.FinishedLaunch)
                        {
                            tutorialanimator.SetExpression("sad");
                            tutorialanimator.DestroyTextbox(curtextbox);
                            curtextbox = tutorialanimator.MakeTextbox(tutorialanimator.GetX() + 110, tutorialanimator.GetY() + 25, "Atleast finish the intro...");
                            await WaitSeconds(3);
                            if (RonnieVariables.ModLaunchAmount > 1)
                            {
                                tutorialanimator.SetExpression("thinking");
                                tutorialanimator.DestroyTextbox(curtextbox);
                                curtextbox = tutorialanimator.MakeTextbox(tutorialanimator.GetX() + 110, tutorialanimator.GetY() + 25, "Or I mean if the mod isn't working you can close PizzaOven+ and I will offer a skip");
                            }
                            if (Process.GetProcessesByName(processName).Length == 0)
                            {
                                var ps = new ProcessStartInfo(Global.config.Launcher)
                                {
                                    WorkingDirectory = Path.GetDirectoryName(Global.config.Launcher),
                                    UseShellExecute = true,
                                    Verb = "open"
                                };
                                Process.Start(ps);
                            }
                            exetimer.Stop();
                            exetimer.Start();
                        }
                    }
                };


                exetimer.Start();

                await WaitUntil(() => File.Exists(path));
                RonnieVariables.FinishedLaunch = true;
                ConfigButton.IsEnabled = false;
                LaunchButton.IsEnabled = false;
                exetimer.Stop();
                PLUSMUSIC.SetTutorialMusicPaused(false);
                File.Delete(path);
                tutorialanimator.SetExpression("sad");
                tutorialanimator.DestroyTextbox(curtextbox);
                curtextbox = tutorialanimator.MakeTextbox(tutorialanimator.GetX() + 110, tutorialanimator.GetY() + 25, "someone is trolling me that mod SUCKED...");


                await tutorialanimator.WaitForClickOnImageAsync();
                tutorialanimator.SetExpression("dumb");
                tutorialanimator.DestroyTextbox(curtextbox);
                curtextbox = tutorialanimator.MakeTextbox(tutorialanimator.GetX() + 110, tutorialanimator.GetY() + 25, "ANYWAYS I think that was pretty helpful, don't you think?");
            }

            Point relativePoint_3 = Settings.TransformToAncestor(this)
               .Transform(new Point(0, 0));

            double tabX_3 = relativePoint_3.X + 100;
            double tabY_3 = relativePoint_3.Y;

            tutorialanimator.SetExpression("pointerup");
            tutorialanimator.MoveTo(tabX_3 - 50, tabY_3 + 50);

            tutorialanimator.DestroyTextbox(curtextbox);
            curtextbox = tutorialanimator.MakeTextbox(tutorialanimator.GetX() + 110, tutorialanimator.GetY() + 25, "Let's go to settings!");
            Settings.IsEnabled = true;

            await WaitUntil(() => Settings.IsSelected);

            Settings.IsEnabled = false;
            ModManager.IsEnabled = false;
            tutorialanimator.MoveTo(this.Width / 2, 200);
            tutorialanimator.SetExpression("normal");
            tutorialanimator.DestroyTextbox(curtextbox);
            curtextbox = tutorialanimator.MakeTextbox(tutorialanimator.GetX() + 110, tutorialanimator.GetY() + 25, "This where you can configure stuff");

            await tutorialanimator.WaitForClickOnImageAsync();
            tutorialanimator.SetExpression("thinking");
            tutorialanimator.DestroyTextbox(curtextbox);
            curtextbox = tutorialanimator.MakeTextbox(tutorialanimator.GetX() + 110, tutorialanimator.GetY() + 25, "You can suggest stuff with our form");

            await tutorialanimator.WaitForClickOnImageAsync();
            tutorialanimator.SetExpression("thinking");
            tutorialanimator.DestroyTextbox(curtextbox);
            curtextbox = tutorialanimator.MakeTextbox(tutorialanimator.GetX() + 110, tutorialanimator.GetY() + 25, "it helps us out alot");

            await tutorialanimator.WaitForClickOnImageAsync();
            tutorialanimator.SetExpression("thinking");
            tutorialanimator.DestroyTextbox(curtextbox);
            curtextbox = tutorialanimator.MakeTextbox(tutorialanimator.GetX() + 110, tutorialanimator.GetY() + 25, "Maybe you want to downgrade your game");

            await tutorialanimator.WaitForClickOnImageAsync();
            tutorialanimator.DestroyTextbox(curtextbox);
            curtextbox = tutorialanimator.MakeTextbox(tutorialanimator.GetX() + 110, tutorialanimator.GetY() + 25, "Look no further than the verisons we offer");

            await tutorialanimator.WaitForClickOnImageAsync();
            tutorialanimator.DestroyTextbox(curtextbox);
            tutorialanimator.SetExpression("happy");
            curtextbox = tutorialanimator.MakeTextbox(tutorialanimator.GetX() + 110, tutorialanimator.GetY() + 25, "LOAD GMLOADER FILES");

            await tutorialanimator.WaitForClickOnImageAsync();
            tutorialanimator.DestroyTextbox(curtextbox);
            curtextbox = tutorialanimator.MakeTextbox(tutorialanimator.GetX() + 110, tutorialanimator.GetY() + 25, "wanna hear the best part?");

            await tutorialanimator.WaitForClickOnImageAsync();
            tutorialanimator.SetExpression("happy2");
            tutorialanimator.DestroyTextbox(curtextbox);
            curtextbox = tutorialanimator.MakeTextbox(tutorialanimator.GetX() + 110, tutorialanimator.GetY() + 25, "if you load an xdelta file you can stack multiple GMLoader mods ontop of eachother");

            await tutorialanimator.WaitForClickOnImageAsync();
            tutorialanimator.DestroyTextbox(curtextbox);
            curtextbox = tutorialanimator.MakeTextbox(tutorialanimator.GetX() + 110, tutorialanimator.GetY() + 25, "mainly because it doesn't check hash(if modded or not)");

            await tutorialanimator.WaitForClickOnImageAsync();
            tutorialanimator.DestroyTextbox(curtextbox);
            curtextbox = tutorialanimator.MakeTextbox(tutorialanimator.GetX() + 110, tutorialanimator.GetY() + 25, "you can turn your files into GMLoader to easily stack em");

            await tutorialanimator.WaitForClickOnImageAsync();
            tutorialanimator.SetExpression("thinking");
            tutorialanimator.DestroyTextbox(curtextbox);
            curtextbox = tutorialanimator.MakeTextbox(tutorialanimator.GetX() + 110, tutorialanimator.GetY() + 25, "Use the Custom Assets folder to customise some of your stuff(yet to be added more)");

            await tutorialanimator.WaitForClickOnImageAsync();
            tutorialanimator.DestroyTextbox(curtextbox);
            curtextbox = tutorialanimator.MakeTextbox(tutorialanimator.GetX() + 110, tutorialanimator.GetY() + 25, "Clean PO. files if a mod is jammed and won't launch, just make sure to verify integrity of game files on steam afterwards, okay?");

            await tutorialanimator.WaitForClickOnImageAsync();
            tutorialanimator.SetExpression("happy2");
            tutorialanimator.DestroyTextbox(curtextbox);
            curtextbox = tutorialanimator.MakeTextbox(tutorialanimator.GetX() + 110, tutorialanimator.GetY() + 25, "You can even toggle my cute little face on your discord with RPC settings");

            await tutorialanimator.WaitForClickOnImageAsync();
            tutorialanimator.SetExpression("happy");
            tutorialanimator.DestroyTextbox(curtextbox);
            curtextbox = tutorialanimator.MakeTextbox(tutorialanimator.GetX() + 110, tutorialanimator.GetY() + 25, "and much more is to come, I will try to cater pookie");

            await tutorialanimator.WaitForClickOnImageAsync();
            tutorialanimator.MoveTo(this.Width / 6, 250);
            ModManager.IsEnabled = true;
            Settings.IsEnabled = false;
            ModManager.IsSelected = true;

            tutorialanimator.SetExpression("thinking");
            tutorialanimator.DestroyTextbox(curtextbox);
            curtextbox = tutorialanimator.MakeTextbox(tutorialanimator.GetX() + 110, tutorialanimator.GetY() + 25, "And that's about all the wacky things you can do here, really! Not so wacky now, huh?");

            await tutorialanimator.WaitForClickOnImageAsync();
            tutorialanimator.DestroyTextbox(curtextbox);
            curtextbox = tutorialanimator.MakeTextbox(tutorialanimator.GetX() + 110, tutorialanimator.GetY() + 25, "Stating this again, but if you want me to improve on this tool, then make sure to spell it out for me on feedback, because believe it or not, I can in fact read.");

            await tutorialanimator.WaitForClickOnImageAsync();
            tutorialanimator.DestroyTextbox(curtextbox);
            curtextbox = tutorialanimator.MakeTextbox(tutorialanimator.GetX() + 110, tutorialanimator.GetY() + 25, "I won't take too much of your time, so how about you embark on your own journey!");

            await tutorialanimator.WaitForClickOnImageAsync();
            tutorialanimator.SetExpression("happy2");
            tutorialanimator.DestroyTextbox(curtextbox);
            curtextbox = tutorialanimator.MakeTextbox(tutorialanimator.GetX() + 110, tutorialanimator.GetY() + 25, "I am still saying right here by your side, probably toiling away for you for the rest of eternity. YAY!!");

            PLUSSavesystem.write_ini("Tutorial", "Finished", "true");
            tutorialanimator.DestroyTextbox(curtextbox);
            await DanceAsync(tutorialanimator, 3, 60);

            TutorialState();
        }

        private PLUSRonnieAnimate introanimator;
        private PLUSRonnieAnimate launchanimator;
        public PLUSRonnieAnimate tutorialanimator;
        private PLUSRonnieAnimate replayanimator;
        public string version;
        public static string PizzaTowerVersion = "1.1.280";
        // Separated from Global.config so that order is updated when datagrid is modified
        public List<string> exes;
        private FileSystemWatcher ModsWatcher;
        private List<FileSystemWatcher> PLUSWatchers = new List<FileSystemWatcher>();
        private MediaPlayer backgroundPlayer;
        private FlowDocument defaultFlow = new FlowDocument();
        private string defaultText = "No mod is currently selected. Pressing launch will start a vanilla Pizza Tower. \n\nyou can also go the PLUS' Settings to play on the older verisons that PLUS provides (if you wish you can even put your own downgrade patch in Downgrades folder.)\n\n" +
            "Start downloading and using mods in the Browse Mods tab on top. Only one mod can be selected at a time.";



        public MainWindow()
        {
            InitializeComponent();
            // Get Version Number
            var PizzaOvenVersion = Assembly.GetExecutingAssembly().GetName().Version.ToString();
            version = PizzaOvenVersion.Substring(0, PizzaOvenVersion.LastIndexOf('.'));
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
                RunTutorial();
            }
            else
            {
                //hi to the people looking at the code!!! this how we check if you still have it lol
                RonnieVariables.KeptMod = File.Exists(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "PizzaTower_GM2", "RonnieTutorial.ini"));
                RonnieVariables.ModDeleted = TutorialModPath() == "";
                if (Directory.Exists(TutorialModPath()))
                {
                    Directory.Delete(TutorialModPath(), true);
                }
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
                if (!RonnieVariables.ModDeleted || RonnieVariables.KeptMod)
                {
                    RunIntro();
                }
                else
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
            if (Global.ronnietutorial && !RonnieVariables.SetupAllow)
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
                        RonnieVariables.SetupSucessful = 1;

                }
                else if (Global.ronnietutorial)
                {
                    RonnieVariables.SetupSucessful = 0;
                }
            });
        }
        private async void Launch_Click(object sender, RoutedEventArgs e)
        {
            // Build Mod Loadout
            if (Global.ronnietutorial && !RonnieVariables.LauncherAllow)
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
                        launchanimator.GlideTo(this.ActualWidth, this.ActualHeight - 200, 20);
                        launchanimator.SetExpression("sad");
                    }
                    else
                    {
                        RonnieVariables.FailedPatch = true;
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
            }
            else
            {
                Global.logger.WriteLine("Please click Setup before starting!", LoggerType.Warning);
                return;
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

                    if (modtype == "GMLOADER")
                    {
                        Global.logger.WriteLine($"GMLoader will launch {path}", LoggerType.Info);
                    }
                    else
                    {
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
                        return ModLoader.BuildGMLoader($@"{Global.assemblyLocation}{Global.s}Mods{Global.s}{mods[0].name}");
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
            if (Global.ronnietutorial && !RonnieVariables.AllowDownloadMod)
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
        private void OnBrowserTabSelected(object sender, RoutedEventArgs e)
        {
            if (!selected)
                InitializeBrowser();
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
        }

        private void OnModManagerUnselected(object sender, RoutedEventArgs e)
        {
            modManagerRefreshed = false;
        }

        private void PLUSrefresh()
        {

            string DowngradePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Downgrades");
            if (Directory.Exists(DowngradePath))
            {
                string[] files = Directory.GetFiles(DowngradePath);

                var DowngradeSave = DowngradeCombo.SelectedItem as string;
                if (string.IsNullOrEmpty(DowngradeSave))
                {
                    DowngradeSave = null;
                }
                DowngradeCombo.Items.Clear();

                for (int i = 0; i < files.Length; i++)
                {
                    string fileName = Path.GetFileName(files[i]);

                    string nameWithoutExt = Path.GetFileNameWithoutExtension(fileName);

                    if (fileName.ToLower().Contains("xdelta"))
                    {
                        DowngradeCombo.Items.Add(nameWithoutExt);
                    }
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

            bool potentialGMLoader = Directory
                .EnumerateDirectories(path, "*", SearchOption.AllDirectories)
                .Any(d => GMLoaderFolder
                    .Contains(Path.GetFileName(d), StringComparer.OrdinalIgnoreCase));

            bool xdeltainfolder = false;

            if (Directory.Exists(path))
            {
                var xdeltaFolders = Directory.EnumerateDirectories(path, "xdelta", SearchOption.AllDirectories);

                xdeltainfolder = xdeltaFolders.Any(xdeltaFolder =>
                    Directory.EnumerateFileSystemEntries(xdeltaFolder)
                             .Any(entry => Path.GetFileName(entry)
                                 .Equals("xdelta", StringComparison.OrdinalIgnoreCase))
                );
            }


            ModJSON modjson;

            var jsonPath = $"{path}{Global.s}mod.json";

            if (File.Exists(jsonPath))
            {
                try
                {
                    string json = File.ReadAllText(jsonPath);
                    modjson = JsonSerializer.Deserialize<ModJSON>(json) ?? new ModJSON();
                }
                catch
                {
                    modjson = new ModJSON();
                }
            }
            else
            {
                modjson = new ModJSON();
            }

            if (modjson.cat == "GMLoader")
            {
                return "GMLOADER";
            }
            else if (modjson.cat == "CYOP/AFOM")
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

        private void UpdatePLUSfilter(object sender, TextChangedEventArgs e)
        {
            string filter = (PLUS_SearchBar?.Text ?? string.Empty).Trim();

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
                if (PLUS_SearchBar != null)
                {
                    bool containsSearch =
                        panel == (DependencyObject)PLUS_SearchBar ||
                        FindVisualChildren<DependencyObject>(panel).Any(c => c == (DependencyObject)PLUS_SearchBar) ||
                        FindLogicalChildren<DependencyObject>(panel).Any(c => c == (DependencyObject)PLUS_SearchBar);

                    if (containsSearch)
                    {
                        panel.Visibility = Visibility.Visible;
                        continue;
                    }
                }

                string searchable = panel.Tag?.ToString() ?? panel.Name ?? string.Empty;

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
                    var tb = FindVisualChildren<TextBlock>(panel).FirstOrDefault();
                    if (tb != null)
                        searchable = tb.Text ?? string.Empty;
                }

                if (string.IsNullOrEmpty(searchable))
                    continue;

                bool match = string.IsNullOrEmpty(filter) ||
                             searchable.IndexOf(filter, StringComparison.OrdinalIgnoreCase) >= 0;

                panel.Visibility = match ? Visibility.Visible : Visibility.Collapsed;
            }

            if (Global.ronnietutorial)
                TutorialPanel.Visibility = Visibility.Collapsed;
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
                    PLUSSavesystem.delete_ini("Folder", saves[i, 0]);
                }
            }
            ModFolderCombo.SelectedItem = "All";
        }
        private void OpenSuggestForm_Click(object sender, RoutedEventArgs e)
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = "https://docs.google.com/forms/d/e/1FAIpQLScI-8L6-ATpE6_ip3gzESXAWi4B_0pwHiHI5g83fb3SlLTM_A/viewform?usp=dialog",
                UseShellExecute = true
            });
        }

        private void OpenEmail_Click(object sender, RoutedEventArgs e)
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = "https://mail.google.com/mail/u/0/#inbox?compose=GTvVlcSGKZhCvzvPvWzHvQZTnWMgDSzDHWTFDjnfWdjQscBHkRtBhmJPRKjjJbkNqlGRbtHlWzDWW",
                UseShellExecute = true
            });
        }
        private void OpenTwitterX_Click(object sender, RoutedEventArgs e)
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = "https://x.com/SurfyCrescent97",
                UseShellExecute = true
            });
        }

        private void AssetsFolder_Click(object sender, RoutedEventArgs e)
        {
            Process process = Process.Start("explorer.exe", Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "PizzaOvenPLUS", "CustomAssets"));
        }

        private void RestoreMissingAssets_Click(object sender, RoutedEventArgs e)
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
        private void InitPOLanguage(bool enabled)
        {
            POLanguage.Content = enabled ? "Do not Apply to Language Files?" : "Do Apply to Language Files?";

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
        }
        private void POLanguage_Click(object sender, RoutedEventArgs e)
        {
            bool enabled = PLUSSavesystem.read_ini("Files", "POLanguage", "true") != "true";
            PLUSSavesystem.write_ini("Files", "POLanguage", enabled.ToString().ToLowerInvariant());
            InitPOLanguage(enabled);
        }
        private void InitMute(bool enabled)
        {
            PLUSMUSIC.MuteEnabled = enabled;
            PLUSMUSIC.ApplyCurrentVolume();
            MuteButton.Content = enabled ? "Disable Mute?" : "Enable Mute?";
        }
        private void Mute_Click(object sender, RoutedEventArgs e)
        {
            bool enabled = PLUSSavesystem.toggle_ini_bool("Audio", "Mute", false);
            InitMute(enabled);
        }

        private void InitUnfocusedMute(bool enabled)
        {
            PLUSMUSIC.unfocusedMuteEnabled = enabled;
            PLUSMUSIC.ApplyCurrentVolume();
            UnfocusedMuteButton.Content = enabled ? "Disable Unfocused Mute?" : "Enable Unfocused Mute?";
        }
        private void UnfocusedMute_Click(object sender, RoutedEventArgs e)
        {
            bool enabled = PLUSSavesystem.toggle_ini_bool("Audio", "UnfocusedMute", true);
            InitUnfocusedMute(enabled);
        }

        private void InitRPC(bool enabled)
        {
            RPCtoggle.Content = enabled ? "Disable RPC?" : "Enable RPC?";
            if (enabled)
                PLUSRPC.DiscordPresenceService.Initialize();
            else
                PLUSRPC.DiscordPresenceService.Shutdown();
        }
        private void RPCtoggle_Click(object sender, RoutedEventArgs e)
        {
            bool enabled = PLUSSavesystem.toggle_ini_bool("Discord", "RPC", true);
            InitRPC(enabled);
        }

        private void InitDebug(bool enabled)
        {
            DebugToggle.Content = enabled ? "Disable Debug?" : "Enable Debug?";
        }
        private void DebugToggle_Click(object sender, RoutedEventArgs e)
        {
            bool enabled = PLUSSavesystem.toggle_ini_bool("Launch", "Debug", true);
            InitDebug(enabled);
        }
        private void InitSteamLaunch(bool enabled)
        {
            SteamLaunchToggle.Content = enabled ? "Don't use Steam?" : "Use Steam?";
        }
        private void SteamLaunch_Click(object sender, RoutedEventArgs e)
        {
            bool enabled = PLUSSavesystem.toggle_ini_bool("Launch", "Steam", true);
            InitSteamLaunch(enabled);
        }


        private void InitModUpdater(bool enabled)
        {
            MODUPDATERtoggle.Content = enabled ? "Disable Check for Mod Updates?" : "Enable Check for Mod Updates?";
        }
        private void MODUPDATERtoggle_Click(object sender, RoutedEventArgs e)
        {
            bool enabled = PLUSSavesystem.toggle_ini_bool("LowEnd", "ModUpdate", true);
            InitModUpdater(enabled);
        }

        private void InitializeToggles()
        {
            InitMute(PLUSSavesystem.read_ini("Audio", "Mute", "false") == "true");
            InitUnfocusedMute(PLUSSavesystem.read_ini("Audio", "UnfocusedMute", "true") == "true");
            InitRPC(PLUSSavesystem.read_ini("Discord", "RPC", "true") == "true");
            InitDebug(PLUSSavesystem.read_ini("Launch", "Debug", "true") == "true");
            InitSteamLaunch(PLUSSavesystem.read_ini("Launch", "Steam", "false") == "true");
            InitModUpdater(PLUSSavesystem.read_ini("LowEnd", "ModUpdate", "true") == "true");
            InitPOLanguage(PLUSSavesystem.read_ini("Files", "POLanguage", "true") == "true");
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

                await WaitUntil(() => replayanimator.GetY() >= 250);
                var curtextbox = replayanimator.MakeTextbox(replayanimator.GetX() + 110, replayanimator.GetY() + 25, "TAKE IT FROM THE TOP");
                await WaitSeconds(5);
                TutorialState("false");
            }
            else
            {
                PLUSSavesystem.write_ini("Tutorial", "Replay", "false");
                PLUSSavesystem.write_ini("Tutorial", "ForcedReplay", "true");
                PLUSSavesystem.write_ini("Tutorial", "Finished", "false");
                RonnieVariables.DeclineReplay += 1;
                replayanimator = new PLUSRonnieAnimate();
                replayanimator.Initialize(this, this.Width / 6, -100, 1.5);
                var curtextbox = replayanimator.MakeTextbox(replayanimator.GetX() + 110, replayanimator.GetY() + 25, "");
                replayanimator.DestroyTextbox(curtextbox);
                replayanimator.GlideTo(this.Width / 6, 250, 40);
                replayanimator.SetExpression("sad");
                await WaitUntil(() => replayanimator.GetY() >= 250);
                if (RonnieVariables.DeclineReplay == 3)
                {
                    replayanimator.DestroyTextbox(curtextbox);
                    curtextbox = replayanimator.MakeTextbox(replayanimator.GetX() + 110, replayanimator.GetY() + 25, "Stop it! or else");
                    await WaitSeconds(3);
                    replayanimator.DestroyTextbox(curtextbox);
                }
                else if (RonnieVariables.DeclineReplay > 3)
                {
                    replayanimator.DestroyTextbox(curtextbox);
                    curtextbox = replayanimator.MakeTextbox(replayanimator.GetX() + 110, replayanimator.GetY() + 25, "You asked for this");
                    await WaitSeconds(3);
                    replayanimator.DestroyTextbox(curtextbox);
                    TutorialState("false");
                }
                replayanimator.GlideTo(this.Width / 6, -250, 40);
                await WaitUntil(() => replayanimator.GetY() <= -250);
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

            string steamUser = GetSteamUsername();
            foreach (var v in ptVersions)
            {
                if (v.version != selectedVersion) 
                    continue;

                if (v.type == "depot")
                { 
                    bool success = await DownloadDowngradeAsync("2231450","2231451",v.manifestID,steamUser,tempDir,ogWinFile,v.version);

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

        private static string GetSteamUsername()
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

        private static async Task<bool> DownloadDowngradeAsync(string appID, string depotID, string manifestID, string username, string outputDir, string ogWinFile, string version)
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
        public void TutorialState(string finished = "true")
        {
            PLUSSavesystem.write_ini("Tutorial", "Finished", finished);
            PLUSSavesystem.write_ini("Tutorial", "BrokenModSkip", "false");
            string exePath = $"{AppDomain.CurrentDomain.BaseDirectory}{Global.s}{AppDomain.CurrentDomain.FriendlyName}";
            Process.Start(exePath);
            Application.Current.Shutdown();
        }
    }
}

