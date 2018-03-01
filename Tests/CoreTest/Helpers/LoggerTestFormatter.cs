using System;
using Yaw.Core.Diagnostics;

namespace Yaw.Tests.CoreTest.Helpers
{
	/// <summary>
	/// Форматтер для юнит тестирования Логгера
	/// </summary>
	class LoggerTestFormatter : IEventFormatter
	{
		/// <summary>
		/// Строка сообщения: Время - "Сообщение", Метод
		/// </summary>
		private const string FORMAT = "{0} - {1}, {2}";

		#region IEventFormatter Members

		public string Format(LoggerEvent loggerEvent)
		{
			return String.Format(FORMAT, loggerEvent["Timestamp"], loggerEvent["Message"], loggerEvent["MethodName"]);
		}

		#endregion

		#region IInitializedType Members

		/// <summary>
		/// Инициализация класа Форматтера
		/// </summary>
		/// <param name="props">параметры из конфига</param>
		public void Init(System.Configuration.NameValueConfigurationCollection props)
		{
		}

		#endregion
	}
}
