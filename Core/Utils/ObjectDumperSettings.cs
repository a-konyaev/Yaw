using System;
using System.Collections.Generic;

namespace Yaw.Core.Utils
{
	/// <summary>
	/// Настройки для <see cref="ObjectDumper"/> 
	/// </summary>
	public class ObjectDumperSettings
	{
		/// <summary>
		/// Настройки, используемые по умолчанию
		/// </summary>
		public static ObjectDumperSettings Default = new ObjectDumperSettings();
        
		private IEnumerable<String> m_propsToIgnore;

		/// <summary>
		/// Наименования свойств, которые явно нужно проигнорировать
		/// </summary>
		public IEnumerable<String> PropsToIgnore
		{
			get { return m_propsToIgnore ?? new String[0]; }
			set { m_propsToIgnore = value; }
		}

		/// <summary>
		/// Не использовать метод <see cref="Object.ToString"/> для объектов,
		/// чей тип совпадает с типом выводимого объекта.
		/// Для случаем использования <see cref="ObjectDumper"/> из реализации ToString() 
		/// (чтобы не было бесконечной рекурсии).
		/// </summary>
		public Boolean DoNotUseToStringMethod;

		/// <summary>
		/// Максимальная глубина вложения выводимых объектов
		/// </summary>
		public Int32 MaxDepth = 3;

		/// <summary>
		/// Максимальное значение выводимых свойств для объекта.
		/// Если свойств больше, то объект выводится через метод <see cref="Object.ToString"/>
		/// </summary>
		public Int32 MaxProps = 10;

		/// <summary>
		/// Максимальное значение выводимых элементов в коллекция.
		/// </summary>
		public Int32 MaxEnumerableItems = 100;

        /// <summary>
        /// Разделитель элементов перечисления
        /// </summary>
	    public string EnumerableDelimiter = ",";
	}
}