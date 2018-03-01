using Yaw.Core.Diagnostics;

namespace Yaw.Tests.CoreTest.Helpers
{
	public class TestEventFilter : IEventFilter
	{
		#region IEventFilter Members

		/// <summary>
		/// Всегда принято
		/// </summary>
		/// <param name="logEvent">событие лога</param>
		/// <returns>true</returns>
		public bool Accepted(LoggerEvent logEvent)
		{
			return true;
		}

		#endregion

		#region IInitializedType Members

		/// <summary>
		/// Ничего не делаем
		/// </summary>
		/// <param name="props"></param>
		public void Init(System.Configuration.NameValueConfigurationCollection props)
		{
		}

		#endregion
	}
}
