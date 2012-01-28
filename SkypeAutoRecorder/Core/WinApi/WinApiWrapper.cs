using System;
using System.Runtime.InteropServices;

namespace SkypeAutoRecorder.Core.WinApi
{
    /// <summary>
    /// Wrapper for Windows API functions.
    /// </summary>
    internal static class WinApiWrapper
    {
        #region External functions

        [DllImport("user32.dll")]
        private static extern uint RegisterWindowMessage(string message);

        [DllImport("user32.dll")]
        private static extern IntPtr SendMessageTimeout(
            IntPtr windowHandle,
            uint message,
            IntPtr wParam,
            IntPtr lParam,
            SendMessageTimeoutFlags flags,
            uint timeout,
            out IntPtr result);

        [DllImport("user32.dll")]
        private static extern IntPtr SendMessageTimeout(
            IntPtr windowHandle,
            uint message,
            IntPtr wParam,
            ref CopyDataStruct lParam,
            SendMessageTimeoutFlags flags,
            uint timeout,
            out IntPtr result);

        [DllImport("user32.dll")]
        private static extern IntPtr FindWindow(string className, string title);

        #endregion

        public static uint RegisterApiMessage(string message)
        {
            var id = RegisterWindowMessage(message);
            if (id == 0)
            {
                throw new WinApiException("RegisterWindowMessage", "Failed to register " + message);
            }

            return id;
        }

        public static void SendMessage(IntPtr receiverHandle, uint message, IntPtr param)
        {
            IntPtr result;
            if (SendMessageTimeout(receiverHandle, message, param, IntPtr.Zero,
                                   SendMessageTimeoutFlags.Normal, 100, out result).ToInt32() == 0)
            {
                throw new WinApiException("SendMessageTimeout", string.Empty);
            }
        }

        public static void SendMessage(IntPtr receiverHandle, uint message, IntPtr param, ref CopyDataStruct data)
        {
            IntPtr result;
            if (SendMessageTimeout(receiverHandle, message, param, ref data,
                                   SendMessageTimeoutFlags.Normal, 100, out result).ToInt32() == 0)
            {
                throw new WinApiException("SendMessageTimeout", string.Empty);
            }
        }

        public static bool WindowExists(string windowClassName)
        {
            return FindWindow(windowClassName, null) != IntPtr.Zero;
        }
    }
}
