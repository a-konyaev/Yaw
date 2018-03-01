using System;
using System.Collections.Generic;
using System.Linq;
using Yaw.Core.Configuration;

namespace Yaw.Core.Diagnostics
{
    /// <summary>
    /// Диспетчирезатор событий
    /// </summary>
    public static class EventDispatcher
    {
        /// <summary>
        /// Список приеников событий (писателей)
        /// </summary>
        private static readonly List<EventWriterTriplet> s_eventWriterTriplets = new List<EventWriterTriplet>();

        /// <summary>
        /// Поле в свойствах события для группировки
        /// </summary>
        public static string GroupByField { get; private set; }

        /// <summary>
        /// Задано ли значение поля для группировки событий
        /// </summary>
        public static bool GroupByFieldDefined { get; private set; }

        /// <summary>
        /// Фильтры событий
        /// </summary>
        public static List<IEventFilter> EventFilters = new List<IEventFilter>();

        /// <summary>
        /// Инициализация доступных писателей
        /// </summary>
        /// <param name="config">Секция конфигурации</param>
        public static void Init(DiagnosticsConfig config)
        {
            if (config == null)
                return;

            // сформируем список фильтров
            if (config.EventFilters != null)
            {
                foreach (FilterConfig filterConfig in config.EventFilters)
                {
                    var filter = ConstructObject(filterConfig.TypeName) as IEventFilter;
                    if (filter != null)
                    {
                        filter.Init(filterConfig.Props);
                        EventFilters.Add(filter);
                    }
                }
            }

            if (config.Writers == null)
                return;

            // получим значение поля для группировки событий
            GroupByField = config.GroupBy.Trim();
            GroupByFieldDefined = !string.IsNullOrEmpty(GroupByField);

            // сформируем список писателей
            foreach (WriterConfig writer in config.Writers)
            {
                var eventWriterTriplet = new EventWriterTriplet
                                             {
                                                 Writer = ConstructObject(writer.TypeName) as IEventWriter
                                             };

                if (eventWriterTriplet.Writer == null)
                    continue;

                eventWriterTriplet.Writer.Init(writer.Props);

                eventWriterTriplet.Formatter = ConstructObject(writer.EventFormatter.TypeName) as IEventFormatter;
                if (eventWriterTriplet.Formatter == null)
                {
                    eventWriterTriplet.Formatter = new Default.EventFormatter();
                }
                else
                {
                    eventWriterTriplet.Raw = writer.EventFormatter.Raw;
                }
                eventWriterTriplet.Formatter.Init(writer.EventFormatter.Props);

                foreach (FilterConfig filterConfig in writer.EventFilters)
                {
                    var filter = ConstructObject(filterConfig.TypeName) as IEventWriterFilter;
                    if (filter != null)
                    {
                        filter.Init(filterConfig.Props);
                        eventWriterTriplet.Filters.Add(filter);
                    }
                }

                s_eventWriterTriplets.Add(eventWriterTriplet);
            }
        }

        /// <summary>
        /// Конструирует объект
        /// </summary>
        /// <param name="typeName">Имя типа</param>
        /// <returns>null если не удалось создать</returns>
        private static object ConstructObject(string typeName)
        {
            if (!string.IsNullOrEmpty(typeName))
            {
                var type = Type.GetType(typeName, false);
                if (type != null)
                {
                    try
                    {
                        return Activator.CreateInstance(type);
                    }
                    catch
                    {
                        return null;
                    }
                }
            }

            return null;
        }

        /// <summary>
        /// Диспетчиризация события
        /// </summary>
        /// <param name="loggerEvent">Событие</param>
        public static void Dispatch(LoggerEvent loggerEvent)
        {
            foreach (var item in s_eventWriterTriplets)
            {
                var triplet = item;

                // если нужно отформатировать сообщение и есть параметры для форматирования
                if (!triplet.Raw && loggerEvent.Properties.ContainsKey(LoggerEvent.PARAMETERS_PROPERTY))
                {
                    loggerEvent[LoggerEvent.MESSAGE_PROPERTY] =
                        string.Format((string) loggerEvent[LoggerEvent.MESSAGE_PROPERTY],
                                      (object[]) loggerEvent[LoggerEvent.PARAMETERS_PROPERTY]);
                    loggerEvent.Properties.Remove(LoggerEvent.PARAMETERS_PROPERTY);
                }

                // сформируем сообщение
                var message = triplet.Formatter.Format(loggerEvent);

                try
                {
                    // проверка по фильтрам
                    var accepted = triplet.Filters.All(filter => filter.Accepted(triplet, loggerEvent, message));

                    if (!accepted)
                        continue;

                    // записываем сообщение
                    triplet.Writer.Write(GetUniqueId(loggerEvent), message);
                }
                catch (Exception ex)
                {
                    throw new Exception("Ошибка при записи сообщения:\n" + message, ex);
                }
            }
        }

        /// <summary>
        /// Получить уникальный идентификатор журнала по событию
        /// </summary>
        /// <param name="loggerEvent">Событие</param>
        /// <returns>Уникальный идентификатор журнала</returns>
        public static string GetUniqueId(LoggerEvent loggerEvent)
        {
            string uniqueId = null;

            // если указано поле для группировки, то ищем его в свойствах
            if (GroupByFieldDefined && loggerEvent.Properties.ContainsKey(GroupByField))
            {
                uniqueId = (string)loggerEvent[GroupByField];
            }

            // если не смогли найти, то группируем по уровням
            if (string.IsNullOrEmpty(uniqueId))
            {
                uniqueId = loggerEvent.EventType.ToString();
            }

            return uniqueId;
        }
    }
}
