namespace Yaw.Core.Diagnostics
{
    /// <summary>
    /// Интерфейс для получения доступа к логгеру
    /// </summary>
    public interface ILoggerContainer
    {
        /// <summary>
        /// Логгер
        /// </summary>
        ILogger Logger { get; }
    }
}
