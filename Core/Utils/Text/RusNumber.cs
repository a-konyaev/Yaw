using System;
using System.Collections.Specialized;
using System.Configuration;
using System.Text;
using System.Xml;

namespace Yaw.Core.Utils.Text
{
    /// <summary>
    /// Класс преобразования числа в пропись (0..999)
    /// </summary>
    public class RusNumber
    {
        /// <summary>
        /// Сотни
        /// </summary>
        private static string[] hunds =
		{
			"", "сто ", "двести ", "триста ", "четыреста ",
			"пятьсот ", "шестьсот ", "семьсот ", "восемьсот ", "девятьсот "
		};

        /// <summary>
        /// Тысячи
        /// </summary>
        private static string[] tens =
		{
			"", "десять ", "двадцать ", "тридцать ", "сорок ", "пятьдесят ",
			"шестьдесят ", "семьдесят ", "восемьдесят ", "девяносто "
		};

        /// <summary>
        /// Преобразование числа в пропись (поддерживается только от 0 до 999)
        /// </summary>
        /// <param name="val">число</param>
        /// <param name="male">мужской род</param>
        /// <param name="one">один</param>
        /// <param name="two">два</param>
        /// <param name="five">пять</param>
        /// <returns>число прописью</returns>
        public static string Str(int val, bool male, string one, string two, string five)
        {
            // числа прописью от 1 до 19
            string[] frac20 =
			{
				"", "один ", "два ", "три ", "четыре ", "пять ", "шесть ",
				"семь ", "восемь ", "девять ", "десять ", "одиннадцать ",
				"двенадцать ", "тринадцать ", "четырнадцать ", "пятнадцать ",
				"шестнадцать ", "семнадцать ", "восемнадцать ", "девятнадцать "
			};

            int num = val % 1000;
            if (0 == num) return "ноль";
            if (num < 0) throw new ArgumentOutOfRangeException("val", "Параметр не может быть отрицательным");

            // Женский род
            if (!male)
            {
                frac20[1] = "одна ";
                frac20[2] = "две ";
            }

            // Добавляем сотни
            StringBuilder r = new StringBuilder(hunds[num / 100]);

            // Десятки
            if (num % 100 < 20)
            {
                r.Append(frac20[num % 100]);
            }
            // Единицы
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
        /// Функция преобразует
        /// </summary>
        /// <param name="val">значение</param>
        /// <param name="one">единица</param>
        /// <param name="two">двойка/тройка/четверка</param>
        /// <param name="five">пятерка и все остальные</param>
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
    /// Информация о валюте
    /// </summary>
    public struct CurrencyInfo
    {
        /// <summary>
        /// Мужской род
        /// </summary>
        public bool male;
        /// <summary>
        /// Один
        /// </summary>
        public string seniorOne;
        /// <summary>
        /// Два (три, четыре)
        /// </summary>
        public string seniorTwo;
        /// <summary>
        /// Пять
        /// </summary>
        public string seniorFive;
        /// <summary>
        /// Одна
        /// </summary>
        public string juniorOne;
        /// <summary>
        /// Две
        /// </summary>
        public string juniorTwo;
        /// <summary>
        /// Пять
        /// </summary>
        public string juniorFive;
    }

    /// <summary>
    /// Обработчик конфиг-секции
    /// </summary>
    public class RusCurrencySectionHandler : IConfigurationSectionHandler
    {
        /// <summary>
        /// Что-то создает :)
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
    /// Преобразование валюты в пропись
    /// </summary>
    public class RusCurrency
    {
        private static readonly HybridDictionary currencies = new HybridDictionary();

        // Конструктор
        static RusCurrency()
        {
            // Рубли
            Register("RUR", true, "рубль", "рубля", "рублей", "копейка", "копейки", "копеек");
            // Евро
            Register("EUR", true, "евро", "евро", "евро", "евроцент", "евроцента", "евроцентов");
            // Доллары (американские)
            Register("USD", true, "доллар", "доллара", "долларов", "цент", "цента", "центов");
        }

        /// <summary>
        /// Регистрирует валюту (добавляет в массив currencies)
        /// </summary>
        /// <param name="currency">ключ записи, которую требуется добавить</param>
        /// <param name="male">признак мужского рода</param>
        /// <param name="seniorOne">один</param>
        /// <param name="seniorTwo">два</param>
        /// <param name="seniorFive">пять</param>
        /// <param name="juniorOne">одна</param>
        /// <param name="juniorTwo">две</param>
        /// <param name="juniorFive">пять</param>
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
        /// Преобразует число в пропись (по умолчанию используются рубли)
        /// </summary>
        /// <param name="val">число</param>
        /// <returns>число прописью</returns>
        public static string Str(double val)
        {
            return Str(val, "RUR");
        }

        /// <summary>
        /// Преобразует валюту в пропись
        /// </summary>
        /// <param name="val">число</param>
        /// <param name="currency">вид валюты</param>
        /// <returns>пропись</returns>
        public static string Str(double val, string currency)
        {
            if (!currencies.Contains(currency))
                throw new ArgumentOutOfRangeException("currency", "Валюта \"" + currency + "\" не зарегистрирована");

            CurrencyInfo info = (CurrencyInfo)currencies[currency];
            return Str(val, info.male,
                info.seniorOne, info.seniorTwo, info.seniorFive,
                info.juniorOne, info.juniorTwo, info.juniorFive);
        }

        /// <summary>
        /// Преобразует валюту в пропись
        /// </summary>
        /// <param name="val">число</param>
        /// <param name="male">мужской род</param>
        /// <param name="seniorOne">один</param>
        /// <param name="seniorTwo">два</param>
        /// <param name="seniorFive">пять</param>
        /// <param name="juniorOne">одна</param>
        /// <param name="juniorTwo">две</param>
        /// <param name="juniorFive">пять</param>
        /// <returns>число прописью</returns>
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

            // тысячи
            r.Insert(0, RusNumber.Str(n, false, "тысяча", "тысячи", "тысяч"));
            n /= 1000;

            // миллионы
            r.Insert(0, RusNumber.Str(n, true, "миллион", "миллиона", "миллионов"));
            n /= 1000;

            // миллиарды
            r.Insert(0, RusNumber.Str(n, true, "миллиард", "миллиарда", "миллиардов"));
            n /= 1000;

            // триллионы
            r.Insert(0, RusNumber.Str(n, true, "триллион", "триллиона", "триллионов"));
            n /= 1000;

            // триллиарды
            r.Insert(0, RusNumber.Str(n, true, "триллиард", "триллиарда", "триллиардов"));
            if (minus) r.Insert(0, "минус ");

            // копейки/центы и т.п. (junior)
            r.Append(remainder.ToString("00 "));
            r.Append(RusNumber.Case(remainder, juniorOne, juniorTwo, juniorFive));

            //Делаем первую букву заглавной
            r[0] = char.ToUpper(r[0]);

            return r.ToString();
        }
    };

    /// <summary>
    /// Кастомный класс преобразования число в пропись с поддержкой до квадриллиона (10^15)
    /// </summary>
    public class CustomRusNumber
    {
        /// <summary>
        /// Метод преобразует числа в прописные
        /// </summary>
        /// <param name="val">значение</param>
        /// <param name="male">мужской род</param>
        /// <returns>число прописью</returns>
        public static string Str(double val, bool male)
        {
            bool minus = false;
            if (val < 0) { val = -val; minus = true; }

            int n = (int)val;
            int remainder = (int)((val - n + 0.005) * 100);

            StringBuilder r = new StringBuilder();

            // для 0:
            if (n == 0)
                return "ноль";

            // 1..999
            if (n % 1000 != 0)
                r.Append(RusNumber.Str(n, male, "", "", ""));

            // тысячи
            n /= 1000;
            int nCur = n % 1000;	// значение в исследуемом разряде
            if (nCur > 0 && nCur < 1000)
                r.Insert(0, RusNumber.Str(nCur, false, "тысяча", "тысячи", "тысяч"));

            // миллионы
            n /= 1000;
            nCur = n % 1000;
            if (nCur > 0 && nCur < 1000)
                r.Insert(0, RusNumber.Str(nCur, true, "миллион", "миллиона", "миллионов"));

            // миллиарды
            n /= 1000;
            nCur = n % 1000;
            if (nCur > 0 && nCur < 1000)
                r.Insert(0, RusNumber.Str(nCur, true, "миллиард", "миллиарда", "миллиардов"));

            // триллионы
            n /= 1000;
            nCur = n % 1000;
            if (nCur > 0 && nCur < 1000)
                r.Insert(0, RusNumber.Str(nCur, true, "триллион", "триллиона", "триллионов"));

            n /= 1000;
            nCur = n % 1000;
            // квадриллионы
            if (nCur > 0 && nCur < 1000)
                r.Insert(0, RusNumber.Str(nCur, true, "квадриллион", "квадриллиона", "квадриллионов"));

            // Если отрицательное, то добавим минус
            if (minus) r.Insert(0, "минус ");

            // возвращаем число прописью
            return r.ToString();
        }
    }
}