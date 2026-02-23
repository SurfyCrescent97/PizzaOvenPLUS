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
    /// Interaction logic for PLUSANNOUNCE.xaml
    /// </summary>
    /// 
    
    public partial class PLUSAnnouncementWindow : Window
    {
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
                PLUSAnnouncement ann = await GetLatestAnnouncement();
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
                announcewindowanimator._overlayCanvas.Width = this.ActualWidth;
                announcewindowanimator._overlayCanvas.Height = this.ActualHeight;

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
            InitializeComponent();
            ShowAnnouncement();
        }
        
    }
}