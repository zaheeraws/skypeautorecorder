using System;
using System.Drawing;
using System.IO;
using System.Reflection;
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

        private NotifyIcon buildTrayIcon()
        {
            var trayIcon = new NotifyIcon
            {
                Icon = _disconnectedIcon,
                ContextMenu = new ContextMenu()
            };

            // Context menu.
            trayIcon.ContextMenu.MenuItems.Add("Settings", onSettingsClick);
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
            _trayIcon = buildTrayIcon();
            
            _connector = new SkypeConnector();

            _connector.Connected += onConnected;
            _connector.Disconnected += onDisconnected;
            _connector.ConversationStarted += onConversationStarted;
            _connector.ConversationEnded += onConversationEnded;

            _connector.Enable();
        }

        private void onConnected(object sender, EventArgs eventArgs)
        {
            _trayIcon.Icon = _connectedIcon;
        }

        private void onDisconnected(object sender, EventArgs eventArgs)
        {
            _trayIcon.Icon = _disconnectedIcon;
        }

        private string _recordFileName;
        private string _tempInFileName;
        private string _tempOutFileName;
        private void onConversationStarted(object sender, ConversationEventArgs conversationEventArgs)
        {
            _recordFileName = Settings.Current.GetFileName(conversationEventArgs.CallerName, DateTime.Now);
            if (_recordFileName != null)
            {
                _trayIcon.Icon = _recordingIcon;
                _tempInFileName = Path.GetTempFileName();
                _tempOutFileName = Path.GetTempFileName();
                _connector.StartRecord(_tempInFileName, _tempOutFileName);
            }
        }

        private void onConversationEnded(object sender, ConversationEventArgs conversationEventArgs)
        {
            if (_recordFileName != null)
            {
                _trayIcon.Icon = _connectedIcon;
                _connector.EndRecord();
                File.Delete(_tempInFileName);
                File.Delete(_tempOutFileName);
                _tempInFileName += ".in";
                _tempOutFileName += ".out";
                if (SoundProcessor.MergeChannels(_tempInFileName, _tempOutFileName, _recordFileName))
                {
                    File.Delete(_tempInFileName);
                    File.Delete(_tempOutFileName);
                }
            }
        }

        private void appExit(object sender, ExitEventArgs e)
        {
            _trayIcon.Visible = false;

            if (_connector != null)
            {
                _connector.Dispose();
            }
        }

        #region Windows

        private SettingsWindow _settingsWindow;
        // private AboutWindow _aboutWindow;

        private void onSettingsClick(object sender, EventArgs eventArgs)
        {
            if (_settingsWindow != null && _settingsWindow.IsLoaded)
            {
                return;
            }

            var settingsCopy = (Settings)Settings.Current.Clone();
            _settingsWindow = new SettingsWindow(settingsCopy);
            _settingsWindow.Closed += settingsWindowOnClosed;
            _settingsWindow.ShowDialog();
        }

        private void settingsWindowOnClosed(object sender, EventArgs eventArgs)
        {
            if (_settingsWindow.DialogResult == true)
            {
                Settings.Current = _settingsWindow.NewSettings;
                Settings.Save();
            }
        }

        private void onAboutClick(object sender, EventArgs eventArgs)
        {
            // if (_aboutWindow == null)
            // {
            //     _aboutWindow = new AboutWindow();
            // }

            // if (_aboutWindow.Visibility != Visibility.Visible)
            // {
            //     _aboutWindow.ShowDialog();
            // }

            System.Windows.MessageBox.Show(
                "Skype Auto Recorder\r\nVersion 0.1\r\nAuthor: Miroshnichenko Kirill",
                "About", MessageBoxButton.OK);
        }

        #endregion
    }
}
