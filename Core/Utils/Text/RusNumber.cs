using System;
using System.Collections.Specialized;
using System.Configuration;
using System.Text;
using System.Xml;

namespace Yaw.Core.Utils.Text
{
    /// <summary>
    /// ����� �������������� ����� � ������� (0..999)
    /// </summary>
    public class RusNumber
    {
        /// <summary>
        /// �����
        /// </summary>
        private static string[] hunds =
		{
			"", "��� ", "������ ", "������ ", "��������� ",
			"������� ", "�������� ", "������� ", "��������� ", "��������� "
		};

        /// <summary>
        /// ������
        /// </summary>
        private static string[] tens =
		{
			"", "������ ", "�������� ", "�������� ", "����� ", "��������� ",
			"���������� ", "��������� ", "����������� ", "��������� "
		};

        /// <summary>
        /// �������������� ����� � ������� (�������������� ������ �� 0 �� 999)
        /// </summary>
        /// <param name="val">�����</param>
        /// <param name="male">������� ���</param>
        /// <param name="one">����</param>
        /// <param name="two">���</param>
        /// <param name="five">����</param>
        /// <returns>����� ��������</returns>
        public static string Str(int val, bool male, string one, string two, string five)
        {
            // ����� �������� �� 1 �� 19
            string[] frac20 =
			{
				"", "���� ", "��� ", "��� ", "������ ", "���� ", "����� ",
				"���� ", "������ ", "������ ", "������ ", "����������� ",
				"���������� ", "���������� ", "������������ ", "���������� ",
				"����������� ", "���������� ", "������������ ", "������������ "
			};

            int num = val % 1000;
            if (0 == num) return "����";
            if (num < 0) throw new ArgumentOutOfRangeException("val", "�������� �� ����� ���� �������������");

            // ������� ���
            if (!male)
            {
                frac20[1] = "���� ";
                frac20[2] = "��� ";
            }

            // ��������� �����
            StringBuilder r = new StringBuilder(hunds[num / 100]);

            // �������
            if (num % 100 < 20)
            {
                r.Append(frac20[num % 100]);
            }
            // �������
            else
            {
                r.Append(tens[num % 100 / 10]);
                r.Append(frac20[num % 10]);
            }

            r.Append(Case(num, one, two, five));

            if (r.Length != 0) r.Append(" ");
            return r.ToString();
        }

        /// <summary>
        /// ������� �����������
        /// </summary>
        /// <param name="val">��������</param>
        /// <param name="one">�������</param>
        /// <param name="two">������/������/��������</param>
        /// <param name="five">������� � ��� ���������</param>
        /// <returns></returns>
        public static string Case(int val, string one, string two, string five)
        {
            int t = (val % 100 > 20) ? val % 10 : val % 20;

            switch (t)
            {
                case 1: return one;
                case 2:
                case 3:
                case 4: return two;
                default: return five;
            }
        }
    }

    /// <summary>
    /// ���������� � ������
    /// </summary>
    public struct CurrencyInfo
    {
        /// <summary>
        /// ������� ���
        /// </summary>
        public bool male;
        /// <summary>
        /// ����
        /// </summary>
        public string seniorOne;
        /// <summary>
        /// ��� (���, ������)
        /// </summary>
        public string seniorTwo;
        /// <summary>
        /// ����
        /// </summary>
        public string seniorFive;
        /// <summary>
        /// ����
        /// </summary>
        public string juniorOne;
        /// <summary>
        /// ���
        /// </summary>
        public string juniorTwo;
        /// <summary>
        /// ����
        /// </summary>
        public string juniorFive;
    }

    /// <summary>
    /// ���������� ������-������
    /// </summary>
    public class RusCurrencySectionHandler : IConfigurationSectionHandler
    {
        /// <summary>
        /// ���-�� ������� :)
        /// </summary>
        /// <param name="parent"></param>
        /// <param name="configContext"></param>
        /// <param name="section"></param>
        /// <returns></returns>
        public object Create(object parent, object configContext, XmlNode section)
        {
            foreach (XmlNode curr in section.ChildNodes)
            {
                if (curr.Name == "currency")
                {
                    XmlNode senior = curr["senior"];
                    XmlNode junior = curr["junior"];
                    RusCurrency.Register(
                        curr.Attributes["code"].InnerText,
                        (curr.Attributes["male"].InnerText == "1"),
                        senior.Attributes["one"].InnerText,
                        senior.Attributes["two"].InnerText,
                        senior.Attributes["five"].InnerText,
                        junior.Attributes["one"].InnerText,
                        junior.Attributes["two"].InnerText,
                        junior.Attributes["five"].InnerText);
                }
            }
            return null;
        }
    };

    /// <summary>
    /// �������������� ������ � �������
    /// </summary>
    public class RusCurrency
    {
        private static readonly HybridDictionary currencies = new HybridDictionary();

        // �����������
        static RusCurrency()
        {
            // �����
            Register("RUR", true, "�����", "�����", "������", "�������", "�������", "������");
            // ����
            Register("EUR", true, "����", "����", "����", "��������", "���������", "����������");
            // ������� (������������)
            Register("USD", true, "������", "�������", "��������", "����", "�����", "������");
        }

