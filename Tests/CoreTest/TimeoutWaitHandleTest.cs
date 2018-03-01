using Yaw.Core.Utils.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Yaw.Tests.CoreTest
{
    /// <summary>
    ///This is a test class for TimeoutWaitHandleTest and is intended
    ///to contain all TimeoutWaitHandleTest Unit Tests
    ///</summary>
	[TestClass]
	public class TimeoutWaitHandleTest
	{
    	/// <summary>
    	///Gets or sets the test context which provides
    	///information about and functionality for the current test run.
    	///</summary>
    	public TestContext TestContext { get; set; }

		/// <summary>
		///A test for Reset
		///</summary>
		[TestMethod]
		public void ResetTest()
		{
			var target = new TimeoutWaitHandle(50);
			var accessor = new TimeoutWaitHandle_Accessor(new PrivateObject(target));
			target.Reset();

			Assert.IsNotNull(accessor._eventThread, "Не запущено ожидание");
		}

		/// <summary>
		///A test for Dispose
		///</summary>
		[TestMethod]
		[DeploymentItem("Yaw.Core.dll")]
		public void DisposeTest()
		{
			var target = new TimeoutWaitHandle(50);
			var accessor = new TimeoutWaitHandle_Accessor(new PrivateObject(target));

			accessor.Dispose(true);
			Assert.IsTrue(accessor._disposed, "Объект не разрушен!");
		}

		/// <summary>
		///A test for TimeoutWaitHandle Constructor
		///</summary>
		[TestMethod]
		public void TimeoutWaitHandleConstructorTest()
		{
			var target = new TimeoutWaitHandle(50);
			var accessor = new TimeoutWaitHandle_Accessor(new PrivateObject(target)); 
			
			Assert.AreEqual(50, accessor._timeout, "Неверно задано время ожидания");
		}

		/// <summary>
		///A test for WaitingForTimeout
		///</summary>
		[TestMethod]
		[DeploymentItem("Yaw.Core.dll")]
		public void WaitingForTimeoutTest()
		{
			var target = new TimeoutWaitHandle(100);

			target.Reset();
			var result = target.WaitOne(200);

			Assert.IsTrue(result, "Не было возбуждено событие");
		}
	}
}
