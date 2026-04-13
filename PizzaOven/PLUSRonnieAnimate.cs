using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Timers;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;


namespace PizzaOven
{
    public class PLUSRonnieAnimate
    {
        public Canvas _overlayCanvas;
        private Window _window;
        private Image _image;
        private double _targetX;
        private double _targetY;
        private double _speed;
        private DispatcherTimer _timer;
        private DispatcherTimer _shakeTimer;
        private double _shakeMagnitude;
        private DateTime _shakeEndTime;
        private double _baseX;
        private double _baseY;
        private Random _random = new Random();
        public Dictionary<int, Canvas> _textboxes = new Dictionary<int, Canvas>();
        private int _nextTextboxId = 1;



        public void Initialize(Window window, double startX, double startY, double scale = 1)
        {
            if (window == null)
                throw new ArgumentNullException(nameof(window));
            _window = window;
            _overlayCanvas = new Canvas
            {
                Background = Brushes.Transparent,
                IsHitTestVisible = false,
                Width = window.Width,
                Height = window.Height
            };

            Panel.SetZIndex(_overlayCanvas, 999);

            this._overlayCanvas.Width = window.ActualWidth;
            this._overlayCanvas.Height = window.ActualHeight;

            if (window.Content is Panel existingPanel)
            {
                existingPanel.Children.Add(_overlayCanvas);
            }
            else
            {
                var newGrid = new Grid();
                var oldContent = window.Content as UIElement;
                window.Content = null;

                if (oldContent != null)
                    newGrid.Children.Add(oldContent);

                newGrid.Children.Add(_overlayCanvas);
                window.Content = newGrid;
            }

            _image = new Image
            {
                Source = new BitmapImage(new Uri("pack://application:,,,/PizzaOven;component/OvenRonnie/normal.png")),
                RenderTransform = new ScaleTransform(scale, scale),
                RenderTransformOrigin = new Point(0.5, 0.5)
            };

            _overlayCanvas.Children.Add(_image);

            Canvas.SetLeft(_image, startX);
            Canvas.SetTop(_image, startY);
        }

        public async Task DanceAsync(int times, int delayMs = 200)
        {
            for (int i = 0; i < times; i++)
            {
                this.SetExpression("happy");
                await Task.Delay(delayMs);

                this.SetExpression("pointerup");
                await Task.Delay(delayMs);

                this.SetExpression("happy2");
                await Task.Delay(delayMs);

                this.SetExpression("pointerup");
                await Task.Delay(delayMs);

            }
        }

        public void Destroy()
        {
            if (_timer != null)
            {
                _timer.Stop();
                _timer.Tick -= Timer_Tick;
                _timer = null;
            }

            if (_image != null && _overlayCanvas != null)
            {
                _overlayCanvas.Children.Remove(_image);
                _image = null;
            }

            if (_overlayCanvas != null)
            {
                var parent = _overlayCanvas.Parent as Panel;
                parent?.Children.Remove(_overlayCanvas);
                _overlayCanvas = null;
            }
        }

        public void SetExpression(string expression)
        {
            _image.Source = new BitmapImage(new Uri($"pack://application:,,,/PizzaOven;component/OvenRonnie/{expression}.png", UriKind.Absolute));
        }

        public void SetExpressionImage(ImageSource imageSource)
        {
            _image.Source = imageSource;
        }

        public void ShakeVisual(double magnitude, double seconds)
        {
            if (_image == null) return;

            TransformGroup group;

            if (_image.RenderTransform is TransformGroup existingGroup)
            {
                group = existingGroup;
            }
            else
            {
                group = new TransformGroup();

                if (_image.RenderTransform != null &&
                    !(_image.RenderTransform is MatrixTransform matrix && matrix.Matrix.IsIdentity))
                {
                    group.Children.Add(_image.RenderTransform);
                }

                _image.RenderTransform = group;
            }

            var transform = group.Children
                .OfType<TranslateTransform>()
                .FirstOrDefault();

            if (transform == null)
            {
                transform = new TranslateTransform();
                group.Children.Add(transform);
            }

            var random = new Random();
            var endTime = DateTime.Now.AddSeconds(seconds);

            var shakeTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(16)
            };

