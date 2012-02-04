using System;
using System.Text.RegularExpressions;
using System.Timers;
using System.Windows.Interop;
using SkypeAutoRecorder.Core.SkypeApi;
using SkypeAutoRecorder.Core.WinApi;

namespace SkypeAutoRecorder.Core
{
    /// <summary>
    /// Privides connection to the Skype and possibilities to control it.
    /// </summary>
    internal partial class SkypeConnector : IDisposable
    {
        private int _currentCallNumber;
        private string _currentCaller;

        /// <summary>
        /// Initializes a new instance of the <see cref="SkypeConnector"/> class.
        /// </summary>
        public SkypeConnector()
        {
            // Subscribe to Skype connection events.
            Connected += (sender, args) => IsConnected = true;
            Disconnected += (sender, args) => IsConnected = false;

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
        /// Enables connector to watch Skype events and messages.
        /// </summary>
        public void Enable()
        {
            // Register API messages for communicating with Skype.
            _skypeApiDiscover = WinApiWrapper.RegisterApiMessage(SkypeControlApiMessages.Discover);
            _skypeApiAttach = WinApiWrapper.RegisterApiMessage(SkypeControlApiMessages.Attach);

            _skypeWatcher.Start();
        }

        /// <summary>
        /// Start record of the currently active call to the files.
        /// </summary>
        /// <param name="callInFileName">Name of the file for input channel (microphone).</param>
        /// <param name="callOutFileName">Name of the file for output channel.</param>
        public void StartRecord(string callInFileName, string callOutFileName)
        {
            var recordInCommand = string.Format(SkypeCommands.StartRecordInput, _currentCallNumber, callInFileName);
            var recordOutCommand = string.Format(SkypeCommands.StartRecordOutput, _currentCallNumber, callOutFileName);

            sendSkypeCommand(recordInCommand);
            sendSkypeCommand(recordOutCommand);
        }

        /// <summary>
        /// End record of the currently active call.
        /// </summary>
        public void EndRecord()
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
            WinApiWrapper.SendMessage(
                _skypeWindowHandle, WinApiConstants.WM_COPYDATA, _windowHandleSource.Handle, ref data);
        }

        /// <summary>
        /// Processes the Skype message.
        /// </summary>
        /// <param name="data">The data that contains message details.</param>
        private void processSkypeMessage(CopyDataStruct data)
        {
            // Status online.
            if (data.Data == SkypeMessages.ConnectionStatusOnline)
            {
                invokeConnected();
                return;
            }

            // Status offline.
            if (data.Data == SkypeMessages.ConnectionStatusOffline)
            {
                invokeDisconnected();
                return;
            }

            // Call in progress.
            var regex = new Regex("CALL (\\d+) STATUS INPROGRESS");
            var match = regex.Match(data.Data);
            if (match.Success)
            {
                _currentCallNumber = int.Parse(match.Groups[1].Value);

                // Ask Skype for caller name.
                sendSkypeCommand(string.Format(SkypeCommands.GetCallerName, _currentCallNumber));
                return;
            }

            // Conversation started.
            regex = new Regex(string.Format("CALL {0} PARTNER_HANDLE (.+)", _currentCallNumber));
            match = regex.Match(data.Data);
            if (match.Success)
            {
                _currentCaller = match.Groups[1].Value;
                invokeConversationStarted(new ConversationEventArgs(_currentCaller));
                return;
            }

            // Conversation ended.
            regex = new Regex("CALL (\\d+) STATUS FINISHED");
            match = regex.Match(data.Data);
            if (match.Success && _currentCallNumber == int.Parse(match.Groups[1].Value))
            {
                EndRecord();
                invokeConversationEnded(new ConversationEventArgs(_currentCaller));
            }
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
