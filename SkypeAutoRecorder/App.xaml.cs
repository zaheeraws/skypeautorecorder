using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Windows;
using System.Windows.Forms;
using SkypeAutoRecorder.Configuration;
using SkypeAutoRecorder.Core;
using SkypeAutoRecorder.Core.Sound;
using SkypeAutoRecorder.Helpers;
using MessageBox = System.Windows.MessageBox;

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
                ContextMenu = new ContextMenu(),
                Visible = true
            };

            // Add context menu.
            trayIcon.ContextMenu.MenuItems.Add("Settings", (sender, args) => openSettingsWindow()).DefaultItem = true;
            trayIcon.ContextMenu.MenuItems.Add("About", onAboutClick);
            trayIcon.ContextMenu.MenuItems.Add("-");
            trayIcon.ContextMenu.MenuItems.Add("Close", (sender, e) => Shutdown());

            return trayIcon;
        }

        private void setTrayIconWaitingSkype()
        {
            _trayIcon.Icon = _disconnectedIcon;
            _trayIcon.Text = Settings.ApplicationName + ": Waiting for Skype";
        }

        private void setTrayIconWaitingCalls()
        {
            _trayIcon.Icon = _connectedIcon;
            _trayIcon.Text = Settings.ApplicationName + ": Waiting for calls";
        }

        private void setTrayIconRecording()
        {
            _trayIcon.Icon = _recordingIcon;
            _trayIcon.Text = Settings.ApplicationName + ": Recording";
        }

        #endregion

        private SkypeConnector _connector;

        private void appStartup(object sender, StartupEventArgs e)
        {
            // Initialize tray icon.
            _trayIcon = buildTrayIcon();
            setTrayIconWaitingSkype();
            _trayIcon.MouseDoubleClick += (o, args) => openSettingsWindow();
            
            // Initialize Skype connector.
            _connector = new SkypeConnector();
            _connector.Connected += (o, args) => setTrayIconWaitingCalls();
            _connector.Disconnected += (o, args) => setTrayIconWaitingSkype();
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

        private string _callerName;
        private DateTime _startRecordDateTime;

        private void onConversationStarted(object sender, ConversationEventArgs conversationEventArgs)
        {
            _callerName = conversationEventArgs.CallerName;
            _startRecordDateTime = DateTime.Now;
            _recordFileName = Settings.Current.GetRawFileName(_callerName);
            if (_recordFileName != null)
            {
                // Update tray icon information.
                setTrayIconRecording();
                
                // Get temp files.
                _tempInFileName = Settings.GetTempFileName("1");
                _tempOutFileName = Settings.GetTempFileName("2");

                _connector.StartRecord(_tempInFileName, _tempOutFileName);
            }
        }

        private void onConversationEnded(object sender, ConversationEventArgs conversationEventArgs)
        {
            if (_recordFileName != null)
            {
                _connector.EndRecord();

                // Start thread for processing sound.
                var fileNames = new ProcessingThreadData
                                {
                                    TempInFileName = _tempInFileName,
                                    TempOutFileName = _tempOutFileName,
                                    RecordRawFileName = _recordFileName,
                                    CallerName = _callerName,
                                    StartRecordDateTime = _startRecordDateTime
                                };
                ThreadPool.QueueUserWorkItem(soundProcessing, fileNames);

                setTrayIconWaitingCalls();
            }
        }

        private void soundProcessing(object dataObject)
        {
            var data = (ProcessingThreadData)dataObject;

            // Wait while files are in use.
            while (FilesHelper.FileIsInUse(data.TempInFileName) || FilesHelper.FileIsInUse(data.TempOutFileName))
            {
            }

            // Merge channels.
            var mergedFileName = Settings.GetTempFileName();

            if (SoundProcessor.MergeChannels(data.TempInFileName, data.TempOutFileName, mergedFileName))
            {
                File.Delete(data.TempInFileName);
                File.Delete(data.TempOutFileName);

                // Encode merged file to MP3.
                var duration = DateTime.Now - data.StartRecordDateTime;
                var recordFileName = Settings.RenderFileName(
                    data.RecordRawFileName, data.CallerName, data.StartRecordDateTime, duration);
                if (!DirectoriesHelper.CreateDirectory(recordFileName) ||
                    !SoundProcessor.EncodeMp3(mergedFileName, recordFileName, Settings.Current.VolumeScale))
                {
                    // Encode to settings folder with default file name if unable encode to the desired file name.
                    var fileName = Path.Combine(Settings.SettingsFolder, Settings.RenderFileName(
                        Settings.DefaultFileName, data.CallerName, data.StartRecordDateTime, duration));

                    if (!SoundProcessor.EncodeMp3(mergedFileName, fileName, Settings.Current.VolumeScale))
                    {
                        // If encoding fails anyway then return WAV file to user.
                        fileName = Path.ChangeExtension(fileName, "wav");
                        File.Copy(mergedFileName, fileName, true);
                    }

                    // Report about error and ask about opening folder with resulting file.
                    var openFolder = MessageBox.Show(
                        string.Format("Saving recorded file as \"{0}\" has failed. File was saved as \"{1}\" instead. Do you want to open folder with file?",
                            data.RecordRawFileName, fileName),
                        "Saving error", MessageBoxButton.YesNo, MessageBoxImage.Exclamation) == MessageBoxResult.Yes;

                    // Open folder.
                    if (openFolder)
                    {
                        Process.Start(Settings.SettingsFolder);
                    }
                }

                File.Delete(mergedFileName);
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
