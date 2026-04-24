using System;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using static PizzaOven.MainWindow;
using static System.Net.Mime.MediaTypeNames;

namespace PizzaOven
{
    /// <summary>
    /// Interaction logic for PLUSAnnouncementWindow.xaml
    /// </summary>
    /// 

    public partial class PLUSAnnouncementWindow : Window
    {
        public bool IsClosed { get; private set; }
        public class PLUSAnnouncement
        {
            public DateTime date { get; set; }
            public bool enabled { get; set; }
            public string message { get; set; }
            public string expression { get; set; }
            public bool shake { get; set; }
            public string url { get; set; }
        }
        public static PLUSAnnouncement GetLatestAnnouncement()
        {
            string url = "https://raw.githubusercontent.com/SurfyCrescent97/PizzaOvenPLUS/main/announcements.json";

            using HttpClient client = new HttpClient();

            string json = client.GetStringAsync(url).GetAwaiter().GetResult();

            return JsonSerializer.Deserialize<PLUSAnnouncement>(json);
        }
        public double MeasureTextboxHeightWithTextBlock(string text, double boxWidth = 373, double fontSize = 21, double sidePadding = 35, double topHeight = 19, double bottomHeight = 19, double middleSliceHeight = 5)
        { 
            var textBlock = new TextBlock
            {
                Text = text,
                Width = boxWidth - sidePadding * 2,
                TextWrapping = TextWrapping.Wrap,
                FontSize = fontSize
            };
            textBlock.Measure(new Size(boxWidth - sidePadding * 2, double.PositiveInfinity));
            double textHeight = textBlock.DesiredSize.Height;

            int middleCount = (int)Math.Ceiling(textHeight / middleSliceHeight);

            double totalHeight = topHeight + middleCount * middleSliceHeight + bottomHeight;

            return totalHeight;
        }
        private async void ShowAnnouncement()
        {
            try
            {
                PLUSAnnouncement ann = GetLatestAnnouncement();
                var parse = PLUSSavesystem.read_ini("Announcement", "lastshown", "");
                if (parse != "")
                {
                    DateTimeOffset parsed = DateTimeOffset.Parse(parse);
                    if (parsed > ann.date.ToUniversalTime())
                    {
                        this.Close();
                        return;
                    }
                }
                if (!ann.enabled)
                {
                    this.Close();
                    return;
                }
                announcewindowanimator = new PLUSRonnieAnimate();
                announcewindowanimator.Initialize(this, 0, 50, 1.5);

                try
                {
                    announcewindowanimator.SetExpression(ann.expression);
                }
                catch { }

                if (ann.shake)
                {
                    announcewindowanimator.ShakeVisual(5, 5);
                }

                try
                {
                    var curtextbox = announcewindowanimator.MakeTextbox(announcewindowanimator.GetX() + 110, announcewindowanimator.GetY() + 25, ann.message);
                    this.Height = MeasureTextboxHeightWithTextBlock(ann.message) + 190;
                }
                catch { }

                this.Width = 500;
                this.SizeChanged += (s, e) =>
                {
                    if (announcewindowanimator?._overlayCanvas == null)
                    {
                        return;
                    }
                    if (IsClosed) 
                    { 
                        return; 
                    }

                    announcewindowanimator._overlayCanvas.Width = this.ActualWidth;
                    announcewindowanimator._overlayCanvas.Height = this.ActualHeight;
                };

                PLUSSavesystem.write_ini("Announcement", "lastshown", DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffffffZ"));


            } 
            catch 
            {
                this.Close();
                return;
            }



        }

        public PLUSRonnieAnimate announcewindowanimator;
        public PLUSAnnouncementWindow()
        {
            Closed += (s, e) => IsClosed = true;
            InitializeComponent();
            ShowAnnouncement();
        }
        
    }
}