using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Yaw.Tests.CoreTest.Helpers;
using Yaw.Core;
using Yaw.Core.Extensions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Yaw.Core.Diagnostics;
using Yaw.Core.Utils.Collections;
using Yaw.Core.Configuration;

namespace Yaw.Tests.CoreTest
{
    /// <summary>
    ///This is a test class for CoreApplicationTest and is intended
    ///to contain all CoreApplicationTest Unit Tests
    ///</summary>
	[TestClass]
	public class CoreApplicationTest
	{
    	/// <summary>
		/// Экземпляр объекта CoreApplication
		/// </summary>
		private static CoreApplication s_coreApplication;

		// имя подсистемы
		private const string SUBSYSTEM_NAME = "mySubsystem";

    	/// <summary>
    	/// Набор значений(LogFileFolder, separateLog, TraceLevel) ожидаемых после инициализации ядра подсистем
    	/// </summary>
    	private static Dictionary<string, Triplet<string, bool, TraceLevel>> s_expectedTestSubsystemsProperties;

    	/// <summary>
    	///Gets or sets the test context which provides
    	///information about and functionality for the current test run.
    	///</summary>
    	public TestContext TestContext { get; set; }

    	#region Инициализация теста

		/// <summary>
		/// Создание ядра перед началом любого из тестов класса
		/// </summary>
		/// <param name="testContext"></param>
		[ClassInitialize]
		public static void MyClassInitialize(TestContext testContext)
		{
			s_coreApplication = new CoreApplication();
			FillExpectedValues();
		}

		#endregion

		/// <summary>
		/// Тест инициализации ядра из конфига.
		///</summary>
		[TestMethod]
		public void OT_01_CoreApplicationConstructorTest()
		{
			// проверка, что конфиг ядра не Null
			Assert.IsNotNull(s_coreApplication.Config, "Конфиг ядра после инициализации Null");
			// проверка имени конфига, ожидаемое значение "bpc"
			Assert.AreEqual("bpc", s_coreApplication.Name);
			// инициализирован логер ядра
			Assert.IsNotNull(s_coreApplication.Logger, "Не инициализирован Logger ядра");
			// сравнение TraceLevel
			Assert.AreEqual(TraceLevel.Verbose, s_coreApplication.TraceLevel, "Неверно задан TraceLevel ядра");
			// logFileFolder
			Assert.AreEqual(@"Log\CoreTest", s_coreApplication.LogFileFolder, "Неверно задан LogFileFolder ядра");
		}

		/// <summary>
		/// Тест метода GetSubsystemsы
		/// </summary>
		[TestMethod]
		public void OT_02_GetSubsystemsTest()
		{
			List<KeyValuePair<String, TestSubsystem>> testSubSystems = s_coreApplication.GetSubsystems<TestSubsystem>();
			// в конфиге 4 подсистемы этого типа
			Assert.AreEqual(4, testSubSystems.Count, "Подсистем типа TestSubsystem более или менее чем ожидалось");

			foreach (var subsystem in testSubSystems)
			{
				Triplet<String, bool, TraceLevel> subsystemExpectedProperties;
				bool subsystemExists = 
					s_expectedTestSubsystemsProperties.TryGetValue(subsystem.Key, out subsystemExpectedProperties);

				// проверим, что такая подсистема есть среди ожидаемых
				Assert.IsTrue(subsystemExists, String.Format("Неизвестная подсистема {0}", subsystem.Key));
				
				// проверка инициализации подсистемы
				TestMySubsystem(subsystem.Value, subsystemExpectedProperties);
			}
		}

		/// <summary>
		/// Тест метода GetSubsystem(string name)
		/// </summary>
		[TestMethod]
		public void OT_03_GetSubsystemByNameAndTypeTest()
		{
			var testSubsystem = s_coreApplication.GetSubsystem<TestSubsystem>(SUBSYSTEM_NAME);

			// проверка инициализации подсистемы
			TestMySubsystem(testSubsystem, s_expectedTestSubsystemsProperties[SUBSYSTEM_NAME]);
		}

		/// <summary>
		/// Тест метода GetSubsystemOrThrow ядра
		/// </summary>
		[TestMethod]
		[ExpectedException(typeof(InvalidOperationException), "Найдено более одной подсистемы типа TestSubsystem")]
		public void OT_04_GetSubsystemOrThrowFailTest()
		{
			s_coreApplication.GetSubsystemOrThrow<TestSubsystem>();
		}

