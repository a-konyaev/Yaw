using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;

namespace Yaw.Core.Configuration
{
    /// <summary>
    /// Коллекция конфиг-элементов типа ValueConfig
    /// </summary>
    public class ValueConfigCollection<T> : ConfigurationElementCollection
    {
        protected override ConfigurationElement CreateNewElement()
        {
            return new ValueConfig<T>();
        }

        protected override Object GetElementKey(ConfigurationElement element)
        {
            return ((ValueConfig<T>)element).Value;
        }

        /// <summary>
        /// Получить список значений конфиг-элементов, которые входят в коллекцию
        /// </summary>
        /// <returns></returns>
        public List<T> ToList()
        {
            return new List<T>(from ValueConfig<T> item in this select item.Value);
        }
    }
}
