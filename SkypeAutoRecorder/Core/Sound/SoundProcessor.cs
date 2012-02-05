using System.Diagnostics;
using System.IO;
using SkypeAutoRecorder.Configuration;

namespace SkypeAutoRecorder.Core.Sound
{
    /// <summary>
    /// Provides methods for processing sound files recorded by Skype.
    /// </summary>
    internal class SoundProcessor
    {
        private static readonly string SoxPath;
        private static readonly string LamePath;

        /// <summary>
        /// Initializes the <see cref="SoundProcessor"/> class.
        /// </summary>
        static SoundProcessor()
        {
            var currentLocation = Path.GetDirectoryName(System.Reflection.Assembly.GetEntryAssembly().Location);
            
            // Get the path for Sox and Lame external applications used for sound processing.
            SoxPath = Path.Combine(currentLocation, "Libs\\Sox\\sox.exe");
            LamePath = Path.Combine(currentLocation, "Libs\\Lame\\lame.exe");
        }

        /// <summary>
        /// Merges the two sound channels.
        /// </summary>
        /// <param name="channel1FileName">Name of the first channel file.</param>
        /// <param name="channel2FileName">Name of the second channel file.</param>
        /// <param name="resultFileName">Name for the resulting file.</param>
        /// <returns><c>true</c> if merging finished successfuly; otherwise, <c>false</c>.</returns>
        public static bool MergeChannels(string channel1FileName, string channel2FileName, string resultFileName)
        {
            var arguments = "-m \"" + channel1FileName + "\" \"" + channel2FileName + "\" \"" + resultFileName + "\"";
            return runProcess(SoxPath, arguments);
        }

        /// <summary>
        /// Encodes sound to MP3.
        /// </summary>
        /// <param name="wavFileName">Name of the input WAV file.</param>
        /// <param name="mp3FileName">Name for the resulting MP3 file.</param>
        /// <param name="volumeScale">Volume scale of the resulting file.</param>
        /// <returns><c>true</c> if encoding finished successfuly; otherwise, <c>false</c>.</returns>
        public static bool EncodeMp3(string wavFileName, string mp3FileName, int volumeScale)
        {
            var arguments = "-V2 --scale " + volumeScale + " \"" + wavFileName + "\" \"" + mp3FileName + "\"";
            return runProcess(LamePath, arguments);
        }

        /// <summary>
        /// Runs the process of external application.
        /// </summary>
        /// <param name="app">The external application file name.</param>
        /// <param name="arguments">The arguments for running.</param>
        /// <returns><c>true</c> if process finished successfuly; otherwise, <c>false</c>.</returns>
        private static bool runProcess(string app, string arguments)
        {
            using (var process = new Process())
            {
                var processStartInfo = new ProcessStartInfo
                                       {
                                           FileName = app,
                                           Arguments = arguments,
                                           CreateNoWindow = true,
                                           UseShellExecute = false
                                       };
                process.StartInfo = processStartInfo;
                process.Start();
                process.WaitForExit();
                return process.ExitCode == 0;
            }
        }
    }
}
