using System;

namespace SkypeAutoRecorder.Core
{
    internal class ConversationEventArgs : EventArgs
    {
        public ConversationEventArgs(string callerName)
        {
            CallerName = callerName;
        }

        public string CallerName { get; set; }
    }
}