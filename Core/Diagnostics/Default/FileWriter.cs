using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Text;
using Yaw.Core.Utils.IO;

namespace Yaw.Core.Diagnostics.Default
{
    /// <summary>
    /// Протоколирование в файл
    /// </summary>
    public class FileWriter : IEventFileSystemWriter
    {
        /// <summary>
        /// Корневой каталог протоколов
        /// </summary>
        private static string s_rootFolder;
        /// <summary>
        /// Словарь точек записи: [идентификатор -> точка записи]
        /// </summary>
        private static readonly Dictionary<string, string> s_writerPoints = new Dictionary<string, string>();
        /// <summary>
        /// Словарь потоков записи: [точка записи -> поток записи]
        /// </summary>
        private static readonly Dictionary<string, StreamWriter> s_logWriters = new Dictionary<string, StreamWriter>();
        /// <summary>
        /// Объект для синхронизации доступа к словарю потоков записи
        /// </summary>
        private static readonly object s_logWritersSync = new object();
        /// <summary>
        /// Текущая дата
        /// </summary>
        private static DateTime s_currentDate;

        /// <summary>
        /// Инициализация
        /// </summary>
        /// <param name="rootFolder"></param>
        public static void Init(string rootFolder)
        {
            s_rootFolder = rootFolder;
            s_currentDate = DateTime.Today;
        }

        public void Init(NameValueConfigurationCollection props)
        {
            // ничего не делаем
        }

		/// <summary>
		/// Закрытие всех писателей в файлы
		/// </summary>
		public static void Close()
		{
            lock (s_logWritersSync)
            {
                foreach (var writer in s_logWriters.Values)
                {
                    writer.Flush();
                    writer.Close();
                }

                s_logWriters.Clear();
            }
		}

        /// <summary>
        /// Получает точку файловой системы, в которую пишется протокол
        /// </summary>
        /// <param name="uniqueId">Уникальный идентификатор</param>
        /// <returns>Полный путь</returns>
        public static string GetWriterPoint(string uniqueId)
        {
            if (!s_writerPoints.ContainsKey(uniqueId))
            {
                var sb = new StringBuilder(64);

                var subsystem = CoreApplication.Instance.GetSubsystem(uniqueId);
                // если нашли подсистему
                if (subsystem != null)
                {
                    sb.Append(!string.IsNullOrEmpty(subsystem.LogFileFolder)
                                  ? subsystem.LogFileFolder
                                  : s_rootFolder);
                    sb.Append('/');
                    sb.Append(subsystem.SeparateLog
                                  ? subsystem.Name
                                  : CoreApplication.Instance.Name);
                }
                    // если такой подсистемы найти не удалось
                else
                {
                    // то протоколируем в журнал с переданным именем
                    sb.Append(s_rootFolder);
                    sb.Append('/');
                    sb.Append(uniqueId);
                }

                s_writerPoints[uniqueId] = sb.ToString();
            }

            return s_writerPoints[uniqueId];
        }

        /// <summary>
        /// Получает точку файловой системы, в которую пишется протокол
        /// </summary>
        /// <param name="uniqueId">Уникальный идентификатор</param>
        /// <returns>Полный путь</returns>
        public string GetPoint(string uniqueId)
        {
            return GetWriterPoint(uniqueId);
        }

        /// <summary>
        /// Объект для синхронизации записи
        /// </summary>
        private readonly object _writeSync = new object();

        /// <summary>
        /// Записать сообщение
        /// </summary>
        /// <param name="uniqueLogId"></param>
        /// <param name="msg"></param>
        public void Write(string uniqueLogId, string msg)
        {
            lock (_writeSync)
            {
                var writer = GetLogWriter(uniqueLogId);

                if (writer != null)
                    writer.WriteLine(msg);
            }
        }

        /// <summary>
        /// Функция создания журнала события и источника события
        /// </summary>
        /// <param name="uniqueId">Идентификатор</param>
        /// <returns>Ссылка на созданный журнал</returns>
        private static StreamWriter GetLogWriter(string uniqueId)
        {
            // получим точку записи
            var writerPoint = GetWriterPoint(uniqueId);

            lock (s_logWritersSync)
            {
                // Попробуем получить поток записи в лог из хештаблицы
                var writer = s_logWriters.ContainsKey(writerPoint)
                                 ? s_logWriters[writerPoint]
                                 : null;

                // если изменилась текущая дата, то создаем новый файл, соответствующий этой дате
                // (также это происходит при первой записи в файл)
                var today = DateTime.Today;

                if (s_currentDate != today && writer != null)
                {
                    s_currentDate = today;
                    writer.Flush();
                    writer.Close();
                    writer = null;
                }

                // если поток записи не определен
                if (writer == null)
                {
                    // то откроем новый

                    // убедимся, что директория, в которой будем создавать файлы, существует
                    FileUtils.EnsureDirExists(Path.GetDirectoryName(writerPoint));

                    // сформируем уникальное имя файла
                    var fileName = FileUtils.CreateUniqueFileWithDateMark(
                        Path.GetDirectoryName(writerPoint),
                        Path.GetFileName(writerPoint),
                        "log",
                        6);

                    // создаем поток записи
                    writer = new StreamWriter(fileName, Encoding.GetEncoding(1251))
                    {
                        AutoFlush = true
                    };

                    // добавим в таблицу созданный поток записи
                    s_logWriters[writerPoint] = writer;
                }

                return writer;
            }
        }

    }
}
