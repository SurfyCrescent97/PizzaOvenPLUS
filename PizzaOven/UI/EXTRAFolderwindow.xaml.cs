using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.IO;
using System.Reflection;
using System.Windows.Input;

namespace PizzaOven.UI
{
    /// <summary>
    /// Interaction logic for ExtraFolderwindow.xaml
    /// </summary>
    public partial class ExtraFolderwindow : Window
    {
        public string _name;
        public bool _folder;
        public string directory = null;
        public string newName;
        public string loadout = null;
        public ExtraFolderwindow(string name, bool folder)
        {
            InitializeComponent();
            _folder = folder;
            if (!String.IsNullOrEmpty(name))
            {
                _name = name;
                NameBox.Text = name;
                Title = $"Edit Folder Name for {name}";
            }
            else
                if (_folder)
                    Title = "Create New Mod";
                else
                    Title = "Create New Loadout";
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void ConfirmButton_Click(object sender, RoutedEventArgs e)
        {
          EditFolderName();
          Close();
        }
        private void EditFolderName()
        {
            EXTRASSavesystem.write_ini("Folder", _name, NameBox.Text as string);
        }
    }
}
