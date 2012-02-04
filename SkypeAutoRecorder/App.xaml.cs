using System;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Windows;
using System.Windows.Forms;
using SkypeAutoRecorder.Configuration;
using SkypeAutoRecorder.Core;
using SkypeAutoRecorder.Core.Sound;

namespace SkypeAutoRecorder
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App
    {
        #region Tray icons

        // Icons from the resources for displaying application status.

        private const string DisconnectedIconResource = "SkypeAutoRecorder.Images.DisconnectedTrayIcon.ico";
        private const string ConnectedIconResource = "SkypeAutoRecorder.Images.ConnectedTrayIcon.ico";
        private const string RecordingIconResource = "SkypeAutoRecorder.Images.RecordingTrayIcon.ico";

        private readonly Icon _disconnectedIcon =
            new Icon(Assembly.GetExecutingAssembly().GetManifestResourceStream(DisconnectedIconResource));

        private readonly Icon _connectedIcon =
            new Icon(Assembly.GetExecutingAssembly().GetManifestResourceStream(ConnectedIconResource));

        private readonly Icon _recordingIcon =
            new Icon(Assembly.GetExecutingAssembly().GetManifestResourceStream(RecordingIconResource));

        private NotifyIcon _trayIcon;

        /// <summary>
        /// Creates tray icon and context menu for it.
        /// </summary>
        /// <returns>Created <see cref="NotifyIcon"/> instance.</returns>
        private NotifyIcon buildTrayIcon()
        {
            var trayIcon = new NotifyIcon
            {
                Icon = _disconnectedIcon,
                ContextMenu = new ContextMenu()
            };

            // Add context menu.
            trayIcon.ContextMenu.MenuItems.Add("Settings", (sender, args) => openSettingsWindow()).DefaultItem = true;
            trayIcon.ContextMenu.MenuItems.Add("About", onAboutClick);
            trayIcon.ContextMenu.MenuItems.Add("-");
            trayIcon.ContextMenu.MenuItems.Add("Close", (sender, e) => Shutdown());

            trayIcon.Visible = true;

            return trayIcon;
        }

        #endregion

        private SkypeConnector _connector;

        private void appStartup(object sender, StartupEventArgs e)
        {
            // Initialize tray icon.
            _trayIcon = buildTrayIcon();
            _trayIcon.MouseDoubleClick += (o, args) => openSettingsWindow();
            
            // Initialize Skype connector.
            _connector = new SkypeConnector();
            _connector.Connected += (o, args) => _trayIcon.Icon = _connectedIcon;
            _connector.Disconnected += (o, args) => _trayIcon.Icon = _disconnectedIcon;
            _connector.ConversationStarted += onConversationStarted;
            _connector.ConversationEnded += onConversationEnded;
            _connector.Enable();
        }

        /// <summary>
        /// Final resulting file name after recording and all sound processing steps.
        /// </summary>
        private string _recordFileName;

        /// <summary>
        /// File name for the incoming channel recorded by Skype.
        /// </summary>
        private string _tempInFileName;

        /// <summary>
        /// File name for the outgoing channel recorded by Skype.
        /// </summary>
        private string _tempOutFileName;

        private void onConversationStarted(object sender, ConversationEventArgs conversationEventArgs)
        {
            _recordFileName = Settings.Current.GetFileName(conversationEventArgs.CallerName, DateTime.Now);
            if (_recordFileName != null)
            {
                _trayIcon.Icon = _recordingIcon;
                
                // Get temp files.
                _tempInFileName = Path.GetTempFileName();
                _tempOutFileName = Path.GetTempFileName();

                _connector.StartRecord(_tempInFileName, _tempOutFileName);
            }
        }

        private void onConversationEnded(object sender, ConversationEventArgs conversationEventArgs)
        {
            if (_recordFileName != null)
            {
                var fileNames = new[] { _tempInFileName, _tempOutFileName, _recordFileName };
                ThreadPool.QueueUserWorkItem(soundProcessing, fileNames);

                _trayIcon.Icon = _connectedIcon;
            }
        }

        private void soundProcessing(object data)
        {
            var fileNames = (string[])data;
            var tempInFileName = fileNames[0];
            var tempOutFileName = fileNames[1];
            var recordFileName = fileNames[2];

            // Merge channels.
            var mergedFileName = Path.GetTempFileName();
            File.Delete(mergedFileName);
            mergedFileName += ".wav";

            if (SoundProcessor.MergeChannels(tempInFileName, tempOutFileName, mergedFileName))
            {
                File.Delete(tempInFileName);
                File.Delete(tempOutFileName);

                // Encode merged file to MP3.
                if (SoundProcessor.EncodeMp3(mergedFileName, recordFileName))
                {
                    File.Delete(mergedFileName);
                }
            }
        }

        private void appExit(object sender, ExitEventArgs e)
        {
            _trayIcon.Dispose();

            if (_connector != null)
            {
                _connector.Dispose();
            }
        }

        #region Windows

        private SettingsWindow _settingsWindow;
        private AboutWindow _aboutWindow;

        /// <summary>
        /// Opens the settings window.
        /// </summary>
        private void openSettingsWindow()
        {
            if (_settingsWindow != null && _settingsWindow.IsLoaded)
            {
                return;
            }

            // Create copy of the current settings to have a possibility of rollback changes.
            var settingsCopy = (Settings)Settings.Current.Clone();

            // Create settings window with copied settings.
            _settingsWindow = new SettingsWindow(settingsCopy);
            _settingsWindow.Closed += settingsWindowOnClosed;
            _settingsWindow.ShowDialog();
        }

        private void settingsWindowOnClosed(object sender, EventArgs eventArgs)
        {
            if (_settingsWindow.DialogResult == true)
            {
                // Replace current settings and save them to file if user has accepted changes in settings window.
                Settings.Current = _settingsWindow.NewSettings;
                Settings.Save();
            }
        }

        private void onAboutClick(object sender, EventArgs eventArgs)
        {
            if (_aboutWindow != null && _aboutWindow.IsLoaded)
            {
                return;
            }

            _aboutWindow = new AboutWindow();
            _aboutWindow.ShowDialog();
        }

        #endregion
    }
}
