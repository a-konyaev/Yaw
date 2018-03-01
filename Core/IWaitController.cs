using System;
using System.Threading;

namespace Yaw.Core
{
    /// <summary>
    /// Интерфейс контроллера ожидания событий
    /// </summary>
    public interface IWaitController
    {
        /// <summary>
        /// Заснуть на заданное время
        /// </summary>
        /// <param name="timeout"></param>
        void Sleep(TimeSpan timeout);

        /// <summary>
        /// Заснуть на заданное время
        /// </summary>
        /// <param name="millisecondsTimeout"></param>
        void Sleep(int millisecondsTimeout);

        /// <summary>
        /// Ожидать одно событие бесконечно
        /// </summary>
        /// <param name="waitHandle"></param>
        void WaitOne(WaitHandle waitHandle);

        /// <summary>
        /// Ожидать одно событие в течение заданного времени
        /// </summary>
        /// <param name="waitHandle"></param>
        /// <param name="timeout"></param>
        /// <returns></returns>
        bool WaitOne(WaitHandle waitHandle, TimeSpan timeout);

        /// <summary>
        /// Ожидать любое из событий
        /// </summary>
        /// <param name="waitHandles"></param>
        /// <returns></returns>
        int WaitAny(WaitHandle[] waitHandles);

        /// <summary>
        /// Ожидать любое из событий в течение заданного времени
        /// </summary>
        /// <param name="waitHandles"></param>
        /// <param name="timeout"></param>
        /// <returns></returns>
        int WaitAny(WaitHandle[] waitHandles, TimeSpan timeout);

        /// <summary>
        /// Ожидать любое из событий в течение заданного времени
        /// </summary>
        /// <param name="waitHandles"></param>
        /// <param name="millisecondsTimeout"></param>
        /// <returns></returns>
        int WaitAny(WaitHandle[] waitHandles, int millisecondsTimeout);

        /// <summary>
        /// Ожидает первое заданное событие или все другие,
        /// т.е. ожидание окончится, когда сработает событие one или когда сработают сразу все события others
        /// </summary>
        /// <param name="one">первое событие</param>
        /// <param name="others">другие события</param>
        /// <returns>индекс сработавшего события:
        /// 0 - сработало первое событие
        /// 1 - сработали все другие события
        /// WaitHandle.WaitTimeout - во время ожидания произошла ошибка</returns>
        int WaitOneOrAllOthers(WaitHandle one, WaitHandle[] others);
    }
}
