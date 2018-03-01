using System.IO;
using Yaw.Tests.CoreTest.Helpers;
using Yaw.Core;
using Yaw.Core.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Yaw.Tests.CoreTest
{
    /// <summary>
    ///This is a test class for LoggerTest and is intended
    ///to contain all LoggerTest Unit Tests
    ///</summary>
	[TestClass]
	public class LoggerTest
	{
    	/// <summary>
    	///Gets or sets the test context which provides
    	///information about and functionality for the current test run.
    	///</summary>
    	public TestContext TestContext { get; set; }

    	/// <summary>
		///A test for Log
		///</summary>
		[TestMethod]
		public void LogTest()
		{
			var core = new CoreApplication();
			// создаем 1-е тестовое событие
			var logEvent = CreateLoggerEvent("Тестовое событие 1", "LogTest", System.Diagnostics.TraceEventType.Error);
			// путь к папке с логами
			var logFolder = core.LogFileFolder;
			// удалим старые логи, если они есть
			if (Directory.Exists(logFolder))
			{
				Directory.Delete(logFolder, true);
			}
			// всегда создаем директорию логов, чтобы FileWatcher не падал при ожидании появления в ней файлов
			Directory.CreateDirectory(core.LogFileFolder);

			core.Logger.Log(logEvent);
			WaitForFileCreatedOrChanged(logFolder);

			var logs = Directory.GetFiles(logFolder, "*.log");
			// проверка, что проставлено имя логгера
			Assert.AreEqual("bpc", logEvent["Logger"], "Имя логгера не совпадает с ожидаемым");
			// проверим, что файл есть
			Assert.IsFalse(logs.Length == 0, "Файл лога не создан");
			// проверим, что он один
			Assert.AreEqual(1, logs.Length, "Создано более 1-го лога");
			// получим размер файла
			int fileSize = GetLogFileSize(logs[0]);
			// если размер файла 0, то по идее сообщение еще не записано туда, подождем еще
			if (fileSize == 0)
			{ 
				WaitForFileCreatedOrChanged(logFolder);
				fileSize = GetLogFileSize(logs[0]);
			}
			
			// создадим второе сообщение
			var logInfoEvent =
				CreateLoggerEvent("Тестовое событие 2", "LogTest", System.Diagnostics.TraceEventType.Information);

			core.Logger.Log(logInfoEvent);
			WaitForFileCreatedOrChanged(logFolder);
	
			// получим размер фала после сообщения
			int newFileSize = GetLogFileSize(logs[0]);
			// проверим, что размер файла не изменился
			Assert.AreEqual(fileSize, newFileSize, "Сообщение не отфильтровано, размер файла лога изменился");
		}

		/// <summary>
		///A test for Logger Constructor
		///</summary>
		[TestMethod]
		[DeploymentItem("Yaw.Core.dll")]
		public void LoggerConstructorTest()
		{
			var core = new CoreApplication();
			const string LOGGER_NAME = "myLogger";
			// создадим новый логер
			var logger = new Logger_Accessor(
				new PrivateObject(core.CreateLogger(LOGGER_NAME, System.Diagnostics.TraceLevel.Error)));
			
			// правильность имени
			Assert.AreEqual(LOGGER_NAME, logger.LoggerName, "Неверно задано имя Логгера");
			// асинхронность логера
			Assert.AreEqual(true, logger.IsAsync, "Создаваемый логер должен быть асинхронным");
			// колличество фильтров
			Assert.AreEqual(1, logger._filters.Count, "У логера должен быть 1 фильтр");
			// очередь и тред не определены
			Assert.IsNotNull(logger._queue, "У логера должна быть очередь");
		}

		/// <summary>
		/// Создает событие логгера с переданными параметрами
		/// </summary>
		/// <param name="message">текст сообщения</param>
		/// <param name="methodName">имя метода</param>
		/// <param name="eventType">тип события</param>
		/// <returns>Событие логгера</returns>
		private LoggerEvent CreateLoggerEvent(string message, string methodName, System.Diagnostics.TraceEventType eventType)
		{
			var logEvent = new LoggerEvent();
			logEvent.Properties.Add("Message", message);
			logEvent.Properties.Add("MethodName", methodName);
			logEvent.EventType = eventType;

			return logEvent;
		}

		/// <summary>
		/// Получает размер файла
		/// </summary>
		/// <param name="logFilePath">Путь к файлу лога</param>
		/// <returns>Размер в байтах</returns>
		private int GetLogFileSize(string logFilePath)
		{
			var f = new FileInfo(logFilePath);
			return (int)f.Length;
		}

		/// <summary>
		/// Ждет не более 3 сек пока изменится файл лога, если изменения произошли ранее информирует об этом
		/// </summary>
		private static void WaitForFileCreatedOrChanged(string logDirPath)
		{
			var fsw = new FileSystemWatcher(logDirPath, "*.log")
			          	{
			          		NotifyFilter = NotifyFilters.FileName | NotifyFilters.Size
			          	};
			fsw.WaitForChanged(WatcherChangeTypes.Created | WatcherChangeTypes.Changed, 3000);
		}

		/// <summary>
		///A test for AddFilter
		///</summary>
		[TestMethod]
		[DeploymentItem("Yaw.Core.dll")]
		public void AddFilterTest()
		{
			var core = new CoreApplication();
			// создадим новый логер
			var logger = new Logger_Accessor(
				new PrivateObject(core.CreateLogger("myLogger", System.Diagnostics.TraceLevel.Error)));
			logger._filters.Clear();

			IEventFilter filter = new TestEventFilter();
			logger.AddFilter(filter);

			Assert.AreEqual(1, logger._filters.Count, "Добавлено более 1-го фильтра");
			Assert.IsTrue(logger._filters.Contains(filter), "Не добавлен требуемый фильтр");
		}
	}
}
