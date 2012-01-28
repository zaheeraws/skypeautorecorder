using System;

namespace SkypeAutoRecorder.Core
{
    internal partial class SkypeConnector
    {
        public delegate void ConnectedEventHandler(object sender, EventArgs e);
        public delegate void DisconnectedEventHandler(object sender, EventArgs e);
        public delegate void ConversationStartedEventHandler(object sender, ConversationEventArgs e);
        public delegate void ConversationEndedEventHandler(object sender, ConversationEventArgs e);

        public event ConnectedEventHandler Connected;
        public event DisconnectedEventHandler Disconnected;
        public event ConversationStartedEventHandler ConversationStarted;
        public event ConversationEndedEventHandler ConversationEnded;

        private void invokeConnected(EventArgs e)
        {
            if (Connected != null)
            {
                Connected(this, e);
            }
        }

        private void invokeDisconnected(EventArgs e)
        {
            if (Disconnected != null)
            {
                Disconnected(this, e);
            }
        }

        private void invokeConversationStarted(ConversationEventArgs e)
        {
            if (ConversationStarted != null)
            {
                ConversationStarted(this, e);
            }
        }

        private void invokeConversationEnded(ConversationEventArgs e)
        {
            if (ConversationEnded != null)
            {
                ConversationEnded(this, e);
            }
        }
    }
}
