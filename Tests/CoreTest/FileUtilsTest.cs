using Yaw.Core.Utils.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.IO;
using System;

namespace Yaw.Tests.CoreTest
{
    /// <summary>
    ///This is a test class for FileUtilsTest and is intended
    ///to contain all FileUtilsTest Unit Tests
    ///</summary>
	[TestClass]
	public class FileUtilsTest
	{
    	/// <summary>
    	///Gets or sets the test context which provides
    	///information about and functionality for the current test run.
    	///</summary>
    	public TestContext TestContext { get; set; }

		/// <summary>
		///A test for CreateUniqueFolder
		///</summary>
		[TestMethod]
		public void CreateUniqueFolderTest()
		{
			string expected = string.Format("./test_{0:yyyyMMdd}.", DateTime.Now);
			Directory.CreateDirectory(expected + "01");
			var actual = FileUtils.CreateUniqueFolder("./", "test", 2);
			
			Assert.AreEqual(expected + "02", actual, "Создана некорректная директория");
			Assert.IsTrue(Directory.Exists(expected + "02"), "Не создана директория на дискке");
		}

		/// <summary>
		///A test for EnsureDirExists
		///</summary>
		[TestMethod]
		public void EnsureDirExistsTest()
		{
			var path = "./TestDir";
			FileUtils.EnsureDirExists(path);
			Assert.IsTrue(Directory.Exists(path), "Не создана директория");
		}

		/// <summary>
		///A test for GetRelativePath
		///</summary>
		[TestMethod]
		public void GetRelativePathTest()
		{
			var actual = FileUtils.GetRelativePath(@"C:\Test1\Test2\Test3", @"C:\Test1");
			Assert.AreEqual(@"..\..\..\Test1", actual);
		}
	}
}
