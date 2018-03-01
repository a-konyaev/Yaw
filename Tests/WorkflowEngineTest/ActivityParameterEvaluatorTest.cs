using Yaw.Workflow.ComponentModel;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Yaw.Tests.WorkflowEngineTest
{
    /// <summary>
    ///This is a test class for ActivityParameterEvaluatorTest and is intended
    ///to contain all ActivityParameterEvaluatorTest Unit Tests
    ///</summary>
    [TestClass]
    public class ActivityParameterEvaluatorTest
    {
        /// <summary>
        ///Gets or sets the test context which provides
        ///information about and functionality for the current test run.
        ///</summary>
        public TestContext TestContext { get; set; }

        /// <summary>
        ///A test for ActivityParameterEvaluator Constructor
        ///</summary>
        [TestMethod]
        public void GetValueTest()
        {
            var target = new ActivityParameterEvaluator(ActivityParameterValueType.PlainValue);
            Assert.AreEqual(ActivityParameterValueType.PlainValue, target.ValueType);
            Assert.IsNull(target.GetValue());
        }

        [TestMethod]
        public void GetValueTest2()
        {
            var target = new ActivityParameterEvaluator(123);
            Assert.AreEqual(ActivityParameterValueType.PlainValue, target.ValueType);
            Assert.AreEqual(123, target.GetValue());
        }

        public int TestProp { get; set; }

        [TestMethod]
        public void GetValueTest3()
        {
            var prop = GetType().GetProperty("TestProp");
            var target = new ActivityParameterEvaluator(prop, this);
            Assert.AreEqual(ActivityParameterValueType.ReferenceToProperty, target.ValueType);

            TestProp = 123;
            Assert.AreEqual(123, target.GetValue());
        }

        [TestMethod]
        public void GetValueTest4()
        {
            var e1 = new ActivityParameterEvaluator(1);
            var e2 = new ActivityParameterEvaluator(2);
            var target = new ActivityParameterEvaluator(new[] {e1, e2});
            Assert.AreEqual(ActivityParameterValueType.Array, target.ValueType);
            var arr = (object[]) target.GetValue();
            Assert.AreEqual(2, arr.Length);
            Assert.AreEqual(1, (int)arr[0]);
            Assert.AreEqual(2, (int)arr[1]);
        }
    }
}
