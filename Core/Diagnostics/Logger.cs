using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Yaw.Core.Diagnostics.Default;
using Yaw.Core.Utils.Collections;
using Yaw.Core.Utils.Threading;

namespace Yaw.Core.Diagnostics
{
    /// <summary>
    /// ������ ���������� <see cref="ICoreApplication"/>.
    /// </summary>
    internal class Logger : ILogger, IDisposable
    {
        /// <summary>
        /// ������� �������
        /// </summary>
        private readonly BlockingQueue<LoggerEvent> _queue;
        /// <summary>
        /// ������� ������������ ��������������
        /// </summary>
        private volatile Boolean _bAsync;
        /// <summary>
        /// �������, ������������� �� �������
        /// </summary>
        private readonly List<IEventFilter> _filters = new List<IEventFilter>();
        /// <summary>
        /// ������ ���������� �� ������ �������
        /// </summary>
        private readonly IEventFilter _traceLevelFilter;
        /// <summary>
        /// ��������
        /// </summary>
        private readonly Logger _parentLogger;

        /// <summary>
        /// ��� �������
        /// </summary>
        public string LoggerName
        {
            get;
            private set;
        }

        /// <summary>
        /// ctor
        /// </summary>
        /// <param name="loggerName">��� �������</param>
        /// <param name="filter">������� �� ���������</param>
        /// <param name="parentLogger">������������ ������</param>
        /// <param name="queueEnabledEvent">������� ������������/������������� ������ � �������</param>
        public Logger(string loggerName, IEventFilter filter, Logger parentLogger, ManualResetEvent queueEnabledEvent)
        {
            CodeContract.Requires(!string.IsNullOrEmpty(loggerName));

            _queueEnabledEvent = queueEnabledEvent;

            LoggerName = loggerName;
            if (filter != null)
            {
                _traceLevelFilter = filter;
                _filters.Add(filter);
            }

            // ���� ��� ����� ����������
            if (parentLogger == null)
            {
                _bAsync = true;
                _queue = new BlockingQueue<LoggerEvent>();
                ThreadUtils.StartBackgroundThread(MonitorQueue);
            }
            else
            {
                _parentLogger = parentLogger;
                _queue = _parentLogger._queue;
            }
        }

        /// <summary>
        /// ��������� ������ � ������� ��������
        /// </summary>
        /// <param name="filter">������</param>
        public void AddFilter(IEventFilter filter)
        {
            if (filter != null)
            {
                _filters.Add(filter);
            }
        }

        /// <summary>
        /// ������� ������������ �����������
        /// </summary>
        internal Boolean IsAsync
        {
            get
            {
                return _parentLogger == null ? _bAsync : _parentLogger.IsAsync;
            }
        }

        /// <summary>
        /// ������� ��� ������������ ������ � �������
        /// </summary>
        private readonly ManualResetEvent _queueEnabledEvent;

        /// <summary>
        /// ���������� ������� ������� �� �������
        /// </summary>
        private void MonitorQueue()
        {
            while (_bAsync)
            {
                try
                {
                    if (_queueEnabledEvent != null)
                    {
                        _queueEnabledEvent.WaitOne();
                    }

                    LoggerEvent logEvent;
                    if (_queue.TryDequeue(out logEvent))
                    {
                        try
                        {
                            EventDispatcher.Dispatch(logEvent);
                        }
                        catch (Exception ex)
                        {
                            // �������� � �������
                            LoggingUtils.LogToConsole(
                                "<{0}>: exception occurred during asynchronous logging: {1}",
                                logEvent[LoggerEvent.LOGGERNAME_PROPERTY], ex);

                            // ���������� ������������ ��� ���������� � ������� ���������
                            while (_queue.TryDequeue(0, out logEvent))
                            {
                                try
                                {
                                    EventDispatcher.Dispatch(logEvent);
                                }
                                catch (Exception exeption)
                                {
                                    // �������� � �������
                                    LoggingUtils.LogToConsole("<{0}>: event not handled, exception occurred: {1}",
                                        logEvent[LoggerEvent.LOGGERNAME_PROPERTY], exeption);
                                }
                            }

                            throw;
                        }
                    }
                    else
                    {
                        // ������� ������� (������� Dispose)
                        break;
                    }
                }
                catch
                {
                    // ���� ��� ����������� ����������� �������� ����������, �� ��������� � ���������� �����
                    // NOTE: ��� ���� ����� �������� ������
                    _bAsync = false;
                    LoggingUtils.LogToConsole(
                        "<{0}>: ����������� ����������� ���������, ����� ������������ ���������� �����",
                        LoggerName);
                }
            }
        }

        /// <summary>
        /// �������������� ����� ������� � ��������� ������
        /// </summary>
        /// <param name="logEvent">�������</param>
        private static void RedirectMessageToApplicationLogger(LoggerEvent logEvent)
        {
            logEvent[LoggerEvent.MESSAGE_PROPERTY] =
                "<" + logEvent[LoggerEvent.LOGGERNAME_PROPERTY] + ">: " +
                logEvent[LoggerEvent.MESSAGE_PROPERTY];
            CoreApplication.Instance.Logger.Log(logEvent);
        }

        /// <inheritdoc/>
        public bool IsAcceptedByEventType(LoggerEvent logEvent)
        {
            return _traceLevelFilter == null || _traceLevelFilter.Accepted(logEvent);
        }

        /// <inheritdoc/>
        public void Log(LoggerEvent logEvent)
        {
            try
            {
                // ���� ����� ������
                if (_queue.IsClosed)
                    return;

                logEvent[LoggerEvent.LOGGERNAME_PROPERTY] = LoggerName;

                // ���� ���� �� ���� ������ �� �������� ��������� ������
                if (_filters.Any(filter => !filter.Accepted(logEvent)))
                    return;

                if (IsAsync)
                {
                    _queue.Enqueue(logEvent);
                }
                else
                {
                    EventDispatcher.Dispatch(logEvent);
                }
            }
            catch (Exception ex)
            {
                var message = IsAsync
                                  ? "������ ��� ��������� ������� � ������� {0} �� ����������� �����������: {1}"
                                  : "������ ��� ������ � ������ {0}: {1}";

                if (this != CoreApplication.Instance.Logger)
                {
                    //  ���� ��� �� �� ����
                    CoreApplication.Instance.Logger.LogError(message, LoggerName, ex);
                    RedirectMessageToApplicationLogger(logEvent);
                }
                else
                {
                    // ����� ������������� �� �������
                    LoggingUtils.LogToConsole(message, LoggerName, ex);
                }
            }
        }

        public void Dispose()
        {
            if (_queue == null || _parentLogger != null)
                return;

            try
            {
                // ��������, ���� ������� �� ��������, �� �� ������ 1 ������
                if (!_queue.EmptiedWaitHandle.WaitOne(TimeSpan.FromMinutes(1), false))
                {
                    const string MSG = "Could not wait for underflowing of event queue '{0}'. Remaining events: {1}";

                    // ���� ��� �� �������� ����� ����������
                    if (this != CoreApplication.Instance.Logger)
                    {
                        // ������������� ����� ����
                        CoreApplication.Instance.Logger.LogError(MSG, LoggerName, _queue.Count);
                    }
                    else
                    {
                        // ����� ������������� �� �������
                        LoggingUtils.LogToConsole(MSG, LoggerName, _queue.Count);
                    }
                }
            }
            catch (ObjectDisposedException)
            {
            }

            _queue.Dispose();
        }
    }
}
