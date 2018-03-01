using System;
using System.Threading;

namespace Yaw.Core.Extensions
{
    /// <summary>
    /// Расширения для Thread
    /// </summary>
    public static class ThreadExtensions
    {
        /// <summary>
        /// Безопасно убивает поток, т.е. гасит все возможные исключения метода Abort
        /// </summary>
        /// <param name="thread">поток, который нужно убить</param>
        public static void SafeAbort(this Thread thread)
        {
            try
            {
                thread.Abort();
            }
            catch
            {
            }
        }

        /// <summary>
        /// Безопасно убивает поток через заданное кол-во миллисекунд,
        /// если поток так и не завершил работу
        /// </summary>
        /// <param name="thread">поток, который нужно убить</param>
        /// <param name="timeout">таймаут, в течение которого ожидается завершение работы потока</param>
        public static void SafeAbort(this Thread thread, TimeSpan timeout)
        {
            if (!thread.Join(timeout))
                thread.SafeAbort();
        }
    }
}