        /// <summary>
        /// ������������ ������ (��������� � ������ currencies)
        /// </summary>
        /// <param name="currency">���� ������, ������� ��������� ��������</param>
        /// <param name="male">������� �������� ����</param>
        /// <param name="seniorOne">����</param>
        /// <param name="seniorTwo">���</param>
        /// <param name="seniorFive">����</param>
        /// <param name="juniorOne">����</param>
        /// <param name="juniorTwo">���</param>
        /// <param name="juniorFive">����</param>
        public static void Register(string currency, bool male,
            string seniorOne, string seniorTwo, string seniorFive,
            string juniorOne, string juniorTwo, string juniorFive)
        {
            CurrencyInfo info;
            info.male = male;
            info.seniorOne = seniorOne; info.seniorTwo = seniorTwo; info.seniorFive = seniorFive;
            info.juniorOne = juniorOne; info.juniorTwo = juniorTwo; info.juniorFive = juniorFive;
            currencies.Add(currency, info);
        }

        /// <summary>
        /// ����������� ����� � ������� (�� ��������� ������������ �����)
        /// </summary>
        /// <param name="val">�����</param>
        /// <returns>����� ��������</returns>
        public static string Str(double val)
        {
            return Str(val, "RUR");
        }

        /// <summary>
        /// ����������� ������ � �������
        /// </summary>
        /// <param name="val">�����</param>
        /// <param name="currency">��� ������</param>
        /// <returns>�������</returns>
        public static string Str(double val, string currency)
        {
            if (!currencies.Contains(currency))
                throw new ArgumentOutOfRangeException("currency", "������ \"" + currency + "\" �� ����������������");

            CurrencyInfo info = (CurrencyInfo)currencies[currency];
            return Str(val, info.male,
                info.seniorOne, info.seniorTwo, info.seniorFive,
                info.juniorOne, info.juniorTwo, info.juniorFive);
        }

        /// <summary>
        /// ����������� ������ � �������
        /// </summary>
        /// <param name="val">�����</param>
        /// <param name="male">������� ���</param>
        /// <param name="seniorOne">����</param>
        /// <param name="seniorTwo">���</param>
        /// <param name="seniorFive">����</param>
        /// <param name="juniorOne">����</param>
        /// <param name="juniorTwo">���</param>
        /// <param name="juniorFive">����</param>
        /// <returns>����� ��������</returns>
        public static string Str(double val, bool male,
            string seniorOne, string seniorTwo, string seniorFive,
            string juniorOne, string juniorTwo, string juniorFive)
        {
            bool minus = false;
            if (val < 0) { val = -val; minus = true; }

            int n = (int)val;
            int remainder = (int)((val - n + 0.005) * 100);

            StringBuilder r = new StringBuilder();

            if (0 == n) r.Append("0 ");
            if (n % 1000 != 0)
                r.Append(RusNumber.Str(n, male, seniorOne, seniorTwo, seniorFive));
            else
                r.Append(seniorFive);

            n /= 1000;

            // ������
            r.Insert(0, RusNumber.Str(n, false, "������", "������", "�����"));
            n /= 1000;

            // ��������
            r.Insert(0, RusNumber.Str(n, true, "�������", "��������", "���������"));
            n /= 1000;

            // ���������
            r.Insert(0, RusNumber.Str(n, true, "��������", "���������", "����������"));
            n /= 1000;

            // ���������
            r.Insert(0, RusNumber.Str(n, true, "��������", "���������", "����������"));
            n /= 1000;

            // ����������
            r.Insert(0, RusNumber.Str(n, true, "���������", "����������", "�����������"));
            if (minus) r.Insert(0, "����� ");

            // �������/����� � �.�. (junior)
            r.Append(remainder.ToString("00 "));
            r.Append(RusNumber.Case(remainder, juniorOne, juniorTwo, juniorFive));

            //������ ������ ����� ���������
            r[0] = char.ToUpper(r[0]);

            return r.ToString();
        }
    };

    /// <summary>
    /// ��������� ����� �������������� ����� � ������� � ���������� �� ������������ (10^15)
    /// </summary>
    public class CustomRusNumber
    {
        /// <summary>
        /// ����� ����������� ����� � ���������
        /// </summary>
        /// <param name="val">��������</param>
        /// <param name="male">������� ���</param>
        /// <returns>����� ��������</returns>
        public static string Str(double val, bool male)
        {
            bool minus = false;
            if (val < 0) { val = -val; minus = true; }

            int n = (int)val;
            int remainder = (int)((val - n + 0.005) * 100);

            StringBuilder r = new StringBuilder();

            // ��� 0:
            if (n == 0)
                return "����";

            // 1..999
            if (n % 1000 != 0)
                r.Append(RusNumber.Str(n, male, "", "", ""));

            // ������
            n /= 1000;
            int nCur = n % 1000;	// �������� � ����������� �������
            if (nCur > 0 && nCur < 1000)
                r.Insert(0, RusNumber.Str(nCur, false, "������", "������", "�����"));

            // ��������
            n /= 1000;
            nCur = n % 1000;
            if (nCur > 0 && nCur < 1000)
                r.Insert(0, RusNumber.Str(nCur, true, "�������", "��������", "���������"));

            // ���������
            n /= 1000;
            nCur = n % 1000;
            if (nCur > 0 && nCur < 1000)
                r.Insert(0, RusNumber.Str(nCur, true, "��������", "���������", "����������"));

            // ���������
            n /= 1000;
            nCur = n % 1000;
            if (nCur > 0 && nCur < 1000)
                r.Insert(0, RusNumber.Str(nCur, true, "��������", "���������", "����������"));

            n /= 1000;
            nCur = n % 1000;
            // ������������
            if (nCur > 0 && nCur < 1000)
                r.Insert(0, RusNumber.Str(nCur, true, "�����������", "������������", "�������������"));

            // ���� �������������, �� ������� �����
            if (minus) r.Insert(0, "����� ");

            // ���������� ����� ��������
            return r.ToString();
        }
    }
}