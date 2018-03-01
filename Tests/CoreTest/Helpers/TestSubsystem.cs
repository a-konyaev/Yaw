using Yaw.Core;
using Yaw.Core.Configuration;

namespace Yaw.Tests.CoreTest.Helpers
{
	/// <summary>
	/// Подсистема для юнит тестирования CoreApplication
	/// </summary>
	[SubsystemConfigurationElementTypeAttribute(typeof(TestSubsystemConfig))]
	public class TestSubsystem : Subsystem
	{
		/// <summary>
		/// Тестовое значение берущиеся из конфига при инициализации
		/// </summary>
		public string TestValue
		{
			get;
			set;
		}
		
		/// <summary>
		/// Инициализация подсистемы
		/// </summary>
		/// <param name="config">Конфиг подсистемы</param>
		public override void Init(SubsystemConfig config)
		{
			TestSubsystemConfig testConfig = (TestSubsystemConfig)config;

			TestValue = testConfig.TestProperty.TestValue;
		}

		/// <summary>
		/// Применение нового конфига
		/// </summary>
		/// <param name="newConfig">Новый конфиг</param>
		public override void ApplyNewConfig(SubsystemConfig newConfig)
		{
			Init(newConfig);
			WasApplyNewConfigCall = true;
		}

		/// <summary>
		/// Был ли вызван метод ApplyNewConfig
		/// </summary>
		public bool WasApplyNewConfigCall
		{
			get; set;
		}
	}
}
