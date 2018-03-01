using System.Configuration;
using Yaw.Core.Configuration;

namespace Yaw.Tests.CoreTest.Helpers
{
	/// <summary>
	/// Конфиг элемент тестовой подсистемы
	/// </summary>
	public class TestSubsystemConfig : SubsystemConfig
	{
		[ConfigurationProperty("testProperty", IsRequired = true)]
		public TestPropertyConfig TestProperty
		{
			get
			{
				return (TestPropertyConfig)this["testProperty"];
			}
			set
			{
				this["testProperty"] = value;
			}
		}
	}

	public class TestPropertyConfig : ConfigurationElement
	{
		[ConfigurationProperty("testValue", IsRequired = true)]
		public string TestValue
		{
			get
			{
				return (string)this["testValue"];
			}
			set
			{
				this["testValue"] = value;
			}
		}
	}
}
