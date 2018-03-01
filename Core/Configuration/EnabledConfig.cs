using System.Configuration;

namespace Yaw.Core.Configuration
{
    /// <summary>
    /// Конфиг-элемент, который содержит атрибут enabled
    /// </summary>
    public class EnabledConfig : ConfigurationElement
    {
        /// <summary>
        /// Включен или выключен
        /// </summary>
        [ConfigurationProperty("enabled", IsRequired = true)]
        public bool Enabled
        {
            get
            {
                return (bool)this["enabled"];
            }
            set
            {
                this["enabled"] = value;
            }
        }
    }
}
