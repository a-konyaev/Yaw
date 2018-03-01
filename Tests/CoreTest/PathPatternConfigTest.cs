using Yaw.Core.Configuration;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;

namespace Yaw.Tests.CoreTest
{
    
    
    /// <summary>
    ///This is a test class for PathPatternConfigTest and is intended
    ///to contain all PathPatternConfigTest Unit Tests
    ///</summary>
    [TestClass()]
    public class PathPatternConfigTest
    {


        private TestContext testContextInstance;

        /// <summary>
        ///Gets or sets the test context which provides
        ///information about and functionality for the current test run.
        ///</summary>
        public TestContext TestContext
        {
            get
            {
                return testContextInstance;
            }
            set
            {
                testContextInstance = value;
            }
        }

        #region Additional test attributes
        // 
        //You can use the following additional attributes as you write your tests:
        //
        //Use ClassInitialize to run code before running the first test in the class
        //[ClassInitialize()]
        //public static void MyClassInitialize(TestContext testContext)
        //{
        //}
        //
        //Use ClassCleanup to run code after all tests in a class have run
        //[ClassCleanup()]
        //public static void MyClassCleanup()
        //{
        //}
        //
        //Use TestInitialize to run code before running each test
        //[TestInitialize()]
        //public void MyTestInitialize()
        //{
        //}
        //
        //Use TestCleanup to run code after each test has run
        //[TestCleanup()]
        //public void MyTestCleanup()
        //{
        //}
        //
        #endregion


        /// <summary>
        ///A test for ResolvePath
        ///</summary>
        [TestMethod()]
        [DeploymentItem("Yaw.Core.dll")]
        public void ResolvePathTest()
        {
            var actual = PathPatternConfig_Accessor.ResolvePath(@"c:\Temp\1\q1\1");
            Assert.AreEqual("", actual);
        }
    }
}
