using System.Collections.Generic;
using Yaw.Core.Utils.Xml;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Yaw.Tests.CoreTest
{
    /// <summary>
    ///This is a test class for XmlMergeTest and is intended
    ///to contain all XmlMergeTest Unit Tests
    ///</summary>
    [TestClass]
    public class XmlMergeTest
    {
        public TestContext TestContext { get; set; }

        /// <summary>
        ///A test for Merge
        ///</summary>
        [TestMethod]
        public void MergeTest()
        {
            var trgXml = "<a a1=\"a\"><b k=\"b1\" x=\"1\"/><b k=\"b2\" x=\"2\"/><b k=\"b3\" x=\"3\"/><c c1=\"3\"/></a>";
            var srcXml = "<a a2=\"new\"><b k=\"b2\" x=\"222\"></b><c c1=\"333\"/></a>";
            var result = "<a a1=\"a\" a2=\"new\"><b k=\"b1\" x=\"1\" /><b k=\"b2\" x=\"222\" /><b k=\"b3\" x=\"3\" /><c c1=\"3\" /></a>";

            var invariableElementXpaths = new List<string>();
            invariableElementXpaths.Add("/a/c");
            var keyAttributeNames = new Dictionary<string, string>();
            keyAttributeNames.Add("b", "k");

            var m = new XmlMerge(invariableElementXpaths, keyAttributeNames);

            var merged = m.Merge(srcXml, trgXml);

            Assert.IsTrue(merged);
            Assert.AreEqual(result, m.Result.OuterXml);
        }
    }
}
