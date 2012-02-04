using System;

namespace SkypeAutoRecorder.Core
{
    internal partial class SkypeConnector
    {
        /// <summary>
        /// Delegate of the <c>Connected</c> event handler.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="System.EventArgs"/> instance containing the event data.</param>
        public delegate void ConnectedEventHandler(object sender, EventArgs e);

        /// <summary>
        /// Delegate of the <c>Disconnected</c> event handler.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="System.EventArgs"/> instance containing the event data.</param>
        public delegate void DisconnectedEventHandler(object sender, EventArgs e);

        /// <summary>
        /// Delegate of the <c>ConversationStarted</c> event handler.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="SkypeAutoRecorder.Core.ConversationEventArgs"/> instance
        /// containing the event data.</param>
        public delegate void ConversationStartedEventHandler(object sender, ConversationEventArgs e);

        /// <summary>
        /// Delegate of the <c>ConversationEnded</c> event handler.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="SkypeAutoRecorder.Core.ConversationEventArgs"/> instance
        /// containing the event data.</param>
        public delegate void ConversationEndedEventHandler(object sender, ConversationEventArgs e);

        /// <summary>
        /// Occurs when application is successfuly connected to the Skype.
        /// </summary>
        public event ConnectedEventHandler Connected;

        /// <summary>
        /// Occurs when application disconnects from the Skype.
        /// </summary>
        public event DisconnectedEventHandler Disconnected;

        /// <summary>
        /// Occurs when some conversation starts.
        /// </summary>
        public event ConversationStartedEventHandler ConversationStarted;

        /// <summary>
        /// Occurs when conversation ends.
        /// </summary>
        public event ConversationEndedEventHandler ConversationEnded;

        private void invokeConnected()
        {
            if (Connected != null)
            {
                Connected(this, EventArgs.Empty);
            }
        }

        private void invokeDisconnected()
        {
            if (Disconnected != null)
            {
                Disconnected(this, EventArgs.Empty);
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
