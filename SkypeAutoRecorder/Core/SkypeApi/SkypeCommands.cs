namespace SkypeAutoRecorder.Core.SkypeApi
{
    /// <summary>
    /// Commands to control Skype when connection is already established.
    /// </summary>
    internal static class SkypeCommands
    {
        public const string StartRecordOutput = "ALTER CALL {0} SET_OUTPUT FILE=\"{1}\"";
        public const string StartRecordInput  = "ALTER CALL {0} SET_CAPTURE_MIC FILE=\"{1}\"";
        public const string EndRecordOutput   = "ALTER CALL {0} SET_OUTPUT SOUNDCARD=\"default\"";
        public const string EndRecordInput    = "ALTER CALL {0} SET_CAPTURE_MIC PORT=\"356\"";
        public const string GetCallerName     = "GET CALL {0} PARTNER_HANDLE";
    }
}
