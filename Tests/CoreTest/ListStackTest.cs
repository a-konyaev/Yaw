using System;
using Yaw.Core.Utils.Collections;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Yaw.Tests.CoreTest
{
    /// <summary>
    ///This is a test class for ListStackTest and is intended
    ///to contain all ListStackTest Unit Tests
    ///</summary>
	[TestClass]
	public class ListStackTest
	{
    	/// <summary>
    	///Gets or sets the test context which provides
    	///information about and functionality for the current test run.
    	///</summary>
    	public TestContext TestContext { get; set; }

		[TestMethod]
		public void TopIndexTest()
		{
			var target = new ListStack<string>();

			Assert.AreEqual(-1, target.TopIndex);
		}

		[TestMethod]
		public void PushTest()
		{
            var target = new ListStack<string>();
			target.Push("1");

			Assert.AreEqual(1, target.Count, "Не добавлены элементы");
			Assert.AreEqual("1", target.Peek(), "Добавлен не верный элемент");
		}

		[TestMethod]
		public void PopTest()
		{
            var target = new ListStack<string> { "0", "1" };

			var topElement = target.Pop();

			Assert.AreEqual("1", topElement, "Неверный элемент стека");
			Assert.AreEqual(1, target.Count, "Взятый элемент не удален из стека");
		}

		[TestMethod]
		[ExpectedException(typeof(InvalidOperationException), "Стек пуст")]
		public void PeekEmptyStackTest()
		{
            var target = new ListStack<string>();

			target.Peek();
		}

		[TestMethod]
		public void PeekTest()
		{
            var target = new ListStack<string> { "0", "1" };

			var topElement = target.Peek();

			Assert.AreEqual("1", topElement, "Неверный элемент стека");
			Assert.AreEqual(2, target.Count, "Взятый элемент удален из стека");
		}
	}
}
