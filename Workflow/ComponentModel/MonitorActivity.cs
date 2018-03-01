using System;

namespace Yaw.Workflow.ComponentModel
{
    /// <summary>
    /// Абстрактное действие, которое используется для работы с блокировками
    /// </summary>
    [Serializable]
    public abstract class MonitorActivity : Activity
    {
        /// <summary>
        /// Имя блокировки
        /// </summary>
        public string LockName
        {
            get;
            set;
        }

        protected MonitorActivity()
        {
            Tracking = false;
        }
    }
}
