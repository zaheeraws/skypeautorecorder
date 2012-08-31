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
        private bool _startConversationHandled;

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
        /// <param name="message">The Skype message.</param>
        private void processSkypeMessage(string message)
        {
            // Status online.
            if (message == SkypeMessages.ConnectionStatusOnline && !_startConversationHandled)
            {
                invokeConnected();
                return;
            }

            // Status offline.
            if (message == SkypeMessages.ConnectionStatusOffline)
            {
                _startConversationHandled = false;
                invokeDisconnected();
                return;
            }

            // Conversation started.
            var numberFromStatus = parseSkypeMessage(message, "CALL (\\d+) STATUS INPROGRESS");
            var numberFromDuration = parseSkypeMessage(message, "CALL (\\d+) DURATION (\\d+)");
            if ((!string.IsNullOrEmpty(numberFromStatus) || !string.IsNullOrEmpty(numberFromDuration)) &&
                !_startConversationHandled)
            {
                _startConversationHandled = true;

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
                _currentCaller = caller;
                invokeConversationStarted(new ConversationEventArgs(_currentCaller));
                return;
            }

            // Conversation ended.
            var statusFinish = Regex.IsMatch(message, string.Format("CALL {0} STATUS FINISHED", _currentCallNumber));
            var statusMissed = Regex.IsMatch(message, string.Format("CALL {0} STATUS MISSED", _currentCallNumber));
            if ((statusFinish || statusMissed) && _startConversationHandled)
            {
                _startConversationHandled = false;
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
