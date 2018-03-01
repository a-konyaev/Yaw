using System;

namespace Yaw.Core
{
	/// <summary>
	/// Параметры события выход из приложения
	/// </summary>
	public class ApplicationExitEventArgs : EventArgs
	{
		/// <summary>
		/// Тип выхода из приложения
		/// </summary>
		public readonly ApplicationExitType ExitType;

		/// <summary>
		/// Конструктор
		/// </summary>
		/// <param name="type">Тип выхода из приложения</param>
        public ApplicationExitEventArgs(ApplicationExitType type)
		{
			ExitType = type;
		}
	}
}
