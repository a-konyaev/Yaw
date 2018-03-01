using Yaw.Workflow.ComponentModel;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Yaw.Tests.WorkflowEngineTest
{
    /// <summary>
    ///This is a test class for ActivityTest and is intended
    ///to contain all ActivityTest Unit Tests
    ///</summary>
    [TestClass]
    public class ActivityTest
    {
        private const string TEST_STR = "Test";
        private readonly WorkflowExecutionContext _emptyContext = new WorkflowExecutionContext(new WorkflowScheme());

        /// <summary>
        ///Gets or sets the test context which provides
        ///information about and functionality for the current test run.
        ///</summary>
        public TestContext TestContext { get; set; }

        /// <summary>
        ///A test for Activity Constructor
        ///</summary>
        [TestMethod]
        public void ActivityConstructorTest()
        {
            var target = new Activity_Accessor();
            Assert.IsNotNull(target.Parameters);
            Assert.IsNotNull(target.NextActivities);
            Assert.IsTrue(target.Tracking);
            Assert.AreEqual(ActivityPriority.Default, target.Priority);
            Assert.AreEqual(false, target._initialized);
        }

        /// <summary>
        ///A test for InitializeMethodCaller
        ///</summary>
        [TestMethod]
        public void InitializeMethodCallerTest()
        {
            var target = new Activity {InitializeMethodCaller = null};
            Assert.IsNull(target.InitializeMethodCaller);
        }

        /// <summary>
        ///A test for UninitializeMethodCaller
        ///</summary>
        [TestMethod]
        public void UninitializeMethodCallerTest()
        {
            var target = new Activity {UninitializeMethodCaller = null};
            Assert.IsNull(target.UninitializeMethodCaller);
        }

        /// <summary>
        ///A test for Tracking
        ///</summary>
        [TestMethod]
        public void TrackingTest()
        {
            var target = new Activity {Tracking = false};
            Assert.IsFalse(target.Tracking);
        }

        /// <summary>
        ///A test for Root
        ///</summary>
        [TestMethod]
        public void RootIfParentIsNullTest()
        {
            var target = new Activity();
            Assert.IsNull(target.Root);
        }

        /// <summary>
        ///A test for Root
        ///</summary>
        [TestMethod]
        public void RootIfParentIsNotNullTest()
        {
            var parent0 = new Activity();
            var parent1 = new Activity {Parent = parent0};
            var target = new Activity {Parent = parent1};
            Assert.AreEqual(parent0, target.Root);
        }

        /// <summary>
        ///A test for Priority
        ///</summary>
        [TestMethod]
        public void PriorityTest()
        {
            var target = new Activity {Priority = ActivityPriority.Highest};
            Assert.AreEqual(ActivityPriority.Highest, target.Priority);
        }

        /// <summary>
        ///A test for Parent
        ///</summary>
        [TestMethod]
        public void ParentTest()
        {
            var parent = new Activity();
            var target = new Activity { Parent = parent };
            Assert.AreEqual(parent, target.Parent);
        }

        /// <summary>
        ///A test for Parameters
        ///</summary>
        [TestMethod]
        public void ParametersTest()
        {
            var target = new Activity_Accessor {Parameters = null};
            Assert.IsNull(target.Parameters);
        }

        /// <summary>
        ///A test for NextActivities
        ///</summary>
        [TestMethod]
        public void NextActivitiesTest()
        {
            var target = new Activity_Accessor {NextActivities = null};
            Assert.IsNull(target.NextActivities);
        }

        /// <summary>
        ///A test for Name
        ///</summary>
        [TestMethod]
        public void NameTest()
        {
            var target = new Activity { Name = TEST_STR };
            Assert.AreEqual(TEST_STR, target.Name);
        }

        /// <summary>
        ///A test for FollowingActivity
        ///</summary>
        [TestMethod]
        public void FollowingActivityTest()
        {
            var fa = new Activity();
            var target = new Activity { FollowingActivity = fa };
            Assert.AreEqual(fa, target.FollowingActivity);
        }

        /// <summary>
        ///A test for ExecutionMethodCaller
        ///</summary>
        [TestMethod]
        public void ExecutionMethodCallerTest()
        {
            var target = new Activity {ExecutionMethodCaller = null};
            Assert.IsNull(target.ExecutionMethodCaller);
        }

        /// <summary>
        ///A test for ToString
        ///</summary>
        [TestMethod]
        public void ToStringTest()
        {
            var target = new Activity { Name = TEST_STR };
            Assert.AreEqual(TEST_STR, target.ToString());
        }

        /// <summary>
        ///A test for Initialize
        ///</summary>
        [TestMethod]
        public void InitializeTest()
        {
            var callValidator = new ActivityMethodsCallValidator();
            var target = new Activity_Accessor
                             {
                                 InitializeMethodCaller = new ActivityUnInitializeMethodCaller_Accessor(
                                     ActivityMethodsCallValidator.INITIALIZE_METHODNAME, callValidator)
                             };

            target.Initialize(null);
            Assert.AreEqual(1, callValidator.InitializeCallCount);

            callValidator.Reset();
            target.InitializeMethodCaller = null;
            target.Initialize(null);
            Assert.AreEqual(0, callValidator.InitializeCallCount);
        }

        /// <summary>
        ///A test for Uninitialize
        ///</summary>
        [TestMethod]
        public void UninitializeTest()
        {
            var callValidator = new ActivityMethodsCallValidator();
            var target = new Activity_Accessor
                             {
                                 UninitializeMethodCaller = new ActivityUnInitializeMethodCaller_Accessor(
                                     ActivityMethodsCallValidator.UNINITIALIZE_METHODNAME, callValidator)
                             };

            target.Uninitialize(null);
            Assert.AreEqual(1, callValidator.UninitializeCallCount);

            callValidator.Reset();
            target.UninitializeMethodCaller = null;
            target.Uninitialize(null);
            Assert.AreEqual(0, callValidator.UninitializeCallCount);
        }

        /// <summary>
        ///A test for _Initialize
        ///</summary>
        [TestMethod]
        public void _InitializeTest()
        {
            var callValidator = new ActivityMethodsCallValidator();
            var target = new Activity_Accessor
                             {
                                 _initialized = false,
                                 InitializeMethodCaller = new ActivityUnInitializeMethodCaller_Accessor(
                                     ActivityMethodsCallValidator.INITIALIZE_METHODNAME, callValidator)
                             };
            
            target._Initialize(null);
            Assert.AreEqual(1, callValidator.InitializeCallCount);
            Assert.IsTrue(target._initialized);

            // повторный вызов инициализации не должен быть выполнен
            target._Initialize(null);
            Assert.AreEqual(1, callValidator.InitializeCallCount);
            Assert.IsTrue(target._initialized);
        }

        /// <summary>
        ///A test for _Uninitialize
        ///</summary>
        [TestMethod]
        public void _UninitializeTest()
        {
            var callValidator = new ActivityMethodsCallValidator();
            var target = new Activity_Accessor
                             {
                                 _initialized = true,
                                 UninitializeMethodCaller = new ActivityUnInitializeMethodCaller_Accessor(
                                     ActivityMethodsCallValidator.UNINITIALIZE_METHODNAME, callValidator)
                             };

            target._Uninitialize(null);
            Assert.AreEqual(1, callValidator.UninitializeCallCount);
            Assert.IsFalse(target._initialized);

            // повторный вызов деинициализации не должен быть выполнен
            target._Uninitialize(null);
            Assert.AreEqual(1, callValidator.UninitializeCallCount);
            Assert.IsFalse(target._initialized);
        }

        /// <summary>
        ///A test for Execute
        ///</summary>
        [TestMethod]
        public void ExecuteWithoutPassParametersTest()
        {
            var target = new Activity_Accessor {Name = TEST_STR};

            var callValidator = new ActivityMethodsCallValidator();
            target.ExecutionMethodCaller = new ActivityExecutionMethodCaller_Accessor(
                ActivityMethodsCallValidator.EXECUTE_METHODNAME, callValidator);

            var actual = target.Execute(_emptyContext);

            Assert.AreEqual(1, callValidator.ExecuteCallCount);
            Assert.AreEqual(target.Parameters, callValidator.PassedParameters);
            Assert.AreEqual(ActivityMethodsCallValidator.TestNextActivityKey, actual);
        }

        /// <summary>
        ///A test for Execute
        ///</summary>
        [TestMethod]
        public void ExecuteWithParametersTest()
        {
            var target = new Activity_Accessor {Name = TEST_STR};

            var callValidator = new ActivityMethodsCallValidator();
            target.ExecutionMethodCaller = new ActivityExecutionMethodCaller_Accessor(
                ActivityMethodsCallValidator.EXECUTE_METHODNAME, callValidator);

            target.Parameters.Add(new ActivityParameter("p1", 1));
            target.Parameters.Add(new ActivityParameter("p2", 2));

            var parameters = new ActivityParameterDictionary
                                 {
                                     new ActivityParameter("p1", 11),
                                     new ActivityParameter("p3", 3)
                                 };
            var actual = target.Execute(_emptyContext, parameters);

            Assert.AreEqual(1, callValidator.ExecuteCallCount);

            Assert.AreEqual(3, callValidator.PassedParameters.Count);
            Assert.AreEqual(parameters["p1"], callValidator.PassedParameters["p1"]);
            Assert.AreEqual(target.Parameters["p2"], callValidator.PassedParameters["p2"]);
            Assert.AreEqual(parameters["p3"], callValidator.PassedParameters["p3"]);
            
            Assert.AreEqual(ActivityMethodsCallValidator.TestNextActivityKey, actual);
        }

        /// <summary>
        ///A test for _Execute
        ///</summary>
        [TestMethod]
        public void _ExecuteTest()
        {
            var target = new Activity_Accessor { Name = TEST_STR };

            var callValidator = new ActivityMethodsCallValidator();
            target.InitializeMethodCaller = new ActivityUnInitializeMethodCaller_Accessor(
                ActivityMethodsCallValidator.INITIALIZE_METHODNAME, callValidator);
            target.UninitializeMethodCaller = new ActivityUnInitializeMethodCaller_Accessor(
                ActivityMethodsCallValidator.UNINITIALIZE_METHODNAME, callValidator);
            target.ExecutionMethodCaller = new ActivityExecutionMethodCaller_Accessor(
                ActivityMethodsCallValidator.EXECUTE_METHODNAME, callValidator);

            var actual = target.Execute(_emptyContext);

            Assert.AreEqual(1, callValidator.InitializeCallCount);
            Assert.AreEqual(1, callValidator.UninitializeCallCount);
            Assert.AreEqual(1, callValidator.ExecuteCallCount);
            Assert.AreEqual(ActivityMethodsCallValidator.TestNextActivityKey, actual);
        }
    }
}
