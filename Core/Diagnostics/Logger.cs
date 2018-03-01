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
    /// Логгер приложения <see cref="ICoreApplication"/>.
    /// </summary>
    internal class Logger : ILogger, IDisposable
    {
        /// <summary>
        /// Очередь событий
        /// </summary>
        private readonly BlockingQueue<LoggerEvent> _queue;
        /// <summary>
        /// Признак асинхронного журналирования
        /// </summary>
        private volatile Boolean _bAsync;
        /// <summary>
        /// Фильтры, накладываемые на события
        /// </summary>
        private readonly List<IEventFilter> _filters = new List<IEventFilter>();
        /// <summary>
        /// Фильтр отсеивания по уровню события
        /// </summary>
        private readonly IEventFilter _traceLevelFilter;
        /// <summary>
        /// Родитель
        /// </summary>
        private readonly Logger _parentLogger;

        /// <summary>
        /// Имя логгера
        /// </summary>
        public string LoggerName
        {
            get;
            private set;
        }

        /// <summary>
        /// ctor
        /// </summary>
        /// <param name="loggerName">имя журнала</param>
        /// <param name="filter">фильтер по умолчанию</param>
        /// <param name="parentLogger">родительский объект</param>
        /// <param name="queueEnabledEvent">событие приостановки/возобновления записи в журналы</param>
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

            // если это логер приложения
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
        /// Добавляем фильтр в цепочку фильтров
        /// </summary>
        /// <param name="filter">Фильтр</param>
        public void AddFilter(IEventFilter filter)
        {
            if (filter != null)
            {
                _filters.Add(filter);
            }
        }

        /// <summary>
        /// Признак асинхронного логирования
        /// </summary>
        internal Boolean IsAsync
        {
            get
            {
                return _parentLogger == null ? _bAsync : _parentLogger.IsAsync;
            }
        }

        /// <summary>
        /// Событие для приостановки записи в журналы
        /// </summary>
        private readonly ManualResetEvent _queueEnabledEvent;

        /// <summary>
        /// Мониторинг очереди событий от логеров
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
                            // ругнемся в консоль
                            LoggingUtils.LogToConsole(
                                "<{0}>: exception occurred during asynchronous logging: {1}",
                                logEvent[LoggerEvent.LOGGERNAME_PROPERTY], ex);

                            // попытаемся залогировать все оставшиеся в очереди сообщения
                            while (_queue.TryDequeue(0, out logEvent))
                            {
                                try
                                {
                                    EventDispatcher.Dispatch(logEvent);
                                }
                                catch (Exception exeption)
                                {
                                    // ругнемся в консоль
                                    LoggingUtils.LogToConsole("<{0}>: event not handled, exception occurred: {1}",
                                        logEvent[LoggerEvent.LOGGERNAME_PROPERTY], exeption);
                                }
                            }

                            throw;
                        }
                    }
                    else
                    {
                        // Очередь закрыли (вызвали Dispose)
                        break;
                    }
                }
                catch
                {
                    // если при асинхронном логировании возникло исключение, то переходим в синхронный режим
                    // NOTE: при этом поток завершит работу
                    _bAsync = false;
                    LoggingUtils.LogToConsole(
                        "<{0}>: асинхронное логирование отключено, далее используется синхронный режим",
                        LoggerName);
                }
            }
        }

        /// <summary>
        /// Перенаправляет вывод события в системный журнал
        /// </summary>
        /// <param name="logEvent">Событие</param>
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
                // если логер закрыт
                if (_queue.IsClosed)
                    return;

                logEvent[LoggerEvent.LOGGERNAME_PROPERTY] = LoggerName;

                // если хотя бы один фильтр не разрешил выполнять запись
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
                                  ? "Ошибка при помещении события в очередь {0} на асинхронное логирование: {1}"
                                  : "Ошибка при записи в логгер {0}: {1}";

                if (this != CoreApplication.Instance.Logger)
                {
                    //  если это не мы сами
                    CoreApplication.Instance.Logger.LogError(message, LoggerName, ex);
                    RedirectMessageToApplicationLogger(logEvent);
                }
                else
                {
                    // Иначе протоколируем на консоль
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
                // подождем, пока очередь не опустеет, но не дольше 1 минуты
                if (!_queue.EmptiedWaitHandle.WaitOne(TimeSpan.FromMinutes(1), false))
                {
                    const string MSG = "Could not wait for underflowing of event queue '{0}'. Remaining events: {1}";

                    // если это не основной логер приложения
                    if (this != CoreApplication.Instance.Logger)
                    {
                        // протоколируем через него
                        CoreApplication.Instance.Logger.LogError(MSG, LoggerName, _queue.Count);
                    }
                    else
                    {
                        // иначе протоколируем на консоль
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
