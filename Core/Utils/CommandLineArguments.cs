using System;
using System.Collections.Specialized;
using System.Text.RegularExpressions;

namespace Yaw.Core.Utils
{
    /// <summary>
    /// Класс разбора командной строки.
    /// Поддерживает следующий формат параметров: <para><c>[-[-]|/]NAME[[=|:]VALUE]</c></para>
    /// Например:
    /// <c>
    /// <para>--help</para>
    /// <para>-?</para>
    /// <para>/?</para>
    /// <para>help</para>
    /// <para>/config=value</para>
    /// <para>--c=value</para>
    /// <para>-c:value</para>
    /// </c>
    /// Имена всех параметров уникальны, т.е. если указано <para><c>-c=1 -c=2</c>,</para>то значением параметра 
    /// <c>c</c> будет <c>2</c> (т.е. последнее указанное значение)
    /// </summary>
    public class CommandLineArguments : StringDictionary
    {
        /// <summary>
        /// Производит разбор командной строки
        /// </summary>
        public CommandLineArguments()
        {
            // регулярное выражение для разбора параметров
            var re = new Regex("(?:-{1,2}|/)([^=:]+)(?:=|:)?(.*)", RegexOptions.Multiline | RegexOptions.IgnoreCase);

            foreach (var arg in Environment.GetCommandLineArgs())
            {
                var match = re.Match(arg);

                if (match.Success)
                {
                    if (ContainsKey(match.Groups[1].Value))
                    {
                        // если параметр уже есть, то перепишем его значение
                        this[match.Groups[1].Value] = match.Groups[2].Value;
                    }
                    else
                    {
                        // добавляем новый параметр
                        Add(match.Groups[1].Value, match.Groups[2].Value);
                    }
                }
                else
                {
                    // есди нет совпадения, то считаем, что это просто 
                    // параметр, представленный своим именем
                    if (!ContainsKey(arg))
                    {
                        Add(arg, "");
                    }
                }
            }
        }
    }
}
