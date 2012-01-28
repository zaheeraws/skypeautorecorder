using System;

namespace SkypeAutoRecorder.Core.WinApi
{
    /// <summary>
    /// Exception thrown when Windows API function fails.
    /// </summary>
    internal class WinApiException : Exception
    {
        private readonly string _function;

        public WinApiException(string winApiFunction, string message)
            : base(message)
        {
            _function = winApiFunction;
        }

        public string WinApiFunction
        {
            get
            {
                return _function;
            }
        }
    }
}