using System;
using System.Collections.Generic;

namespace Yaw.Core.Utils
{
	/// <summary>
	/// ��������� ��� <see cref="ObjectDumper"/> 
	/// </summary>
	public class ObjectDumperSettings
	{
		/// <summary>
		/// ���������, ������������ �� ���������
		/// </summary>
		public static ObjectDumperSettings Default = new ObjectDumperSettings();
        
		private IEnumerable<String> m_propsToIgnore;

		/// <summary>
		/// ������������ �������, ������� ���� ����� ���������������
		/// </summary>
		public IEnumerable<String> PropsToIgnore
		{
			get { return m_propsToIgnore ?? new String[0]; }
			set { m_propsToIgnore = value; }
		}

		/// <summary>
		/// �� ������������ ����� <see cref="Object.ToString"/> ��� ��������,
		/// ��� ��� ��������� � ����� ���������� �������.
		/// ��� ������� ������������� <see cref="ObjectDumper"/> �� ���������� ToString() 
		/// (����� �� ���� ����������� ��������).
		/// </summary>
		public Boolean DoNotUseToStringMethod;

		/// <summary>
		/// ������������ ������� �������� ��������� ��������
		/// </summary>
		public Int32 MaxDepth = 3;

		/// <summary>
		/// ������������ �������� ��������� ������� ��� �������.
		/// ���� ������� ������, �� ������ ��������� ����� ����� <see cref="Object.ToString"/>
		/// </summary>
		public Int32 MaxProps = 10;

		/// <summary>
		/// ������������ �������� ��������� ��������� � ���������.
		/// </summary>
		public Int32 MaxEnumerableItems = 100;

        /// <summary>
        /// ����������� ��������� ������������
        /// </summary>
	    public string EnumerableDelimiter = ",";
	}
}