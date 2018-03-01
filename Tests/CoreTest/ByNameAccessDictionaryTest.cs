using Yaw.Core.Utils.Collections;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rhino.Mocks;

namespace Yaw.Tests.CoreTest
{
    /// <summary>
    ///This is a test class for ByNameAccessDictionaryTest and is intended
    ///to contain all ByNameAccessDictionaryTest Unit Tests
    ///</summary>
	[TestClass]
	public class ByNameAccessDictionaryTest
	{
    	/// <summary>
    	///Gets or sets the test context which provides
    	///information about and functionality for the current test run.
    	///</summary>
    	public TestContext TestContext { get; set; }

		/// <summary>
		/// Получение объекта INamed
		/// </summary>
		/// <param name="name"></param>
		/// <returns></returns>
		private static INamed GetMock(string name)
		{
			var mock = new MockRepository();
			var namedObj = mock.DynamicMock<INamed>();
			Expect.Call(namedObj.Name).Return(name);

			mock.ReplayAll();

			return namedObj;
		}

		[TestMethod]
		public void RemoveTest()
		{
			var dict = new ByNameAccessDictionary<INamed> {GetMock("testItem1")};
			dict.Remove(GetMock("testItem1"));

			Assert.AreEqual(0, dict.Count, "Элемент не удален из словаря");
		}

		[TestMethod]
		public void ContainsTest()
		{
			var dict = new ByNameAccessDictionary<INamed> { GetMock("testItem1") };

			Assert.IsTrue(dict.Contains(GetMock("testItem1")));
			Assert.IsTrue(dict.Contains("testItem1"));
		}

		[TestMethod]
		public void AddTest()
		{
			var dict = new ByNameAccessDictionary<INamed>();

			dict.Add(GetMock("testItem1"));
			Assert.IsTrue(dict.ContainsKey("testItem1"));
		}
	}
}
