using System;
using System.Collections.Generic;
using System.Threading;
using System.Xml;
using Yaw.Workflow.ComponentModel;
using Yaw.Workflow.Runtime;
using Yaw.Tests.WorkflowEngineTest.Activities;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Yaw.Tests.WorkflowEngineTest
{
    [TestClass]
    [DeploymentItem("Activities\\TestNextActivityKeys.xsd", "Activities")]
    public class WorkflowTest
    {
        /// <summary>
        /// Запускает выполнение потока работ с заданной схемой и ожидает завершение выполнения
        /// </summary>
        /// <param name="schemeUri"></param>
        private static WorkflowInstance ExecWorkflow(string schemeUri)
        {
            var runtime = new WorkflowRuntime();
            object result = null;
            string reason = null;
            var done = new AutoResetEvent(false);
            runtime.WorkflowCompleted += (s, e) => { result = e.Result; done.Set(); };
            runtime.WorkflowTerminated += (s, e) => { reason = e.Reason; done.Set(); };

            var wi = runtime.CreateWorkflow(
                Guid.NewGuid(),
                schemeUri,
                new[]
                    {
                        new KeyValuePair<string, XmlReader>(
                            "http://schemas.yaw.ru/Workflow/TestNextActivityKeys",
                            XmlReader.Create("./Activities/TestNextActivityKeys.xsd"))
                    });

            wi.Start();
            done.WaitOne();

            if (reason != null)
                Assert.Fail(reason);

            Assert.AreEqual(TestNextActivityKeys.Yes, result);

            return wi;
        }

        /// <summary>
        /// Тест создания экземпляра потока работ по элементарной схеме
        /// </summary>
        [TestMethod]
        [DeploymentItem("Activities\\CreateWorkflowTest.wf", "Activities")]
        public void CreateWorkflowTest()
        {
            var target = new WorkflowRuntime_Accessor();
            var workflowCreatedEventDone = false;
            target.add_WorkflowCreated((s, o) => workflowCreatedEventDone = true);

            var id = Guid.NewGuid();
            var workflowInstance = target.RestoreOrCreateWorkflow(
                id,
                "./Activities/CreateWorkflowTest.wf",
                new[]
                    {
                        new KeyValuePair<string, XmlReader>(
                            "http://schemas.yaw.ru/Workflow/TestNextActivityKeys",
                            XmlReader.Create("./Activities/TestNextActivityKeys.xsd"))
                    });

            Assert.IsTrue(target.IsStarted);
            Assert.AreEqual(1, target._instances.Count);
            Assert.AreEqual(workflowInstance, target._instances[id]);
            Assert.IsTrue(workflowCreatedEventDone);
            Assert.IsFalse(workflowInstance.ExecutionContext.Scheme.RootActivity.Tracking);
        }

        /// <summary>
        /// Тест инициализации и деинициализации
        /// </summary>
        [TestMethod]
        [DeploymentItem("Activities\\InitUninitTest.wf", "Activities")]
        public void InitUninitTest()
        {
            ExecWorkflow("./Activities/InitUninitTest.wf");
        }

        /// <summary>
        /// Тест установки приоритета
        /// </summary>
        [TestMethod]
        [DeploymentItem("Activities\\SetPriorityTest.wf", "Activities")]
        public void SetPriorityTest()
        {
            var wi = ExecWorkflow("./Activities/SetPriorityTest.wf");
            var activities = wi.ExecutionContext.Scheme.Activities;
            Assert.AreEqual(ActivityPriority.Default, activities["R.1"].Priority);
            Assert.AreEqual(new ActivityPriority(7), activities["R.2"].Priority);
        }

        /// <summary>
        /// Тест установки Tracking-а
        /// </summary>
        [TestMethod]
        [DeploymentItem("Activities\\SetTrackingTest.wf", "Activities")]
        public void SetTrackingTest()
        {
            var wi = ExecWorkflow("./Activities/SetTrackingTest.wf");
            var activities = wi.ExecutionContext.Scheme.Activities;
            Assert.IsTrue(activities["R.1"].Tracking);
            Assert.IsTrue(activities["R.2"].Tracking);
            Assert.IsFalse(activities["R.3"].Tracking);
        }

        /// <summary>
        /// Тест установки DefaultNextActivity
        /// </summary>
        [TestMethod]
        [DeploymentItem("Activities\\SetDefaultNextActivityTest.wf", "Activities")]
        public void SetDefaultNextActivityTest()
        {
            var wi = ExecWorkflow("./Activities/SetDefaultNextActivityTest.wf");
            var activities = wi.ExecutionContext.Scheme.Activities;
            Assert.AreEqual(activities["R.2"], activities["R.1"].NextActivities[NextActivityKey.DefaultNextActivityKey]);
            Assert.IsFalse(activities["R.2"].NextActivities.ContainsKey(NextActivityKey.DefaultNextActivityKey));
        }

        /// <summary>
        /// Тест установки NextActivities
        /// </summary>
        [TestMethod]
        [DeploymentItem("Activities\\SetNextActivitiesTest.wf", "Activities")]
        public void SetNextActivitiesTest()
        {
            var wi = ExecWorkflow("./Activities/SetNextActivitiesTest.wf");
            var activities = wi.ExecutionContext.Scheme.Activities;
            
            Assert.AreEqual(activities["R.2"], activities["R.1"].NextActivities[TestNextActivityKeys.Yes]);
            Assert.AreEqual(activities["R.3"], activities["R.1"].NextActivities[TestNextActivityKeys.No]);

            Assert.AreEqual(activities["R.3"], activities["R.2"].NextActivities[TestNextActivityKeys.Yes]);
            Assert.AreEqual(activities["R.4"], activities["R.2"].NextActivities[TestNextActivityKeys.No]);

            Assert.AreEqual(activities["R.4"], activities["R.3"].NextActivities[TestNextActivityKeys.Yes]);
            Assert.AreEqual(activities["R.1"], activities["R.3"].NextActivities[TestNextActivityKeys.No]);

            Assert.AreEqual(0, activities["R.4"].NextActivities.Count);

            Assert.AreEqual(activities["R.6"], activities["R.5"].NextActivities[TestNextActivityKeys.Yes]);

            Assert.IsTrue(activities["R.6"].NextActivities[TestNextActivityKeys.Yes] is ReturnActivity);
        }

        /// <summary>
        /// Тест установки параметров
        /// </summary>
        [TestMethod]
        [DeploymentItem("Activities\\SetParametersTest.wf", "Activities")]
        public void SetParametersTest()
        {
            ExecWorkflow("./Activities/SetParametersTest.wf");
        }

        /// <summary>
        /// Тест выполнения ReferenceActivity
        /// </summary>
        [TestMethod]
        [DeploymentItem("Activities\\ReferenceActivityTest.wf", "Activities")]
        public void ReferenceActivityTest()
        {
            ExecWorkflow("./Activities/ReferenceActivityTest.wf");
        }

        /// <summary>
        /// Тест обработки событий
        /// </summary>
        [TestMethod]
        [DeploymentItem("Activities\\EventHandlingTest.wf", "Activities")]
        public void EventHandlingTest()
        {
            ExecWorkflow("./Activities/EventHandlingTest.wf");
        }

        /// <summary>
        /// Тест обработки событий 2
        /// </summary>
        [TestMethod]
        [DeploymentItem("Activities\\EventHandlingTest2.wf", "Activities")]
        public void EventHandlingTest2()
        {
            ExecWorkflow("./Activities/EventHandlingTest2.wf");
        }

        /// <summary>
        /// Тест обработки событий 3
        /// </summary>
        [TestMethod]
        [DeploymentItem("Activities\\EventHandlingTest3.wf", "Activities")]
        public void EventHandlingTest3()
        {
            ExecWorkflow("./Activities/EventHandlingTest3.wf");
        }

        /// <summary>
        /// Тест обработки событий 4
        /// </summary>
        [TestMethod]
        [DeploymentItem("Activities\\EventHandlingTest4.wf", "Activities")]
        public void EventHandlingTest4()
        {
            ExecWorkflow("./Activities/EventHandlingTest4.wf");
        }

        /// <summary>
        /// Тест использования регионов
        /// </summary>
        [TestMethod]
        [DeploymentItem("Activities\\RegionTest.wf", "Activities")]
        public void RegionTest()
        {
            ExecWorkflow("./Activities/RegionTest.wf");
        }

        /// <summary>
        /// Тест подключения вложенных схем
        /// </summary>
        [TestMethod]
        [DeploymentItem("Activities\\IncludeTest.wf", "Activities")]
        [DeploymentItem("Activities\\IncludeTest_a.wf", "Activities")]
        [DeploymentItem("Activities\\IncludeTest_b.wf", "Activities")]
        [DeploymentItem("Activities\\IncludeTest_c.wf", "Activities")]
        public void IncludeTest()
        {
            ExecWorkflow("./Activities/IncludeTest.wf");
        }

        /// <summary>
        /// Тест биндинга параметров
        /// </summary>
        [TestMethod]
        [DeploymentItem("Activities\\ParametersBindingTest.wf", "Activities")]
        [DeploymentItem("Activities\\ParametersBindingTest_a.wf", "Activities")]
        public void ParametersBindingTest()
        {
            ExecWorkflow("./Activities/ParametersBindingTest.wf");
        }
    }
}