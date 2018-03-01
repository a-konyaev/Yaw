using System.Threading;
using Yaw.Core.Extensions;

namespace Yaw.Core.Utils.Threading
{
	public class TimeoutWaitHandle : EventWaitHandleEx
	{
		/// <summary>
		/// Время ожидания до вызова события
		/// </summary>
		private readonly int _timeout;

		/// <summary>
		/// Тред в котором запускаем ожидание события
		/// </summary>
		private Thread _eventThread;

		/// <summary>
		/// Признак, что объект освобожден
		/// </summary>
		private bool _disposed;

		/// <summary>
		/// Конструктор
		/// </summary>
		/// <param name="timeout">Время, через которое запускать событие</param>
		public TimeoutWaitHandle(int timeout)
			: base(false, false)
		{
			_timeout = timeout;
		}

		/// <summary>
		/// Запуск события
		/// </summary>
		new public void Reset()
		{
			// если тред уже есть
			if (_eventThread != null)
				_eventThread.SafeAbort();

			// запустим поток ожидания таймаута
			_eventThread = ThreadUtils.StartBackgroundThread(WaitingForTimeout);
		}

		/// <summary>
		/// Ожидание таймаута и выставление состояния дескриптора ожидания события в "Включен"
		/// </summary>
		private void WaitingForTimeout()
		{
			// ждем сколько указано в таймауте
			Thread.Sleep(_timeout);

			// вызовем Set у базового класса
			if(!_disposed)
				Set();
		}

		/// <summary>
		/// Освобождение ресурсов
		/// </summary>
		/// <param name="explicitDisposing"></param>
		protected override void Dispose(bool explicitDisposing)
		{
			// пометим объект как dispose
			_disposed = true;

			base.Dispose(explicitDisposing);
		}
	}
}
