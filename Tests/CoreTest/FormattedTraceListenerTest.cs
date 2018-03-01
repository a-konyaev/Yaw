using Yaw.Core.Diagnostics.Default;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Yaw.Tests.CoreTest
{
    /// <summary>
    ///This is a test class for FormattedTraceListenerTest and is intended
    ///to contain all FormattedTraceListenerTest Unit Tests
    ///</summary>
	[TestClass]
	public class FormattedTraceListenerTest
	{
    	/// <summary>
    	///Gets or sets the test context which provides
    	///information about and functionality for the current test run.
    	///</summary>
    	public TestContext TestContext { get; set; }

		/// <summary>
		///A test for WriteLine
		///</summary>
		[TestMethod]
		public void WriteLineTest()
		{
			string format = "{Test1}\t[{Test2}]\t{Int1:d}\t{dateTime1:dd.MM.yyyy HH:mm:ss}";
			var target = new FormattedTraceListener(format);
			var accessor = new FormattedTraceListener_Accessor(new PrivateObject(target));

			string message = "Test1: value1\r\n Test2: value2\n Some text\nInt1: 4";
			var result = accessor.FormatLine(message);

			Assert.AreEqual("value1\t[value2]\t4\t", result);
		}
	}
}
