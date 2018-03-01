using System;

namespace Yaw.Core.Extensions
{
    /// <summary>
    /// Расширения для работы с событиями
    /// </summary>
    public static class EventExtensions
    {
        /// <summary>
        /// Возбуждает событие с пустым аргументом
        /// </summary>
        /// <param name="ev"></param>
        /// <param name="sender"></param>
        public static void RaiseEvent(this EventHandler ev, object sender)
        {
            RaiseEvent(ev, sender, EventArgs.Empty);
        }

        /// <summary>
        /// Возбуждает событие с заданным аргументом
        /// </summary>
        /// <param name="ev"></param>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public static void RaiseEvent(this EventHandler ev, object sender, EventArgs e)
        {
            var handler = ev;
            if (handler != null)
                handler(sender, e);
        }

        /// <summary>
        /// Возбуждает событие с аргументом по умолчанию
        /// </summary>
        /// <typeparam name="TEventArgs"></typeparam>
        /// <param name="ev"></param>
        /// <param name="sender"></param>
        public static void RaiseEvent<TEventArgs>(this EventHandler<TEventArgs> ev, object sender)
            where TEventArgs : EventArgs
        {
            RaiseEvent(ev, sender, default(TEventArgs));
        }

        /// <summary>
        /// Возбуждает событие с заданным аргументом
        /// </summary>
        /// <typeparam name="TEventArgs"></typeparam>
        /// <param name="ev"></param>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public static void RaiseEvent<TEventArgs>(this EventHandler<TEventArgs> ev, object sender, TEventArgs e)
            where TEventArgs : EventArgs
        {
            var handler = ev;
            if (handler != null)
                handler(sender, e);
        }
    }
}
