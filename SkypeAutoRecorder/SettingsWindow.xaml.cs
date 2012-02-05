using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Navigation;
using Microsoft.Win32;
using SkypeAutoRecorder.Configuration;
using Button = System.Windows.Controls.Button;
using Clipboard = System.Windows.Clipboard;

namespace SkypeAutoRecorder
{
    /// <summary>
    /// Interaction logic for SettingsWindow.xaml
    /// </summary>
    public partial class SettingsWindow
    {
        private const string AutostartRegistryKey = "Software\\Microsoft\\Windows\\CurrentVersion\\Run";
        private const string AutostartValueName = Settings.ApplicationName;

        /// <summary>
        /// Shows how many validation errors filter list view contains.
        /// </summary>
        private int filtersValidationErrors;
        
        public static RoutedCommand OkCommand = new RoutedCommand();
        
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

        private void okCommandExecuted(object sender, ExecutedRoutedEventArgs e)
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
                Registry.CurrentUser.OpenSubKey(AutostartRegistryKey, true).DeleteValue(AutostartValueName, false);
            }

            DialogResult = true;
        }

        private void okCommandCanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = ValidationHelper.InputsAreValid(sender as DependencyObject) && filtersValidationErrors <= 0;
        }

        private void addButtonClick(object sender, RoutedEventArgs e)
        {
            NewSettings.Filters.Add(new Filter());
        }

        private void removeButtonClick(object sender, RoutedEventArgs e)
        {
            var filter = (Filter)((Button)sender).Tag;
            NewSettings.Filters.Remove(filter);
        }

        private void onPlaceholderClick(object sender, RequestNavigateEventArgs e)
        {
            Clipboard.SetText(((Hyperlink)sender).Tag.ToString());
            e.Handled = true;
        }

        private void browseFilterFolderButtonClick(object sender, RoutedEventArgs e)
        {
            var dialog = new FolderBrowserDialog();
            if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                ((Filter)((Button)sender).Tag).RawFileName = dialog.SelectedPath;
            }
        }

        private void browseDefaultFolderButtonClick(object sender, RoutedEventArgs e)
        {
            var dialog = new FolderBrowserDialog();
            if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                NewSettings.DefaultRawFileName = dialog.SelectedPath;
            }
        }

        private void filtersListError(object sender, ValidationErrorEventArgs e)
        {
            if (e.Action == ValidationErrorEventAction.Added)
            {
                filtersValidationErrors++;
            }
            else
            {
                filtersValidationErrors--;
            }
        }
    }
}
