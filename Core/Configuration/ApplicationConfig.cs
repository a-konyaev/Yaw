using System;
using System.Configuration;
using System.IO;
using System.Xml;
using Yaw.Core.Utils.Xml;

namespace Yaw.Core.Configuration
{
    /// <summary>
    /// Конфиг-секция приложения
    /// </summary>
    public class ApplicationConfig : ConfigurationSection
    {
        /// <summary>
        /// Название секции
        /// </summary>
        public const string SECTION_NAME = "yaw.application";

        /// <summary>
        /// Загрузка конфиг-секции из xml-строки
        /// </summary>
        /// <param name="xml"></param>
        /// <returns></returns>
        public static ApplicationConfig FromXml(string xml)
        {
            try
            {
                var config = new ApplicationConfig();
                using (var stream = new StringReader(xml))
                using (var reader = XmlReader.Create(stream))
                    config.DeserializeSection(reader);

                return config;
            }
            catch (Exception ex)
            {
                throw new Exception("Ошибка загрузки конфиг-секции из xml: " + xml, ex);
            }
        }

		/// <summary>
		/// Преобразовать в xml
		/// </summary>
		/// <returns></returns>
		public string ToXml()
		{
            using (var memStream = new MemoryStream())
            {
				using (var xmlWriter = new PrettyPrintXmlWriter(memStream))
                {
                    // преобразуем в xml
                    SerializeToXmlElement(xmlWriter, SECTION_NAME);

					return xmlWriter.ToFormatString();
                }
            }
		}

        /// <summary>
        /// Имя приложения
        /// </summary>
        [ConfigurationProperty("name", IsRequired = false)]
        public String Name
        {
            get
            {
                return (string)this["name"];
            }
            set
            {
                this["name"] = value;
            }
        }

        /// <summary>
        /// Папка, в которой логгер приложения будет создавать лог-файлы
        /// </summary>
        [ConfigurationProperty("logFileFolder", IsRequired = false)]
        public PathPatternConfig LogFileFolder
        {
            get
            {
                return (PathPatternConfig)base["logFileFolder"];
            }
        }

        /// <summary>
        /// Название уровеня трасировки
        /// </summary>
        [ConfigurationProperty("traceLevel", IsRequired = false)]
        public string TraceLevelName
        {
            get
            {
                return (string)this["traceLevel"];
            }
            set
            {
                this["traceLevel"] = value;
            }
        }

        /// <summary>
        /// Настройка диагностики
        /// </summary>
        [ConfigurationProperty("diagnostics", IsRequired = false)]
        public DiagnosticsConfig DiagnosticsConfig
        {
            get
            {
                return (DiagnosticsConfig)this["diagnostics"];
            }
        }

        /// <summary>
        /// Конфиги подсистем
        /// </summary>
        [ConfigurationProperty("subsystems", IsDefaultCollection = true)]
        [ConfigurationCollection(typeof(SubsystemConfigCollection), AddItemName = "subsystem")]
        public SubsystemConfigCollection Subsystems
        {
            get
            {
                return (SubsystemConfigCollection)base["subsystems"];
            }
        }
    }
}
