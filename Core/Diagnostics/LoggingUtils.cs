using System;
using System.Linq;
using System.Threading;
using Yaw.Core.Utils;
using Yaw.Core.Utils.Text;

namespace Yaw.Core.Diagnostics
{
	/// <summary>
	/// Класс утилитарных методов для логирования
	/// </summary>
	public static class LoggingUtils
	{
	    /// <summary>
        /// Выводит сообщение в консоль
        /// </summary>
        /// <param name="message"></param>
        /// <param name="args"></param>
        public static void LogToConsole(string message, params object[] args)
        {
            if (args != null && args.Length > 0)
                message = string.Format(message, args);

            Console.WriteLine(string.Format("[{0:yyyy.MM.dd HH:mm:ss.fff}][{1}] {2}",
                DateTime.Now, Thread.CurrentThread.ManagedThreadId, message));
            Console.Out.Flush();
        }

		/// <summary>
		/// Добавляет в переданную коллекцию элементы с информацией о текущем контексте выполнения.
		/// </summary>
		/// <param name="properties">Коллекция</param>
        public static void FillCommonContextProperies(EventProperties properties)
		{
            properties[LoggerEvent.TIMESTAMP_PROPERTY] = DateTime.Now;
            properties[LoggerEvent.THREAD_ID] = Thread.CurrentThread.ManagedThreadId;
		}

		/// <summary>
		/// Форматирует коллекцию параметров в виде текста.
		/// </summary>
		/// <param name="properties">Коллекция параметров.</param>
		/// <returns></returns>
        public static TextBuilder Format(EventProperties properties)
		{
			var textBuilder = new TextBuilder();
			Format(textBuilder, properties);
			return textBuilder;
		}

		/// <summary>
		/// Форматирует коллекцию параметров в виде текста.
		/// </summary>
		/// <param name="textBuilder"></param>
		/// <param name="properties">Коллекция параметров.</param>
        public static void Format(TextBuilder textBuilder, EventProperties properties)
		{
			if (properties.ContainsKey(String.Empty))
				textBuilder.Append("EventData");
			ObjectDumper.DumpObject(
				properties.OrderBy(pair => pair.Key),
				textBuilder);
		}

		/// <summary>
		/// Добавляет разделитель
		/// </summary>
		/// <param name="textBuilder"></param>
		public static void AddSeparator(TextBuilder textBuilder)
		{
			textBuilder
				.EmptyLine()
				.Line("-------------------------------------------------------------------------------")
				.EmptyLine();
		}
	}
}