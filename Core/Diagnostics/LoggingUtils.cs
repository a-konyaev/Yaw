using System;
using System.Linq;
using System.Threading;
using Yaw.Core.Utils;
using Yaw.Core.Utils.Text;

namespace Yaw.Core.Diagnostics
{
	/// <summary>
	/// ����� ����������� ������� ��� �����������
	/// </summary>
	public static class LoggingUtils
	{
	    /// <summary>
        /// ������� ��������� � �������
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
		/// ��������� � ���������� ��������� �������� � ����������� � ������� ��������� ����������.
		/// </summary>
		/// <param name="properties">���������</param>
        public static void FillCommonContextProperies(EventProperties properties)
		{
            properties[LoggerEvent.TIMESTAMP_PROPERTY] = DateTime.Now;
            properties[LoggerEvent.THREAD_ID] = Thread.CurrentThread.ManagedThreadId;
		}

		/// <summary>
		/// ����������� ��������� ���������� � ���� ������.
		/// </summary>
		/// <param name="properties">��������� ����������.</param>
		/// <returns></returns>
        public static TextBuilder Format(EventProperties properties)
		{
			var textBuilder = new TextBuilder();
			Format(textBuilder, properties);
			return textBuilder;
		}

		/// <summary>
		/// ����������� ��������� ���������� � ���� ������.
		/// </summary>
		/// <param name="textBuilder"></param>
		/// <param name="properties">��������� ����������.</param>
        public static void Format(TextBuilder textBuilder, EventProperties properties)
		{
			if (properties.ContainsKey(String.Empty))
				textBuilder.Append("EventData");
			ObjectDumper.DumpObject(
				properties.OrderBy(pair => pair.Key),
				textBuilder);
		}

		/// <summary>
		/// ��������� �����������
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