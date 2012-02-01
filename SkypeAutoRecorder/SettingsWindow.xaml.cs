using System.Windows.Controls;
using SkypeAutoRecorder.Configuration;

namespace SkypeAutoRecorder
{
    /// <summary>
    /// Interaction logic for SettingsWindow.xaml
    /// </summary>
    public partial class SettingsWindow
    {
        public SettingsWindow(Settings currentSettings)
        {
            InitializeComponent();

            NewSettings = currentSettings ?? new Settings();
            mainGrid.DataContext = NewSettings;
        }

        public Settings NewSettings { get; set; }

        private void addButtonClick(object sender, System.Windows.RoutedEventArgs e)
        {
            NewSettings.Filters.Add(new Filter());
        }

        private void removeButtonClick(object sender, System.Windows.RoutedEventArgs e)
        {
            if (filtersListView.SelectedIndex >= 0)
            {
                NewSettings.Filters.RemoveAt(filtersListView.SelectedIndex);
            }
        }

        private void okButtonClick(object sender, System.Windows.RoutedEventArgs e)
        {
            DialogResult = true;
        }
    }
}
