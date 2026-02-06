using System;
using System.IO;
using NAudio.Wave;
using System.Threading.Tasks;

namespace PizzaOven
{
    public static class PLUSMUSIC
    {
        private static WaveOutEvent? outputDevice;
        private static AudioFileReader? startReader;
        private static AudioFileReader? loopReader;

        private static LoopStream? loopStream;

        public static async Task InitializeAsync()
        {
            string customAssets = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),"PizzaOvenPLUS", "CustomAssets");

            string startFile = Path.Combine(customAssets, "BGMusic_Start.mp3");
            string loopFile  = Path.Combine(customAssets, "BGMusic_Loop.mp3");

            outputDevice = new WaveOutEvent();

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
            outputDevice?.Stop();
            startReader?.Dispose();
            loopReader?.Dispose();
            outputDevice?.Dispose();

            startReader = null;
            loopReader = null;
            outputDevice = null;
            loopStream = null;
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
