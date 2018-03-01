using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text.RegularExpressions;

namespace Yaw.Core.Utils.Text.RegExpressions
{
    /// <summary>
    /// Класс для компиляции регулярных выражений в отдельную сборку
    /// </summary>
    public static class RegExpressionsCompiler
    {
        /// <summary>
        /// Компилирует регулярные выражения в сборку
        /// </summary>
        /// <param name="enumType">тип перечисления</param>
        /// <param name="assemblyName">имя сборки</param>
        /// <param name="assemblyVersion">версия сборки</param>
        public static void Compile(Type enumType, string assemblyName, string assemblyVersion)
        {
            var compilationList = new List<RegexCompilationInfo>();
            foreach (var name in Enum.GetNames(enumType))
            {
                try
                {
                    var field = enumType.GetField(
                    name, BindingFlags.Static | BindingFlags.GetField | BindingFlags.Public)
                    .GetCustomAttributes(true).OfType<RegExpressionAttribute>().First();

                    var expr = new RegexCompilationInfo(
                        field.Pattern,
                        RegexOptions.CultureInvariant | field.Options,
                        name,
                        assemblyName,
                        true);

                    compilationList.Add(expr);
                }
                catch
                {
                    Console.WriteLine("Не найден шаблон регулярного выражения для элемента " + name);
                }
            }

            if (compilationList.Count == 0)
                throw new Exception(string.Format(
                    "Перечисление '{0}' не содержит ни одного регулярного выражения", enumType.Name));

            try
            {
                var ctor = typeof(AssemblyTitleAttribute).GetConstructor(new[] { typeof(string) });
                var attBuilder = new[] { new CustomAttributeBuilder(ctor, new[] { assemblyName }) };
                var assemName = new AssemblyName(
                    string.Format("{0}, Version={1}, Culture=neutral, PublicKeyToken=null",
                    assemblyName, assemblyVersion));

                Regex.CompileToAssembly(compilationList.ToArray(), assemName, attBuilder);
            }
            catch (Exception ex)
            {
                throw new Exception("Ошибка компиляции сборки с регулярными выражениями", ex);
            }
        }
    }
}
