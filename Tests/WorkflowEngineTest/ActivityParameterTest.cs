using System;
using Yaw.Workflow.ComponentModel;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Yaw.Tests.WorkflowEngineTest
{
    /// <summary>
    ///This is a test class for ActivityParameterTest and is intended
    ///to contain all ActivityParameterTest Unit Tests
    ///</summary>
    [TestClass]
    public class ActivityParameterTest
    {
        private const string TEST_STR = "Test";

        /// <summary>
        ///Gets or sets the test context which provides
        ///information about and functionality for the current test run.
        ///</summary>
        public TestContext TestContext { get; set; }
        
        /// <summary>
        ///A test for Name
        ///</summary>
        [TestMethod]
        public void NameTest()
        {
            var target = new ActivityParameter {Name = TEST_STR};
            Assert.AreEqual(TEST_STR, target.Name);
        }

        /// <summary>
        ///A test for Evaluator
        ///</summary>
        [TestMethod]
        public void EvaluatorTest()
        {
            var evaluator = new ActivityParameterEvaluator(null);
            var target = new ActivityParameter { Evaluator = evaluator };
            Assert.AreEqual(evaluator, target.Evaluator);
        }

        /// <summary>
        ///A test for ActivityParameter Constructor
        ///</summary>
        [TestMethod]
        public void ActivityParameterConstructorTest()
        {
            object plainValue = 123;
            var target = new ActivityParameter(TEST_STR, plainValue);
            Assert.AreEqual(TEST_STR, target.Name);
            Assert.IsNotNull(target.Evaluator);
            Assert.AreEqual(plainValue, target.Evaluator.PlainValue);
        }

        /// <summary>
        ///A test for GetValue
        ///</summary>
        [TestMethod]
        public void GetValueTest()
        {
            var target = new ActivityParameter();
            Assert.IsNull(target.GetValue());

            target.Evaluator = new ActivityParameterEvaluator(123);
            Assert.AreEqual(123, target.GetValue());
        }

        /// <summary>
        ///A test for GetValue
        ///</summary>
        [TestMethod]
        public void GetCastedValueTest()
        {
            var target = new ActivityParameter {Evaluator = new ActivityParameterEvaluator(TEST_STR)};
            Assert.AreEqual(TEST_STR, target.GetValue<string>());
        }

        /// <summary>
        ///A test for GetParamValueAsArray
        ///</summary>
        [TestMethod]
        public void GetParamValueAsArrayTest()
        {
            var arr = new object[] {123, "123"};
            var target = new ActivityParameter {Evaluator = new ActivityParameterEvaluator(arr)};
            Assert.AreEqual(arr, target.GetParamValueAsArray());
        }

        /// <summary>
        ///A test for GetParamValueAsEnumerable
        ///</summary>
        [TestMethod]
        public void GetParamValueAsEnumerableTest()
        {
            var arr = new object[] { "1", "2" };
            var target = new ActivityParameter { Evaluator = new ActivityParameterEvaluator(arr) };
            var en = target.GetParamValueAsEnumerable<string>().GetEnumerator();

            Assert.IsTrue(en.MoveNext());
            Assert.AreEqual("1", en.Current);
            Assert.IsTrue(en.MoveNext());
            Assert.AreEqual("2", en.Current);
            Assert.IsFalse(en.MoveNext());
        }

        /// <summary>
        ///A test for GetParamValueAsArray
        ///</summary>
        [TestMethod]
        public void GetParamValueAsCastedArrayTest()
        {
            var target = new ActivityParameter { Evaluator = new ActivityParameterEvaluator(new[] { "1", "2" }) };
            var actual = target.GetParamValueAsArray<string>();

            Assert.AreEqual(2, actual.Length);
            Assert.AreEqual("1", actual[0]);
            Assert.AreEqual("2", actual[1]);
        }

        private enum TestEnum
        {
            A
        }

        [TestMethod]
        public void CastValueTest()
        {
            var target = new ActivityParameter_Accessor();
            Assert.AreEqual(1, target.CastValue<int>(1));
            Assert.AreEqual(1L, target.CastValue<long>(1));
            Assert.AreEqual("1", target.CastValue<string>("1"));
            Assert.AreEqual(this, target.CastValue<ActivityParameterTest>(this));
            Assert.AreEqual(TestEnum.A, target.CastValue<TestEnum>("A"));
            const string TIME_STR = "123.23:59:59.777";
            Assert.AreEqual(TimeSpan.Parse(TIME_STR), target.CastValue<TimeSpan>(TIME_STR));
        }
    }
}
