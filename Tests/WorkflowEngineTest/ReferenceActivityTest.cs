using Yaw.Tests.WorkflowEngineTest;
using Yaw.Workflow.ComponentModel;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Yaw.Tests.WorkflowEngineTest
{
    /// <summary>
    ///This is a test class for ReferenceActivityTest and is intended
    ///to contain all ReferenceActivityTest Unit Tests
    ///</summary>
    [TestClass]
    public class ReferenceActivityTest
    {
        /// <summary>
        ///Gets or sets the test context which provides
        ///information about and functionality for the current test run.
        ///</summary>
        public TestContext TestContext { get; set; }

        /// <summary>
        ///A test for ExecuteReferencedActivity
        ///</summary>
        [TestMethod]
        public void ExecuteReferencedActivityTest()
        {
            var callValidator = new ActivityMethodsCallValidator();
            var target = new ReferenceActivity
                             {
                                 Name = "ra",
                                 ActivityForExecute = new Activity
                                                          {
                                                              Name = "a",
                                                              ExecutionMethodCaller = new ActivityExecutionMethodCaller(
                                                                  ActivityMethodsCallValidator.EXECUTE_METHODNAME,
                                                                  callValidator)
                                                          }
                             };

            var res = target.Execute(new WorkflowExecutionContext(new WorkflowScheme()));

            Assert.AreEqual(1, callValidator.ExecuteCallCount);
            Assert.AreEqual(ActivityMethodsCallValidator.TestNextActivityKey, res);
        }
    }
}
