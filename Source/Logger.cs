namespace LordKuper.WorkManager
{
    internal static class Logger
    {
        /// <summary>
        ///     Logs a general informational message.
        /// </summary>
        /// <param name="message">The message to log.</param>
        internal static void LogMessage(string message)
        {
            Common.Logger.LogMessage(WorkManagerMod.ModId, message);
        }
    }
}