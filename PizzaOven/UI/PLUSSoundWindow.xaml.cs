using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace PizzaOven.UI
{
    public partial class PLUSSoundWindow : Window
    {
        private byte[] sound;

        private string outputMain;
        private string outputLoop;

        private double soundLength;

        private bool draggingStart;
        private bool draggingEnd;
        private bool draggingLoop;

        private double dragOffset;

        public List<string> filesToDelete = new List<string>();
        
        public PLUSSoundWindow()
        {
            InitializeComponent();

            LoopStartHandle.MouseLeftButtonDown += LoopStartHandle_MouseLeftButtonDown;
            LoopEndHandle.MouseLeftButtonDown += LoopEndHandle_MouseLeftButtonDown;
            LoopArea.MouseLeftButtonDown += LoopArea_MouseLeftButtonDown;

            WaveformCanvas.MouseMove += WaveformCanvas_MouseMove;
            WaveformCanvas.MouseLeftButtonUp += WaveformCanvas_MouseLeftButtonUp;
        }

        public static string ConvertToMp3(string inputPath, string outputPath)
        {
            string ffmpegPath = $"{Global.assemblyLocation}{Global.s}Dependencies{Global.s}AcoustIDFFmpeg{Global.s}ffmpeg.exe";

            using var process = new Process();

            process.StartInfo = new ProcessStartInfo
            {
                FileName = ffmpegPath,
                Arguments = $"-y -i \"{inputPath}\" -codec:a libmp3lame -b:a 192k \"{outputPath}\"",
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardError = true,
                WindowStyle = ProcessWindowStyle.Hidden,
                RedirectStandardOutput = true
            };

            process.Start();

            string error = process.StandardError.ReadToEnd();
            process.WaitForExit();

            if (process.ExitCode != 0)
            {
                throw new Exception($"FFmpeg failed with exit code {process.ExitCode}:\n{error}");
            }

            return outputPath;
        }

        public void Init(string soundPath, string outputMain, string outputLoop, double width, double height)
        {
            sound = File.ReadAllBytes(soundPath);

            this.outputMain = outputMain;
            this.outputLoop = outputLoop;

            Width = width;
            Height = height;

            using (var reader = new Mp3FileReader(soundPath))
            {
                soundLength = reader.TotalTime.TotalSeconds;
            }

            LoopStartText.Text = "00.00.00";
            LoopEndText.Text = FormatTime(soundLength);

            Loaded += (sender, e) =>
            {
                DrawWaveform(soundPath);
                UpdateLoopVisuals();
            };

            SizeChanged += (sender, e) =>
            {
                UpdateLoopVisuals();
            };
        }

        public void SetLoopRegion(double start, double end)
        {
            start = Math.Clamp(start, 0, soundLength);
            end = Math.Clamp(end, start, soundLength);

            LoopStartText.Text = FormatTime(start);
            LoopEndText.Text = FormatTime(end);

            UpdateLoopVisuals();
        }

        private void DrawWaveform(string soundPath)
        {
            double width = WaveformCanvas.ActualWidth;
            double height = WaveformCanvas.ActualHeight;

            if (width <= 0 || height <= 0)
            {
                return;
            }

            using var reader = new Mp3FileReader(soundPath);

            int channels = reader.WaveFormat.Channels;
            int bytesPerSample = reader.WaveFormat.BitsPerSample / 8;
            int bytesPerFrame = channels * bytesPerSample;

            if (bytesPerFrame <= 0)
            {
                return;
            }

            int sampleCount = Math.Max(1, (int)width);

            double[] peaks = new double[sampleCount];

            byte[] buffer = new byte[bytesPerFrame * 4096];

            long totalBytes = reader.Length;
            long bytesReadTotal = 0;

            int read;

            while ((read = reader.Read(buffer, 0, buffer.Length)) > 0)
            {
                int frameCount = read / bytesPerFrame;

                for (int frame = 0; frame < frameCount; frame++)
                {
                    long framePosition = bytesReadTotal + frame * bytesPerFrame;

                    double progress = (double)framePosition / totalBytes;

                    int x = (int)(progress * sampleCount);

                    if (x < 0)
                    {
                        x = 0;
                    }

                    if (x >= sampleCount)
                    {
                        x = sampleCount - 1;
                    }

                    double amplitude = 0;

                    for (int channel = 0; channel < channels; channel++)
                    {
                        int offset = frame * bytesPerFrame + channel * bytesPerSample;

                        if (bytesPerSample == 2)
                        {
                            short sample = BitConverter.ToInt16(buffer, offset);

                            amplitude += Math.Abs(sample / 32768.0);
                        }
                    }

                    amplitude /= channels;

                    if (amplitude > peaks[x])
                    {
                        peaks[x] = amplitude;
                    }
                }

                bytesReadTotal += read;
            }

            var points = new PointCollection();

            double centre = height / 2;
            double waveformHeight = centre * 0.9;

            for (int x = 0; x < sampleCount; x++)
            {
                double amplitude = peaks[x] * waveformHeight;

                points.Add(new Point(x, centre - amplitude));
            }

            for (int x = sampleCount - 1; x >= 0; x--)
            {
                double amplitude = peaks[x] * waveformHeight;

                points.Add(new Point(x, centre + amplitude));
            }

            Waveform.Points = points;
        }

        private void UpdateLoopVisuals()
        {
            double width = WaveformCanvas.ActualWidth;
            double height = WaveformCanvas.ActualHeight;

            if (width <= 0 || height <= 0 || soundLength <= 0)
            {
                return;
            }

            double start = ParseTime(LoopStartText.Text);
            double end = ParseTime(LoopEndText.Text);

            start = Math.Clamp(start, 0, soundLength);
            end = Math.Clamp(end, 0, soundLength);

            double startX = start / soundLength * width;
            double endX = end / soundLength * width;

            Canvas.SetLeft(LoopArea, startX);
            Canvas.SetTop(LoopArea, 0);

            LoopArea.Width = Math.Max(0, endX - startX);
            LoopArea.Height = height;

            Canvas.SetLeft(LoopStartHandle, startX - 2);
            Canvas.SetTop(LoopStartHandle, 0);

            LoopStartHandle.Height = height;

            Canvas.SetLeft(LoopEndHandle, endX - 2);
            Canvas.SetTop(LoopEndHandle, 0);

            LoopEndHandle.Height = height;
        }

        private void LoopStartHandle_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            draggingStart = true;

            LoopStartHandle.CaptureMouse();

            e.Handled = true;
        }

        private void LoopEndHandle_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            draggingEnd = true;

            LoopEndHandle.CaptureMouse();

            e.Handled = true;
        }

        private void LoopArea_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            draggingLoop = true;

            double width = WaveformCanvas.ActualWidth;

            Point position = e.GetPosition(WaveformCanvas);

            double start = ParseTime(LoopStartText.Text);

            double startX = start / soundLength * width;

            dragOffset = position.X - startX;

            LoopArea.CaptureMouse();

            e.Handled = true;
        }

        private void WaveformCanvas_MouseMove(object sender, MouseEventArgs e)
        {
            if (!draggingStart && !draggingEnd && !draggingLoop)
            {
                return;
            }

            double width = WaveformCanvas.ActualWidth;

            if (width <= 0)
            {
                return;
            }

            Point position = e.GetPosition(WaveformCanvas);

            double time = position.X / width * soundLength;

            time = Math.Clamp(time, 0, soundLength);

            if (draggingStart)
            {
                double end = ParseTime(LoopEndText.Text);

                if (time > end)
                {
                    time = end;
                }

                LoopStartText.Text = FormatTime(time);
            }
            else if (draggingEnd)
            {
                double start = ParseTime(LoopStartText.Text);

                if (time < start)
                {
                    time = start;
                }

                LoopEndText.Text = FormatTime(time);
            }
            else if (draggingLoop)
            {
                double start = ParseTime(LoopStartText.Text);
                double end = ParseTime(LoopEndText.Text);

                double loopLength = end - start;

                double newStartX = position.X - dragOffset;

                double newStart = newStartX / width * soundLength;

                newStart = Math.Clamp(newStart, 0, soundLength - loopLength);

                double newEnd = newStart + loopLength;

                LoopStartText.Text = FormatTime(newStart);
                LoopEndText.Text = FormatTime(newEnd);
            }

            UpdateLoopVisuals();
        }

        private void WaveformCanvas_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            draggingStart = false;
            draggingEnd = false;
            draggingLoop = false;

            LoopStartHandle.ReleaseMouseCapture();
            LoopEndHandle.ReleaseMouseCapture();
            LoopArea.ReleaseMouseCapture();
        }

        private void LoopStartText_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                SetLoopStart();
                e.Handled = true;
            }
        }

        private void LoopEndText_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                SetLoopEnd();
                e.Handled = true;
            }
        }

        private void SetLoopStart()
        {
            double start = ParseTime(LoopStartText.Text);
            double end = ParseTime(LoopEndText.Text);

            start = Math.Clamp(start, 0, soundLength);

            if (start > end)
            {
                start = end;
            }

            LoopStartText.Text = FormatTime(start);

            UpdateLoopVisuals();
        }

        private void SetLoopEnd()
        {
            double start = ParseTime(LoopStartText.Text);
            double end = ParseTime(LoopEndText.Text);

            end = Math.Clamp(end, 0, soundLength);

            if (end < start)
            {
                end = start;
            }

            LoopEndText.Text = FormatTime(end);

            UpdateLoopVisuals();
        }

        private double ParseTime(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return 0;
            }

            string[] parts = text.Split('.');

            if (parts.Length != 3)
            {
                return 0;
            }

            if (!double.TryParse(parts[0], out double minutes))
            {
                return 0;
            }

            if (!double.TryParse(parts[1], out double seconds))
            {
                return 0;
            }

            if (!double.TryParse(parts[2], out double hundredths))
            {
                return 0;
            }

            seconds = Math.Clamp(seconds, 0, 59);
            hundredths = Math.Clamp(hundredths, 0, 99);

            return minutes * 60.0 + seconds + hundredths / 100.0;
        }

        private string FormatTime(double time)
        {
            time = Math.Max(0, time);

            int minutes = (int)(time / 60);

            time -= minutes * 60;

            int seconds = (int)time;

            int hundredths = (int)((time - seconds) * 100);

            return $"{minutes:00}.{seconds:00}.{hundredths:00}";
        }

        private void TimeTextBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            e.Handled = false;
        }

        private void ConfirmButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                double loopStart = ParseTime(LoopStartText.Text);
                double loopEnd = ParseTime(LoopEndText.Text);

                loopStart = Math.Clamp(loopStart, 0, soundLength);
                loopEnd = Math.Clamp(loopEnd, loopStart, soundLength);

                CreateOutputFiles(loopStart, loopEnd);

                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to create sound files.\n\n{ex.Message}", "Sound Editor", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CreateOutputFiles(double loopStart, double loopEnd)
        {
            string? mainDirectory = Path.GetDirectoryName(outputMain);
            string? loopDirectory = Path.GetDirectoryName(outputLoop);

            if (!string.IsNullOrEmpty(mainDirectory))
            {
                Directory.CreateDirectory(mainDirectory);
            }

            if (!string.IsNullOrEmpty(loopDirectory))
            {
                Directory.CreateDirectory(loopDirectory);
            }

            using var mainReader = new Mp3FileReader(new MemoryStream(sound));
            using var loopReader = new Mp3FileReader(new MemoryStream(sound));

            var mainProvider = new OffsetSampleProvider(mainReader.ToSampleProvider())
            {
                SkipOver = TimeSpan.Zero,
                Take = TimeSpan.FromSeconds(loopEnd)
            };

            var loopProvider = new OffsetSampleProvider(loopReader.ToSampleProvider())
            {
                SkipOver = TimeSpan.FromSeconds(loopStart),
                Take = TimeSpan.FromSeconds(loopEnd - loopStart)
            };

            SaveAsMp3(mainProvider, outputMain);
            SaveAsMp3(loopProvider, outputLoop);
        }

        private void SaveAsMp3(ISampleProvider source, string outputPath)
        {
            var waveProvider = new SampleToWaveProvider(source);

            byte[] wavData;

            using (var wavStream = new MemoryStream())
            {
                using (var writer = new WaveFileWriter(wavStream, waveProvider.WaveFormat))
                {
                    byte[] buffer = new byte[16384];

                    int bytesRead;

                    while ((bytesRead = waveProvider.Read(buffer, 0, buffer.Length)) > 0)
                    {
                        writer.Write(buffer, 0, bytesRead);
                    }

                    writer.Flush();
                }

                wavData = wavStream.ToArray();
            }

            using (var waveStream = new MemoryStream(wavData))
            using (var waveReader = new WaveFileReader(waveStream))
            {
                MediaFoundationEncoder.EncodeToMp3(waveReader, outputPath, 192000);
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);

            foreach (string path in filesToDelete)
            {
                try
                {
                    if (File.Exists(path))
                    {
                        File.Delete(path);
                    }
                }
                catch { }
            }
        }   
    }
}