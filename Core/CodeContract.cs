using System;

namespace Yaw.Core
{
    /// <summary>
    /// ��������� ����� ��� ���������� ���������� ������������ � .NET 3.5.
    /// TODO: �������� ������������� ������ �� ������� �� .NET 4.0
    /// </summary>
    public static class CodeContract
    {
        public static void Requires(bool condition)
        {
            if (!condition)
            {
                var str = (new System.Diagnostics.StackTrace()).ToString();
                str = str.Substring(str.IndexOf(Environment.NewLine) + 5);
                str = str.Substring(0, str.IndexOf(Environment.NewLine));
                throw new ArgumentException("��������� ���������: " + str);
            }
        }
    }
}
