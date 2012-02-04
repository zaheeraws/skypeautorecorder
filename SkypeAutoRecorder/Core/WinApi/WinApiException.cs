using System;

namespace SkypeAutoRecorder.Core.WinApi
{
    /// <summary>
    /// Exception thrown when Windows API function fails.
    /// </summary>
    internal class WinApiException : Exception
    {
        private readonly string _function;

        /// <summary>
        /// Initializes a new instance of the <see cref="WinApiException"/> class.
        /// </summary>
        /// <param name="winApiFunction">The Windows API function name that failed to execute.</param>
        /// <param name="message">The message.</param>
        public WinApiException(string winApiFunction, string message)
            : base(message)
        {
            _function = winApiFunction;
        }

        /// <summary>
        /// Gets the Windows API function name that failed to execute.
        /// </summary>
        public string WinApiFunction
        {
            get
            {
                return _function;
            }
        }
    }
}