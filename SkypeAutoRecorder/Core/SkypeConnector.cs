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

        public SkypeConnector()
        {
            Connected += onConnected;
            Disconnected += onDisconnected;

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
        /// Records the currently active call to the files.
        /// </summary>
        /// <param name="callInFileName">Name of the file for input channel (microphone).</param>
        /// <param name="callOutFileName">Name of the file for output channel.</param>
        public void Record(string callInFileName, string callOutFileName)
        {
            var recordInCommand = string.Format(SkypeCommands.StartRecordInput, _currentCallNumber, callInFileName);
            var recordOutCommand = string.Format(SkypeCommands.StartRecordOutput, _currentCallNumber, callOutFileName);

            sendSkypeCommand(recordInCommand);
            sendSkypeCommand(recordOutCommand);
        }

        private void sendSkypeCommand(string command)
        {
            var data = new CopyDataStruct { Id = "1", Size = command.Length + 1, Data = command };
            WinApiWrapper.SendMessage(
                _skypeWindowHandle, WinApiConstants.WM_COPYDATA, _windowHandleSource.Handle, ref data);
        }

        private void processSkypeMessage(CopyDataStruct data)
        {
            // Status online.
            if (data.Data == SkypeMessages.ConnectionStatusOnline)
            {
                invokeConnected(EventArgs.Empty);
                return;
            }

            // Status offline.
            if (data.Data == SkypeMessages.ConnectionStatusOffline)
            {
                invokeDisconnected(EventArgs.Empty);
                return;
            }

            // Call in progress.
            var regex = new Regex("CALL (\\d+) STATUS INPROGRESS");
            var match = regex.Match(data.Data);
            if (match.Success)
            {
                _currentCallNumber = int.Parse(match.Groups[1].Value);
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

            // Call in progress.
            regex = new Regex("CALL (\\d+) STATUS FINISHED");
            match = regex.Match(data.Data);
            if (match.Success && _currentCallNumber == int.Parse(match.Groups[1].Value))
            {
                invokeConversationEnded(new ConversationEventArgs(_currentCaller));
            }
        }

        private void onConnected(object sender, EventArgs eventArgs)
        {
            IsConnected = true;
        }

        private void onDisconnected(object sender, EventArgs eventArgs)
        {
            IsConnected = false;
        }

        public void Dispose()
        {
            _windowHandleSource.RemoveHook(apiMessagesHandler);
        }
    }
}
