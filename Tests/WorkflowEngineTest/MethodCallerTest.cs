using System;
using Yaw.Tests.WorkflowEngineTest;
using Yaw.Workflow.ComponentModel;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Yaw.Tests.WorkflowEngineTest
{
    /// <summary>
    ///This is a test class for MethodCallerTest and is intended
    ///to contain all MethodCallerTest Unit Tests
    ///</summary>
    [TestClass]
    public class MethodCallerTest
    {
        /// <summary>
        ///Gets or sets the test context which provides
        ///information about and functionality for the current test run.
        ///</summary>
        public TestContext TestContext { get; set; }

        #region MethodCaller

        /// <summary>
        ///A test for MethodCaller Constructor
        ///</summary>
        [TestMethod]
        public void MethodCallerConstructorTest()
        {
            var callValidator = new ActivityMethodsCallValidator();
            var target = new MethodCaller(
                typeof (Action<WorkflowExecutionContext>),
                ActivityMethodsCallValidator.INITIALIZE_METHODNAME, callValidator);

            Assert.AreEqual(callValidator, target.MethodOwner);
            Assert.AreEqual(
                callValidator.GetType().GetMethod(ActivityMethodsCallValidator.INITIALIZE_METHODNAME), 
                target.Method);
        }


        /// <summary>
        ///A test for Call
        ///</summary>
        [TestMethod]
        public void CallTest()
        {
            var callValidator = new ActivityMethodsCallValidator();
            var target = new MethodCaller(
                typeof(Func<WorkflowExecutionContext, ActivityParameterDictionary, NextActivityKey>),
                ActivityMethodsCallValidator.EXECUTE_METHODNAME, callValidator);

            var parameters = new ActivityParameterDictionary();
            var res = (NextActivityKey) target.Call(new object[] {null, parameters});

            Assert.AreEqual(1, callValidator.ExecuteCallCount);
            Assert.AreEqual(parameters, callValidator.PassedParameters);
            Assert.AreEqual(res, ActivityMethodsCallValidator.TestNextActivityKey);
        }

        #endregion MethodCaller
    }
}