            shakeTimer.Tick += (s, e) =>
            {
                if (DateTime.Now >= endTime)
                {
                    transform.X = 0;
                    transform.Y = 0;
                    shakeTimer.Stop();
                    return;
                }

                transform.X = (random.NextDouble() * 2 - 1) * magnitude;
                transform.Y = (random.NextDouble() * 2 - 1) * magnitude;
            };

            shakeTimer.Start();
        }

        public int GetX()
        {
            if (_image == null)
                return 0;
            return (int)Canvas.GetLeft(_image);
        }

        public int GetY()
        {
            if (_image == null)
                return 0;
            return (int)Canvas.GetTop(_image);
        }

        public void MoveTo(double x, double y)
        {
            if (_image != null)
            {
                Canvas.SetLeft(_image, x);
                Canvas.SetTop(_image, y);
            }
        }

        public void GlideTo(double x, double y, double speed = 5)
        {
            if (_image == null) return;

            _targetX = x;
            _targetY = y;
            _speed = speed;

            if (_timer == null)
            {
                _timer = new DispatcherTimer();
                _timer.Interval = TimeSpan.FromMilliseconds(16);
                _timer.Tick += Timer_Tick;
            }

            _timer.Start();
        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            if (_image == null)
            {
                _timer.Stop();
                return;
            }

            double currentX = Canvas.GetLeft(_image);
            double currentY = Canvas.GetTop(_image);

            double dx = _targetX - currentX;
            double dy = _targetY - currentY;

            if (Math.Abs(dx) < 1 && Math.Abs(dy) < 1)
            {
                Canvas.SetLeft(_image, _targetX);
                Canvas.SetTop(_image, _targetY);
                _timer.Stop();
                return;
            }

            double distance = Math.Sqrt(dx * dx + dy * dy);
            double moveX = dx / distance * _speed;
            double moveY = dy / distance * _speed;

            if (Math.Abs(moveX) > Math.Abs(dx)) moveX = dx;
            if (Math.Abs(moveY) > Math.Abs(dy)) moveY = dy;

            Canvas.SetLeft(_image, currentX + moveX);
            Canvas.SetTop(_image, currentY + moveY);
        }

        public async Task MakeSkipButtonAsync(Canvas parent, Action onClickAction)
        {
            var skipButton = new Button
            {
                Content = new Image
                {
                    Source = new BitmapImage(
                        new Uri("pack://application:,,,/PizzaOven;component/OvenRonnie/skip.png")),
                    Stretch = Stretch.None
                },

                Style = null,                   

                Background = Brushes.Transparent,
                BorderBrush = Brushes.Transparent,
                BorderThickness = new Thickness(0),
                Padding = new Thickness(0),

                Focusable = false,
                FocusVisualStyle = null,

                Template = new ControlTemplate(typeof(Button))
                {
                    VisualTree = new FrameworkElementFactory(typeof(ContentPresenter))
                }
            };

            parent.IsHitTestVisible = true;

            parent.Children.Add(skipButton);

            Canvas.SetRight(skipButton, 10);
            Canvas.SetBottom(skipButton, 30);

            var tcs = new TaskCompletionSource<bool>();

            skipButton.Click += (s, e) =>
            {
                tcs.TrySetResult(true);
            };
            
            await PLUSWait.WaitSeconds(1);
            var finishedTask = await Task.WhenAny(tcs.Task, this.WaitForClickOnImageAsync());

            if (parent.Children.Contains(skipButton))
                parent.Children.Remove(skipButton);

            if (finishedTask == tcs.Task)
                onClickAction?.Invoke();
        }




        private Image CreateImageTextBox(string relativePath, double width)
        {
            return new Image
            {
                Source = new BitmapImage(
                    new Uri($"pack://application:,,,/PizzaOven;component/{relativePath}")),
                Width = width,
                Stretch = Stretch.None
            };
        }

        public static double MeasureTextBlockHeight(string text, double boxWidth = 373, double sidePadding = 35, double fontSize = 21, double topHeight = 19, double bottomHeight = 19)
        {
            var tb = new TextBlock
            {
                Text = text,
                Width = boxWidth - sidePadding * 2,
                TextWrapping = TextWrapping.Wrap,
                FontSize = fontSize,
                Foreground = Brushes.Black
            };

            tb.Measure(new Size(tb.Width, double.PositiveInfinity));
            double textHeight = tb.DesiredSize.Height;

            double totalMiddleHeight = textHeight;
            int middleCount = (int)Math.Ceiling(totalMiddleHeight);

            double totalHeight = topHeight + middleCount + bottomHeight;

            tb = null;

            return totalHeight;
        }


