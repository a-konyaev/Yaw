using System;
using System.Configuration;

namespace Yaw.Core.Utils
{
    /// <summary>
    /// Содержит вспомогательные методы для работы с конфигом
    /// </summary>
    public static class ConfigurationUtils
    {
        /// <summary>
        /// Загрузить конфигурацию из заданного конфиг-файла
        /// </summary>
        /// <param name="configFilePath">путь к конфиг-файлу</param>
        /// <returns></returns>
        public static System.Configuration.Configuration OpenConfigurationFromFile(string configFilePath)
        {
            var fileMap = new ConfigurationFileMap(configFilePath);
            return ConfigurationManager.OpenMappedMachineConfiguration(fileMap);
        }

        /// <summary>
        /// Загрузить конфигурацию из заданного конфиг-файла и вернуть секцию с заданным именем
        /// </summary>
        /// <param name="configFilePath">Путь к файлу конфигурации</param>
        /// <param name="sectionName">Имя секции</param>
        /// <returns></returns>
        public static T GetSection<T>(string configFilePath, string sectionName) 
            where T : ConfigurationSection
        {
            var config = OpenConfigurationFromFile(configFilePath);
            var section = (T)config.GetSection(sectionName);
            if (section == null)
                throw new Exception("Секция не найдена: " + sectionName);

            return section;
        }
    }
}
