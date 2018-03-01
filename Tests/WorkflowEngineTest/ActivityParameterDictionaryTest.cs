using System;
using System.Runtime.Serialization;
using Yaw.Workflow.ComponentModel;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Yaw.Tests.WorkflowEngineTest
{
    /// <summary>
    ///This is a test class for ActivityParameterDictionaryTest and is intended
    ///to contain all ActivityParameterDictionaryTest Unit Tests
    ///</summary>
    [TestClass]
    public class ActivityParameterDictionaryTest
    {
        /// <summary>
        ///Gets or sets the test context which provides
        ///information about and functionality for the current test run.
        ///</summary>
        public TestContext TestContext { get; set; }

        /// <summary>
        ///A test for ActivityParameterDictionary Constructor
        ///</summary>
        [TestMethod]
        public void ActivityParameterDictionaryConstructorTest()
        {
            var target = new ActivityParameterDictionary();
            Assert.IsNotNull(target);
            target = new ActivityParameterDictionary(null, new StreamingContext());
            Assert.IsNotNull(target);
        }

        /// <summary>
        ///A test for CheckParameter
        ///</summary>
        [TestMethod]
        public void CheckExistedParameterTest()
        {
            var target = new ActivityParameterDictionary {new ActivityParameter("p", 1)};
            target.CheckParameter("p");
        }

        /// <summary>
        ///A test for CheckParameter
        ///</summary>
        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void CheckNotExistedParameterTest()
        {
            var target = new ActivityParameterDictionary();
            target.CheckParameter("p");
        }

        /// <summary>
        ///A test for GetParamValueOrThrow
        ///</summary>
        [TestMethod]
        public void GetParamValueOrThrowTest()
        {
            var target = new ActivityParameterDictionary { new ActivityParameter("p", 1.2) };
            Assert.AreEqual(1.2, target.GetParamValueOrThrow<double>("p"));
        }

        /// <summary>
        ///A test for GetParamValueOrThrow
        ///</summary>
        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void GetParamValueOrThrowTest2()
        {
            var target = new ActivityParameterDictionary();
            target.GetParamValueOrThrow<double>("p");
        }

        /// <summary>
        ///A test for GetParamValue
        ///</summary>
        [TestMethod]
        public void GetParamDefaultValueTest()
        {
            var target = new ActivityParameterDictionary();
            Assert.AreEqual(0, target.GetParamValue<int>("p"));
            Assert.AreEqual(-1, target.GetParamValue("p", -1));
            target.Add(new ActivityParameter("p", 123));
            Assert.AreEqual(123, target.GetParamValue<int>("p"));
        }

        /// <summary>
        ///A test for GetParamValueAsEnumerableOrThrow
        ///</summary>
        [TestMethod]
        public void GetParamValueAsEnumerableOrThrowTest()
        {
            var target = new ActivityParameterDictionary {new ActivityParameter("p", new object[] {1})};
            var en = target.GetParamValueAsEnumerableOrThrow<int>("p").GetEnumerator();
            Assert.IsTrue(en.MoveNext());
            Assert.AreEqual(1, en.Current);
            Assert.IsFalse(en.MoveNext());
        }

        /// <summary>
        ///A test for GetParamValueAsEnumerableOrThrow
        ///</summary>
        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void GetParamValueAsEnumerableOrThrowTest2()
        {
            var target = new ActivityParameterDictionary();
            target.GetParamValueAsEnumerableOrThrow<int>("p");
        }

        /// <summary>
        ///A test for GetParamValueAsEnumerable
        ///</summary>
        [TestMethod]
        public void GetParamValueAsEnumerableTest()
        {
            var target = new ActivityParameterDictionary();
            var en = target.GetParamValueAsEnumerable("p", new[] {1}).GetEnumerator();
            en.MoveNext();
            Assert.AreEqual(1, en.Current);
            Assert.IsFalse(en.MoveNext());

            target.Add(new ActivityParameter("p", new object[] {2}));
            en = target.GetParamValueAsEnumerable("p", new[] { 1 }).GetEnumerator();
            en.MoveNext();
            Assert.AreEqual(2, en.Current);
            Assert.IsFalse(en.MoveNext());
        }

        /// <summary>
        ///A test for GetParamValueAsArrayOrThrow
        ///</summary>
        [TestMethod]
        public void GetParamValueAsArrayOrThrowTest()
        {
            var target = new ActivityParameterDictionary {new ActivityParameter("p", new object[] {1})};
            var arr = target.GetParamValueAsArrayOrThrow<int>("p");
            Assert.AreEqual(1, arr.Length);
            Assert.AreEqual(1, arr[0]);
        }

        /// <summary>
        ///A test for GetParamValueAsArrayOrThrow
        ///</summary>
        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void GetParamValueAsArrayOrThrowTest2()
        {
            var target = new ActivityParameterDictionary();
            target.GetParamValueAsArrayOrThrow<int>("p");
        }

        /// <summary>
        ///A test for GetParamValueAsArrayOrThrow
        ///</summary>
        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void GetParamValueAsArrayOrThrowTest3()
        {
            var target = new ActivityParameterDictionary();
            target.GetParamValueAsArrayOrThrow("p");
        }

        /// <summary>
        ///A test for GetParamValueAsArrayOrThrow
        ///</summary>
        [TestMethod]
        public void GetParamValueAsArrayOrThrowTest4()
        {
            var target = new ActivityParameterDictionary { new ActivityParameter("p", new object[] { 1 }) };
            var arr = target.GetParamValueAsArrayOrThrow("p");
            Assert.AreEqual(1, arr.Length);
            Assert.AreEqual(1, arr[0]);
        }

        [TestMethod]
        public void GetParamValueAsArrayTest()
        {
            var target = new ActivityParameterDictionary();
            var arr = target.GetParamValueAsArray("p");
            Assert.AreEqual(0, arr.Length);

            arr = target.GetParamValueAsArray("p", new object[] {1});
            Assert.AreEqual(1, arr.Length);
            Assert.AreEqual(1, (int)arr[0]);
        }

        [TestMethod]
        public void GetParamValueAsArrayTest2()
        {
            var target = new ActivityParameterDictionary {new ActivityParameter("p", new object[] {1})};
            var arr = target.GetParamValueAsArray("p");
            Assert.AreEqual(1, arr.Length);
            Assert.AreEqual(1, (int) arr[0]);
        }
    }
}
