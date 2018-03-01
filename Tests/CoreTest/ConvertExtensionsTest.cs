using Yaw.Core.Extensions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
namespace Yaw.Tests.CoreTest
{
    /// <summary>
    ///This is a test class for ConvertExtensionsTest and is intended
    ///to contain all ConvertExtensionsTest Unit Tests
    ///</summary>
	[TestClass]
	public class ConvertExtensionsTest
	{
    	/// <summary>
    	///Gets or sets the test context which provides
    	///information about and functionality for the current test run.
    	///</summary>
    	public TestContext TestContext { get; set; }

		/// <summary>
		///A test for ToBool
		///</summary>
		[TestMethod]
		public void ToBoolTest()
		{
			Assert.AreEqual(false, 0.ToBool());
			Assert.AreEqual(true, 1.ToBool());
		}

		/// <summary>
		///A test for ToInt
		///</summary>
		[TestMethod]
		public void ToIntTest()
		{
			Assert.AreEqual(0, false.ToInt());
			Assert.AreEqual(1, true.ToInt());
		}
	}
}
