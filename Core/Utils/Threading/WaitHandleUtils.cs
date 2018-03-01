using System.Threading;
using Yaw.Core.Extensions;

namespace Yaw.Core.Utils.Threading
{
    /// <summary>
    /// Расширения для WaitHandle
    /// </summary>
    public static class WaitHandleUtils
    {
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
        public static int WaitOneOrAllOthers(WaitHandle one, WaitHandle[] others)
        {
            CodeContract.Requires(one != null);
            CodeContract.Requires(others != null && others.Length > 1);

            var occurredEventIndex = WaitHandle.WaitTimeout;
            var eventSignaled = new ManualResetEvent(false);

            var waitOneThread = ThreadUtils.StartBackgroundThread(
                () =>
                    {
                        try
                        {
                            one.WaitOne();
                            occurredEventIndex = 0;   
                        }
                        finally
                        {
                            eventSignaled.Set();
                        }
                    });

            var waitOthersThread = ThreadUtils.StartBackgroundThread(
                () =>
                    {
                        try
                        {
                            WaitHandle.WaitAll(others);
                            occurredEventIndex = 1;
                        }
                        finally
                        {
                            eventSignaled.Set();
                        }
                    });

            // ждем, когда какой-нибудь поток завершит ожидание своих событий
            eventSignaled.WaitOne();

            // убиваем потоки, чтобы не ждали больше
            waitOneThread.SafeAbort();
            waitOthersThread.SafeAbort();
            
            return occurredEventIndex;
        }

        /// <summary>
        /// Ожидает первое событие или второе событие или все другие,
        /// т.е. ожидание окончится, когда сработает событие one или two или когда сработают сразу все события others
        /// </summary>
        /// <param name="one">первое событие</param>
        /// <param name="two">второе событие</param>
        /// <param name="others">другие события</param>
        /// <returns>индекс сработавшего события:
        /// 0 - сработало первое событие
        /// 1 - сработало второе событие
        /// 2 - сработали все другие события
        /// WaitHandle.WaitTimeout - во время ожидания произошла ошибка</returns>
        public static int WaitOneOrTwoOrAllOthers(WaitHandle one, WaitHandle two, WaitHandle[] others)
        {
            CodeContract.Requires(one != null);
            CodeContract.Requires(two != null);
            CodeContract.Requires(others != null && others.Length > 1);

            var occurredEventIndex = WaitHandle.WaitTimeout;
            var eventSignaled = new ManualResetEvent(false);

            var waitOneThread = ThreadUtils.StartBackgroundThread(
                () =>
                {
                    try
                    {
                        one.WaitOne();
                        occurredEventIndex = 0;
                    }
                    finally
                    {
                        eventSignaled.Set();
                    }
                });

            var waitTwoThread = ThreadUtils.StartBackgroundThread(
                () =>
                {
                    try
                    {
                        two.WaitOne();
                        occurredEventIndex = 1;
                    }
                    finally
                    {
                        eventSignaled.Set();
                    }
                });

            var waitOthersThread = ThreadUtils.StartBackgroundThread(
                () =>
                {
                    try
                    {
                        WaitHandle.WaitAll(others);
                        occurredEventIndex = 2;
                    }
                    finally
                    {
                        eventSignaled.Set();
                    }
                });

            // ждем, когда какой-нибудь поток завершит ожидание своих событий
            eventSignaled.WaitOne();

            // убиваем потоки, чтобы не ждали больше
            waitOneThread.SafeAbort();
            waitTwoThread.SafeAbort();
            waitOthersThread.SafeAbort();

            return occurredEventIndex;
        }
    }
}
