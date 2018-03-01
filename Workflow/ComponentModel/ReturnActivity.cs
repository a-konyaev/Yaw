using System;

namespace Yaw.Workflow.ComponentModel
{
    /// <summary>
    /// Специальное действие, которое означает, что нужно выполнить выход из составного
    /// действия с заданным результатом (ключом след. действия)
    /// </summary>
    [Serializable]
    public class ReturnActivity : Activity
    {
        private const string NAME_FORMAT_STRING = "@@Return({0})";

        /// <summary>
        /// Результат - ключ след. действия
        /// </summary>
        public NextActivityKey Result
        {
            get;
            private set;
        }

        public ReturnActivity(NextActivityKey result)
        {
            Result = result;
            Name = string.Format(NAME_FORMAT_STRING, result);
            Tracking = false;
        }
    }
}
