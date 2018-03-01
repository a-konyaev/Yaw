using System.Configuration;

namespace Yaw.Core.Configuration
{
    /// <summary>
    /// Конфиг-элемент, который содержит атрибут value типа Т
    /// </summary>
    public class ValueConfig<T> : ConfigurationElement
    {
        /// <summary>
        /// Значение
        /// </summary>
        [ConfigurationProperty("value", IsRequired = true)]
        public T Value
        {
            get
            {
                return (T)this["value"];
            }
            set
            {
                this["value"] = value;
            }
        }
    }
}

