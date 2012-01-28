using System;
using System.Runtime.InteropServices;
using System.Windows.Interop;
using SkypeAutoRecorder.Core.SkypeApi;
using SkypeAutoRecorder.Core.WinApi;

namespace SkypeAutoRecorder.Core
{
    internal partial class SkypeConnector
    {
        private static readonly IntPtr BroadcastHandle = new IntPtr(-1);
        private readonly HwndSource _windowHandleSource;
        private IntPtr _skypeWindowHandle;
        private uint _skypeApiDiscover;
        private uint _skypeApiAttach;

        private IntPtr apiMessagesHandler(
            IntPtr windowHandle, int message, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            // On successful attach.
            if (message == _skypeApiAttach && (SkypeAttachResult)lParam == SkypeAttachResult.AttachSuccess)
            {
                // Save Skype window handle.
                _skypeWindowHandle = wParam;

                handled = true;
                return new IntPtr(1);
            }

            // Process message from Skype.
            if (message == WinApiConstants.WM_COPYDATA && wParam == _skypeWindowHandle)
            {
                // Get the passed data.
                var data = (CopyDataStruct)Marshal.PtrToStructure(lParam, typeof(CopyDataStruct));
                processSkypeMessage(data);

                handled = true;
                return new IntPtr(1);
            }

            return IntPtr.Zero;
        }
    }
}
