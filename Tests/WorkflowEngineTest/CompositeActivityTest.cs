using Yaw.Tests.WorkflowEngineTest;
using Yaw.Workflow.ComponentModel;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Yaw.Core.Utils.Collections;
using Rhino.Mocks;

namespace Yaw.Tests.WorkflowEngineTest
{
    /// <summary>
    ///This is a test class for CompositeActivityTest and is intended
    ///to contain all CompositeActivityTest Unit Tests
    ///</summary>
    [TestClass]
    public class CompositeActivityTest
    {
        /// <summary>
        ///Gets or sets the test context which provides
        ///information about and functionality for the current test run.
        ///</summary>
        public TestContext TestContext { get; set; }

        /// <summary>
        ///A test for CompositeActivity Constructor
        ///</summary>
        [TestMethod]
        public void CompositeActivityConstructorTest()
        {
            var target = new CompositeActivity();
            Assert.IsNotNull(target.Activities);
            Assert.IsNotNull(target.ExecutionMethodCaller);
        }

        /// <summary>
        ///A test for GetChildActivity
        ///</summary>
        [TestMethod]
        public void GetChildActivityTest()
        {
            var target = new CompositeActivity {Name = "a1"};
            var child = new Activity { Name = "a2" };
            target.Activities.Add("a1.a2", child);
            Assert.AreEqual(child, target.GetChildActivity<Activity>("a2"));
        }

        /// <summary>
        ///A test for InitProperties
        ///</summary>
        [TestMethod]
        public void InitPropertiesTest()
        {
            var target = new CompositeActivity_Accessor();
            target.InitProperties(null, 
                new ActivityParameterDictionary { new ActivityParameter("StartActivity", "123") });
            Assert.AreEqual("123", target.StartActivity);
        }

        /// <summary>
        ///A test for GetStartActivity
        ///</summary>
        [TestMethod]
        public void GetStartActivityTest()
        {
            // NOTE: не проверяет случай, когда context.Restoring = true
            var context = new WorkflowExecutionContext(new WorkflowScheme());
            var target = new CompositeActivity_Accessor {Name = "c"};
            var child1 = new Activity {Name = "a1"};
            target.Activities.Add("c.a1", child1);
            var child2 = new Activity { Name = "a2" };
            target.Activities.Add("c.a2", child2);

            var res = target.GetStartActivity(context);
            Assert.AreEqual(child1, res);

            target.StartActivity = "a2";
            res = target.GetStartActivity(context);
            Assert.AreEqual(child2, res);
        }

        /// <summary>
        ///A test for GetNextActivity
        ///</summary>
        [TestMethod]
        public void GetNextActivityTest()
        {
            var a1 = new Activity();
            var a2 = new Activity();
            var a3 = new Activity();
            var nak = new NextActivityKey("n");
            a1.NextActivities.Add(nak, a2);
            a1.FollowingActivity = a3;

            var res = CompositeActivity_Accessor.GetNextActivity(a1, nak);
            Assert.AreEqual(a2, res);

            res = CompositeActivity_Accessor.GetNextActivity(a1, new NextActivityKey("2"));
            Assert.AreEqual(a3, res);

            var a4 = new Activity();
            a1.NextActivities.Add(NextActivityKey.DefaultNextActivityKey, a4);
            res = CompositeActivity_Accessor.GetNextActivity(a1, new NextActivityKey("2"));
            Assert.AreEqual(a4, res);
        }

        /// <summary>
        ///A test for ExecuteNestedActivity
        ///</summary>
        [TestMethod]
        public void ExecuteNestedActivityTest()
        {
            var context = new WorkflowExecutionContext(new WorkflowScheme());
            var target = new CompositeActivity_Accessor { Name = "c" };
            var nak = new NextActivityKey("n");
            target.Activities.Add(new ReturnActivity(nak));
            
            var res = target.ExecuteNestedActivity(context, new ActivityParameterDictionary());
            Assert.AreEqual(nak, res);
        }

        /// <summary>
        ///A test for ExecuteNestedActivity
        ///</summary>
        [TestMethod]
        public void ExecuteNestedActivityTest2()
        {
            var dnak = new NextActivityKey("d");
            var context = new WorkflowExecutionContext(new WorkflowScheme {DefaultNextActivityKey = dnak});
            var target = new CompositeActivity_Accessor { Name = "c" };
            var callValidator = new ActivityMethodsCallValidator();
            target.Activities.Add(new Activity
                                      {
                                          Name = "a1",
                                          ExecutionMethodCaller = new ActivityExecutionMethodCaller(
                                              ActivityMethodsCallValidator.EXECUTE_METHODNAME, callValidator)
                                      });

            var res = target.ExecuteNestedActivity(context, new ActivityParameterDictionary());
            Assert.AreEqual(dnak, res);
            Assert.AreEqual(1, callValidator.ExecuteCallCount);
        }
    }
}
