using System;

namespace Yaw.Tests.CoreTest.Helpers
{
	/// <summary>
	/// Класс для тестирования методов класса TypeExtensions
	/// </summary>
	public class TestType
	{
		public string PropertyWithOutGetAccessor
		{
			private get;
			set;
		}

		public string PropertyWithOutSetAccessor
		{
			get;
			private set;
		}

		public string PropertyWithGetSetAccessor
		{
			get;
			set;
		}

		/// <summary>
		/// Метод для юнит тестирования метода GetMethodParametersTypesTest класса TypeExtensionsы
		/// </summary>
		/// <param name="param1"></param>
		/// <param name="param2"></param>
		/// <param name="param3"></param>
		public void TestMethodForGetParametersType(string param1, Int16 param2, System.Diagnostics.TraceEventType param3)
		{
			// ничего не делает
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="s"></param>
		/// <returns></returns>
		public string TestMethod(string s)
		{
			return s;
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="s"></param>
		/// <param name="i"></param>
		/// <returns></returns>
		public int TestMethod(string s, int i)
		{
			return i;
		}
	}
}
