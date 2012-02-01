using System.Diagnostics;
using System.IO;

namespace SkypeAutoRecorder.Core.Sound
{
    internal class SoundProcessor
    {
        private static readonly string SoxPath = Path.Combine(
            Path.GetDirectoryName(System.Reflection.Assembly.GetEntryAssembly().Location), "Libs\\Sox\\sox.exe");
        
        public static bool MergeChannels(string channel1FileName, string channel2FileName, string resultFileName)
        {
            using (var process = new Process())
            {
                var processStartInfo = new ProcessStartInfo
                                       {
                                           FileName = SoxPath,
                                           Arguments = "-m \"" + channel1FileName + "\" \"" + channel2FileName +
                                                       "\" \"" + resultFileName + "\"",
                                           CreateNoWindow = true,
                                           UseShellExecute = false
                                       };
                process.StartInfo = processStartInfo;
                process.Start();
                process.WaitForExit(900000);
                if (!process.HasExited)
                {
                    process.Kill();
                    return false;
                }
                return true;
            }
        }
    }
}
