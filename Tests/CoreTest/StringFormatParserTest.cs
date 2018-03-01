using System;
using System.Collections.Generic;
using Yaw.Core.Utils.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Yaw.Tests.CoreTest
{
    /// <summary>
    ///This is a test class for StringFormatParserTest and is intended
    ///to contain all StringFormatParserTest Unit Tests
    ///</summary>
	[TestClass]
	public class StringFormatParserTest
	{
        public TestContext TestContext { get; set; }

		/// <summary>
		///A test for Parse
		///</summary>
		[TestMethod]
		public void ParseTest()
		{
			string format = "{Param1}Text1\tText2{Param2}Text3\r\n{Param1:d} Text4 {{Фигурные собки}} {Param3}";
			
			List<string> keys = new List<string>(); 
			List<string> keysExpected = ExpectedParams();
			
			// ожидаемый результат
			string expected = "{0}Text1\tText2{1}Text3\r\n{0:d} Text4 {{Фигурные собки}} {2}";
			
			var actual = StringFormatParser.Parse(format, out keys);
			// проверим, что правильно сформирован список ключей
			foreach (var key in keys)
			{
				Assert.IsTrue(keysExpected.Contains(key), String.Format("Ключ {0} не содержится в исходной строке", key));
			}
			Assert.AreEqual(3, keys.Count, "Количество полученных ключей не совпадает с реальным количеством ключей в строке");

			// проверим, что результат, совпадает с ожидаемым
			Assert.AreEqual(expected, actual);
		}

		[TestMethod]
		public void EmptyFormatParseTest()
		{
			var list = new List<string>();
			try
			{
				StringFormatParser.Parse(string.Empty, out list);
				// если не произошло исключение
				Assert.Fail("Не произошла ошибка при парсе пустой строки");
			}
			catch (Exception ex)
			{
				// проверим правильност сообщения об ошибке
				Assert.AreEqual("Ошибка разбора строки формата: Строка формата не задана",
									ex.Message, "Несовпадение текста исключения");
			}
		}

		/// <summary>
		/// Проверка исключения при неправильном расположении открывающих {
		/// </summary>
		[TestMethod]
		public void UnexpectedOpenBraceTest()
		{
			string format = "{P{aram1}Text1 {Param2}";
			
			List<string> keys = new List<string>();
			try
			{
				var actual = StringFormatParser.Parse(format, out keys);
				// если не произошло исключение
				Assert.Fail(String.Format("Не произошло исключение при парсе строки {0}", format));
			}
			catch (Exception ex)
			{
				// проверим правильност сообщения об ошибке
				Assert.AreEqual("Ошибка разбора строки формата: Неожиданная открывающая скобка '{' (символ 3)",
									ex.Message, "Несовпадение текста исключения");
			}
		}

		/// <summary>
		/// Проверка исключения при неправильном расположении открывающих }
		/// </summary>
		[TestMethod]
		public void UnexpectedCloseBraceTest()
		{
			string format = "{Pa}r}am1Text1 {Param2}";

			List<string> keys = new List<string>();
			try
			{
				var actual = StringFormatParser.Parse(format, out keys);
				// если не произошло исключение
				Assert.Fail(String.Format("Не произошло исключение при парсе строки {0}", format));
			}
			catch (Exception ex)
			{
				// проверим правильност сообщения об ошибке
				Assert.AreEqual("Ошибка разбора строки формата: Неожиданная закрывающия скобка '}' (символ 6)",
									ex.Message, "Несовпадение текста исключения");
			}
		}

		/// <summary>
		/// Проверка исключения при {}
		/// </summary>
		[TestMethod]
		public void EmptyParameterTest()
		{
			string format = "{} text1";

			List<string> keys = new List<string>();
			try
			{
				var actual = StringFormatParser.Parse(format, out keys);
				// если не произошло исключение
				Assert.Fail(String.Format("Не произошло исключение при парсе строки {0}", format));
			}
			catch (Exception ex)
			{
				// проверим правильност сообщения об ошибке
				Assert.AreEqual("Ошибка разбора строки формата: Пустой ключ (символ 2)",
									ex.Message, "Несовпадение текста исключения");
			}
		}

		/// <summary>
		/// Проверка исключения при отсутствии последней закрывающей
		/// </summary>
		[TestMethod]
		public void NoCloseBraceTest()
		{
			string format = "text1 {Param1";

			List<string> keys = new List<string>();
			try
			{
				var actual = StringFormatParser.Parse(format, out keys);
				// если не произошло исключение
				Assert.Fail(String.Format("Не произошло исключение при парсе строки {0}", format));
			}
			catch (Exception ex)
			{
				// проверим правильност сообщения об ошибке
				Assert.AreEqual("Ошибка разбора строки формата: Нет закрывающей скобки '}' в конце строки",
									ex.Message, "Несовпадение текста исключения");
			}
		}

		/// <summary>
		/// Проверка исключения при последнем символе } без открывающей {
		/// </summary>
		[TestMethod]
		public void NoOpenBraceTest()
		{
			string format = "text1 Param1}";

			List<string> keys = new List<string>();
			try
			{
				var actual = StringFormatParser.Parse(format, out keys);
				// если не произошло исключение
				Assert.Fail(String.Format("Не произошло исключение при парсе строки {0}", format));
			}
			catch (Exception ex)
			{
				// проверим правильност сообщения об ошибке
				Assert.AreEqual("Ошибка разбора строки формата: Неожиданная закрывающия скобка '}' в конце строки",
									ex.Message, "Несовпадение текста исключения");
			}
		}

		/// <summary>
		/// Проверка исключения при {} в конце строки
		/// </summary>
		[TestMethod]
		public void EmptyParameterInTheEndTest()
		{
			string format = "text1{}";

			List<string> keys = new List<string>();
			try
			{
				var actual = StringFormatParser.Parse(format, out keys);
				// если не произошло исключение
				Assert.Fail(String.Format("Не произошло исключение при парсе строки {0}", format));
			}
			catch (Exception ex)
			{
				// проверим правильност сообщения об ошибке
				Assert.AreEqual("Ошибка разбора строки формата: Пустой ключ (символ 7)",
									ex.Message, "Несовпадение текста исключения");
			}
		}

		/// <summary>
		/// Проверка исключения при сочетании }{}
		/// </summary>
		[TestMethod]
		public void UnexpectedCloseBraceBeforeOpenBraceTest()
		{
			string format = "{Param1}t}{}ext1}";

			List<string> keys = new List<string>();
			try
			{
				var actual = StringFormatParser.Parse(format, out keys);
				// если не произошло исключение
				Assert.Fail(String.Format("Не произошло исключение при парсе строки {0}", format));
			}
			catch (Exception ex)
			{
				// проверим правильност сообщения об ошибке
				Assert.AreEqual("Ошибка разбора строки формата: Неожиданная закрывающая скобка '}' (символ 10)",
									ex.Message, "Несовпадение текста исключения");
			}
		}

		/// <summary>
		/// Формирует список параметров, которые должен создать парсер после выполнения теста
		/// </summary>
		/// <returns>Список параметров</returns>
		private List<string> ExpectedParams()
		{
			List<string> result = new List<string>();

			result.Add("Param1");
			result.Add("Param2");
			result.Add("Param3");

			return result;
		}
	}
}
