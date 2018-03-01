namespace Yaw.Core.Diagnostics
{
	/// <summary>
    /// Реализация ILogger, которая ничего не делает.
	/// </summary>
	internal class NullLogger : ILogger
	{
        /// <inheritdoc/>
        public void Log(LoggerEvent logEvent)
		{
		}

        /// <inheritdoc/>
        public bool IsAcceptedByEventType(LoggerEvent logEvent)
        {
            return false;
        }
	}
}