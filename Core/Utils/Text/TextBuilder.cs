using System;
using System.Text;

namespace Yaw.Core.Utils.Text
{
	/// <summary>
	/// Аналог <see cref="StringBuilder"/> для формирования текста.
	/// Поддерживает 
	/// </summary>
	public class TextBuilder
	{
		private readonly StringBuilder _builder;
		private int _indent;

		public TextBuilder() : this(new StringBuilder())
		{
		}
        
		public TextBuilder(StringBuilder builder)
		{
			_builder = builder;
		}

		/// <summary>
		/// Добавляет значение в текущую позицию.
		/// </summary>
		/// <param name="sValue">Значение</param>
		/// <returns>Текущий объект</returns>
		public TextBuilder Append(String sValue)
		{
			_builder.Append(sValue);
			return this;
		}

		/// <summary>
		/// Добавляет новую строку с текущим отступом в начале.
		/// </summary>
		/// <param name="sValue">Значение</param>
		/// <returns>Текущий объект</returns>
		public TextBuilder BeginLine(String sValue)
		{
			if (_indent > 0)
				_builder.Append('\t', _indent);
			_builder.Append(sValue);
			return this;
		}

		/// <summary>
		/// Добавляет значение и перенос строки.
		/// </summary>
		/// <param name="sValue">Значение</param>
		/// <returns>Текущий объект</returns>
		public TextBuilder EndLine(String sValue)
		{
			_builder.Append(sValue);
			_builder.Append(Environment.NewLine);
			return this;
		}

		/// <summary>
		/// Добавляет новую строку с текущим отступом в начале и с переносом в конце.
		/// </summary>
		/// <param name="sValue">Значение</param>
		/// <returns>Текущий объект</returns>
		public TextBuilder Line(String sValue)
		{
			if (_indent > 0)
				_builder.Append('\t', _indent);
			_builder.Append(sValue);
			_builder.Append(Environment.NewLine);
			return this;
		}

		/// <summary>
		/// Добавляет новую строку с текущим отступом в начале и переносом в конце.
		/// </summary>
		/// <param name="sValue">Формат-строка</param>
		/// <param name="arg0">Параметр ля формат-строки</param>
		/// <returns>Текущий объект</returns>
		public TextBuilder FormatLine(String sValue, object arg0)
		{
			if (_indent > 0)
				_builder.Append('\t', _indent);
			_builder.AppendFormat(sValue, arg0);
			_builder.Append(Environment.NewLine);
			return this;
		}

		/// <summary>
		/// Добавляет перенос строки.
		/// </summary>
		/// <returns>Текущий объект</returns>
		public TextBuilder EmptyLine()
		{
			_builder.Append(Environment.NewLine);
			return this;
		}

		/// <summary>
		/// Увеличивает текущий отступ.
		/// </summary>
		/// <returns>Текущий объект</returns>
		public TextBuilder IncreaseIndent()
		{
			++_indent;
			return this;
		}

		/// <summary>
		/// Уменьшает текущий отступ.
		/// </summary>
		/// <returns>Текущий объект</returns>
		public TextBuilder DecreaseIndent()
		{
			if (_indent == 0)
				throw new InvalidOperationException("Текущий отступ уже равен 0");
			--_indent;

			return this;
		}

		public override string ToString()
		{
			return _builder.ToString();
		}
	}
}