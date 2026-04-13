using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Media;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using static PizzaOven.MainWindow;
using Path = System.IO.Path;

namespace PizzaOven
{
    public class PLUSTutorial
    {
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
            public static int RonnieExplainSettings = -1;
            public static int publictextbox = 0;
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
        public static async Task WaitForRonnieModClick()
        {
            while (true)
            {
                var first = Global.config.ModList.FirstOrDefault(x => x.enabled);

                if (first != null && first.name == "Ronnie Oven Mod")
                    return;

                await Task.Delay(100);
            }
        }
        public static async Task RunTutorial(MainWindow window)
        {
            bool tutorialskip = false;
            if (File.Exists(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "PizzaTower_GM2", "RonnieTutorial.ini")))
            {
                File.Delete(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "PizzaTower_GM2", "RonnieTutorial.ini"));
            }
            Application.Current.Dispatcher.BeginInvoke(new Action(() =>
            {
                var uri = new Uri("pack://application:,,,/PizzaOven;component/OvenRonnie/RonnieJingle.wav");

                var streamResourceInfo = Application.GetResourceStream(uri);
                var player = new SoundPlayer(streamResourceInfo.Stream);

                player.Play();
            }));

            window.PatchNotes.IsEnabled = false;
            window.TutorialPanel.Visibility = Visibility.Collapsed;


            window.tutorialanimator = new PLUSRonnieAnimate();
            window.tutorialanimator.Initialize(window, window.Width / 6, -100, 1.5);
            window.ModBrowser.IsEnabled = false;
            window.Settings.IsEnabled = false;

            window.SizeChanged += (s, e) =>
            {
                window.tutorialanimator._overlayCanvas.Width = window.ActualWidth;
                window.tutorialanimator._overlayCanvas.Height = window.ActualHeight;
            };


            window.tutorialanimator.GlideTo(window.Width / 6, 250, 5);

            await PLUSWait.WaitUntil(() => window.tutorialanimator.GetY() >= 250);
            await Task.Delay(2000);

            window.tutorialanimator.SetExpression("normal");
            PLUSMUSIC.Play_TutorialMusic();
            var curtextbox = window.tutorialanimator.MakeTextbox(window.tutorialanimator.GetX() + 110, window.tutorialanimator.GetY() + 25, "Hi!! Hello!! Hi!![click to proceed]");

            await window.tutorialanimator.WaitForClickOnImageAsync();

