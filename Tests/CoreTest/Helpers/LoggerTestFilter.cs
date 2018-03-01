using System.Diagnostics;
using Yaw.Core.Diagnostics;

namespace Yaw.Tests.CoreTest.Helpers
{
	/// <summary>
	/// Тестовый фильтр для логгера
	/// </summary>
	public class LoggerTestFilter : IEventWriterFilter
	{

		#region IInitializedType Members

		/// <summary>
		/// Инициализация
		/// </summary>
		/// <param name="props">параметры инициализации из конфига</param>
		public void Init(System.Configuration.NameValueConfigurationCollection props)
		{
		}

		#endregion

		#region IEventWriterFilter Members

		/// <summary>
		/// Проходит ли сообщение фильтрацию
		/// </summary>
		/// <param name="writerTriplet">Тройка для записи в лог</param>
		/// <param name="loggerEvent">Событие логирования</param>
		/// <param name="message">Текст сообщения</param>
		/// <returns>true - сообщение удовлетворяет условию фильтра</returns>
		public bool Accepted(EventWriterTriplet writerTriplet, LoggerEvent loggerEvent, string message)
		{
			// пропускаем только Ошибки
			return (loggerEvent.EventType == TraceEventType.Error);
		}

		#endregion
	}
}
