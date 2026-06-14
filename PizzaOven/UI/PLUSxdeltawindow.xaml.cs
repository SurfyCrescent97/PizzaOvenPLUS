using System.Collections.Generic;
using System.Linq;
using System.Windows;

namespace PizzaOven.UI
{
	/// <summary>
    /// Interaction logic for PLUSxdeltawindow.xaml
    /// </summary>
    public partial class PLUSxdeltawindow : Window
    {
        private List<string> _xdeltas = new List<string>();

        public string[] ResultXDeltas { get; private set; }

        public PLUSxdeltawindow(IEnumerable<string> xdeltas)
        {
            InitializeComponent();

            _xdeltas = xdeltas?.ToList() ?? new List<string>();

            foreach (var xdelta in _xdeltas)
            {
                XDeltaCombo.Items.Add(xdelta);
            }

            XDeltaCombo.Items.Add("Unsure (Takes Longer)");

            if (XDeltaCombo.Items.Count > 0)
                XDeltaCombo.SelectedIndex = 0;
        }

        private void ConfirmButton_Click(object sender, RoutedEventArgs e)
        {
            string selected = XDeltaCombo.SelectedItem?.ToString();

            if (selected == "Unsure (Takes Longer)")
            {
                ResultXDeltas = _xdeltas.ToArray();
            }
            else
            {
                ResultXDeltas = new[] { selected };
            }

            DialogResult = true;
            Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            ResultXDeltas = null;
            Close();
        }
    }
}