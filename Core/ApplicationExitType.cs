namespace Yaw.Core
{
    /// <summary>
    /// Тип выхода из приложения
    /// </summary>
    public enum ApplicationExitType
    {
        /// <summary>
        /// Завершить работу приложения
        /// </summary>
        Exit = 1,
        /// <summary>
        /// Завершить работу приложения и потом запустить его заново
        /// </summary>
        RestartApplication = 2,
        /// <summary>
        /// Завершить работу приложения и потом перезагрузить ОС
        /// </summary>
        RebootOperationSystem = 3,
        /// <summary>
        /// Выключить сканер
        /// </summary>
        PowerOff = 4,
		/// <summary>
		/// Перезапуск драйвера, а затем приложения
		/// </summary>
		RestartDriverAndApplication = 5,
        /// <summary>
        /// Останов приложения
        /// </summary>
        /// <remarks>
        /// от других типов выхода отличается тем, что 
        /// не выполняется вызов метода Environment.Exit()
        /// </remarks>
        Stop = 6
    }
}
