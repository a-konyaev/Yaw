using System;
using Yaw.Core.Utils;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Configuration;
using Yaw.Core.Configuration;

namespace Yaw.Tests.CoreTest
{
    /// <summary>
    ///This is a test class for ConfigurationUtilsTest and is intended
    ///to contain all ConfigurationUtilsTest Unit Tests
    ///</summary>
	[TestClass]
	public class ConfigurationUtilsTest
	{
    	/// <summary>
    	///Gets or sets the test context which provides
    	///information about and functionality for the current test run.
    	///</summary>
    	public TestContext TestContext { get; set; }

		/// <summary>
		/// Путь к файлу конфига
		/// </summary>
        const string CONFIG_FILE_PATH = "./yaw.tests.coretest.dll.config";

		/// <summary>
		///A test for GetSection
		///</summary>
		public void GetSectionTestHelper<T>(string sectionName)
			where T : ConfigurationSection
		{
			var actual = ConfigurationUtils.GetSection<T>(CONFIG_FILE_PATH, sectionName);
			Assert.IsNotNull(actual, "Не загружена секция конфигурации");
		}

		/// <summary>
		/// Проверка метода GetSectionTest для несуществующей секции
		/// </summary>
		[TestMethod]
		public void GetSectionTest()
		{
			GetSectionTestHelper<ApplicationConfig>("yaw.application");
		}

		/// <summary>
		/// Проверка метода GetSectionTest для несуществующей секции
		/// </summary>
		[TestMethod]
		public void GetNonExistsSectionTest()
		{
			try
			{
				GetSectionTestHelper<ApplicationConfig>("some.bad.section");
				Assert.Fail("Не произошло ошибки при загрузке несуществующей секции");
			}
			catch (Exception ex)
			{
				Assert.AreEqual("Секция не найдена: some.bad.section", ex.Message);
			}
		}

		/// <summary>
		///A test for OpenConfigurationFromFile
		///</summary>
		[TestMethod]
		public void OpenConfigurationFromFileTest()
		{
			var actual = ConfigurationUtils.OpenConfigurationFromFile(CONFIG_FILE_PATH);

			Assert.IsNotNull(actual, "Конфигурация не загружена");
		}
	}
}
