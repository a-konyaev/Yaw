using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;

namespace Yaw.Core.Utils.Collections
{
	/// <summary>
	/// Реализация потоко-безопасной блокирующей на ожидании очереди.
	/// При выталкивании объекта из очереди (<see cref="Dequeue()"/>) поток блокируется до тех пор пока,
	/// в очереди не появится объект (помещенный с помощью <see cref="Enqueue"/>).
	/// Очередь поддерживает закрытие (<see cref="Close"/>) и открытие (<see cref="Open"/>). 
	/// Попытка помещения объекта в закрытую очередь заканчивается исключение.
	/// При закрытии очередь не очищается
	/// </summary>
	public class BlockingQueue<T> : IDisposable
	{
		private readonly Queue<T> _queue;
		private Boolean _open;
		private Boolean _disposed;
		private readonly EventWaitHandle _eventEmpty = new ManualResetEvent(true);

		/// <summary>
		/// Create new BlockingQueue.
		/// </summary>
		public BlockingQueue()
		{
			_queue = new Queue<T>();
			_open = true;
		}

		/// <summary>
		/// Событие об опустошении очереди.
		/// </summary>
		public WaitHandle EmptiedWaitHandle
		{
			get { return _eventEmpty; }
		}

		public void Dispose()
		{
			lock (SyncRoot)
			{
				if (!_disposed)
				{
					_open = false;
					_disposed = true;
					_queue.Clear();
					_eventEmpty.Close();
					Monitor.PulseAll(SyncRoot); // resume any waiting threads
				}
			}
		}

		private void ThrowIfDisposed()
		{
			if (_disposed)
				throw new ObjectDisposedException(GetType().FullName);
		}

		private Object SyncRoot
		{
			get { return ((ICollection) _queue).SyncRoot; }
		}

		/// <summary>
		/// Возвращает количество объектов в очереди.
		/// </summary>
		/// <remarks>
		/// Для закрытой и disposed очередень возвращает 0.
		/// </remarks>
		public Int32 Count
		{
			get
			{
				lock (SyncRoot)
					return _queue.Count;
			}
		}

		/// <summary>
		/// Удаляет все объекты из очереди.
		/// </summary>
		/// <exception cref="ObjectDisposedException">Объект был освобожден (вызван метод <see cref="Dispose"/>)</exception>
		public void Clear()
		{
			lock (SyncRoot)
			{
				ThrowIfDisposed();
				_queue.Clear();
				_eventEmpty.Set();
			}
		}

		/// <summary>
		/// Закрывает очередь на прием новых объектов.
		/// При попытке помещения нового события в <see cref="Enqueue"/> будет сгенерировано <see cref="InvalidOperationException"/>.
		/// </summary>
		public void Close()
		{
			lock (SyncRoot)
			{
				if (_disposed)
					return;

				_open = false;
				Monitor.PulseAll(SyncRoot); // resume any waiting threads
			}
		}

		/// <summary>
		/// Удаляет и возвращает объект из начала очереди.
		/// </summary>
		/// <exception cref="InvalidOperationException">Истек таймаут, либо очередь была закрыта</exception>
		/// <exception cref="ObjectDisposedException">Объект был освобожден (вызван метод <see cref="Dispose"/>)</exception>
		/// <returns>Объект.</returns>
		public T Dequeue()
		{
			return Dequeue(Timeout.Infinite);
		}

		/// <summary>
		/// Удаляет и возвращает объект из начала очереди.
		/// </summary>
		/// <param name="timeout">Тайм-аут ожидания перед возвратом</param>
		/// <exception cref="InvalidOperationException">Истек таймаут, либо очередь была закрыта</exception>
		/// <exception cref="ObjectDisposedException">Объект был освобожден (вызван метод <see cref="Dispose"/>)</exception>
		/// <returns>Объект.</returns>
		public T Dequeue(TimeSpan timeout)
		{
			return Dequeue(timeout.Milliseconds);
		}

