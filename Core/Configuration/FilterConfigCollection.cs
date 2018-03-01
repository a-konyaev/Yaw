using System.Configuration;

namespace Yaw.Core.Configuration
{
    /// <summary>
    /// Конфигурация фильтров протоколирования
    /// </summary>
    public class FilterConfigCollection : ConfigurationElementCollection
    {
        protected override ConfigurationElement CreateNewElement()
        {
            return new FilterConfig();
        }

        protected override object GetElementKey(ConfigurationElement element)
        {
            return ((FilterConfig)element).TypeName;
        }
    }
}
