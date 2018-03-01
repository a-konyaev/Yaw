using System;
using System.Text.RegularExpressions;

namespace Yaw.Core.Utils.Text.RegExpressions
{
    /// <summary>
    /// Атрибут для хранения шаблона регулярного выражения
    /// </summary>
    [AttributeUsage(AttributeTargets.Field)]
    public sealed class RegExpressionAttribute : Attribute
    {
        /// <summary>
        /// Шаблон регулярного выражения
        /// </summary>
        public string Pattern { get; set; }

        /// <summary>
        /// Параметры регулярного выражения
        /// </summary>
        public RegexOptions Options { get; set; }

        /// <summary>
        /// Конструктор
        /// </summary>
        /// <param name="pattern">шаблон регулярного выражения</param>
        public RegExpressionAttribute(string pattern)
        {
            if (string.IsNullOrEmpty(pattern))
                throw new ArgumentNullException("pattern");

            Pattern = pattern;
        }
    }
}
