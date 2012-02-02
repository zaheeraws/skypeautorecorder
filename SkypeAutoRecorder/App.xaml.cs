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
                _connector.EndRecord();

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
            _trayIcon.Visible = false;

            if (_connector != null)
            {
                _connector.Dispose();
            }
        }

        #region Windows

        private SettingsWindow _settingsWindow;
        private AboutWindow _aboutWindow;

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
