using System;
using System.Diagnostics;

namespace Yaw.Core.Diagnostics
{
	/// <summary>
	/// Описание события для логгера ILogger
	/// </summary>
	public class LoggerEvent
	{
        public const string MESSAGE_PROPERTY = "Message";
        public const string EXCEPTION_PROPERTY = "Exception";
        public const string TIMESTAMP_PROPERTY = "Timestamp";
        public const string THREAD_ID = "ThreadId";
        public const string METHODNAME_PROPERTY = "MethodName";
	    public const string LOGGERNAME_PROPERTY = "Logger";
        public const string PARAMETERS_PROPERTY = "Params";
        public const string EVENTTYPE_PROPERTY = "EventType";

		/// <summary>
		/// Важность события
		/// </summary>
		public TraceEventType EventType { get; set; }

		/// <summary>
		/// Данные события. Пара имя-значение.
		/// </summary>
        public EventProperties Properties { get; set; }

        /// <summary>
        /// Индексатор по данным события
        /// </summary>
        public object this[string index]
	    {
	        get
	        {
	            if (Properties.ContainsKey(index))
                    return Properties[index];
                
	            if (index == EVENTTYPE_PROPERTY)
	                return EventType;

	            return string.Empty;
	        }
            set
            {
                if (index != EVENTTYPE_PROPERTY)
                {
                    Properties[index] = value;
                }
                else
                {
                    EventType = (TraceEventType)value;
                }
            }
	    }

        /// <summary>
        /// Уникальный идентификатор
        /// </summary>
        public Guid Id { get; private set; }

        /// <summary>
        /// Конструктор
        /// </summary>
        public LoggerEvent()
        {
            Id = Guid.NewGuid();
            Properties = new EventProperties();
            LoggingUtils.FillCommonContextProperies(Properties);
        }
	}
}