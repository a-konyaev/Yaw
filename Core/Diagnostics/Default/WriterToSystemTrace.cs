using System.Configuration;
using System.Diagnostics;

namespace Yaw.Core.Diagnostics.Default
{
    /// <summary>
    /// Писатель в системный трейс
    /// </summary>
    public sealed class SystemTraceWriter : IEventWriter
    {
        public SystemTraceWriter()
        {
            Trace.AutoFlush = true;
        }

        public void Write(string uniqueLogId, string message)
        {
            Trace.WriteLine(message);
            Trace.Flush();
        }

        public void Init(NameValueConfigurationCollection props)
        {
            // ничего не делаем
        }
    }
}
