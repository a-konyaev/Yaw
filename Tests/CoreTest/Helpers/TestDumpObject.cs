using System;

namespace Yaw.Tests.CoreTest.Helpers
{
	/// <summary>
	/// Класс для тестов ObjectDumper
	/// </summary>
	public class TestDumpObject
	{
		public string TestProperty1
		{
			get; set;
		}

		public bool IgnoredTestProperty
		{
			get;
			set;
		}

		private int TestProperty2
		{
			get; set;
		}

		public DateTime TestField1 = DateTime.Now;

		public int IgnoredField;

		private string _testField2 = "test value 2";

		public override string ToString()
		{
			if(IgnoredTestProperty)
				return "ToString was called";

			return "Line1" + Environment.NewLine + "Line2";
		}
	}
}
