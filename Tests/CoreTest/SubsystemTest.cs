using System;
using System.Threading;
using Yaw.Core;
using Yaw.Core.Utils.Threading;
using Yaw.Tests.CoreTest.Helpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Yaw.Tests.CoreTest
{
    /// <summary>
    ///This is a test class for SubsystemTest and is intended
    ///to contain all SubsystemTest Unit Tests
    ///</summary>
	[TestClass]
	public class SubsystemTest
	{
		/// <summary>
		/// Тестируемая подсистема
		/// </summary>
    	private static OtherTestSubsystem s_subsystem;

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
			var coreApplication = new CoreApplication();

			s_subsystem = coreApplication.GetSubsystemOrThrow<OtherTestSubsystem>();
		}

		#endregion

		/// <summary>
		///A test for WaitOne
		///</summary>
		[TestMethod]
		[DeploymentItem("Yaw.Core.dll")]
		public void OT_01_WaitOneTest()
		{
			// аксессор подсистемы
			var target = new Subsystem_Accessor(new PrivateObject(s_subsystem));
			// событие, которого будем ждать 0.5 сек
			var ev = new TimeoutWaitHandle(500);
			IWaitController waitCtrl = null;

			ev.Reset();
			var actual = target.WaitOne(ev, waitCtrl);
			// если не дождались, то был вызван деструктор
			Assert.IsTrue(actual, "Не дождались события");
		}

		/// <summary>
		///A test for Sleep
		///</summary>
		[TestMethod]
		[DeploymentItem("Yaw.Core.dll")]
		public void OT_02_SleepTest()
		{
			// аксессор подсистемы
			var target = new Subsystem_Accessor(new PrivateObject(s_subsystem));
			IWaitController waitCtrl = null;

			var timeout = TimeSpan.FromMilliseconds(500);
			var actual = target.Sleep(timeout, waitCtrl);

			Assert.IsTrue(actual, "Было вызвано освобождение подсистемы");
		}

		/// <summary>
		///A test for Sleep, если во время сна произошло Dispose
		///</summary>
		[TestMethod]
		[DeploymentItem("Yaw.Core.dll")]
		public void OT_03_SleepWhileDisposeTest()
		{
			// аксессор подсистемы
			var target = new Subsystem_Accessor(new PrivateObject(s_subsystem));
			IWaitController waitCtrl = null;

			ThreadUtils.StartBackgroundThread(SetDispose);

			var timeout = TimeSpan.FromMilliseconds(600);
			var actual = target.Sleep(timeout, waitCtrl);

			Assert.IsFalse(actual, "Не было отловлено освобождение подсистемы");
		}

		/// <summary>
		/// Вызывает метод Диспозе через 0.5 сек
		/// </summary>
		private static void SetDispose()
		{
			Thread.Sleep(500);

			var acsessor = new Subsystem_Accessor(new PrivateObject(s_subsystem));
			acsessor._disposeEvent.Set();
		}
	}
}
