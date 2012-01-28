using System;
using System.Drawing;
using System.Reflection;
using System.Windows;
using System.Windows.Forms;
using SkypeAutoRecorder.Core;

namespace SkypeAutoRecorder
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App
    {
        private const string DisconnectedIconResource = "SkypeAutoRecorder.Images.DisconnectedTrayIcon.ico";
        private const string ConnectedIconResource = "SkypeAutoRecorder.Images.ConnectedTrayIcon.ico";
        private const string RecordingIconResource = "SkypeAutoRecorder.Images.RecordingTrayIcon.ico";

        private readonly Icon _disconnectedIcon =
            new Icon(Assembly.GetExecutingAssembly().GetManifestResourceStream(DisconnectedIconResource));
        private readonly Icon _connectedIcon =
            new Icon(Assembly.GetExecutingAssembly().GetManifestResourceStream(ConnectedIconResource));
        private readonly Icon _recordingIcon =
            new Icon(Assembly.GetExecutingAssembly().GetManifestResourceStream(RecordingIconResource));

        private NotifyIcon _trayIcon;
        private SkypeConnector _connector;

        private void appStartup(object sender, StartupEventArgs e)
        {
            _trayIcon = buildTrayIcon();
            
            _connector = new SkypeConnector();

            _connector.Connected += onConnected;
            _connector.Disconnected += onDisconnected;
            _connector.ConversationStarted += onConversationStarted;
            _connector.ConversationEnded += onConversationEnded;

            _connector.Enable();
        }

        private void onConnected(object sender, EventArgs eventArgs)
        {
            _trayIcon.Icon = _connectedIcon;
        }

        private void onDisconnected(object sender, EventArgs eventArgs)
        {
            _trayIcon.Icon = _disconnectedIcon;
        }

        private void onConversationStarted(object sender, ConversationEventArgs conversationEventArgs)
        {
            _trayIcon.Icon = _recordingIcon;
        }

        private void onConversationEnded(object sender, ConversationEventArgs conversationEventArgs)
        {
            _trayIcon.Icon = _connectedIcon;
        }

        private void appExit(object sender, ExitEventArgs e)
        {
            _trayIcon.Visible = false;

            if (_connector != null)
            {
                _connector.Dispose();
            }
        }

        private NotifyIcon buildTrayIcon()
        {
            var trayIcon = new NotifyIcon
            {
                Icon = _disconnectedIcon,
                ContextMenu = new ContextMenu()
            };

            // Context menu.
            trayIcon.ContextMenu.MenuItems.Add("Close", (sender, e) => Shutdown());

            trayIcon.Visible = true;

            return trayIcon;
        }
    }
}
