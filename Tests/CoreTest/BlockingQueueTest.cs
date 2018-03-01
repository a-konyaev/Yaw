using System;
using Yaw.Core.Utils.Collections;
using Yaw.Core.Utils.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Threading;

namespace Yaw.Tests.CoreTest
{
    /// <summary>
    ///This is a test class for BlockingQueueTest and is intended
    ///to contain all BlockingQueueTest Unit Tests
    ///</summary>
	[TestClass]
	public class BlockingQueueTest
	{
    	/// <summary>
    	///Gets or sets the test context which provides
    	///information about and functionality for the current test run.
    	///</summary>
    	public TestContext TestContext { get; set; }

		private static BlockingQueue_Accessor<string> GetAccessor()
		{
            var queue = new BlockingQueue<string>();
            return new BlockingQueue_Accessor<string>(new PrivateObject(queue));
		}

		[TestMethod]
		public void OpenTest()
		{
			var accessor = GetAccessor();
			accessor._open = false;
			accessor.Open();

			Assert.IsTrue(accessor._open, "Очередь не открыта");
		}

		[TestMethod]
		[ExpectedException(typeof(ObjectDisposedException))]
		public void OpenDisposedTest()
		{
			var accessor = GetAccessor();
			accessor._disposed = true;
			accessor.Open();
		}

		[TestMethod]
		public void EnqueueTest()
		{
			var accessor = GetAccessor();
			accessor.Enqueue("s1");

			Assert.AreEqual("s1", accessor._queue.Peek(), "Элемент не помещен в очередь");
		}

		[TestMethod]
		[ExpectedException(typeof(InvalidOperationException), "Помещение объекта в закрытую очередь недопустимо")]
		public void EnqueueWhenCloseTest()
		{
			var accessor = GetAccessor();
			accessor._open = false;
			accessor.Enqueue("s1");
		}

		[TestMethod]
		public void DequeueTest()
		{
			var accessor = GetAccessor();
			// добавление произведем в отдельном потоке, чтобы убедится в работоспособности
			ThreadUtils.StartBackgroundThread(
                ()=>
                    {
                        Thread.Sleep(100);
                        accessor.Enqueue("s1");                        
                    });

			var actual = accessor.Dequeue(150);

			Assert.AreEqual("s1", actual);
		}

		[TestMethod]
		[ExpectedException(typeof(InvalidOperationException), "Timeout")]
		public void DequeueWhenEmptyTest()
		{
			GetAccessor().Dequeue(50);
		}

		[TestMethod]
		[DeploymentItem("Yaw.Core.dll")]
		public void SignalIfEmptyUnsafeTest()
		{
			var accessor = GetAccessor();
			accessor._eventEmpty.Reset();
			accessor.Enqueue("s1");
			accessor.SignalIfEmptyUnsafe();

			Assert.IsFalse(accessor.EmptiedWaitHandle.WaitOne(0), "Было вызвано событие Очередь пуста");
		}

		[TestMethod]
		[DeploymentItem("Yaw.Core.dll")]
		public void SignalIfEmptyUnsafeSignalTest()
		{
			var accessor = GetAccessor();
			accessor._eventEmpty.Reset();
			accessor.Clear();
			accessor.SignalIfEmptyUnsafe();

            Assert.IsTrue(accessor.EmptiedWaitHandle.WaitOne(0), "Не было вызвано событие Очередь пуста");
		}
	}
}
