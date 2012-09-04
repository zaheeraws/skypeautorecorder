using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Windows;
using SkypeAutoRecorder.Configuration;
using SkypeAutoRecorder.Core;
using SkypeAutoRecorder.Core.Sound;
using SkypeAutoRecorder.Helpers;
using MessageBox = System.Windows.MessageBox;

namespace SkypeAutoRecorder
{
    public partial class App
    {
        private readonly SkypeConnector _connector = new SkypeConnector();
        
        private readonly object _locker = new object();

        private string _tempInFileName;
        private string _tempOutFileName;

        /// <summary>
        /// Final resulting file name after recording and all sound processing steps.
        /// </summary>
        private string _recordFileName;

        private string _callerName;

        private DateTime _startRecordDateTime;

        private void initSkypeConnector()
        {
            _connector.Connected += (o, args) => setTrayIconWaitingCalls();
            _connector.Disconnected += connectorOnDisconnected;
            _connector.ConversationStarted += onConversationStarted;
            _connector.ConversationEnded += onConversationEnded;
            _connector.Enable();
        }

        private void onConversationStarted(object sender, ConversationEventArgs conversationEventArgs)
        {
            _callerName = conversationEventArgs.CallerName;
            _startRecordDateTime = DateTime.Now;
            _recordFileName = Settings.Current.GetRawFileName(_callerName);

            if (_recordFileName == null)
            {
                return;
            }

            // Update tray icon information.
            setTrayIconRecording();
                
            // Get temp files.
            _tempInFileName = Settings.GetTempFileName("1");
            _tempOutFileName = Settings.GetTempFileName("2");

            _connector.StartRecording(_tempInFileName, _tempOutFileName);
        }

        private void connectorOnDisconnected(object sender, EventArgs eventArgs)
        {
            convertRecordedFile();
            setTrayIconWaitingSkype();
        }

        private void onConversationEnded(object sender, ConversationEventArgs conversationEventArgs)
        {
            convertRecordedFile();
        }

        private void convertRecordedFile()
        {
            if (_recordFileName == null)
                return;

            _connector.StopRecording();

            // Start thread for processing sound.
            var fileNames = new ProcessingThreadData
                            {
                                TempInFileName = _tempInFileName,
                                TempOutFileName = _tempOutFileName,
                                RecordRawFileName = _recordFileName,
                                CallerName = _callerName,
                                StartRecordDateTime = _startRecordDateTime
                            };

            // Need to use Thread not from ThreadPool, because we want to run sound processing
            // even after application closes.
            new Thread(soundProcessing).Start(fileNames);

            setTrayIconWaitingCalls();
        }

        private void soundProcessing(object dataObject)
        {
            var data = (ProcessingThreadData)dataObject;

            // Wait while files are in use.
            while (FilesHelper.FileIsInUse(data.TempInFileName) || FilesHelper.FileIsInUse(data.TempOutFileName))
            {
            }

            // Join channels.
            var joinedFileName = Settings.GetTempFileName();

            if (SoundProcessor.JoinChannels(
                data.TempInFileName, data.TempOutFileName, joinedFileName, Settings.Current.SeparateSoundChannels))
            {
                File.Delete(data.TempInFileName);
                File.Delete(data.TempOutFileName);

                // Encode merged file to MP3.
                var duration = DateTime.Now - data.StartRecordDateTime;
                var recordFileName = Settings.RenderFileName(
                    data.RecordRawFileName, data.CallerName, data.StartRecordDateTime, duration);
                if (!DirectoriesHelper.CreateDirectory(recordFileName) ||
                    !SoundProcessor.EncodeMp3(joinedFileName, recordFileName, Settings.Current.VolumeScale,
                        Settings.Current.HighQualitySound, Settings.Current.SoundSampleFrequency,
                        Settings.Current.SoundBitrate))
                {
                    // Encode to settings folder with default file name if unable encode to the desired file name.
                    recordFileName = Path.Combine(Settings.SettingsFolder, Settings.RenderFileName(
                        Settings.DefaultFileName, data.CallerName, data.StartRecordDateTime, duration));

                    if (!SoundProcessor.EncodeMp3(joinedFileName, recordFileName, Settings.Current.VolumeScale,
                            Settings.Current.HighQualitySound, Settings.Current.SoundSampleFrequency,
                            Settings.Current.SoundBitrate))
                    {
                        // If encoding fails anyway then return WAV file to user.
                        recordFileName = Path.ChangeExtension(recordFileName, "wav");
                        File.Copy(joinedFileName, recordFileName, true);
                    }

                    // Report about error and ask about opening folder with resulting file.
                    var openFolder = MessageBox.Show(
                        string.Format("Saving recorded file as \"{0}\" has failed. File was saved as \"{1}\" instead. Do you want to open folder with file?",
                            data.RecordRawFileName, recordFileName),
                        "Saving error", MessageBoxButton.YesNo, MessageBoxImage.Exclamation) == MessageBoxResult.Yes;

                    // Open folder.
                    if (openFolder)
                        Process.Start(Settings.SettingsFolder);
                }

                updateLastRecordFileName(recordFileName);
                File.Delete(joinedFileName);
            }
        }
    }
}