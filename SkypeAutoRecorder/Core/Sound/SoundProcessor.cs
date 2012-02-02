using System.Diagnostics;
using System.IO;

namespace SkypeAutoRecorder.Core.Sound
{
    internal class SoundProcessor
    {
        private static readonly string SoxPath;
        private static readonly string LamePath;

        static SoundProcessor()
        {
            var currentLocation = Path.GetDirectoryName(System.Reflection.Assembly.GetEntryAssembly().Location);
            
            SoxPath = Path.Combine(currentLocation, "Libs\\Sox\\sox.exe");
            LamePath = Path.Combine(currentLocation, "Libs\\Lame\\lame.exe");
        }
        
        public static bool MergeChannels(string channel1FileName, string channel2FileName, string resultFileName)
        {
            var arguments = "-m \"" + channel1FileName + "\" \"" + channel2FileName + "\" \"" + resultFileName + "\"";
            return runProcess(SoxPath, arguments);
        }

        public static bool EncodeMp3(string wavFileName, string mp3FileName)
        {
            var arguments = "-V2 \"" + wavFileName + "\" \"" + mp3FileName + "\"";
            return runProcess(LamePath, arguments);
        }

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
