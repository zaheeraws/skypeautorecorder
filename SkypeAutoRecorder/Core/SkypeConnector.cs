using System;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows.Interop;
using SkypeAutoRecorder.Core.SkypeApi;
using SkypeAutoRecorder.Core.WinApi;
using SkypeAutoRecorder.Helpers;
using Timer = System.Timers.Timer;

namespace SkypeAutoRecorder.Core
{
    /// <summary>
    /// Privides connection to the Skype and possibilities to control it.
    /// </summary>
    internal partial class SkypeConnector : IDisposable
    {
        private int _currentCallNumber;
        
        /// <summary>
        /// Initializes a new instance of the <see cref="SkypeConnector"/> class.
        /// </summary>
        public SkypeConnector()
        {
            // Subscribe to own events.
            Connected += (sender, args) => IsConnected = true;
            Disconnected += onDisconnected;
            RecordingStarted += (sender, args) => IsRecording = true;
            RecordingStopped += onRecordingStopOrCancel;
            RecordingCanceled += onRecordingStopOrCancel;

            // Create dummy handle source to catch Windows API messages.
            _windowHandleSource = new HwndSource(new HwndSourceParameters());

            // Hook messages to window.
            _windowHandleSource.AddHook(apiMessagesHandler);

            // Initialize watcher.
            _skypeWatcher = new Timer(WatchInterval);
            _skypeWatcher.Elapsed += skypeWatcherHandler;
        }

        /// <summary>
        /// Gets a value indicating whether <see cref="SkypeConnector"/> is connected.
        /// </summary>
        /// <value>
        /// <c>true</c> if <see cref="SkypeConnector"/> is connected; otherwise, <c>false</c>.
        /// </value>
        public bool IsConnected { get; private set; }

        /// <summary>
        /// Gets a value indicating whether <see cref="SkypeConnector"/> is recording conversation.
        /// </summary>
        /// <value>
        /// <c>true</c> if <see cref="SkypeConnector"/> is recording conversation; otherwise, <c>false</c>.
        /// </value>
        public bool IsRecording { get; private set; }

        /// <summary>
        /// Gets a value indicating whether conversation is active.
        /// </summary>
        /// <value>
        /// <c>true</c> if conversation is active; otherwise, <c>false</c>.
        /// </value>
        public bool ConversationIsActive { get; private set; }

        /// <summary>
        /// Gets the name of the current caller.
        /// </summary>
        /// <value>
        /// The name of the current caller.
        /// </value>
        public string CurrentCaller { get; private set; }

        /// <summary>
        /// Gets the file name of input sound channel.
        /// </summary>
        /// <value>
        /// The file name of input sound channel.
        /// </value>
        public string CallInFileName { get; private set; }

        /// <summary>
        /// Gets the file name of output sound channel.
        /// </summary>
        /// <value>
        /// The file name of output sound channel.
        /// </value>
        public string CallOutFileName { get; private set; }

        /// <summary>
        /// Parses the skype message and returns its parameter.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <param name="pattern">The pattern.</param>
        /// <returns>First message parameter.</returns>
        private static string parseSkypeMessage(string message, string pattern)
        {
            var match = Regex.Match(message, pattern);
            return match.Success ? match.Groups[1].Value : null;
        }

        /// <summary>
        /// Enables connector to watch Skype events and messages.
        /// </summary>
        public void Enable()
        {
            _skypeWatcher.Start();
        }

        /// <summary>
        /// Starts record of the currently active call to the files.
        /// </summary>
        /// <param name="callInFileName">Name of the file for input channel (microphone).</param>
        /// <param name="callOutFileName">Name of the file for output channel.</param>
        public void StartRecording(string callInFileName, string callOutFileName)
        {
            if (IsRecording && ConversationIsActive)
                return;

            CallInFileName = callInFileName;
            CallOutFileName = callOutFileName;

            var recordInCommand = string.Format(SkypeCommands.StartRecordInput, _currentCallNumber, callInFileName);
            var recordOutCommand = string.Format(SkypeCommands.StartRecordOutput, _currentCallNumber, callOutFileName);

            sendSkypeCommand(recordInCommand);
            sendSkypeCommand(recordOutCommand);

            invokeRecordingStarted(new RecordingEventArgs(CurrentCaller, CallInFileName, CallOutFileName));
        }