		/// <summary>
		/// Тест метода GetSubsystemOrThrow ядра
		/// </summary>
		[TestMethod]
		public void OT_05_GetSubsystemOrThrowTest()
		{
			var testSubsystem = s_coreApplication.GetSubsystemOrThrow<OtherTestSubsystem>();

			TestMySubsystem(testSubsystem, s_expectedTestSubsystemsProperties["mySubsystem2"]);
		}

        /// <summary>
        /// Тест метода FindSubsystemImplementsInterface ядра
        /// </summary>
        [TestMethod]
        public void FindSubsystemImplementsInterfaceTest()
        {
            var res = s_coreApplication.FindSubsystemImplementsInterface<IOneTestInterface>();
            Assert.IsNotNull(res);
        }

        /// <summary>
        /// Тест метода FindSubsystemImplementsInterface ядра
        /// </summary>
        [TestMethod]
        public void FindSubsystemImplementsInterfaceFailTest()
        {
            var res = s_coreApplication.FindSubsystemImplementsInterface<ISecondTestInterface>();
            Assert.IsNull(res);
        }

        /// <summary>
        /// Тест метода FindSubsystemImplementsInterfaceOrThrow ядра
        /// </summary>
        [TestMethod]
        [ExpectedException(typeof(ArgumentException), 
            "Не возникло исключения при неудачной попытке найти подсистему с заданным интерфейсом")]
        public void FindSubsystemImplementsInterfaceOrThrowTest()
        {
            s_coreApplication.FindSubsystemImplementsInterfaceOrThrow<ISecondTestInterface>();
        }

		/// <summary>
		///A test for GetSubsystem(string name)
		///</summary>
		[TestMethod]
		public void OT_07_GetSubsystemByNameTest()
		{
			// подсистема - TestSubsystem
			var testSubsystem = s_coreApplication.GetSubsystem(SUBSYSTEM_NAME);
			
			// проверка инициализации подсистемы
			TestMySubsystem((TestSubsystem)testSubsystem, s_expectedTestSubsystemsProperties[SUBSYSTEM_NAME]);
		}

		/// <summary>
		///A test for GetSubsystem(string name), исключение если подсистема не реализует требуемый интерфейс
		///</summary>
		[TestMethod]
		[ExpectedException(typeof(ArgumentException),
			"Запрошенная подсистема 'myTestSubsystem' не реализует требуемый интерфейс OtherTestSubsystem")]
		public void OT_10_GetSubsystemByNameExceptionTest()
		{
			// подсистема - OtherTestSubsystem
			s_coreApplication.GetSubsystem<OtherTestSubsystem>(SUBSYSTEM_NAME);
		}

		/// <summary>
		/// Тест возвращаемой ошибки при запросе подсистемы от null
		/// </summary>
		[TestMethod]
		[ExpectedException(typeof(ArgumentException), "Нарушение контракта")]
		public void OT_08_GetSubsystemByNullNameFailTest()
		{
			s_coreApplication.GetSubsystem(null);
		}

		/// <summary>
		/// Тест возвращаемой ошибки при запросе подсистемы от пустой строки
		/// </summary>
		[TestMethod]
		[ExpectedException(typeof(ArgumentException), "Нарушение контракта")]
		public void OT_09_GetSubsystemByEmptyNameFailTest()
		{
			s_coreApplication.GetSubsystem("");
		}

		/// <summary>
		/// Проверка инициализации подсистемы TestSubsystem
		/// </summary>
		/// <param name="subsystem">Подсистема</param>]
		/// <param name="subsystemProperties">Ожидаемые значения свойств подсистемы</param>
		private void TestMySubsystem(TestSubsystem subsystem, Triplet<string, bool, TraceLevel> subsystemProperties)
		{
			// ожидаемое значение свойства TestValue подсистемы
			const string TEST_PROPERTY_VALUE = "MyTestValue";
			// ожидаемое значение свойства TraceLevel подсистемы
			var expectedTraceLevel = subsystemProperties.Third;
			// ожидаемое значение свойства SeparateLog подсистемы
			var expectedSeparateLog = subsystemProperties.Second;
			// ожидаемое значение свойства LogFileFolder подсистемы
			var expectedLogFileFolder = subsystemProperties.First;

			// проверим, что создана
			Assert.IsNotNull(subsystem, String.Format("Подсистема {0} не создана", subsystem.Name));

			// проверим, что случился Init у подсистемы, т.е. заполнилось ее свойство
			Assert.AreEqual(TEST_PROPERTY_VALUE, subsystem.TestValue,
								String.Format("Неверно заполнено свойство TestValue подсистемы {0}", subsystem.Name));

			// проверим основные свойства подсистемы TestSubsystem
			SubsystemPropertiesCheck(subsystem, expectedTraceLevel, expectedSeparateLog, expectedLogFileFolder);
		}

