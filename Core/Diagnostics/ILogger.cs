namespace Yaw.Core.Diagnostics
{
	/// <summary>
	/// ИыQерфейс ЃEггерЃEЃEильEенЃE.
	/// </summary>
	public interface ILogger
	{
		/// <summary>
		/// Дь@авЃEыGЃEсобытия ЃEЃEггер.
		/// </summary>
		/// <param name="logEvent">событиЃE/param>
		void Log(LoggerEvent logEvent);

        /// <summary>
        /// ПроверкЃEыDь@ходиЃEстЃEжурналирьAанЃE события ЃE фиЃEтрЃE"важнъBти"
        /// </summary>
        /// <param name="logEvent">событиЃE/param>
        /// <returns>ЃEизъьЃEыDь@ходиЃEстЃEЃEъCьIьJирьAанЃE</returns>
        bool IsAcceptedByEventType(LoggerEvent logEvent);
	}
}