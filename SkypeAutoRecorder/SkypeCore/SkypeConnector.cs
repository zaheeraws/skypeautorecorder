using System;
using System.Threading;
using SKYPE4COMLib;

namespace SkypeAutoRecorder.SkypeCore
{
    internal class SkypeConnector
    {
        private readonly Skype _skype;
        private readonly Timer _watcher;

        public SkypeConnector()
        {
            _skype = new Skype();
            _watcher = new Timer(watcherHandler, null, 0, 100);
        }

        public bool IsConnected { get; private set; }

        private void watcherHandler(object state)
        {
            if (_skype.Client.IsRunning && ((ISkype)_skype).AttachmentStatus != TAttachmentStatus.apiAttachSuccess)
            {
                _skype.Attach(8, false);
            }
        }
    }
}