		/// <summary>
		/// Проверка подсистемы на правильность инициализации
		/// </summary>
		/// <param name="subsystem">Подсистема</param>
		/// <param name="traceLevel">Ожидаемый уровень трассировки</param>
		/// <param name="separateLog">Ожидаемый separateLog</param>
		/// <param name="logFileFolder">Ожидаемая папка для лога</param>
		private void SubsystemPropertiesCheck(
			ISubsystem subsystem,
			TraceLevel traceLevel,
			bool separateLog,
			string logFileFolder)
		{
			//Сравнение TraceLevel
			Assert.AreEqual(traceLevel, subsystem.TraceLevel,
								String.Format("Неверно задан TraceLevel подсистемы {0}", subsystem.Name));
			//Логер подсистемы SoundManager
			Assert.IsNotNull(subsystem.Logger, String.Format("Не создан Logger подсистемы {0}", subsystem.Name));
			//SeparateLog
			Assert.IsTrue(subsystem.SeparateLog == separateLog, String.Format("Неверное значение SeparateLog подсистемы {0}", subsystem.Name));
			//Имя папки лога
			Assert.AreEqual(logFileFolder, subsystem.LogFileFolder,
								String.Format("Неверно задан LogFileFolder подсистемы {0}", subsystem.Name));
		}

		/// <summary>
		/// Заполняет ожидаемые значения свойств подсистем
		/// </summary>
		private static void FillExpectedValues()
		{
			s_expectedTestSubsystemsProperties = new Dictionary<string, Triplet<string, bool, TraceLevel>>();
			// заполним значения первой подсистемы
			var firstSubsystemValues = 
				new Triplet<string, bool, TraceLevel>("Log\\CoreTest/MyLog", true, TraceLevel.Error);
			s_expectedTestSubsystemsProperties.Add("mySubsystem", firstSubsystemValues);
			
			// заполним значения второй подсистемы
			var secontSubsystemValues =
				new Triplet<string, bool, TraceLevel>("Log\\CoreTest", false, TraceLevel.Verbose);
			s_expectedTestSubsystemsProperties.Add("mySubsystem1", secontSubsystemValues);
			
			// заполним значения третей подсистемы
			var thirdSubsystemValues =
				new Triplet<string, bool, TraceLevel>("Log\\CoreTest", false, TraceLevel.Info);
			s_expectedTestSubsystemsProperties.Add("TestSubsystem", thirdSubsystemValues);
			
			// заполним значения четвертой подсистемы
			var fourthSubsystemValues =
				new Triplet<string, bool, TraceLevel>("Log\\CoreTest", false, TraceLevel.Error);
			s_expectedTestSubsystemsProperties.Add("mySubsystem2", fourthSubsystemValues);
		}

		/// <summary>
		///A test for ExitThread
		///</summary>
		[TestMethod]
		[DeploymentItem("Yaw.Core.dll")]
		public void OT_12_ExitThreadTest()
		{
		    // аксессор ядра
		    var accessor = new CoreApplication_Accessor(new PrivateObject(s_coreApplication));

		    accessor.ExitThread(ApplicationExitType.Stop);
		    
		    foreach (var subsystem in accessor._subsystems)
		    {
		        // проверим освобождение подсистем и их логгеров
		        var subsystemAccessor = new Subsystem_Accessor(new PrivateObject(subsystem.Value));
		        var logger = new Logger_Accessor(new PrivateObject(subsystem.Value.Logger));
		        var lQueue =
		            new BlockingQueue_Accessor<LoggerEvent>(new PrivateObject(logger._queue));

		        Assert.IsTrue(subsystemAccessor._disposed, "Подсистема " + subsystem.Value.Name + " не утилизирована");
		        Assert.IsTrue(lQueue._disposed, "Logger подсистемы " + subsystem.Value.Name + " не утилизирован");
		    }

		    // проверим освобождение логгера ядра
		    var coreLogger = new Logger_Accessor(new PrivateObject(s_coreApplication.Logger));
		    var coreLoggerQueue =
		        new BlockingQueue_Accessor<LoggerEvent>(new PrivateObject(coreLogger._queue));
		    Assert.IsTrue(coreLoggerQueue._disposed, "Logger ядра не утилизирован");
		}

        /// <summary>
		/// Тест метода GetTraceLevelByName с корректными данными
		///</summary>
		[TestMethod]
		[DeploymentItem("Yaw.Core.dll")]
		public void GetTraceLevelByNameTest()
		{
			const string ERROR_TRACE_LEVEL_NAME = "Error";
			const TraceLevel DEFAULT_TRACE_LEVEL = TraceLevel.Verbose;
			
			// получим уровень трассировки
			var actual = CoreApplication_Accessor.GetTraceLevelByName(ERROR_TRACE_LEVEL_NAME, DEFAULT_TRACE_LEVEL);

			Assert.AreEqual(TraceLevel.Error, actual, "Получен не корректный уровень трассировки");
		}

