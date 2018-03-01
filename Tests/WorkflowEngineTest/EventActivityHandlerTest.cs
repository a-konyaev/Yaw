using System;
using Yaw.Workflow.ComponentModel;
using Yaw.Workflow.Runtime;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Yaw.Tests.WorkflowEngineTest
{
    /// <summary>
    ///This is a test class for EventActivityHandlerTest and is intended
    ///to contain all EventActivityHandlerTest Unit Tests
    ///</summary>
    [TestClass]
    public class EventActivityHandlerTest
    {
        /// <summary>
        ///Gets or sets the test context which provides
        ///information about and functionality for the current test run.
        ///</summary>
        public TestContext TestContext { get; set; }

        private readonly WorkflowInstance _workflowInstance = new WorkflowInstance(
                Guid.NewGuid(), new WorkflowRuntime(), new WorkflowExecutionContext(new WorkflowScheme()));

        /// <summary>
        ///A test for EventActivityHandler Constructor
        ///</summary>
        [TestMethod]
        public void EventActivityHandlerConstructorTest()
        {
            var target = new EventActivityHandler_Accessor(_workflowInstance);
            Assert.IsNotNull(target._workflowInstance);
            Assert.IsNotNull(target.Method);
        }

        /// <summary>
        ///A test for ContainsActivities
        ///</summary>
        [TestMethod]
        public void ContainsActivitiesTest()
        {
            var target = new EventActivityHandler_Accessor(_workflowInstance);
            Assert.IsFalse(target.ContainsActivities);
            target.SyncActivity = new Activity();
            Assert.IsTrue(target.ContainsActivities);
        }

        /// <summary>
        ///A test for SetWorkflowInstance
        ///</summary>
        [TestMethod]
        public void SetWorkflowInstanceTest()
        {
            var target = new EventActivityHandler_Accessor(_workflowInstance);
            var tmpWi = new WorkflowInstance(
                Guid.NewGuid(), new WorkflowRuntime(), new WorkflowExecutionContext(new WorkflowScheme()));
            target.SetWorkflowInstance(tmpWi);
            Assert.AreEqual(tmpWi, target._workflowInstance);
        }

        /// <summary>
        ///A test for AddActivity
        ///</summary>
        [TestMethod]
        public void AddRemoveSyncActivityTest()
        {
            var target = new EventActivityHandler_Accessor(_workflowInstance);
            var a = new Activity();
            target.AddActivity(a, EventHandlingType.Sync);
            Assert.AreEqual(a, target.SyncActivity);

            target.RemoveActivity(a);
            Assert.IsNull(target.SyncActivity);
        }
    }
}
