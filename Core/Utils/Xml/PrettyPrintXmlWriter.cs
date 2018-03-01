using System.IO;
using System.Text;
using System.Xml;

namespace Yaw.Core.Utils.Xml
{
	/// <summary>
	/// Класс формирования xml с красивым выравниванием
	/// </summary>
	public class PrettyPrintXmlWriter : XmlTextWriter
	{
		/// <summary>
		/// Создает xmlWriter с красивым форматированием
		/// </summary>
		/// <param name="stream">поток для записи</param>
		public PrettyPrintXmlWriter(Stream stream)
			: base(stream, Encoding.Unicode)
		{
			Formatting = Formatting.Indented;
			Indentation = 4;
			QuoteChar = '\'';
		}

		/// <summary>
		/// Возвращает xml, содержащийся в объекте в виде строки
		/// </summary>
		/// <returns>строка с сформированным xml</returns>
		public string ToFormatString()
		{
			// выведем все в поток
			Flush();
			// выставим позицию исходного потока в начало
			BaseStream.Position = 0;

			// прочитаем получившийся xml и вернем его как строку
			using (var streamReader = new StreamReader(BaseStream))
			{
				var resXml = streamReader.ReadToEnd();
				return resXml;
			}
		}
	}
}