		/// <summary>
		/// Тест метода GetTraceLevelByName с передачей пустой строки
		///</summary>
		[TestMethod]
		[DeploymentItem("Yaw.Core.dll")]
		public void GetTraceLevelByNameTestNullString()
		{
			const TraceLevel DEFAULT_TRACE_LEVEL = TraceLevel.Verbose;

			// получим уровень трассировки
			var actual = CoreApplication_Accessor.GetTraceLevelByName(null, DEFAULT_TRACE_LEVEL);

			Assert.AreEqual(DEFAULT_TRACE_LEVEL, actual, "Уровень трассировки не совпадает с уровнем по умолчанию");
		}

		/// <summary>
		/// Тест метода GetTraceLevelByName с корректными данными
		///</summary>
		[TestMethod]
		[DeploymentItem("Yaw.Core.dll")]
		public void GetTraceLevelByNameTestException()
		{
			const string ERROR_TRACE_LEVEL_NAME = "BatTraceLevel";
			const TraceLevel DEFAULT_TRACE_LEVEL = TraceLevel.Verbose;

			// получим уровень трассировки
			try
			{
				CoreApplication_Accessor.GetTraceLevelByName(ERROR_TRACE_LEVEL_NAME, DEFAULT_TRACE_LEVEL);
				Assert.Fail("Не произошло ошибки при передачи некорректного уровня трассировки");
			}
			catch (Exception exception)
			{
				Assert.AreEqual(
					"Некорректно задан уровень трассировки: 'BatTraceLevel'",
					exception.Message,
					"Не совпадение текста исключения");
			}
		}

		/// <summary>
		///A test for ApplicationVersion
		///</summary>
		[TestMethod]
		public void ApplicationVersionTest()
		{
			var actual = s_coreApplication.ApplicationVersion;
			// найдем сборку Yaw.Core и возьмем ее версию
			var assemblies = AppDomain.CurrentDomain.GetAssemblies();
			var expected = assemblies.Single(a => !string.IsNullOrEmpty(a.FullName)
			                                      && a.FullName.StartsWith("Yaw.Core,")).GetName().Version;

			Assert.AreEqual(expected, actual, "Неверная версия приложения");
		}

		/// <summary>
		///A test for SubsystemConfigUpdated
		///</summary>
		[TestMethod]
		[DeploymentItem("Yaw.Core.dll")]
		public void OT_11_SubsystemConfigUpdatedTest()
		{
			// подпишемся на событие изменения ядра приложения
			s_coreApplication.ConfigUpdated += CoreConfigUpdated;
			
			// найдем нужную подсистему
			var subsystem = (TestSubsystem)s_coreApplication.Subsystems.Single(s => s.Name == SUBSYSTEM_NAME);
			subsystem.WasApplyNewConfigCall = false;
			var subsystemAcsessor = new Subsystem_Accessor(new PrivateObject(subsystem));
			// вызовем событие изменения конфигурации
			var e = new ConfigUpdatedEventArgs(SUBSYSTEM_NAME, "test1", 1, 2);
			subsystemAcsessor.RaiseConfigUpdatedEvent(e);

			// проверим данные
			Assert.IsTrue(subsystem.WasApplyNewConfigCall, "Не была вызвана переинициализация подсистемы");
			Assert.IsTrue(_wasCoreConfigUpdatedCall, "Не было вызвано событие изменения конфигурации у ядра приложения");
		}

		/// <summary>
		/// Был ли вызван метод CoreConfigUpdated
		/// </summary>
    	private bool _wasCoreConfigUpdatedCall;

		/// <summary>
		/// Объновление конфигурации ядра
		/// </summary>
		/// <param name="sender">подсистема</param>
		/// <param name="e">параметры события</param>
    	private void CoreConfigUpdated(object sender, ConfigUpdatedEventArgs e)
    	{
			_wasCoreConfigUpdatedCall = true;
    	}

		/// <summary>
		///A test for ApplyNewConfig
		///</summary>
		[TestMethod]
		public void OT_06_ApplyNewConfigTest()
		{
			ApplicationConfig newConfig = s_coreApplication.Config;
			var	actual = s_coreApplication.ApplyNewConfig(newConfig, true);

			Assert.AreEqual(true, actual, "Не выполнена переинициализация приложения");
		}
	}
}
