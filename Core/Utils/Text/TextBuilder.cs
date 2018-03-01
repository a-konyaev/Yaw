using System;
using System.Text;

namespace Yaw.Core.Utils.Text
{
	/// <summary>
	/// ������ <see cref="StringBuilder"/> ��� ������������ ������.
	/// ������������ 
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
		/// ��������� �������� � ������� �������.
		/// </summary>
		/// <param name="sValue">��������</param>
		/// <returns>������� ������</returns>
		public TextBuilder Append(String sValue)
		{
			_builder.Append(sValue);
			return this;
		}

		/// <summary>
		/// ��������� ����� ������ � ������� �������� � ������.
		/// </summary>
		/// <param name="sValue">��������</param>
		/// <returns>������� ������</returns>
		public TextBuilder BeginLine(String sValue)
		{
			if (_indent > 0)
				_builder.Append('\t', _indent);
			_builder.Append(sValue);
			return this;
		}

		/// <summary>
		/// ��������� �������� � ������� ������.
		/// </summary>
		/// <param name="sValue">��������</param>
		/// <returns>������� ������</returns>
		public TextBuilder EndLine(String sValue)
		{
			_builder.Append(sValue);
			_builder.Append(Environment.NewLine);
			return this;
		}

		/// <summary>
		/// ��������� ����� ������ � ������� �������� � ������ � � ��������� � �����.
		/// </summary>
		/// <param name="sValue">��������</param>
		/// <returns>������� ������</returns>
		public TextBuilder Line(String sValue)
		{
			if (_indent > 0)
				_builder.Append('\t', _indent);
			_builder.Append(sValue);
			_builder.Append(Environment.NewLine);
			return this;
		}

		/// <summary>
		/// ��������� ����� ������ � ������� �������� � ������ � ��������� � �����.
		/// </summary>
		/// <param name="sValue">������-������</param>
		/// <param name="arg0">�������� �� ������-������</param>
		/// <returns>������� ������</returns>
		public TextBuilder FormatLine(String sValue, object arg0)
		{
			if (_indent > 0)
				_builder.Append('\t', _indent);
			_builder.AppendFormat(sValue, arg0);
			_builder.Append(Environment.NewLine);
			return this;
		}

		/// <summary>
		/// ��������� ������� ������.
		/// </summary>
		/// <returns>������� ������</returns>
		public TextBuilder EmptyLine()
		{
			_builder.Append(Environment.NewLine);
			return this;
		}

		/// <summary>
		/// ����������� ������� ������.
		/// </summary>
		/// <returns>������� ������</returns>
		public TextBuilder IncreaseIndent()
		{
			++_indent;
			return this;
		}

		/// <summary>
		/// ��������� ������� ������.
		/// </summary>
		/// <returns>������� ������</returns>
		public TextBuilder DecreaseIndent()
		{
			if (_indent == 0)
				throw new InvalidOperationException("������� ������ ��� ����� 0");
			--_indent;

			return this;
		}

		public override string ToString()
		{
			return _builder.ToString();
		}
	}
}