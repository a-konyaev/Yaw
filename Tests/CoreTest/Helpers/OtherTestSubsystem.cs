using Yaw.Core.Configuration;

namespace Yaw.Tests.CoreTest.Helpers
{
    /// <summary>
    /// Первый тестовый интерфейс
    /// </summary>
    public interface IOneTestInterface
    {
    }

    /// <summary>
    /// Второй тестовый интерфейс
    /// </summary>
    public interface ISecondTestInterface
    {
    }

	/// <summary>
	/// Подсистема для юнит тестирования CoreApplication
	/// </summary>
	[SubsystemConfigurationElementTypeAttribute(typeof(TestSubsystemConfig))]
    public class OtherTestSubsystem : TestSubsystem, IOneTestInterface
	{
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
		}
	}
}
