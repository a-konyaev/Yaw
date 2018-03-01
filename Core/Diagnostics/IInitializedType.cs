using System.Configuration;

namespace Yaw.Core.Diagnostics
{
    /// <summary>
    /// Интерфейс класса, который может иметь секцию иницилизации в конфиге
    /// </summary>
    public interface IInitializedType
    {
        /// <summary>
        /// Инициализация фильтра
        /// </summary>
        /// <param name="props"></param>
        void Init(NameValueConfigurationCollection props);
    }
}
