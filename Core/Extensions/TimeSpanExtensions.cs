using System;

namespace Yaw.Core.Extensions
{
    /// <summary>
    /// Расширения для TimeSpan
    /// </summary>
    public static class TimeSpanExtensions
    {
        /// <summary>
        /// Округлить кол-во минут в зависимости от кол-ва секунд
        /// </summary>
        /// <param name="time">временной интервал, который нужно округлить</param>
        /// <returns>результат округления</returns>
        public static TimeSpan RoundMinutes(this TimeSpan time)
        {
            return new TimeSpan(
                time.Days,
                time.Hours,
                time.Seconds >= 30 ? time.Minutes + 1 : time.Minutes,
                0,
                0);
        }
    }
}