        /// <summary>
        /// Ends record of the currently active call.
        /// </summary>
        public void StopRecording()
        {
            if (!IsRecording)
                return;

            sendStopRecordingCommands();
            invokeRecordingStopped(new RecordingEventArgs(CurrentCaller, CallInFileName, CallOutFileName));
        }

        /// <summary>
        /// Cancels record of the currently active call.
        /// </summary>
        public void CancelRecording()
        {
            if (!IsRecording)
                return;

            sendStopRecordingCommands();

            // Delete temp files.
            new Thread(state =>
                       {
                           while (FilesHelper.FileIsInUse(CallInFileName) ||
                                  FilesHelper.FileIsInUse(CallOutFileName)) {}
                           File.Delete(CallInFileName);
                           File.Delete(CallOutFileName);
                       }).Start();

            invokeRecordingCanceled(new RecordingEventArgs(CurrentCaller, null, null));
        }

        private void sendStopRecordingCommands()
        {
            var endRecordInCommand = string.Format(SkypeCommands.EndRecordInput, _currentCallNumber);
            var endRecordOutCommand = string.Format(SkypeCommands.EndRecordOutput, _currentCallNumber);

            sendSkypeCommand(endRecordInCommand);
            sendSkypeCommand(endRecordOutCommand);
        }


        /// <summary>
        /// Sends the Skype command using Windows API.
        /// </summary>
        /// <param name="command">The command.</param>
        private void sendSkypeCommand(string command)
        {
            var data = new CopyDataStruct { Id = "1", Size = command.Length + 1, Data = command };
            try
            {
                WinApiWrapper.SendMessage(
                    _skypeWindowHandle, WinApiConstants.WM_COPYDATA, _windowHandleSource.Handle, ref data);
            }
            catch (WinApiException)
            {
            }
        }

        /// <summary>
        /// Processes the Skype message.
        /// </summary>
        /// <param name="message">The Skype message.</param>
        private void processSkypeMessage(string message)
        {
            // Status online.
            if (message == SkypeMessages.ConnectionStatusOnline && !ConversationIsActive)
            {
                invokeConnected();
                return;
            }

            // Status offline.
            if (message == SkypeMessages.ConnectionStatusOffline)
            {
                invokeDisconnected();
                return;
            }

            // Conversation started.
            var numberFromStatus = parseSkypeMessage(message, "CALL (\\d+) STATUS INPROGRESS");
            var numberFromDuration = parseSkypeMessage(message, "CALL (\\d+) DURATION (\\d+)");
            if ((!string.IsNullOrEmpty(numberFromStatus) || !string.IsNullOrEmpty(numberFromDuration)) &&
                !ConversationIsActive)
            {
                ConversationIsActive = true;

                var newCallNumber = int.Parse(numberFromStatus ?? numberFromDuration);
                if (newCallNumber == _currentCallNumber)
                    return;
                _currentCallNumber = newCallNumber;

                // Ask Skype for caller name.
                sendSkypeCommand(string.Format(SkypeCommands.GetCallerName, _currentCallNumber));
                return;
            }

            // Message with caller name.
            var caller = parseSkypeMessage(message, string.Format("CALL {0} PARTNER_HANDLE (.+)", _currentCallNumber));
            if (!string.IsNullOrEmpty(caller))
            {
                CurrentCaller = caller;
                invokeConversationStarted(new ConversationEventArgs(CurrentCaller));
                return;
            }

            // Conversation ended.
            var statusFinish = Regex.IsMatch(message, string.Format("CALL {0} STATUS FINISHED", _currentCallNumber));
            var statusMissed = Regex.IsMatch(message, string.Format("CALL {0} STATUS MISSED", _currentCallNumber));
            if ((statusFinish || statusMissed) && ConversationIsActive)
            {
                ConversationIsActive = false;
                
                if (IsRecording)
                    StopRecording();

                invokeConversationEnded(new ConversationEventArgs(CurrentCaller));
            }
        }

        private void onRecordingStopOrCancel(object sender, RecordingEventArgs recordingEventArgs)
        {
            IsRecording = false;
        }

        private void onDisconnected(object sender, EventArgs eventArgs)
        {
            IsConnected = false;
            ConversationIsActive = false;
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            // Remove hook of Windows API messages.
            _windowHandleSource.RemoveHook(apiMessagesHandler);
        }
    }
}
