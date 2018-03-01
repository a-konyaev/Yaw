using Yaw.Core.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Yaw.Core.Utils;

namespace Yaw.Tests.CoreTest
{
    /// <summary>
    ///This is a test class for LoggingUtilsTest and is intended
    ///to contain all LoggingUtilsTest Unit Tests
    ///</summary>
	[TestClass]
	public class LoggingUtilsTest
	{
    	/// <summary>
    	///Gets or sets the test context which provides
    	///information about and functionality for the current test run.
    	///</summary>
    	public TestContext TestContext { get; set; }

		/// <summary>
		///A test for Format
		///</summary>
		[TestMethod]
		public void FormatTest()
		{
			var properties = new EventProperties
			                 	{
			                 		{"test1", 1},
									{"test2", 2},
									{"", null}
			                 	};

			var actual = LoggingUtils.Format(properties);

			Assert.AreEqual("EventData: <NULL>\r\ntest1: 1\r\ntest2: 2", actual.ToString());
		}
	}
}
