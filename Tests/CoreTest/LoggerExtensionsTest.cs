using System.Diagnostics;
using Yaw.Core.Diagnostics.Default;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Yaw.Core.Diagnostics;
using Rhino.Mocks;
using System;

namespace Yaw.Tests.CoreTest
{
    /// <summary>
    ///This is a test class for LoggerExtensionsTest and is intended
    ///to contain all LoggerExtensionsTest Unit Tests
    ///</summary>
	[TestClass]
	public class LoggerExtensionsTest
	{
    	private static ILogger s_logger;

    	private static LoggerEvent s_lastLoggerEvent;

    	private const string MESSAGE = "{0} message";

    	/// <summary>
    	///Gets or sets the test context which provides
    	///information about and functionality for the current test run.
    	///</summary>
    	public TestContext TestContext { get; set; }

    	#region Инициализация теста

		[ClassInitialize]
		public static void MyClassInitialize(TestContext testContext)
		{
			var mock = new MockRepository();
			s_logger = mock.DynamicMock<ILogger>();

			Expect.Call(() => s_logger.Log(null)).IgnoreArguments().Do(new LogEventDelegate(LogEvent));
			Expect.Call(s_logger.IsAcceptedByEventType(null)).IgnoreArguments().Return(true);

			mock.ReplayAll();
		}

		/// <summary>
		/// Делегат метода логирования
		/// </summary>
		/// <param name="loggerEvent">событие диагностики</param>
    	private delegate void LogEventDelegate(LoggerEvent loggerEvent);

		/// <summary>
		/// Метод, который будет выполнятся при логировании
		/// </summary>
		/// <param name="loggerEvent">событие диагностики</param>
		private static void LogEvent(LoggerEvent loggerEvent)
		{
			s_lastLoggerEvent = loggerEvent;
		}

    	#endregion

		/// <summary>
		///A test for LogWarning
		///</summary>
		[TestMethod]
		public void LogWarningTest()
		{
			s_logger.LogWarning(MESSAGE, "Warning");

			var actual = s_lastLoggerEvent;

			Assert.AreEqual(actual.EventType, TraceEventType.Warning, "Несовпадение уровня трассировки сообщения");
			Assert.AreEqual(MESSAGE, actual.Properties[LoggerEvent.MESSAGE_PROPERTY], "Несовпадение текстов сообщения");
			
			var parameters = (object[]) actual.Properties[LoggerEvent.PARAMETERS_PROPERTY];
			Assert.AreEqual(1, parameters.Length, "Число параметров не равно 1");
			Assert.AreEqual("Warning", parameters[0], "Нет параметра");
		}

		/// <summary>
		///A test for LogVerbose
		///</summary>
		[TestMethod]
		public void LogVerboseTest()
		{
			s_logger.LogVerbose(MESSAGE, "Verbose");

			var actual = s_lastLoggerEvent;
			Assert.AreEqual(TraceEventType.Verbose, actual.EventType, "Несовпадение уровня трассировки сообщения");
			Assert.AreEqual(MESSAGE, actual.Properties[LoggerEvent.MESSAGE_PROPERTY], "Несовпадение текстов сообщения");

			var parameters = (object[])actual.Properties[LoggerEvent.PARAMETERS_PROPERTY];
			Assert.AreEqual(1, parameters.Length, "Число параметров не равно 1");
			Assert.AreEqual("Verbose", parameters[0], "Нет параметра");
		}

		/// <summary>
		///A test for LogInfo
		///</summary>
		[TestMethod]
		public void LogInfoTest()
		{
			s_logger.LogInfo(MESSAGE, "Info");

			var actual = s_lastLoggerEvent;
			Assert.AreEqual(TraceEventType.Information, actual.EventType, "Несовпадение уровня трассировки сообщения");
			Assert.AreEqual(MESSAGE, actual.Properties[LoggerEvent.MESSAGE_PROPERTY], "Несовпадение текстов сообщения");

			var parameters = (object[])actual.Properties[LoggerEvent.PARAMETERS_PROPERTY];
			Assert.AreEqual(1, parameters.Length, "Число параметров не равно 1");
			Assert.AreEqual("Info", parameters[0], "Нет параметра");
		}

		/// <summary>
		///A test for LogException
		///</summary>
		[TestMethod]
		public void LogExceptionTest()
		{
			var ex = new ArgumentException("Тестовое исключение");
			s_logger.LogException(MESSAGE, ex, new object[] { });

			var actual = s_lastLoggerEvent;
			Assert.AreEqual(TraceEventType.Error, actual.EventType, "Несовпадение уровня трассировки сообщения");
			Assert.AreEqual(MESSAGE, actual.Properties[LoggerEvent.MESSAGE_PROPERTY], "Несовпадение текстов сообщения");
			Assert.AreEqual(ex, actual.Properties[LoggerEvent.EXCEPTION_PROPERTY], "Несовпадение исключений");

			Assert.IsFalse(actual.Properties.ContainsKey(LoggerEvent.PARAMETERS_PROPERTY), "Не удалены пустые свойства");
		}

		/// <summary>
		///A test for LogError
		///</summary>
		[TestMethod]
		public void LogErrorTest()
		{
			s_logger.LogError(MESSAGE, "Error");

			var actual = s_lastLoggerEvent;
			Assert.AreEqual(TraceEventType.Error, actual.EventType, "Несовпадение уровня трассировки сообщения");
			Assert.AreEqual(MESSAGE, actual.Properties[LoggerEvent.MESSAGE_PROPERTY], "Несовпадение текстов сообщения");

			var parameters = (object[])actual.Properties[LoggerEvent.PARAMETERS_PROPERTY];
			Assert.AreEqual(1, parameters.Length, "Число параметров не равно 1");
			Assert.AreEqual("Error", parameters[0], "Нет параметра");
		}

		/// <summary>
		///A test for GetCallerMethodName
		///</summary>
		[TestMethod]
		public void GetCallerMethodNameTest()
		{
			var type = typeof(LoggerExtensions);
			string expected = "LoggerExtensionsTest.GetCallerMethodNameTest";
			
			var actual = LoggerExtensions.GetCallerMethodName(type);
			Assert.AreEqual(expected, actual);
		}
	}
}
