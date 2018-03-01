using System.Configuration;
using Yaw.Core.Diagnostics;
using System.Text;

namespace Yaw.Tests.CoreTest.Helpers
{
	public class TestFileSystemWriter : IEventFileSystemWriter
	{
		public void Init(NameValueConfigurationCollection props)
		{
		}

		public void Write(string uniqueLogId, string message)
		{
			Lines.AppendLine(message);
		}

		public string GetPoint(string uniqueId)
		{
			return "TestWriterPoint";
		}

		/// <summary>
		/// Строки с записанной информацией
		/// </summary>
		public StringBuilder Lines = new StringBuilder();
	}
}