		/// <summary>
		/// Удаляет и возвращает объект из начала очереди.
		/// </summary>
		/// <param name="timeoutMilliseconds">Тайм-аут ожидания перед возвратом (в милисекундах)</param>
		/// <exception cref="InvalidOperationException">Истек таймаут, либо очередь была закрыта</exception>
		/// <exception cref="ObjectDisposedException">Объект был освобожден (вызван метод <see cref="Dispose"/>)</exception>
		/// <returns>Объект.</returns>
		public T Dequeue(Int32 timeoutMilliseconds)
		{
			lock (SyncRoot)
			{
				ThrowIfDisposed();
				while (_open && (_queue.Count == 0))
				{
					if (!Monitor.Wait(SyncRoot, timeoutMilliseconds))
						throw new InvalidOperationException("Timeout");
				}
				if (_open)
				{
					var value = _queue.Dequeue();
					SignalIfEmptyUnsafe();
					return value;
				}

				throw new InvalidOperationException("Queue Closed");
			}
		}

		/// <summary>
		/// Устанавливает событие, доступное через публичное свойство <see cref="EmptiedWaitHandle"/>, если очередь пуста.
		/// </summary>
		private void SignalIfEmptyUnsafe()
		{
			if (_queue.Count == 0)
				_eventEmpty.Set();
		}

		/// <summary>
		/// Пытается достать объект из начала очереди.
		/// </summary>
		/// <param name="value">Объект из очереди</param>
		/// <exception cref="InvalidOperationException">Истек таймаут, либо очередь была закрыта</exception>
		/// <exception cref="ObjectDisposedException">Объект был освобожден (вызван метод <see cref="Dispose"/>)</exception>
		/// <returns><value>true</value> - объект успешно извлечен из очереди, <value>false</value> - очередь была закрыта</returns>
		public Boolean TryDequeue(out T value)
		{
			return TryDequeue(Timeout.Infinite, out value);
		}

		/// <summary>
		/// Пытается достать объект из начала очереди.
		/// </summary>
		/// <param name="timeoutMilliseconds">Тайм-аут ожидания</param>
		/// <param name="value">Объект из очереди</param>
		/// <exception cref="InvalidOperationException">Истек таймаут, либо очередь была закрыта</exception>
		/// <exception cref="ObjectDisposedException">Объект был освобожден (вызван метод <see cref="Dispose"/>)</exception>
		/// <returns><value>true</value> - объект успешно извлечен из очереди, <value>false</value> - истек тайм-аут, либо очередь была закрыта</returns>
		public Boolean TryDequeue(Int32 timeoutMilliseconds, out T value)
		{
			value = default(T);
			lock (SyncRoot)
			{
				if (!_open)
					return false;

				while (_open && _queue.Count == 0)
				{
					if (!Monitor.Wait(SyncRoot, timeoutMilliseconds))
						return false;
				}
				if (_open)
				{
					value = _queue.Dequeue();
					SignalIfEmptyUnsafe();
					return true;
				}
				return false;
			}
		}

		/// <summary>
		/// Помещает объект в конец очереди.
		/// </summary>
		/// <exception cref="InvalidOperationException">Очередь закрыта</exception>
		/// <exception cref="ObjectDisposedException">Объект был освобожден (вызван метод <see cref="Dispose"/>)</exception>
		/// <param name="obj">Объект для помещения в очередь</param>
		public void Enqueue(T obj)
		{
			lock (SyncRoot)
			{
				ThrowIfDisposed();
				if (!_open)
					throw new InvalidOperationException("Помещение объекта в закрытую очередь недопустимо");

				_queue.Enqueue(obj);
				_eventEmpty.Reset();
				Monitor.Pulse(SyncRoot);
			}
		}

        /// <summary>
        /// Помещает объект в конец очереди.
        /// </summary>
        /// <param name="obj">Объект для помещения в очередь</param>
        /// <returns>
        /// true - объект был помещен в конец очереди
        /// false - очередь закрыта или была уничтожена</returns>
        public bool TryEnqueue(T obj)
        {
            lock (SyncRoot)
            {
                // если очередь закрыта или уничтожена
                if (!_open || _disposed)
                    return false;

                _queue.Enqueue(obj);
                _eventEmpty.Reset();
                Monitor.Pulse(SyncRoot);
                return true;
            }
        }

		/// <summary>
		/// Открывает очередь - разрешено помещение новых объектов.
		/// </summary>
		/// <exception cref="ObjectDisposedException">Объект был освобожден (вызван метод <see cref="Dispose"/>)</exception>
		public void Open()
		{
			lock (SyncRoot)
			{
				ThrowIfDisposed();
				_open = true;
			}
		}

		/// <summary>
		/// Возвращает признак того, что очередь закрыта.
		/// </summary>
		public Boolean IsClosed
		{
			get { return !_open; }
		}
	}
}