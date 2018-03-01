using System.Threading;
using Yaw.Core.Utils.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Yaw.Tests.CoreTest
{
    /// <summary>
    ///This is a test class for WaitHandleExtensionsTest and is intended
    ///to contain all WaitHandleExtensionsTest Unit Tests
    ///</summary>
    [TestClass]
    public class WaitHandleUtilsTest
    {
        /// <summary>
        ///Gets or sets the test context which provides
        ///information about and functionality for the current test run.
        ///</summary>
        public TestContext TestContext{ get; set;}
        
        /// <summary>
        /// Проверяем, что отловим сигнал первого события
        /// </summary>
        [TestMethod]
        public void WaitOneOrAllOthersTest()
        {
            var oneEvent = new ManualResetEvent(false);
            var twoEvent = new ManualResetEvent(false);
            var threeEvent = new ManualResetEvent(false);

            ThreadUtils.StartBackgroundThread(
                () =>
                    {
                        Thread.Sleep(100);
                        oneEvent.Set();
                    });

            var index = WaitHandleUtils.WaitOneOrAllOthers(oneEvent, new WaitHandle[] {twoEvent, threeEvent});

            Assert.AreEqual(0, index);
        }

        /// <summary>
        /// Проверяем, что отловим сигналы других событий
        /// </summary>
        [TestMethod]
        public void WaitOneOrAllOthersTest2()
        {
            var oneEvent = new ManualResetEvent(false);
            var twoEvent = new ManualResetEvent(false);
            var threeEvent = new ManualResetEvent(false);

            ThreadUtils.StartBackgroundThread(
                () =>
                {
                    Thread.Sleep(100);
                    twoEvent.Set();
                    Thread.Sleep(100);
                    threeEvent.Set();
                });

            var index = WaitHandleUtils.WaitOneOrAllOthers(oneEvent, new WaitHandle[] { twoEvent, threeEvent });

            Assert.AreEqual(1, index);
        }

        /// <summary>
        /// Проверяем, что отловим сигналы первого события, т.к. не все из других событий сработают
        /// </summary>
        [TestMethod]
        public void WaitOneOrAllOthersTest3()
        {
            var oneEvent = new ManualResetEvent(false);
            var twoEvent = new ManualResetEvent(false);
            var threeEvent = new ManualResetEvent(false);

            ThreadUtils.StartBackgroundThread(
                () =>
                {
                    Thread.Sleep(100);
                    twoEvent.Set();
                    oneEvent.Set();
                    Thread.Sleep(100);
                    threeEvent.Set();
                });

            var index = WaitHandleUtils.WaitOneOrAllOthers(oneEvent, new WaitHandle[] { twoEvent, threeEvent });

            Assert.AreEqual(0, index);
        }
    }
}