            window.tutorialanimator.SetExpression("dumb");
            window.tutorialanimator.DestroyTextbox(curtextbox);
            curtextbox = window.tutorialanimator.MakeTextbox(window.tutorialanimator.GetX() + 110, window.tutorialanimator.GetY() + 25, "I'm Ronnie, Ronnie the Oven!");
            RonnieVariables.RonnieModSkip = IsTutorialDownloaded();
            if (!RonnieVariables.RonnieModSkip)
            {
                PLUSSavesystem.write_ini("Tutorial", "SettingsSection", "false");
                PLUSSavesystem.write_ini("Tutorial", "BrokenModSkip", "false");
                await window.tutorialanimator.WaitForClickOnImageAsync();
                window.tutorialanimator.SetExpression("happy2");
                window.tutorialanimator.DestroyTextbox(curtextbox);
                curtextbox = window.tutorialanimator.MakeTextbox(window.tutorialanimator.GetX() + 110, window.tutorialanimator.GetY() + 25, "Welcome to PizzaOven+ (Plus)");

                await window.tutorialanimator.WaitForClickOnImageAsync();
                window.tutorialanimator.SetExpression("inspect");
                window.tutorialanimator.DestroyTextbox(curtextbox);
                curtextbox = window.tutorialanimator.MakeTextbox(window.tutorialanimator.GetX() + 110, window.tutorialanimator.GetY() + 25, "Nice to meet you, Random Pizza Tower fan! or so I think you are");

                await window.tutorialanimator.WaitForClickOnImageAsync();
                window.tutorialanimator.SetExpression("happy2");
                window.tutorialanimator.DestroyTextbox(curtextbox);
                curtextbox = window.tutorialanimator.MakeTextbox(window.tutorialanimator.GetX() + 110, window.tutorialanimator.GetY() + 25, "I will be your personnel and annoying guide to understand this wacky and wonderful tool!\r\n");

                await window.tutorialanimator.WaitForClickOnImageAsync();
                window.tutorialanimator.SetExpression("normal");
                window.tutorialanimator.DestroyTextbox(curtextbox);
                curtextbox = window.tutorialanimator.MakeTextbox(window.tutorialanimator.GetX() + 110, window.tutorialanimator.GetY() + 25, "What's this for you ask? You are using a Pizza Oven extension! It's basically the same thing but with more stuff.");

                await window.tutorialanimator.WaitForClickOnImageAsync();
                window.tutorialanimator.SetExpression("normal");
                window.tutorialanimator.DestroyTextbox(curtextbox);
                curtextbox = window.tutorialanimator.MakeTextbox(window.tutorialanimator.GetX() + 110, window.tutorialanimator.GetY() + 25, "Allow me to give you a bunch of carefree tips to make your life easier! Consider me as a useful buddy.\r\n");

                await window.tutorialanimator.WaitForClickOnImageAsync();
                window.tutorialanimator.SetExpression("sad");
                window.tutorialanimator.DestroyTextbox(curtextbox);
                curtextbox = window.tutorialanimator.MakeTextbox(window.tutorialanimator.GetX() + 110, window.tutorialanimator.GetY() + 25, "You can choose to hate me but I don't really care because I don't make much friends anyway");

                await window.tutorialanimator.WaitForClickOnImageAsync();
                window.tutorialanimator.SetExpression("happy");
                window.tutorialanimator.DestroyTextbox(curtextbox);
                curtextbox = window.tutorialanimator.MakeTextbox(window.tutorialanimator.GetX() + 110, window.tutorialanimator.GetY() + 25, "but in case you don't, GREAT! Let me show you around...");

                await window.tutorialanimator.WaitForClickOnImageAsync();
                window.tutorialanimator.SetExpression("sad");
                window.tutorialanimator.DestroyTextbox(curtextbox);
                curtextbox = window.tutorialanimator.MakeTextbox(window.tutorialanimator.GetX() + 110, window.tutorialanimator.GetY() + 25, "If you'd like to skip the tutorial the button is there... heheh... or you can click me and listen to me");

                await window.tutorialanimator.MakeSkipButtonAsync(window.tutorialanimator._overlayCanvas, () =>
                {
                    tutorialskip = true;
                });


                window.tutorialanimator._overlayCanvas.IsHitTestVisible = false;


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
                    window.tutorialanimator.DestroyTextbox(curtextbox);
                    curtextbox = window.tutorialanimator.MakeTextbox(window.tutorialanimator.GetX() + 110, window.tutorialanimator.GetY() + 25, "Oh.. I guess I'll leave sigh");
                    window.tutorialanimator.GlideTo((window.Width / 6), 100, 1);
                    await PLUSWait.WaitUntil(() => window.tutorialanimator.GetY() <= 100);

                    window.tutorialanimator.DestroyTextbox(curtextbox);
                    curtextbox = window.tutorialanimator.MakeTextbox(window.tutorialanimator.GetX() + 110, window.tutorialanimator.GetY() + 25, "Are you sure?");

                    await PLUSWait.WaitSeconds(3);

                    window.tutorialanimator.DestroyTextbox(curtextbox);
                    curtextbox = window.tutorialanimator.MakeTextbox(window.tutorialanimator.GetX() + 110, window.tutorialanimator.GetY() + 25, "No no you're right bye...");
                    window.tutorialanimator.GlideTo(window.tutorialanimator.GetX(), -100, 1);

                    await PLUSWait.WaitUntil(() => window.tutorialanimator.GetY() <= -100);

                    TutorialState();
                }
                else
                {
                    window.tutorialanimator.SetExpression("happy2");
                    window.tutorialanimator.DestroyTextbox(curtextbox);
                    curtextbox = window.tutorialanimator.MakeTextbox(window.tutorialanimator.GetX() + 110, window.tutorialanimator.GetY() + 25, "REALLY WOWOWOW");

                    await window.tutorialanimator.WaitForClickOnImageAsync();

                    window.tutorialanimator.SetExpression("happy2");
                    window.tutorialanimator.DestroyTextbox(curtextbox);
                    curtextbox = window.tutorialanimator.MakeTextbox(window.tutorialanimator.GetX() + 110, window.tutorialanimator.GetY() + 25, "THIS MAKES ME WANNA DANCE");
                    await window.tutorialanimator.DanceAsync(10, 150);

                    window.tutorialanimator.SetExpression("thinking");
                    window.tutorialanimator.DestroyTextbox(curtextbox);
                    curtextbox = window.tutorialanimator.MakeTextbox(window.tutorialanimator.GetX() + 110, window.tutorialanimator.GetY() + 25, "Let's start off by looking for a mod to download.");

                    System.Windows.Point relativePoint = window.ModBrowser.TransformToAncestor(window).Transform(new System.Windows.Point(0, 0));

                    double tabX = relativePoint.X + 100;
                    double tabY = relativePoint.Y;

                    window.tutorialanimator.SetExpression("pointerup");
                    window.tutorialanimator.MoveTo(tabX - 50, tabY + 50);
                    window.tutorialanimator.DestroyTextbox(curtextbox);
                    curtextbox = window.tutorialanimator.MakeTextbox(window.tutorialanimator.GetX() + 110, window.tutorialanimator.GetY() + 25, "Click on browse mods to see what kind of goofy shenaenaes fellow people have been up to!");


                    window.ModBrowser.IsEnabled = true;

                    await PLUSWait.WaitUntil(() => window.ModBrowser.IsSelected);

                    window.ModManager.IsEnabled = false;

                    window.tutorialanimator.MoveTo(window.Width / 2, 200);
                    window.tutorialanimator.SetExpression("thinking");
                    window.tutorialanimator.DestroyTextbox(curtextbox);
                    curtextbox = window.tutorialanimator.MakeTextbox(window.tutorialanimator.GetX() + 110, window.tutorialanimator.GetY() + 25, "Hmmm let's see...");

                    await window.tutorialanimator.WaitForClickOnImageAsync();

                    window.tutorialanimator.SetExpression("happy");
                    window.tutorialanimator.DestroyTextbox(curtextbox);
                    curtextbox = window.tutorialanimator.MakeTextbox(window.tutorialanimator.GetX() + 110, window.tutorialanimator.GetY() + 25, "OH LOOK THERE I AM!");

                    await window.tutorialanimator.WaitForClickOnImageAsync();

                    window.tutorialanimator.SetExpression("sad");
                    window.tutorialanimator.DestroyTextbox(curtextbox);
                    curtextbox = window.tutorialanimator.MakeTextbox(window.tutorialanimator.GetX() + 110, window.tutorialanimator.GetY() + 25, "sniff... How heartwarming... someone made a mod for me! WOW!");

                    await window.tutorialanimator.WaitForClickOnImageAsync();
                    window.tutorialanimator.SetExpression("thinking");
                    window.tutorialanimator.DestroyTextbox(curtextbox);
                    curtextbox = window.tutorialanimator.MakeTextbox(window.tutorialanimator.GetX() + 110, window.tutorialanimator.GetY() + 25, "If you ever want a mod added to your collection, simply download it!");

                    await window.tutorialanimator.WaitForClickOnImageAsync();
                    window.tutorialanimator.SetExpression("inspect");
                    window.tutorialanimator.DestroyTextbox(curtextbox);
                    curtextbox = window.tutorialanimator.MakeTextbox(window.tutorialanimator.GetX() + 110, window.tutorialanimator.GetY() + 25, "Press on more info to check out the mod description!");

                    await window.tutorialanimator.WaitForClickOnImageAsync();
                    window.tutorialanimator.SetExpression("normal");
                    window.tutorialanimator.DestroyTextbox(curtextbox);
                    curtextbox = window.tutorialanimator.MakeTextbox(window.tutorialanimator.GetX() + 110, window.tutorialanimator.GetY() + 25, "Download this for me real quick, it really won't take long!");

                    RonnieVariables.AllowDownloadMod = true;

                    await WaitUntilTutorialDownloaded();

                    window.tutorialanimator.SetExpression("happy2");
                    window.tutorialanimator.DestroyTextbox(curtextbox);
                    curtextbox = window.tutorialanimator.MakeTextbox(window.tutorialanimator.GetX() + 110, window.tutorialanimator.GetY() + 25, "Yippee!!");

                    await window.tutorialanimator.WaitForClickOnImageAsync();

                    window.tutorialanimator.SetExpression("thinking");
                    window.tutorialanimator.DestroyTextbox(curtextbox);
                    curtextbox = window.tutorialanimator.MakeTextbox(window.tutorialanimator.GetX() + 110, window.tutorialanimator.GetY() + 25, "OKAY now that you have a cool mod to your collection, give it a lil swirl!");

                    await window.tutorialanimator.WaitForClickOnImageAsync();

                    System.Windows.Point relativePoint_2 = window.ModManager.TransformToAncestor(window).Transform(new System.Windows.Point(0, 0));

                    double tabX_2 = relativePoint_2.X + 100;
                    double tabY_2 = relativePoint_2.Y;

                    window.tutorialanimator.SetExpression("pointerup");
                    window.tutorialanimator.MoveTo(tabX_2 - 50, tabY_2 + 50);
                    window.tutorialanimator.DestroyTextbox(curtextbox);
                    curtextbox = window.tutorialanimator.MakeTextbox(window.tutorialanimator.GetX() + 110, window.tutorialanimator.GetY() + 25, "Now Let's now click back");


                    window.ModManager.IsEnabled = true;

                    await PLUSWait.WaitUntil(() => window.ModManager.IsSelected);

                    window.tutorialanimator.MoveTo(window.Width / 6, 250);
                    window.ModBrowser.IsEnabled = false;
                }
            }
            else
            {
                await window.tutorialanimator.WaitForClickOnImageAsync();
                window.tutorialanimator.SetExpression("thinking");
                window.tutorialanimator.DestroyTextbox(curtextbox);
                curtextbox = window.tutorialanimator.MakeTextbox(window.tutorialanimator.GetX() + 110, window.tutorialanimator.GetY() + 25, "Oh it seems like you have my mod installed");
                await window.tutorialanimator.WaitForClickOnImageAsync();
                window.tutorialanimator.SetExpression("pointerup");
                window.tutorialanimator.DestroyTextbox(curtextbox);
                curtextbox = window.tutorialanimator.MakeTextbox(window.tutorialanimator.GetX() + 110, window.tutorialanimator.GetY() + 25, "We can get to launching!!");

                if (PLUSSavesystem.read_ini("Tutorial", "BrokenModSkip", "false") == "true")
                {
                    await PLUSWait.WaitSeconds(3);
                    window.tutorialanimator.SetExpression("thinking");
                    window.tutorialanimator.DestroyTextbox(curtextbox);
                    if (PLUSSavesystem.read_ini("Tutorial", "SettingsSection", "false") == "true")
                    {
                        window.tutorialanimator.SetExpression("sad");
                        curtextbox = window.tutorialanimator.MakeTextbox(window.tutorialanimator.GetX() + 110, window.tutorialanimator.GetY() + 25, "Wait I NOW REMEMBER THIS MOD WAS BROKEN. we should move to settings....");
                        RonnieVariables.BrokenModSkip = true;
                        await window.tutorialanimator.WaitForClickOnImageAsync();
                    }
                    else
                    {
                        curtextbox = window.tutorialanimator.MakeTextbox(window.tutorialanimator.GetX() + 110, window.tutorialanimator.GetY() + 25, "Oh the mod might be broken uhm you can skip if you want lol to settings section or you can click me to try again");

                        await PLUSWait.WaitSeconds(1);
                        await window.tutorialanimator.MakeSkipButtonAsync(window.tutorialanimator._overlayCanvas, () =>
                        {
                            RonnieVariables.BrokenModSkip = true;
                        });

                        window.tutorialanimator._overlayCanvas.IsHitTestVisible = false;
                    }

                }
                else
                {
                    await window.tutorialanimator.WaitForClickOnImageAsync();
                }

            }
            if (!RonnieVariables.BrokenModSkip)
            {
                RonnieVariables.SetupAllow = true;
                window.tutorialanimator.SetExpression("thinking");
                window.tutorialanimator.DestroyTextbox(curtextbox);
                curtextbox = window.tutorialanimator.MakeTextbox(window.tutorialanimator.GetX() + 110, window.tutorialanimator.GetY() + 25, "Before we do this though, we need to make sure we have your files in check! Click on setup just to make sure...");


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
                        await PLUSWait.WaitSeconds(3);
                        if (RonnieVariables.SetupSucessful == 0)
                        {
                            window.tutorialanimator.SetExpression("sad");
                            window.tutorialanimator.DestroyTextbox(curtextbox);

                            curtextbox = window.tutorialanimator.MakeTextbox(window.tutorialanimator.GetX() + 110, window.tutorialanimator.GetY() + 25, "Looks like I wasn't able to find your Pizza Tower folder.. Could you click on it for me, pretty pleaaaase?");

                            RonnieVariables.SetupSucessful = -1;
                        }
                    }
                };


                setupTimer.Start();

                await PLUSWait.WaitUntil(() => RonnieVariables.SetupSucessful == 1);

                setupTimer.Stop();

                window.tutorialanimator.SetExpression("happy");
                window.tutorialanimator.DestroyTextbox(curtextbox);
                curtextbox = window.tutorialanimator.MakeTextbox(window.tutorialanimator.GetX() + 110, window.tutorialanimator.GetY() + 25, "Alright! Now we're all fired up! First select the mod...");

                await WaitForRonnieModClick();

                window.tutorialanimator.SetExpression("normal");
                window.tutorialanimator.DestroyTextbox(curtextbox);
                curtextbox = window.tutorialanimator.MakeTextbox(window.tutorialanimator.GetX() + 110, window.tutorialanimator.GetY() + 25, "And then launch it!");

                if (!IsTutorialDownloaded())
                {
                    await PLUSWait.WaitSeconds(3);
                    window.tutorialanimator.SetExpression("sad");
                    window.tutorialanimator.DestroyTextbox(curtextbox);
                    curtextbox = window.tutorialanimator.MakeTextbox(window.tutorialanimator.GetX() + 110, window.tutorialanimator.GetY() + 25, "Wait... You were not meant to delete the mod...");
                    await PLUSWait.WaitSeconds(3);
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
                        window.tutorialanimator.SetExpression("thinking");
                        window.tutorialanimator.DestroyTextbox(curtextbox);
                        curtextbox = window.tutorialanimator.MakeTextbox(window.tutorialanimator.GetX() + 110, window.tutorialanimator.GetY() + 25, "Oh... that's weird... Uhmmmm why is it... not... working...");
                        await window.tutorialanimator.WaitForClickOnImageAsync();

                        window.tutorialanimator.SetExpression("sad");
                        window.tutorialanimator.DestroyTextbox(curtextbox);
                        curtextbox = window.tutorialanimator.MakeTextbox(window.tutorialanimator.GetX() + 110, window.tutorialanimator.GetY() + 25, "Oh god im such a useless failure aren't I... I can't do anything right!");
                        await window.tutorialanimator.WaitForClickOnImageAsync();

                        window.tutorialanimator.SetExpression("thinking");
                        window.tutorialanimator.DestroyTextbox(curtextbox);
                        curtextbox = window.tutorialanimator.MakeTextbox(window.tutorialanimator.GetX() + 110, window.tutorialanimator.GetY() + 25, "Hmm... oh I know! you should try messing around your steam settings! Trust me, it's super simple...");
                        await window.tutorialanimator.WaitForClickOnImageAsync();

                        window.tutorialanimator.DestroyTextbox(curtextbox);
                        curtextbox = window.tutorialanimator.MakeTextbox(window.tutorialanimator.GetX() + 110, window.tutorialanimator.GetY() + 25, "Then, going into your steam, go to Pizza Tower and click on properties.");
                        await window.tutorialanimator.WaitForClickOnImageAsync();

                        window.tutorialanimator.DestroyTextbox(curtextbox);
                        curtextbox = window.tutorialanimator.MakeTextbox(window.tutorialanimator.GetX() + 110, window.tutorialanimator.GetY() + 25, "You're gonna want to click on Installed files and then verify integrity Once you've done that and it says no files are missing");
                        await window.tutorialanimator.WaitForClickOnImageAsync();

                        window.tutorialanimator.SetExpression("happy");
                        window.tutorialanimator.DestroyTextbox(curtextbox);
                        curtextbox = window.tutorialanimator.MakeTextbox(window.tutorialanimator.GetX() + 110, window.tutorialanimator.GetY() + 25, "try launching the mod again after that. make sure this will overwrite your existing modifed pt files");
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
                        await PLUSWait.WaitUntil(() =>
                        Process.GetProcessesByName(processName).Length > 0);
                        PLUSMUSIC.SetTutorialMusicPaused(true);
                        await PLUSWait.WaitUntil(() =>
                        Process.GetProcessesByName(processName).Length == 0);
                        PLUSMUSIC.SetTutorialMusicPaused(false);
                        if (!File.Exists(path) && !RonnieVariables.FinishedLaunch)
                        {
                            window.tutorialanimator.SetExpression("sad");
                            window.tutorialanimator.DestroyTextbox(curtextbox);
                            curtextbox = window.tutorialanimator.MakeTextbox(window.tutorialanimator.GetX() + 110, window.tutorialanimator.GetY() + 25, "Atleast finish the intro...");
                            await PLUSWait.WaitSeconds(3);
                            if (RonnieVariables.ModLaunchAmount > 1)
                            {
                                window.tutorialanimator.SetExpression("thinking");
                                window.tutorialanimator.DestroyTextbox(curtextbox);
                                curtextbox = window.tutorialanimator.MakeTextbox(window.tutorialanimator.GetX() + 110, window.tutorialanimator.GetY() + 25, "Or I mean if the mod isn't working you can close PizzaOven+ and I will offer a skip");
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

                await PLUSWait.WaitUntil(() => File.Exists(path));
                RonnieVariables.FinishedLaunch = true;
                window.ConfigButton.IsEnabled = false;
                window.LaunchButton.IsEnabled = false;
                exetimer.Stop();
                PLUSMUSIC.SetTutorialMusicPaused(false);
                File.Delete(path);
                window.tutorialanimator.SetExpression("sad");
                window.tutorialanimator.DestroyTextbox(curtextbox);
                PLUSSavesystem.write_ini("Tutorial", "SettingsSection", "true");
                curtextbox = window.tutorialanimator.MakeTextbox(window.tutorialanimator.GetX() + 110, window.tutorialanimator.GetY() + 25, "someone is trolling me that mod SUCKED...");


                await window.tutorialanimator.WaitForClickOnImageAsync();
                window.tutorialanimator.SetExpression("dumb");
                window.tutorialanimator.DestroyTextbox(curtextbox);
                curtextbox = window.tutorialanimator.MakeTextbox(window.tutorialanimator.GetX() + 110, window.tutorialanimator.GetY() + 25, "ANYWAYS I think that was pretty helpful, don't you think?");
            }

            System.Windows.Point relativePoint_3 = window.Settings.TransformToAncestor(window).Transform(new System.Windows.Point(0, 0));

            double tabX_3 = relativePoint_3.X + 100;
            double tabY_3 = relativePoint_3.Y;

            window.tutorialanimator.SetExpression("pointerup");
            window.tutorialanimator.MoveTo(tabX_3 - 50, tabY_3 + 50);

            window.tutorialanimator.DestroyTextbox(curtextbox);
            curtextbox = window.tutorialanimator.MakeTextbox(window.tutorialanimator.GetX() + 110, window.tutorialanimator.GetY() + 25, "Let's go to settings!");
            window.Settings.IsEnabled = true;

            await PLUSWait.WaitUntil(() => window.Settings.IsSelected);

            PLUSSavesystem.write_ini("Tutorial", "SettingsSection", "true");
            window.Settings.IsEnabled = false;
            window.ModManager.IsEnabled = false;
            window.tutorialanimator.MoveTo(window.Width / 8, window.Height / 2);
            window.tutorialanimator.SetExpression("normal");
            window.tutorialanimator.DestroyTextbox(curtextbox);
            curtextbox = window.tutorialanimator.MakeTextbox(window.tutorialanimator.GetX() + 110, window.tutorialanimator.GetY() + 25, "This where you can configure stuff");

            await window.tutorialanimator.WaitForClickOnImageAsync();
            window.tutorialanimator.SetExpression("happy");
            window.tutorialanimator.DestroyTextbox(curtextbox);
            RonnieVariables.RonnieExplainSettings = 0;
            curtextbox = window.tutorialanimator.MakeTextbox(window.tutorialanimator.GetX() + 110, window.tutorialanimator.GetY() + 25, "Feel free to look around to see what I can cater pookie, you may also click on me when you're done or hover over settings for a brief overview");

            await PLUSWait.WaitUntil(() => RonnieVariables.RonnieExplainSettings == 1);

            window.tutorialanimator.DestroyTextbox(curtextbox);

            await window.tutorialanimator.WaitForClickOnImageAsync();
            window.tutorialanimator.MoveTo(window.Width / 6, 250);
            window.ModManager.IsEnabled = true;
            window.Settings.IsEnabled = false;
            window.ModManager.IsSelected = true;

            window.tutorialanimator.SetExpression("thinking");
            window.tutorialanimator.DestroyTextbox(curtextbox);
            curtextbox = window.tutorialanimator.MakeTextbox(window.tutorialanimator.GetX() + 110, window.tutorialanimator.GetY() + 25, "And that's about all the wacky things you can do here, really! Not so wacky now, huh?");

            await window.tutorialanimator.WaitForClickOnImageAsync();
            window.tutorialanimator.DestroyTextbox(curtextbox);
            curtextbox = window.tutorialanimator.MakeTextbox(window.tutorialanimator.GetX() + 110, window.tutorialanimator.GetY() + 25, "If you want me to improve on this tool, then make sure to spell it out for me on feedback in the Links section, because believe it or not, I can in fact read.");

            await window.tutorialanimator.WaitForClickOnImageAsync();
            window.tutorialanimator.DestroyTextbox(curtextbox);
            curtextbox = window.tutorialanimator.MakeTextbox(window.tutorialanimator.GetX() + 110, window.tutorialanimator.GetY() + 25, "I won't take too much of your time, so how about you embark on your own journey!");

            await window.tutorialanimator.WaitForClickOnImageAsync();
            window.tutorialanimator.SetExpression("happy2");
            window.tutorialanimator.DestroyTextbox(curtextbox);
            curtextbox = window.tutorialanimator.MakeTextbox(window.tutorialanimator.GetX() + 110, window.tutorialanimator.GetY() + 25, "I am still saying right here by your side, probably toiling away for you for the rest of eternity. YAY!!");

            PLUSSavesystem.write_ini("Tutorial", "Finished", "true");
            window.tutorialanimator.DestroyTextbox(curtextbox);
            await window.tutorialanimator.DanceAsync(3, 60);

            TutorialState();
        }
        public static async Task RunIntro(MainWindow window)
        {
            window.Settings.IsEnabled = false;
            var curtextbox = window.introanimator.MakeTextbox(window.introanimator.GetX() + 110, window.introanimator.GetY() + 25, "");
            if (!RonnieVariables.ModDeleted)
            {
                window.introanimator.SetExpression("happy2");
                window.introanimator.DestroyTextbox(curtextbox);
                curtextbox = window.introanimator.MakeTextbox(window.introanimator.GetX() + 110, window.introanimator.GetY() + 25, "I saw that broken mod's thumbnail and deleted the mod for you! You are welcome");
                await PLUSWait.WaitSeconds(5);
            }

            if (RonnieVariables.KeptMod)
            {
                window.introanimator.SetExpression("inspect");
                window.introanimator.DestroyTextbox(curtextbox);
                curtextbox = window.introanimator.MakeTextbox(window.introanimator.GetX() + 110, window.introanimator.GetY() + 25, "I also know you kept the mod and played it after the tutorial");
                await PLUSWait.WaitSeconds(5);
            }
            window.introanimator.DestroyTextbox(curtextbox);
            window.introanimator.SetExpression("normal");
            curtextbox = window.introanimator.MakeTextbox(window.introanimator.GetX() + 110, window.introanimator.GetY() + 25, "Safe Travels");
            var _followtimer = new DispatcherTimer();
            _followtimer.Interval = TimeSpan.FromSeconds(0.01);
            _followtimer.Tick += (s, e) =>
            {
                Canvas.SetLeft(window.introanimator.GetTextbox(curtextbox), window.introanimator.GetX() + 110);
                Canvas.SetTop(window.introanimator.GetTextbox(curtextbox), window.introanimator.GetY() + 25);
            };
            _followtimer.Start();
            window.Settings.IsEnabled = true;

            window.introanimator.GlideTo((window.Width / 2), -300, 5);
            await PLUSWait.WaitUntil(() => window.introanimator.GetY() <= 0);
            _followtimer.Stop();
            window.introanimator.DestroyTextbox(curtextbox);
            await PLUSWait.WaitUntil(() => window.introanimator.GetY() <= -300);

            window.introanimator.Destroy();
        }

        public static void TutorialState(string finished = "true")
        {
            PLUSSavesystem.write_ini("Tutorial", "Finished", finished);
            PLUSSavesystem.write_ini("Tutorial", "BrokenModSkip", "false");
            PLUSSavesystem.write_ini("Tutorial", "SettingsSection", "false");
            string exePath = $"{AppDomain.CurrentDomain.BaseDirectory}{Global.s}{AppDomain.CurrentDomain.FriendlyName}";
            Process.Start(exePath);
            Application.Current.Shutdown();
        }
    }
}
