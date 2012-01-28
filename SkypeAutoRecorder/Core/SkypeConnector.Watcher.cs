using System;
using System.Timers;
using SkypeAutoRecorder.Core.WinApi;

namespace SkypeAutoRecorder.Core
{
    internal partial class SkypeConnector
    {
        private const string SkypeMainWindowClass = "tSkMainForm";
        private const string SkypeLoginWindowClass = "TLoginForm";

        private const double WatchInterval = 500;
        private readonly Timer _skypeWatcher;

        private void skypeWatcherHandler(object sender, ElapsedEventArgs elapsedEventArgs)
        {
            var skypeActive = WinApiWrapper.WindowExists(SkypeMainWindowClass) &&
                              !WinApiWrapper.WindowExists(SkypeLoginWindowClass);

            if (!IsConnected && skypeActive)
            {
                // Register Skype messages handler.
                WinApiWrapper.SendMessage(BroadcastHandle, _skypeApiDiscover, _windowHandleSource.Handle);
            }
            else if (IsConnected && !skypeActive)
            {
                invokeDisconnected(EventArgs.Empty);
            }
        }
    }
}
