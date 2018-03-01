﻿using System.Configuration;
using System.Xml;

namespace Yaw.Core.Configuration
{
    /// <summary>
    /// Базовый конфиг-элемент с настройками подсистемы
    /// </summary>
    /// <remarks>
    /// Разбор атрибутов SubsystemConfig-а выполняем самостоятельно (см. SubsystemConfigCollection),
    /// но для того, чтобы код работал под Mono необходимо было добавить ConfigurationProperty для св-в,
    /// которые соответствуют атрибутам, при этом указать IsRequired=false, чтобы .net не пытался
    /// самостоятельно распарсить эти атрибуты
    /// </remarks>
    public class SubsystemConfig : ConfigurationElement
    {
		/// <summary>
		/// При вызове SerializeToXmlElement у ApplicationConfig автосвойства не воспринимаются, 
		/// поэтому укажем их явно
		/// </summary>
		/// <param name="writer">писатель</param>
		/// <param name="elementName">имя сериализуемого элемента</param>
		/// <returns>успех операции</returns>
		protected override bool SerializeToXmlElement(XmlWriter writer, string elementName)
		{
			this["name"] = SubsystemName;
			this["type"] = SubsystemTypeName;
			this["logFileFolder"] = LogFileFolder;
			this["traceLevel"] = TraceLevelName;
			this["separateLog"] = SeparateLog;
            this["disposeOrder"] = DisposeOrder;

			return base.SerializeToXmlElement(writer, elementName);
		}

        /// <summary>
        /// Имя подсистемы
        /// </summary>
		[ConfigurationProperty("name", IsRequired = false, IsKey = true)]
		public string SubsystemName
		{
			get;
			set;
		}

		/// <summary>
        /// Тип класса, который содержит реализацию подсистемы
        /// </summary>
		[ConfigurationProperty("type", IsRequired = false)]
		public string SubsystemTypeName
		{
			get;
			set;
		}

        /// <summary>
        /// Папка, в которой логгер подсистемы будет создавать лог-файлы
        /// </summary>
        [ConfigurationProperty("logFileFolder", IsRequired = false)]
        public string LogFileFolder
        {
            get;
            set;
        }

        /// <summary>
        /// Уровень трассировки
        /// </summary>
        [ConfigurationProperty("traceLevel", IsRequired = false)]
        public string TraceLevelName
        {
            get;
            set;
        }

        /// <summary>
        /// Признак необходимости писать логи в отдельный файл
        /// </summary>
        [ConfigurationProperty("separateLog", IsRequired = false, DefaultValue = false)]
        public bool SeparateLog
        {
            get;
            set;
        }

        /// <summary>
        /// Неопределенный порядковый номер остановки подсистемы.
        /// Задается в случае, если атрибут disposeOrder не был задан
        /// </summary>
        public const int DISPOSE_ORDER_UNDEFINED = 1000;

        /// <summary>
        /// Порядковый номер, в соответствие с которым подсистема будет останавливаться
        /// </summary>
        [ConfigurationProperty("disposeOrder", IsRequired = false, DefaultValue = DISPOSE_ORDER_UNDEFINED)]
        public int DisposeOrder
        {
            get;
            set;
        }
    }
}
