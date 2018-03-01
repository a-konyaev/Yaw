using System;
using System.Text.RegularExpressions;

namespace Yaw.Workflow.ComponentModel
{
    /// <summary>
    /// Ключ для определения следующего действия, которое нужно выполнить
    /// </summary>
    [Serializable]
    public class NextActivityKey
    {
        /// <summary>
        /// Ключ для определения следующего действия по умолчанию
        /// </summary>
        public static readonly NextActivityKey DefaultNextActivityKey = 
            new NextActivityKey { Name = "@@Default" };

        /// <summary>
        /// Имя ключа
        /// </summary>
        public string Name
        {
            get;
            protected set;
        }

        /// <summary>
        /// Закрытый конструктор
        /// </summary>
        private NextActivityKey()
        {
        }

        /// <summary>
        /// Конструктор, который валидирует значение ключа
        /// </summary>
        /// <param name="name"></param>
        public NextActivityKey(string name)
        {
            if (string.IsNullOrEmpty(name))
                throw new ArgumentNullException("name", "Не задано имя ключа");

            var regex = new Regex(@"^\w+$");
            if (!regex.IsMatch(name))
                throw new ArgumentException("Имя ключа может содержать только буквы, цифры и '_': " + name);

            Name = name;
        }

        public override bool Equals(object obj)
        {
            if (obj == null)
                return false;

            var other = obj as NextActivityKey;
            if (other == null)
                return false;

            return other.Name.Equals(Name);
        }

        public override int GetHashCode()
        {
            return Name.GetHashCode();
        }

        public override string ToString()
        {
            return Name;
        }
    }
}
