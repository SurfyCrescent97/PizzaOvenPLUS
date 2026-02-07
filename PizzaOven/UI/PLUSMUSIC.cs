using System;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows;
using NAudio.Wave;

namespace PizzaOven
{
    public static class PLUSMUSIC
    {
        private static WaveOutEvent? outputDevice;
        private static AudioFileReader? startReader;
        private static AudioFileReader? loopReader;
        private static LoopStream? loopStream;

        private static FileSystemWatcher? bgMusicWatcher;

        public static bool unfocusedMuteEnabled = true;
        public static bool MuteEnabled = true;
        private static float foregroundVolume = 1.0f;

        public static async Task InitializeAsync()
        {
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

            unfocusedMuteEnabled = PLUSSavesystem.read_ini("Audio", "UnfocusedMute", "true").ToLower() == "true";
            MuteEnabled = PLUSSavesystem.read_ini("Audio", "Mute", "true").ToLower() == "true";

            string customAssets = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),"PizzaOvenPLUS","CustomAssets");

            string startFile = Path.Combine(customAssets, "BGMusic_Start.mp3");
            string loopFile = Path.Combine(customAssets, "BGMusic_Loop.mp3");

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

            if (File.Exists(startFile))
            {
                startReader = new AudioFileReader(startFile);
                outputDevice.Init(startReader);
                outputDevice.Play();

                await WaitForPlaybackEndAsync(outputDevice);
            }

            if (File.Exists(loopFile))
            {
                loopReader = new AudioFileReader(loopFile);
                loopStream = new LoopStream(loopReader);

                outputDevice.Init(loopStream);
                outputDevice.Play();
            }
        }

        public static void ApplyCurrentVolume()
        {
            bool appIsActive = true;
            foregroundVolume = MuteEnabled ? 0 : 1;

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

            outputDevice?.Stop();
            startReader?.Dispose();
            loopReader?.Dispose();
            outputDevice?.Dispose();

            startReader = null;
            loopReader = null;
            outputDevice = null;
            loopStream = null;
        }

        public static void StartMusicWatcher()
        {
            string customAssets = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "PizzaOvenPLUS",
                "CustomAssets"
            );

            if (!Directory.Exists(customAssets))
                Directory.CreateDirectory(customAssets);

            bgMusicWatcher = new FileSystemWatcher(customAssets)
            {
                NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName | NotifyFilters.Size,
                Filter = "*.mp3",
                EnableRaisingEvents = true,
                IncludeSubdirectories = false
            };

            bgMusicWatcher.Changed += OnMusicFileChanged;
            bgMusicWatcher.Renamed += OnMusicFileChanged;
        }

        private static void OnMusicFileChanged(object sender, FileSystemEventArgs e)
        {
            string fileName = Path.GetFileName(e.FullPath);
            if (fileName == "BGMusic_Start.mp3" || fileName == "BGMusic_Loop.mp3")
            {
                Task.Delay(100).ContinueWith(async _ => await InitializeAsync());
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

            while (totalBytesRead < count)
            {
                int bytesRead = sourceStream.Read(buffer, offset + totalBytesRead, count - totalBytesRead);
                if (bytesRead == 0)
                {
                    sourceStream.Position = 0;
                }
                totalBytesRead += bytesRead;
            }

            return totalBytesRead;
        }
    }
}
