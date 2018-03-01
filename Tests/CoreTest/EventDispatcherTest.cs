using System.Collections.Generic;
using Yaw.Tests.CoreTest.Helpers;
using Yaw.Core.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
namespace Yaw.Tests.CoreTest
{
    /// <summary>
    ///This is a test class for EventDispatcherTest and is intended
    ///to contain all EventDispatcherTest Unit Tests
    ///</summary>
	[TestClass]
	public class EventDispatcherTest
	{
    	/// <summary>
    	///Gets or sets the test context which provides
    	///information about and functionality for the current test run.
    	///</summary>
    	public TestContext TestContext { get; set; }

		/// <summary>
		///A test for ConstructObject, результат null
		///</summary>
		[TestMethod]
		[DeploymentItem("Yaw.Core.dll")]
		public void ConstructObjectWithNoConstructorTest()
		{
			const string TYPE_NAME = "System.String";
			// должна выпасть ошибка "No parameterless constructor defined for this object"
			// и вернуться null
			var actual = EventDispatcher_Accessor.ConstructObject(TYPE_NAME);

			Assert.IsNull(actual);
		}

		/// <summary>
		///A test for ConstructObject, результат null
		///</summary>
		[TestMethod]
		[DeploymentItem("Yaw.Core.dll")]
		public void ConstructNullObjectTest()
		{
			var actual = EventDispatcher_Accessor.ConstructObject(null);

			Assert.IsNull(actual, "Создан объект из null");
		}


		/// <summary>
		///A test for ConstructObject, результат null
		///</summary>
		[TestMethod]
		[DeploymentItem("Yaw.Core.dll")]
		public void ConstructObjectTest()
		{
			const string TYPE_NAME = "System.IO.MemoryStream";
			var actual = EventDispatcher_Accessor.ConstructObject(TYPE_NAME);

			Assert.AreEqual(typeof(System.IO.MemoryStream), actual.GetType());
		}

		/// <summary>
		///A test for Dispatch
		///</summary>
		[TestMethod]
		public void DispatchTest()
		{
			// создадим свою тройку для записи
			const string RESULT_MESSAGE = "param1=value1, param2=value2";
			var ev = new EventWriterTriplet
			                        	{
			                        		Raw = false,
			                        		Formatter = new Yaw.Core.Diagnostics.Default.EventFormatter(),
			                        		Writer = new TestFileSystemWriter()
			                        	};
			EventDispatcher_Accessor.s_eventWriterTriplets.Add(ev);
			
			// создадим событие логера
			var loggerEvent = new LoggerEvent();
			loggerEvent.Properties.Add(LoggerEvent.PARAMETERS_PROPERTY, new[] {"value1", "value2"});
			loggerEvent.Properties.Add(LoggerEvent.MESSAGE_PROPERTY, "param1={0}, param2={1}");

			EventDispatcher.Dispatch(loggerEvent);

			// проверим, что параметры удалены из события
			Assert.IsFalse(loggerEvent.Properties.ContainsKey(LoggerEvent.PARAMETERS_PROPERTY),
							"Параметры не удалены из события логера");
			// сформировано верное сообщение
			Assert.AreEqual(RESULT_MESSAGE,
			                loggerEvent.Properties[LoggerEvent.MESSAGE_PROPERTY],
			                "Неверный текст сообщения");
			// сообщение записано в лог
			Assert.IsTrue(((TestFileSystemWriter) ev.Writer).Lines.ToString().Contains(RESULT_MESSAGE),
			                "Неверный текст сообщения");
		}
	}
}
