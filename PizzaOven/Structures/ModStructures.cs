using PizzaOven.UI;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows;
using System.Text.Json.Serialization;
using System.ComponentModel;

namespace PizzaOven
{
    public class Mod : INotifyPropertyChanged
    {
        public string name { get; set; }

        public bool enabled { get; set; }

        public Uri preview { get; set; }

        private bool _gmlLoader;
        [JsonIgnore]
        public bool GMLoader
        {
            get => _gmlLoader;
            set
            {
                if (_gmlLoader != value)
                {
                    _gmlLoader = value;
                    OnPropertyChanged(nameof(GMLoader));
                    OnPropertyChanged(nameof(GMLoaderVisibility));
                }
            }
        }

        private bool _gmlLoaderEnabled = false;
        [JsonIgnore]
        public bool GMLoader_enabled
        {
            get => _gmlLoaderEnabled;
            set
            {
                if (_gmlLoaderEnabled != value)
                {
                    _gmlLoaderEnabled = value;
                    OnPropertyChanged(nameof(GMLoader_enabled));
                }
            }
        }

        [JsonIgnore]
        public Visibility GMLoaderVisibility
        {
            get
            {
                return GMLoader ? Visibility.Visible : Visibility.Collapsed;
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
    public class Metadata
    {
        public string title { get; set; }
        public Uri preview { get; set; }
        public string submitter { get; set; }
        public Uri avi { get; set; }
        public Uri upic { get; set; }
        public Uri caticon { get; set; }
        public string cat { get; set; }
        public string description { get; set; }
        public string filedescription { get; set; }
        public Uri homepage { get; set; }
        public DateTime? lastupdate { get; set; }
    }
    public class Config
    {
        public string Launcher { get; set; }
        public bool FirstOpen { get; set; }
        public string ModsFolder { get; set; }
        public ObservableCollection<Mod> ModList { get; set; }
        public double? LeftGridWidth { get; set; }
        public double? RightGridWidth { get; set; }
        public double? TopGridHeight { get; set; }
        public double? BottomGridHeight { get; set; }
        public double? Height { get; set; }
        public double? Width { get; set; }
        public bool Maximized { get; set; }
    }
    public class Choice
    {
        public string OptionText { get; set; }
        public string OptionSubText { get; set; }
        public int Index { get; set; }
    }
}
