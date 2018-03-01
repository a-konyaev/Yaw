namespace Yaw.Core.Extensions
{
    /// <summary>
    /// Вспомогательный класс для хранения тройки значений
    /// </summary>
    public class Triplet<TFirst, TSecond, TThird>
    {
        /// <summary>
        /// Первое значение
        /// </summary>
        public TFirst First
        {
            get;
            private set;
        }

        /// <summary>
        /// Второе значение
        /// </summary>
        public TSecond Second
        {
            get;
            private set;
        }

        /// <summary>
        /// Третье значение
        /// </summary>
        public TThird Third
        {
            get;
            private set;
        }

        public Triplet(TFirst first, TSecond second, TThird third)
        {
            First = first;
            Second = second;
            Third = third;
        }
    }
}
