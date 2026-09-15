using NAudio.Wave;
using System;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace PizzaOven
{
    public static class PLUSMUSIC
    {
        private static WaveOutEvent? outputDevice;
        private static WaveStream? startReader;
        private static WaveStream? loopReader;
        private static LoopStream? loopStream;

        private static FileSystemWatcher? bgMusicWatcher;
        private static readonly object musicWatcherLock = new();

        public static bool unfocusedMuteEnabled = true;
        public static bool MuteEnabled = true;
        private static float foregroundVolume = 1.0f;

        private static WaveOutEvent tutorialOutput;
        private static WaveStream tutorialReader;
        private static LoopStream tutorialLoop;

        private static CancellationTokenSource initCts;

        private static readonly SemaphoreSlim initLock = new SemaphoreSlim(1, 1);

        public static string musicfolder = PLUSSavesystem.read_ini("Audio", "MusicFolder", "Default");
        public static async Task Play_TutorialMusic()
        {
            try
            {
                string resourceUri = "PizzaOven;component/OvenRonnie/TutorialMusic.wav";

                var streamResourceInfo = Application.GetResourceStream(new Uri($"pack://application:,,,/{resourceUri}"));

                var memoryStream = new MemoryStream();
                streamResourceInfo.Stream.CopyTo(memoryStream);
                memoryStream.Position = 0;

                tutorialReader = new WaveFileReader(memoryStream);
                tutorialLoop = new LoopStream(tutorialReader);

                tutorialOutput = new WaveOutEvent();
                tutorialOutput.Init(tutorialLoop);

                tutorialOutput.Volume = 1.0f;
                tutorialOutput.Play();
            }
            catch 
            {
               
            }
        }

        public static void Stop_TutorialMusic()
        {
            tutorialOutput?.Stop();
            tutorialReader?.Dispose();
            tutorialLoop?.Dispose();
            tutorialOutput?.Dispose();

            tutorialOutput = null;
            tutorialReader = null;
            tutorialLoop = null;
        }
        public static async Task FadeOutTutorialMusic(float durationSeconds = 2.0f)
        {
            if (tutorialOutput == null || tutorialReader == null)
                return;

            float startVolume = tutorialOutput.Volume;
            float fadeTime = durationSeconds;
            int steps = 20;
            float stepTime = fadeTime / steps;

            for (int i = 0; i < steps; i++)
            {
                tutorialOutput.Volume = startVolume * (1.0f - ((float)i / steps));
                await Task.Delay((int)(stepTime * 1000));
            }

            tutorialOutput.Volume = 0.0f;
            tutorialOutput.Stop();

            tutorialOutput.Dispose();
            tutorialLoop.Dispose();
            tutorialReader.Dispose();

            tutorialOutput = null;
            tutorialLoop = null;
            tutorialReader = null;
        }
        public static void Pause_TutorialMusic()
        {
            if (tutorialOutput == null) return;

            if (tutorialOutput.PlaybackState == PlaybackState.Playing)
            {
                tutorialOutput.Pause();
            }
            else if (tutorialOutput.PlaybackState == PlaybackState.Paused)
            {
                tutorialOutput.Play();
            }
        }
        public static void SetTutorialMusicPaused(bool paused)
        {
            if (tutorialOutput == null) return;

            if (paused)
            {
                if (tutorialOutput.PlaybackState == PlaybackState.Playing)
                    tutorialOutput.Pause();
            }
            else
            {
                if (tutorialOutput.PlaybackState == PlaybackState.Paused)
                    tutorialOutput.Play();
            }
        }
        public static async Task ReloadAsync()
        {
            Stop();
            await InitializeAsync();
        }
        public static async Task InitializeAsync()
        {
            await initLock.WaitAsync();
            try
            {
                if (Global.ronnietutorial)
                    return;

                lock (musicWatcherLock)
                {
                    bgMusicWatcher?.Dispose();
                    bgMusicWatcher = null;
                }

                if (outputDevice != null)
                {
                    outputDevice.Stop();
                    outputDevice.Dispose();
                    outputDevice = null;
                }

                startReader?.Dispose();
                loopReader?.Dispose();
                loopStream?.Dispose();
                startReader = null;
                loopReader = null;
                loopStream = null;

                unfocusedMuteEnabled = PLUSSavesystem.read_ini_bool("Audio", "UnfocusedMute", true);
                MuteEnabled = PLUSSavesystem.read_ini_bool("Audio", "Mute", true);

                string customAssets = Global.customassetsfolder;
                string musicDirectory = Path.Combine(customAssets, "Music", musicfolder);
                string startFile = Path.Combine(musicDirectory, "BGMusic_Start.mp3");
                string loopFile = Path.Combine(musicDirectory, "BGMusic_Loop.mp3");

                outputDevice = new WaveOutEvent();
                ApplyCurrentVolume();

                try
                {
                    if (Application.Current != null)
                    {
                        Application.Current.Activated += OnAppActivated;
                        Application.Current.Deactivated += OnAppDeactivated;
                    }
                }
                catch { }

                try
                {
                    if (File.Exists(startFile))
                    {
                        startReader = await CreateMp3ReaderFromFileAsync(startFile);
                        if (startReader != null)
                        {
                            outputDevice.Init(startReader);
                            outputDevice.Play();
                            await WaitForPlaybackEndAsync(outputDevice);
                        }
                    }
                }
                catch { }

                try
                {
                    if (File.Exists(loopFile))
                    {
                        loopReader = await CreateMp3ReaderFromFileAsync(loopFile);
                        if (loopReader != null)
                        {
                            loopStream = new LoopStream(loopReader);
                            outputDevice.Init(loopStream);
                            outputDevice.Play();
                        }
                    }
                }
                catch { }

                StartMusicWatcher();
            }
            finally
            {
                initLock.Release();
            }
        }

        public static void ApplyCurrentVolume()
        {
            bool appIsActive = true;
            foregroundVolume = MuteEnabled ? 0 : 1;
            foregroundVolume *= float.Parse(PLUSSavesystem.read_ini("Audio", "SoundVolume", "100")) / 100f;

            try
            {
                if (Application.Current != null)
                {
                    appIsActive = Application.Current.MainWindow?.IsActive ?? true;
                }
            }
            catch
            {
                appIsActive = true;
            }

            if (outputDevice != null)
            {
                outputDevice.Volume = (unfocusedMuteEnabled && !appIsActive) ? 0.0f : foregroundVolume;
            }
        }

        private static void OnAppActivated(object? s, EventArgs e)
        {
            if (outputDevice != null)
            {
                outputDevice.Volume = foregroundVolume;
                outputDevice.Volume *= float.Parse(PLUSSavesystem.read_ini("Audio", "SoundVolume", "100")) / 100f;
            }
        }

        private static void OnAppDeactivated(object? s, EventArgs e)
        {
            if (outputDevice != null && unfocusedMuteEnabled)
            {
                outputDevice.Volume = 0.0f;
            }
        }

        private static Task WaitForPlaybackEndAsync(WaveOutEvent device)
        {
            var tcs = new TaskCompletionSource<bool>();

            void handler(object? s, StoppedEventArgs e)
            {
                device.PlaybackStopped -= handler;
                tcs.SetResult(true);
            }

            device.PlaybackStopped += handler;

            if (device.PlaybackState == PlaybackState.Stopped)
                tcs.SetResult(true);

            return tcs.Task;
        }

        public static void Stop()
        {
            try
            {
                if (Application.Current != null)
                {
                    Application.Current.Activated -= OnAppActivated;
                    Application.Current.Deactivated -= OnAppDeactivated;
                }
            }
            catch { }

            StopAndDisposeOutputDevice();

            startReader?.Dispose();
            loopReader?.Dispose();
            loopStream?.Dispose();

            startReader = null;
            loopReader = null;
            loopStream = null;
        }

        private static async Task<WaveStream?> CreateMp3ReaderFromFileAsync(string filePath)
        {
            if (!File.Exists(filePath))
                return null;

            try
            {
                byte[] data = await File.ReadAllBytesAsync(filePath);
                var stream = new MemoryStream(data, writable: false);
                return new Mp3FileReader(stream);
            }
            catch
            {
                return null;
            }
        }

        public static void StartMusicWatcher()
        {
            lock (musicWatcherLock)
            {
                if (bgMusicWatcher != null)
                {
                    bgMusicWatcher.EnableRaisingEvents = false;
                    bgMusicWatcher.Dispose();
                    bgMusicWatcher = null;
                }

                string musicRoot = Path.Combine(Global.customassetsfolder, "Music");
                if (!Directory.Exists(musicRoot))
                    Directory.CreateDirectory(musicRoot);

                bgMusicWatcher = new FileSystemWatcher(musicRoot)
                {
                    NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName | NotifyFilters.Size | NotifyFilters.DirectoryName,
                    Filter = "*.mp3",
                    EnableRaisingEvents = true,
                    IncludeSubdirectories = true
                };

                bgMusicWatcher.Created += OnMusicFileChanged;
                bgMusicWatcher.Deleted += OnMusicFileChanged;
                bgMusicWatcher.Changed += OnMusicFileChanged;
                bgMusicWatcher.Renamed += OnMusicFileChanged;
            }
        }

        private static void OnMusicFileChanged(object sender, FileSystemEventArgs e)
        {
            try
            {
                string fileName = Path.GetFileName(e.FullPath);
                string targetFolder = Path.Combine(Global.customassetsfolder, "Music", musicfolder);
                string changedFolder = Path.GetDirectoryName(e.FullPath);

                if (string.Equals(fileName, "BGMusic_Start.mp3", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(fileName, "BGMusic_Loop.mp3", StringComparison.OrdinalIgnoreCase))
                {
                    if (string.Equals(changedFolder, targetFolder, StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(Path.GetDirectoryName(changedFolder), targetFolder, StringComparison.OrdinalIgnoreCase))
                    {
                        _ = Task.Run(async () =>
                        {
                            await Task.Delay(150);
                            await InitializeAsync();
                        });
                    }
                }
            }
            catch { }
        }

        private static void StopAndDisposeOutputDevice()
        {
            if (outputDevice == null) return;

            using var mre = new ManualResetEventSlim(false);
            void handler(object? s, StoppedEventArgs e) => mre.Set();

            outputDevice.PlaybackStopped += handler;
            try
            {
                try 
                { 
                    outputDevice.Stop(); 
                } 
                catch { }
                mre.Wait(2000); 
            }
            finally
            {
                outputDevice.PlaybackStopped -= handler;
                try 
                { 
                    outputDevice.Dispose(); 
                } catch { }
                outputDevice = null;
            }
        }
    }

    public class LoopStream : WaveStream
    {
        private readonly WaveStream sourceStream;

        public LoopStream(WaveStream source)
        {
            sourceStream = source;
        }

        public override WaveFormat WaveFormat => sourceStream.WaveFormat;
        public override long Length => sourceStream.Length;

        public override long Position
        {
            get => sourceStream.Position;
            set => sourceStream.Position = value;
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            int totalBytesRead = 0;
            try
            {
                while (totalBytesRead < count)
                {
                    int bytesRead = sourceStream.Read(buffer, offset + totalBytesRead, count - totalBytesRead);
                    if (bytesRead == 0)
                    {
                        if (sourceStream.Length == 0) break;
                        sourceStream.Position = 0;
                        bytesRead = sourceStream.Read(buffer, offset + totalBytesRead, count - totalBytesRead);
                        if (bytesRead == 0) break;
                    }
                    totalBytesRead += bytesRead;
                }
            }
            catch (ObjectDisposedException) { }
            catch (InvalidOperationException) { }
            return totalBytesRead;
        }

    }
}
