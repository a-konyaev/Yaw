using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;

namespace Yaw.Core.Utils.Collections
{
	/// <summary>
	/// ���������� ������-���������� ����������� �� �������� �������.
	/// ��� ������������ ������� �� ������� (<see cref="Dequeue()"/>) ����� ����������� �� ��� ��� ����,
	/// � ������� �� �������� ������ (���������� � ������� <see cref="Enqueue"/>).
	/// ������� ������������ �������� (<see cref="Close"/>) � �������� (<see cref="Open"/>). 
	/// ������� ��������� ������� � �������� ������� ������������� ����������.
	/// ��� �������� ������� �� ���������
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
		/// ������� �� ����������� �������.
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
		/// ���������� ���������� �������� � �������.
		/// </summary>
		/// <remarks>
		/// ��� �������� � disposed ��������� ���������� 0.
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
		/// ������� ��� ������� �� �������.
		/// </summary>
		/// <exception cref="ObjectDisposedException">������ ��� ���������� (������ ����� <see cref="Dispose"/>)</exception>
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
		/// ��������� ������� �� ����� ����� ��������.
		/// ��� ������� ��������� ������ ������� � <see cref="Enqueue"/> ����� ������������� <see cref="InvalidOperationException"/>.
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
		/// ������� � ���������� ������ �� ������ �������.
		/// </summary>
		/// <exception cref="InvalidOperationException">����� �������, ���� ������� ���� �������</exception>
		/// <exception cref="ObjectDisposedException">������ ��� ���������� (������ ����� <see cref="Dispose"/>)</exception>
		/// <returns>������.</returns>
		public T Dequeue()
		{
			return Dequeue(Timeout.Infinite);
		}

		/// <summary>
		/// ������� � ���������� ������ �� ������ �������.
		/// </summary>
		/// <param name="timeout">����-��� �������� ����� ���������</param>
		/// <exception cref="InvalidOperationException">����� �������, ���� ������� ���� �������</exception>
		/// <exception cref="ObjectDisposedException">������ ��� ���������� (������ ����� <see cref="Dispose"/>)</exception>
		/// <returns>������.</returns>
		public T Dequeue(TimeSpan timeout)
		{
			return Dequeue(timeout.Milliseconds);
		}

		/// <summary>
		/// ������� � ���������� ������ �� ������ �������.
		/// </summary>
		/// <param name="timeoutMilliseconds">����-��� �������� ����� ��������� (� ������������)</param>
		/// <exception cref="InvalidOperationException">����� �������, ���� ������� ���� �������</exception>
		/// <exception cref="ObjectDisposedException">������ ��� ���������� (������ ����� <see cref="Dispose"/>)</exception>
		/// <returns>������.</returns>
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
		/// ������������� �������, ��������� ����� ��������� �������� <see cref="EmptiedWaitHandle"/>, ���� ������� �����.
		/// </summary>
		private void SignalIfEmptyUnsafe()
		{
			if (_queue.Count == 0)
				_eventEmpty.Set();
		}

		/// <summary>
		/// �������� ������� ������ �� ������ �������.
		/// </summary>
		/// <param name="value">������ �� �������</param>
		/// <exception cref="InvalidOperationException">����� �������, ���� ������� ���� �������</exception>
		/// <exception cref="ObjectDisposedException">������ ��� ���������� (������ ����� <see cref="Dispose"/>)</exception>
		/// <returns><value>true</value> - ������ ������� �������� �� �������, <value>false</value> - ������� ���� �������</returns>
		public Boolean TryDequeue(out T value)
		{
			return TryDequeue(Timeout.Infinite, out value);
		}

		/// <summary>
		/// �������� ������� ������ �� ������ �������.
		/// </summary>
		/// <param name="timeoutMilliseconds">����-��� ��������</param>
		/// <param name="value">������ �� �������</param>
		/// <exception cref="InvalidOperationException">����� �������, ���� ������� ���� �������</exception>
		/// <exception cref="ObjectDisposedException">������ ��� ���������� (������ ����� <see cref="Dispose"/>)</exception>
		/// <returns><value>true</value> - ������ ������� �������� �� �������, <value>false</value> - ����� ����-���, ���� ������� ���� �������</returns>
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
		/// �������� ������ � ����� �������.
		/// </summary>
		/// <exception cref="InvalidOperationException">������� �������</exception>
		/// <exception cref="ObjectDisposedException">������ ��� ���������� (������ ����� <see cref="Dispose"/>)</exception>
		/// <param name="obj">������ ��� ��������� � �������</param>
		public void Enqueue(T obj)
		{
			lock (SyncRoot)
			{
				ThrowIfDisposed();
				if (!_open)
					throw new InvalidOperationException("��������� ������� � �������� ������� �����������");

				_queue.Enqueue(obj);
				_eventEmpty.Reset();
				Monitor.Pulse(SyncRoot);
			}
		}

        /// <summary>
        /// �������� ������ � ����� �������.
        /// </summary>
        /// <param name="obj">������ ��� ��������� � �������</param>
        /// <returns>
        /// true - ������ ��� ������� � ����� �������
        /// false - ������� ������� ��� ���� ����������</returns>
        public bool TryEnqueue(T obj)
        {
            lock (SyncRoot)
            {
                // ���� ������� ������� ��� ����������
                if (!_open || _disposed)
                    return false;

                _queue.Enqueue(obj);
                _eventEmpty.Reset();
                Monitor.Pulse(SyncRoot);
                return true;
            }
        }

		/// <summary>
		/// ��������� ������� - ��������� ��������� ����� ��������.
		/// </summary>
		/// <exception cref="ObjectDisposedException">������ ��� ���������� (������ ����� <see cref="Dispose"/>)</exception>
		public void Open()
		{
			lock (SyncRoot)
			{
				ThrowIfDisposed();
				_open = true;
			}
		}

		/// <summary>
		/// ���������� ������� ����, ��� ������� �������.
		/// </summary>
		public Boolean IsClosed
		{
			get { return !_open; }
		}
	}
}