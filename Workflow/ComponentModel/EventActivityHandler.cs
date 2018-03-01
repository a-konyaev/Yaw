using System;
using Yaw.Core;
using Yaw.Workflow.Runtime;

namespace Yaw.Workflow.ComponentModel
{
    /// <summary>
    /// Обработчик события, где в качестве обработчиков указан список действий,
    /// которые должны начать выполняться при возникновении события
    /// </summary>
    /// <remarks>
    /// Действия-обработчики делатся на
    /// 1) Синхронный обработчик - это может быть только одно действие. При возникновении события, если
    /// синхронный обработчик задан, то выполнение основного потока работ прерывается и управление передается 
    /// в данное действие-обработчик
    /// 2) Асинхронные обработчики - это список действий действий. При возникновении события основной поток работ
    /// продолжит выполняться, а для каждого действия-обработчика будет создан дополнительный поток поток работ,
    /// которые будут работать параллельно с основным потоком работ. 
    /// При этом трекинг для этих потоков работ будет отключен.
    /// </remarks>
    [Serializable]
    internal class EventActivityHandler
    {
        /// <summary>
        /// Экземпляр потока работ
        /// </summary>
        [NonSerialized]
        private WorkflowInstance _workflowInstance;

        /// <summary>
        /// Делегат метода-обработчика события
        /// </summary>
        public EventHandler Method
        {
            get;
            private set;
        }

        /// <summary>
        /// Cинхронное действие-обработчик
        /// </summary>
        public Activity SyncActivity
        {
            get;
            private set;
        }

        /// <summary>
        /// Содержатся ли действия-обработчики?
        /// </summary>
        public bool ContainsActivities
        {
            get
            {
                return SyncActivity != null;
            }
        }

        /// <summary>
        /// Конструктор
        /// </summary>
        public EventActivityHandler(WorkflowInstance workflowInstance)
        {
            CodeContract.Requires(workflowInstance != null);

            _workflowInstance = workflowInstance;
            Method = new EventHandler(OnEvent);
        }

        /// <summary>
        /// Установить экземпляр потока работ
        /// </summary>
        /// <param name="workflowInstance"></param>
        internal void SetWorkflowInstance(WorkflowInstance workflowInstance)
        {
            CodeContract.Requires(workflowInstance != null);
            _workflowInstance = workflowInstance;
        }

        /// <summary>
        /// Добавить действие-обработчик с заданным типом обработки
        /// </summary>
        /// <param name="handlerActivity"></param>
        /// <param name="handlingType"></param>
        public void AddActivity(Activity handlerActivity, EventHandlingType handlingType)
        {
            switch (handlingType)
            {
                case EventHandlingType.Sync:
                    SyncActivity = handlerActivity;
                    break;

                case EventHandlingType.Async:
                    throw new NotImplementedException();
            }
        }

        /// <summary>
        /// Удалить действие-обработчик
        /// </summary>
        /// <param name="handlerActivity"></param>
        public void RemoveActivity(Activity handlerActivity)
        {
            if (SyncActivity == handlerActivity)
                SyncActivity = null;
        }

        /// <summary>
        /// Метод-обработчик события
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnEvent(object sender, EventArgs e)
        {
            if (SyncActivity != null)
                _workflowInstance.GoToActivity(SyncActivity);
        }
    }
}
