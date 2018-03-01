using System.Threading;

namespace Yaw.Core.Utils.Threading
{
    /// <summary>
    /// Расширения для работы с потоками
    /// </summary>
    public static class ThreadUtils
    {
        /// <summary>
        /// Запускает фоновый поток
        /// </summary>
        /// <param name="threadMethod">делегат метода потока</param>
        /// <returns>запущенный поток</returns>
        public static Thread StartBackgroundThread(ThreadStart threadMethod)
        {
            var thread = new Thread(threadMethod) {IsBackground = true};
            thread.Start();
            return thread;
        }

        /// <summary>
        /// Запускает фоновый поток
        /// </summary>
        /// <param name="threadMethod">делегат метода потока</param>
        /// <param name="threadParameter">параметр метода потока</param>
        /// <returns>запущенный поток</returns>
        public static Thread StartBackgroundThread(ParameterizedThreadStart threadMethod, object threadParameter)
        {
            var thread = new Thread(threadMethod) {IsBackground = true};
            thread.Start(threadParameter);
            return thread;
        }
    }
}
