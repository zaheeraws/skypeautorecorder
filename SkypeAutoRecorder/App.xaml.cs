using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Text;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Input;
using GlobalHotKey;
using SkypeAutoRecorder.Configuration;
using SkypeAutoRecorder.Core;
using SkypeAutoRecorder.Helpers;
using MouseEventArgs = System.Windows.Forms.MouseEventArgs;

namespace SkypeAutoRecorder
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App
    {
        private readonly UniqueInstanceChecker _instanceChecker =
            new UniqueInstanceChecker("SkypeAutoRecorderOneInstanceMutex");

        // Icons from the resources for displaying application status.
        private readonly Icon _disconnectedIcon = new Icon(Assembly.GetExecutingAssembly()
            .GetManifestResourceStream("SkypeAutoRecorder.Images.DisconnectedTrayIcon.ico"));
        private readonly Icon _connectedIcon = new Icon(Assembly.GetExecutingAssembly()
            .GetManifestResourceStream("SkypeAutoRecorder.Images.ConnectedTrayIcon.ico"));
        private readonly Icon _recordingIcon = new Icon(Assembly.GetExecutingAssembly()
            .GetManifestResourceStream("SkypeAutoRecorder.Images.RecordingTrayIcon.ico"));

        private NotifyIcon _trayIcon;
        private MenuItem _startRecordingMenuItem;
        private MenuItem _cancelRecordingMenuItem;
        private MenuItem _browseDefaultMenuItem;
        private MenuItem _browseLastRecordMenuItem;

        private HotKeyManager _hotKeyManager;
        private HotKey _startRecordingHotKey;
        private HotKey _cancelRecordingHotKey;

        private string _lastRecordFileName;

        private SettingsWindow _settingsWindow;
        private AboutWindow _aboutWindow;

        private void appStartup(object sender, StartupEventArgs e)
        {
            // Only one instance of SkypeAutoRecorder is allowed.
            if (_instanceChecker.IsAlreadyRunning())
            {
                System.Windows.MessageBox.Show("SkypeAutoRecorder is already running.", "SkypeAutoRecorder",
                                               MessageBoxButton.OK, MessageBoxImage.Exclamation);
                Shutdown();
                return;
            }

            buildTrayIcon();
            createHotKeyManager();
            initSkypeConnector();
        }

        private void buildTrayIcon()
        {
            _trayIcon = new NotifyIcon
            {
                ContextMenu = new ContextMenu(),
                Visible = true
            };

            setTrayIconWaitingSkype();

            // Add context menu.
            _startRecordingMenuItem = new MenuItem("Start recording", (sender, args) => startRecording())
                                      {
                                          Shortcut = Shortcut.CtrlShiftF5, Enabled = false, DefaultItem = true
                                      };
            _trayIcon.ContextMenu.MenuItems.Add(_startRecordingMenuItem);

            _cancelRecordingMenuItem = new MenuItem("Cancel recording", (sender, args) => cancelRecording())
                                       {
                                           Shortcut = Shortcut.CtrlShiftF10, Enabled = false
                                       };
            _trayIcon.ContextMenu.MenuItems.Add(_cancelRecordingMenuItem);

            _trayIcon.ContextMenu.MenuItems.Add("-");

            _browseDefaultMenuItem = new MenuItem("Browse records", (sender, args) => openRecordsDefaultFolder());
            updateBrowseDefaultMenuItem();
            _trayIcon.ContextMenu.MenuItems.Add(_browseDefaultMenuItem);

            _browseLastRecordMenuItem =
                new MenuItem("Browse last record", (sender, args) => openLastRecordFolder()) { Enabled = false };
            _trayIcon.ContextMenu.MenuItems.Add(_browseLastRecordMenuItem);

            _trayIcon.ContextMenu.MenuItems.Add("-");
            _trayIcon.ContextMenu.MenuItems.Add("Settings", (sender, args) => openSettingsWindow());
            _trayIcon.ContextMenu.MenuItems.Add("About", openAboutWindow);
            _trayIcon.ContextMenu.MenuItems.Add("-");
            _trayIcon.ContextMenu.MenuItems.Add("Close", (sender, e) => Shutdown());

            _trayIcon.MouseDoubleClick += trayIconOnMouseDoubleClick;
        }

        private void createHotKeyManager()
        {
            _hotKeyManager = new HotKeyManager();
            _hotKeyManager.KeyPressed += onHotKeyPressed;
            _startRecordingHotKey = _hotKeyManager.Register(Key.F5, ModifierKeys.Control | ModifierKeys.Shift);
            _cancelRecordingHotKey = _hotKeyManager.Register(Key.F10, ModifierKeys.Control | ModifierKeys.Shift);
        }

        private StringBuilder LOG = new StringBuilder();

        private void updateGuiConnected(object sender, EventArgs eventArgs)
        {
            setTrayIconReady();
            updateStartCancelRecordMenuItems(false, false);
            LOG.AppendLine("CONNECTED: false, false");
        }

        private void updateGuiDisconnected()
        {
            setTrayIconWaitingSkype();
            updateStartCancelRecordMenuItems(false, false);
            LOG.AppendLine("DISCONNECTED: false, false");
        }

        private void updateGuiConversationStarted()
        {
            updateStartCancelRecordMenuItems(true, false);
            LOG.AppendLine("CONV STARTED: true, false");
        }

        private void updateGuiConversationEnded(object sender, ConversationEventArgs eventArgs)
        {
            updateStartCancelRecordMenuItems(false, false);
            LOG.AppendLine("CONV ENDED: false, false");
        }

        private void updateGuiRecordingStarted()
        {
            setTrayIconRecording();
            updateStartCancelRecordMenuItems(false, true);
            LOG.AppendLine("REC STARTED: false, true");
        }

        private void updateGuiRecordingStopped()
        {
            setTrayIconReady();
            updateStartCancelRecordMenuItems(true, false);
            LOG.AppendLine("REC STOPPED: false, false");
        }

        private void updateGuiRecordingCanceled(object sender, RecordingEventArgs eventArgs)
        {
            setTrayIconReady();
            updateStartCancelRecordMenuItems(true, false);
            LOG.AppendLine("REC CANCELED: true, false");
        }

        private void updateStartCancelRecordMenuItems(bool enableStart, bool enableCancel)
        {
            _startRecordingMenuItem.Enabled = enableStart;
            _cancelRecordingMenuItem.Enabled = enableCancel;

            _startRecordingMenuItem.DefaultItem = enableStart || !enableCancel;
            _cancelRecordingMenuItem.DefaultItem = enableCancel;
        }

        private void setTrayIconWaitingSkype()
        {
            _trayIcon.Icon = _disconnectedIcon;
            _trayIcon.Text = Settings.ApplicationName + ": Waiting for Skype";
        }

        private void setTrayIconReady()
        {
            _trayIcon.Icon = _connectedIcon;
            _trayIcon.Text = Settings.ApplicationName + ": Ready";
        }

        private void setTrayIconRecording()
        {
            _trayIcon.Icon = _recordingIcon;
            _trayIcon.Text = Settings.ApplicationName + ": Recording";
        }

        private void trayIconOnMouseDoubleClick(object sender, MouseEventArgs mouseEventArgs)
        {
            if (_startRecordingMenuItem.DefaultItem)
                startRecording();
            else if (_cancelRecordingMenuItem.DefaultItem)
                cancelRecording();
        }

        private void updateBrowseDefaultMenuItem()
        {
            _browseDefaultMenuItem.Enabled = !string.IsNullOrEmpty(Settings.Current.DefaultRawFileName);
        }

        private void updateLastRecordFileName(string fileName)
        {
            lock (_locker)
            {
                _lastRecordFileName = fileName;
                _browseLastRecordMenuItem.Enabled = !string.IsNullOrEmpty(_lastRecordFileName);
            }
        }

        private void onHotKeyPressed(object sender, KeyPressedEventArgs keyPressedEventArgs)
        {
            if (keyPressedEventArgs.HotKey == _startRecordingHotKey)
                startRecording();
            else if (keyPressedEventArgs.HotKey == _cancelRecordingHotKey)
                cancelRecording();
        }

        private void openRecordsDefaultFolder()
        {
            // Clear default records path from all placeholders. Need to remove chars starting from the first
            // placeholder and then fix it by removing all chars after last backslash.
            var path = Settings.Current.DefaultRawFileName;
            path = path.Remove(path.IndexOf('{'));
            path = path.Remove(path.LastIndexOf('\\'));

            // Try to open resulting path without placeholders.
            // If it's incorrect or doesn't exist, Explorer opens some default folder automatically.
            Process.Start("explorer.exe", path);
        }

        private void openLastRecordFolder()
        {
            lock (_locker)
            {
                var args = File.Exists(_lastRecordFileName)
                               ? string.Format("/select,\"{0}\"", _lastRecordFileName)
                               : "\"" + Path.GetDirectoryName(_lastRecordFileName) + "\"";
                Process.Start("explorer.exe", args);
            }
        }

        private void openSettingsWindow()
        {
            if (_settingsWindow != null && _settingsWindow.IsLoaded)
                return;

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

                updateBrowseDefaultMenuItem();
            }
        }

        private void openAboutWindow(object sender, EventArgs eventArgs)
        {
            if (_aboutWindow != null && _aboutWindow.IsLoaded)
                return;

            _aboutWindow = new AboutWindow();
            _aboutWindow.ShowDialog();
        }

        private void onApplicationExit(object sender, ExitEventArgs e)
        {
            if (_connector.IsRecording)
                _connector.CancelRecording();

            if (_trayIcon != null)
                _trayIcon.Dispose();
            if (_connector != null)
                _connector.Dispose();

            _instanceChecker.Release();
        }
    }
}