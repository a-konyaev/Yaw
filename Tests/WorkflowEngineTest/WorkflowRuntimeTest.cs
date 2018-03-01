using System;
using System.Collections.Generic;
using System.Xml;
using Yaw.Workflow.ComponentModel;
using Yaw.Workflow.Runtime;
using Yaw.Workflow.Runtime.Hosting;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Yaw.Tests.WorkflowEngineTest
{
    /// <summary>
    ///This is a test class for WorkflowRuntimeTest and is intended
    ///to contain all WorkflowRuntimeTest Unit Tests
    ///</summary>
    [TestClass]
    public class WorkflowRuntimeTest
    {
        /// <summary>
        ///Gets or sets the test context which provides
        ///information about and functionality for the current test run.
        ///</summary>
        public TestContext TestContext { get; set; }

        [TestMethod]
        public void ConstructorTest()
        {
            var target = new WorkflowRuntime_Accessor();
            Assert.IsNotNull(target._services);
            Assert.IsNotNull(target._instances);
            Assert.IsFalse(target.IsStarted);
        }

        #region Сервисы

        private class TestService : WorkflowRuntimeService { }
        
        private class TestWslService1 : WorkflowSchemeLoaderService
        {
            protected internal override WorkflowScheme CreateInstance(string workflowSchemeUri, IEnumerable<KeyValuePair<string, XmlReader>> customXmlSchemas)
            {
                return new WorkflowScheme();
            }
        }

        private class TestWslService2 : TestWslService1 { }

        private class TestWpService1 : WorkflowPersistenceService
        {
            public override WorkflowExecutionContext LoadWorkflowInstanceState(Guid instanceId)
            {
                return null;
            }

            public override void SaveWorkflowInstanceState(WorkflowExecutionContext context)
            {
            }
        }

        private class TestWpService2 : TestWpService1 { }

        [TestMethod]
        public void AddServiceTest()
        {
            var target = new WorkflowRuntime_Accessor();
            var service = new TestService();
            target.AddService(service);
            target.AddService(new TestWslService1());
            target.AddService(new TestWpService1());

            Assert.AreEqual(3, target._services.Count);
            Assert.AreEqual(target, service.Runtime);
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidOperationException))]
        public void AddServiceTest2()
        {
            var target = new WorkflowRuntime_Accessor();
            var service = new TestService();
            target.AddService(service);
            target.AddService(service);
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidOperationException))]
        public void AddServiceTest3()
        {
            var target = new WorkflowRuntime_Accessor();
            target.AddService(new TestWslService1());
            target.AddService(new TestWslService2());
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidOperationException))]
        public void AddServiceTest4()
        {
            var target = new WorkflowRuntime_Accessor();
            target.AddService(new TestWpService1());
            target.AddService(new TestWpService2());
        }

        [TestMethod]
        public void GetAllServicesTest()
        {
            var target = new WorkflowRuntime_Accessor();
            target.AddService(new TestService());
            var s = new TestWslService1();
            target.AddService(s);

            var res = target.GetAllServices<WorkflowSchemeLoaderService>();
            Assert.AreEqual(1, res.Count);
            Assert.AreEqual(s, res[0]);

            var res2 = target.GetAllServices<WorkflowRuntimeService>();
            Assert.AreEqual(2, res2.Count);
        }

        [TestMethod]
        public void GetServiceTest()
        {
            var target = new WorkflowRuntime_Accessor();
            target.AddService(new TestService());
            var s = new TestWslService1();
            target.AddService(s);

            var res = target.GetService<WorkflowSchemeLoaderService>();
            Assert.AreEqual(s, res);
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidOperationException))]
        public void GetServiceTest2()
        {
            var target = new WorkflowRuntime_Accessor();
            target.AddService(new TestService());
            target.AddService(new TestWslService1());

            target.GetService<WorkflowRuntimeService>();
        }

        #endregion

        #region Создание экземпляра потока работ

        [TestMethod]
        public void StartRuntimeTest()
        {
            var target = new WorkflowRuntime_Accessor();
            target.StartRuntime();
            Assert.AreEqual(1, target._services.Count);
            Assert.IsTrue(target._services[0] is DefaultWorkflowSchemeLoaderService);
            Assert.IsTrue(target.IsStarted);
        }

        [TestMethod]
        public void StopRuntimeTest()
        {
            var runtime = new WorkflowRuntime();
            var target = new WorkflowRuntime_Accessor(new PrivateObject(runtime));
            WorkflowRuntime_Accessor.s_runtimes.Add(runtime);
            
            target.StopRuntime();

            Assert.IsFalse(target.IsStarted);
        }

        #endregion
    }
}
