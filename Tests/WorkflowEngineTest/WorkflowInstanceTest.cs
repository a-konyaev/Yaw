using System;
using System.Threading;
using Yaw.Workflow.ComponentModel;
using Yaw.Workflow.Runtime;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Yaw.Tests.WorkflowEngineTest
{
    /// <summary>
    ///This is a test class for WorkflowInstanceTest and is intended
    ///to contain all WorkflowInstanceTest Unit Tests
    ///</summary>
    [TestClass]
    public class WorkflowInstanceTest
    {
        /// <summary>
        ///Gets or sets the test context which provides
        ///information about and functionality for the current test run.
        ///</summary>
        public TestContext TestContext { get; set; }

        [TestMethod]
        public void ConstructorTest()
        {
            var context = new WorkflowExecutionContext(new WorkflowScheme());
            var id = Guid.NewGuid();
            var runtime = new WorkflowRuntime();
            var target = new WorkflowInstance_Accessor(id, runtime, context);

            Assert.AreEqual(id, target.InstanceId);
            Assert.AreEqual(runtime, target.Runtime);
            Assert.AreEqual(id, target.InstanceId);
        }

        [TestMethod]
        public void StopTest()
        {
            var context = new WorkflowExecutionContext(new WorkflowScheme{ExitActivity = new Activity{Name = "1"}});
            var contextAccessor = new WorkflowExecutionContext_Accessor(new PrivateObject(context));
            var target = new WorkflowInstance_Accessor(Guid.NewGuid(), new WorkflowRuntime(), context);

            target.Stop();
            Assert.IsTrue(contextAccessor._interruptExecutionEvent.WaitOne(0));
        }

        [TestMethod]
        public void StartTest()
        {
            var a = new Activity {Name = "a"};
            var aAccessor = new Activity_Accessor(new PrivateObject(a));
            var callValidator = new ActivityMethodsCallValidator();
            aAccessor.ExecutionMethodCaller = new ActivityExecutionMethodCaller_Accessor(
                ActivityMethodsCallValidator.EXECUTE_METHODNAME, callValidator);

            var scheme = new WorkflowScheme();
            var schemeAccessor = new WorkflowScheme_Accessor(new PrivateObject(scheme));
            schemeAccessor.Activities.Add(a);
            schemeAccessor.RootActivityName = a.Name;

            var target = new WorkflowInstance_Accessor(
                Guid.NewGuid(), new WorkflowRuntime(), new WorkflowExecutionContext(scheme));

            target.Start();
            Thread.Sleep(100);
            Assert.AreEqual(1, callValidator.ExecuteCallCount);
        }
    }
}
