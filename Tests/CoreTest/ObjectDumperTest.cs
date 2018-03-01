using System.Collections.Generic;
using Yaw.Core.Utils;
using Yaw.Core.Utils.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using Yaw.Tests.CoreTest.Helpers;
using System.Collections;

namespace Yaw.Tests.CoreTest
{
    /// <summary>
    ///This is a test class for ObjectDumperTest and is intended
    ///to contain all ObjectDumperTest Unit Tests
    ///</summary>
	[TestClass]
	public class ObjectDumperTest
	{
    	private TestDumpObject _dumpObj;

    	private ObjectDumper_Accessor.DumpContext _ctx;

    	/// <summary>
    	///Gets or sets the test context which provides
    	///information about and functionality for the current test run.
    	///</summary>
    	public TestContext TestContext { get; set; }

		[TestInitialize]
		public void TestInit()
		{
			// объект, который будем использовать
			_dumpObj = new TestDumpObject
			{
				TestProperty1 = "TestValue1",
			};

			_ctx = new ObjectDumper_Accessor.DumpContext
			{
				Settings = new ObjectDumperSettings(),
				Builder = new TextBuilder()
			};
			_ctx.Settings.PropsToIgnore = new[] { "IgnoredTestProperty", "IgnoredField" };
		}

    	/// <summary>
		///A test for DumpProps
		///</summary>
		[TestMethod]
		[DeploymentItem("Yaw.Core.dll")]
		public void DumpPropsTest()
		{
			const string DUMP_RESULT = "TestProperty1: TestValue1\r\nTestField1: {0}";
			var dateTime = DateTime.Now;

    		_dumpObj.TestField1 = dateTime;

			ObjectDumper_Accessor.DumpProps(_dumpObj, typeof(TestDumpObject), _ctx);
			Assert.AreEqual(
				string.Format(DUMP_RESULT, dateTime),
				_ctx.Builder.ToString(),
				"Неверный результат приведения объекта к строке");
		}

		/// <summary>
		///A test for WriteObject, ToString возвращает 1 строку
		///</summary>
		[TestMethod]
		[DeploymentItem("Yaw.Core.dll")]
		public void WriteObjectOneLineTest()
		{
			_ctx.Settings.DoNotUseToStringMethod = true;
			_dumpObj.IgnoredTestProperty = true;

			ObjectDumper_Accessor.WriteObject(_dumpObj, typeof(TestDumpObject), _ctx);
			Assert.AreEqual("ToString was called", _ctx.Builder.ToString());
		}

		/// <summary>
		///A test for WriteObject, ToString возвращает >1 строку
		///</summary>
		[TestMethod]
		[DeploymentItem("Yaw.Core.dll")]
		public void WriteObjectManyLinesTest()
		{
			_ctx.Settings.DoNotUseToStringMethod = true;
			_dumpObj.IgnoredTestProperty = false;

			ObjectDumper_Accessor.WriteObject(_dumpObj, typeof(TestDumpObject), _ctx);
			Assert.AreEqual("\r\n\tLine1\r\n\tLine2", _ctx.Builder.ToString());
		}

		/// <summary>
		///A test for DumpEnumerable
		///</summary>
		[TestMethod]
		[DeploymentItem("Yaw.Core.dll")]
		public void DumpSimpleEnumerableTest()
		{
			IEnumerable enumerable = new List<String> {"value1", "value2", null};

			_ctx.Settings.MaxEnumerableItems = 10;
			ObjectDumper_Accessor.DumpEnumerable(enumerable, _ctx);
			Assert.AreEqual("value1,value2,<NULL>", _ctx.Builder.ToString());
		}

		/// <summary>
		///A test for DumpEnumerable
		///</summary>
		[TestMethod]
		[DeploymentItem("Yaw.Core.dll")]
		public void DumpDictEnumerableTest()
		{
			IEnumerable enumerable = new Dictionary<int, string> {{0, "value1"}, {1, "value2"}, {2, null}};

			_ctx.Settings.MaxEnumerableItems = 10;
			ObjectDumper_Accessor.DumpEnumerable(enumerable, _ctx);
			Assert.AreEqual("0: value1\r\n1: value2\r\n2: <NULL>", _ctx.Builder.ToString());
		}

		/// <summary>
		///A test for DumpEnumerable
		///</summary>
		[TestMethod]
		[DeploymentItem("Yaw.Core.dll")]
		public void DumpManyLinesEnumerableTest()
		{
			IEnumerable enumerable = new Dictionary<int, string> { { 0, "value1" }, { 1, "value2" }, { 2, null } };
			_ctx.Settings.MaxEnumerableItems = 2;

			ObjectDumper_Accessor.DumpEnumerable(enumerable, _ctx);
			Assert.AreEqual("0: value1\r\n1: value2\r\n... (first 2 items, 3 items total)", _ctx.Builder.ToString());
		}
	}
}
