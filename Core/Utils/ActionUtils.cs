using System;
using System.Threading;

namespace Yaw.Core.Utils
{
    /// <summary>
    /// Содержит вспомогательные методы для выполнения различных действий
    /// </summary>
    public static class ActionUtils
    {
        /// <summary>
        /// Выполнить действие за несколько попыток
        /// </summary>
        /// <param name="action">действие</param>
        /// <param name="maxTryCount">максимальное кол-во попыток</param>
        /// <param name="delayBetweenAttempts">задержка между попытками</param>
        public static void DoSeveralAttempts(Action action, int maxTryCount, TimeSpan delayBetweenAttempts)
        {
            DoSeveralAttempts(action, maxTryCount, delayBetweenAttempts, null);
        }

        /// <summary>
        /// Выполнить действие за несколько попыток
        /// </summary>
        /// <param name="action">действие</param>
        /// <param name="maxTryCount">максимальное кол-во попыток</param>
        /// <param name="delayBetweenAttempts">задержка между попытками</param>
        /// <param name="stopEvent">событие о необходимости прервать попытки выполнить действие</param>
        /// <returns>true - действие выполнено, false - попытки выполнить действие были прерваны</returns>
        public static bool DoSeveralAttempts(Action action, int maxTryCount, TimeSpan delayBetweenAttempts, WaitHandle stopEvent)
        {
            var tryCount = 0;
            while (true)
            {
                try
                {
                    action();
                    return true;
                }
                catch (Exception)
                {
                    // если все попытки израсходованы
                    if (++tryCount == maxTryCount)
                        // то пропускаем исключение
                        throw;

                    // если событие прекращения выполнения попыток не задано
                    if (stopEvent == null)
                    {
                        // просто спим некоторое время
                        Thread.Sleep(delayBetweenAttempts);
                    }
                    // иначе, ждем событие
                    else if (stopEvent.WaitOne(delayBetweenAttempts))
                    {
                        // событие случилось => выходим
                        return false;
                    }

                    // идем на след. попытку
                    continue;
                }
            }
        }
    }
}
