using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Navigation;
using Microsoft.Win32;
using SkypeAutoRecorder.Configuration;

namespace SkypeAutoRecorder
{
    /// <summary>
    /// Interaction logic for SettingsWindow.xaml
    /// </summary>
    public partial class SettingsWindow
    {
        private const string AutostartRegistryKey = "Software\\Microsoft\\Windows\\CurrentVersion\\Run";
        private const string AutostartValueName = "SkypeAutoRecorder";

        public SettingsWindow(Settings currentSettings)
        {
            InitializeComponent();

            NewSettings = currentSettings ?? new Settings();
            mainGrid.DataContext = NewSettings;

            // Check if application is present in Run registry section and its file name is valid.
            var currentValue =
                (string)Registry.CurrentUser.OpenSubKey(AutostartRegistryKey).GetValue(AutostartValueName, null);
            Autostart = !string.IsNullOrEmpty(currentValue) && currentValue == Assembly.GetEntryAssembly().Location;
        }

        public bool Autostart { get; set; }

        public Settings NewSettings { get; set; }

        private void addButtonClick(object sender, RoutedEventArgs e)
        {
            NewSettings.Filters.Add(new Filter());
        }

        private void removeButtonClick(object sender, RoutedEventArgs e)
        {
            var filter = (Filter)((Button)sender).Tag;
            if (filter != null)
            {
                NewSettings.Filters.Remove(filter);
            }
        }

        private void okButtonClick(object sender, RoutedEventArgs e)
        {
            // Update registry Run section for autostart.
            if (Autostart)
            {
                // Enable autostart - add registry record.
                Registry.CurrentUser.OpenSubKey(AutostartRegistryKey, true).SetValue(
                    AutostartValueName, Assembly.GetEntryAssembly().Location);
            }
            else
            {
                // Disable autostart - remove registry record.
                Registry.CurrentUser.OpenSubKey(AutostartRegistryKey, true).DeleteValue(AutostartValueName);
            }

            DialogResult = true;
        }

        private void onPlaceholderClick(object sender, RequestNavigateEventArgs e)
        {
            Clipboard.SetText(((Hyperlink)sender).Tag.ToString());
            e.Handled = true;
        }
    }
}