        public int MakeTextbox(double x, double y, string text)
        {
            if (_overlayCanvas == null)
                return -1;

            const double boxWidth = 373;
            const double sidePadding = 35;

            var container = new Canvas();
            int textboxId = _nextTextboxId++;

            container.Tag = textboxId;

            var topImg = CreateImageTextBox("OvenRonnie/textbox_top.png", boxWidth);
            var bottomImg = CreateImageTextBox("OvenRonnie/textbox_bottom.png", boxWidth);

            var textBlock = new TextBlock
            {
                Text = text,
                Width = boxWidth - sidePadding * 2,
                TextWrapping = TextWrapping.Wrap,
                Foreground = Brushes.Black,
                FontSize = 21
            };

            textBlock.Measure(new Size(boxWidth - sidePadding * 2, double.PositiveInfinity));
            double textHeight = textBlock.DesiredSize.Height;

            double topHeight = 19;
            double bottomHeight = 19;

            double totalMiddleHeight = textHeight;
            int middleCount = (int)Math.Ceiling(totalMiddleHeight);

            for (int i = 0; i < middleCount; i++)
            {
                var middleSlice = CreateImageTextBox("OvenRonnie/textbox_middle.png", boxWidth);
                Canvas.SetTop(middleSlice, topHeight + i);
                container.Children.Add(middleSlice);
            }

            Canvas.SetTop(topImg, 0);
            container.Children.Add(topImg);

            Canvas.SetTop(bottomImg, topHeight + middleCount);
            container.Children.Add(bottomImg);

            Canvas.SetLeft(textBlock, sidePadding);
            Canvas.SetTop(textBlock, topHeight);
            container.Children.Add(textBlock);

            Canvas.SetLeft(container, x);
            Canvas.SetTop(container, y);

            _overlayCanvas.Children.Add(container);
            _textboxes[textboxId] = container;

            return textboxId;
        }

        public Canvas GetTextbox(int id)
        {
            if (_textboxes.ContainsKey(id))
                return _textboxes[id];

            return null;
        }

        public void DestroyTextbox(int id)
        {
            if (!_textboxes.ContainsKey(id))
                return;

            var textbox = _textboxes[id];

            _overlayCanvas.Children.Remove(textbox);
            _textboxes.Remove(id);
        }

        public void SetTextboxText(int id, string newText)
        {
            if (!_textboxes.ContainsKey(id))
                return;

            var container = _textboxes[id];

            foreach (var child in container.Children)
            {
                if (child is TextBlock tb)
                {
                    tb.Text = newText;
                    break;
                }
            }
        }

        public static async Task WaitForMouseUpAsync()
        {
            while (Mouse.LeftButton == MouseButtonState.Pressed)
            {
                await Task.Delay(10);
            }
        }

        public async Task WaitForClickOnImageAsync()
        {
            if (_image == null || _overlayCanvas == null || _window == null)
                throw new InvalidOperationException("Image, overlayCanvas, or window not initialized.");

            while (Mouse.LeftButton == MouseButtonState.Pressed)
                await Task.Delay(10);

            var tcs = new TaskCompletionSource<bool>();

            void MouseDownHandler(object sender, MouseButtonEventArgs e)
            {
                Point pos = e.GetPosition(_overlayCanvas);

                double imgX = Canvas.GetLeft(_image);
                double imgY = Canvas.GetTop(_image);
                double imgW = _image.ActualWidth;
                double imgH = _image.ActualHeight;

                if (pos.X >= imgX && pos.X <= imgX + imgW &&
                    pos.Y >= imgY && pos.Y <= imgY + imgH)
                {
                    tcs.TrySetResult(true);
                }
            }

            _window.PreviewMouseLeftButtonDown += MouseDownHandler;

            await tcs.Task;

            _window.PreviewMouseLeftButtonDown -= MouseDownHandler;
        }
    }
}
