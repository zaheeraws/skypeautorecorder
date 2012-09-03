using System;

namespace SkypeAutoRecorder.Core
{
    internal partial class SkypeConnector
    {
        /// <summary>
        /// Delegate of the connection events handler.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="System.EventArgs"/> instance containing the event data.</param>
        public delegate void ConnectionEventHandler(object sender, EventArgs e);

        /// <summary>
        /// Delegate of the conversation events handler.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="SkypeAutoRecorder.Core.ConversationEventArgs"/> instance
        /// containing the event data.</param>
        public delegate void ConversationEventHandler(object sender, ConversationEventArgs e);

        /// <summary>
        /// Delegate of the recording events handler.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="SkypeAutoRecorder.Core.ConversationEventArgs"/> instance
        /// containing the event data.</param>
        public delegate void RecordingEventHandler(object sender, ConversationEventArgs e);

        /// <summary>
        /// Occurs when application is successfuly connected to the Skype.
        /// </summary>
        public event ConnectionEventHandler Connected;

        /// <summary>
        /// Occurs when application disconnects from the Skype.
        /// </summary>
        public event ConnectionEventHandler Disconnected;

        /// <summary>
        /// Occurs when conversation starts.
        /// </summary>
        public event ConversationEventHandler ConversationStarted;

        /// <summary>
        /// Occurs when conversation ends.
        /// </summary>
        public event ConversationEventHandler ConversationEnded;

        /// <summary>
        /// Occurs when recording starts.
        /// </summary>
        public event ConversationEventHandler RecordingStarted;

        /// <summary>
        /// Occurs when recording stops.
        /// </summary>
        public event ConversationEventHandler RecordingStopped;

        private void invokeConnected()
        {
            var handler = Connected;
            if (handler != null)
            {
                handler(this, EventArgs.Empty);
            }
        }

        private void invokeDisconnected()
        {
            var handler = Disconnected;
            if (handler != null)
            {
                handler(this, EventArgs.Empty);
            }
        }

        private void invokeConversationStarted(ConversationEventArgs e)
        {
            var handler = ConversationStarted;
            if (handler != null)
            {
                handler(this, e);
            }
        }

        private void invokeConversationEnded(ConversationEventArgs e)
        {
            var handler = ConversationEnded;
            if (handler != null)
            {
                handler(this, e);
            }
        }

        private void invokeRecordingStarted(ConversationEventArgs e)
        {
            var handler = RecordingStarted;
            if (handler != null)
            {
                handler(this, e);
            }
        }

        private void invokeRecordingStopped(ConversationEventArgs e)
        {
            var handler = RecordingStopped;
            if (handler != null)
            {
                handler(this, e);
            }
        }
    }
}
