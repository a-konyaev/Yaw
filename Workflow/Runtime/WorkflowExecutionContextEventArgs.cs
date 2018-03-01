using System;
using Yaw.Core;
using Yaw.Workflow.ComponentModel;

namespace Yaw.Workflow.Runtime
{
    /// <summary>
    /// Аргументы события контекста выполнения экземпляра потока работ
    /// </summary>
    public class WorkflowExecutionContextEventArgs : EventArgs
    {
        /// <summary>
        /// Контекст выполнения экземпляра потока работ
        /// </summary>
        public readonly WorkflowExecutionContext Context;
        /// <summary>
        /// Действие, к которому относится событие
        /// </summary>
        public readonly Activity Activity;

        /// <summary>
        /// Конструктор
        /// </summary>
        /// <param name="context"></param>
        /// <param name="activity"></param>
        public WorkflowExecutionContextEventArgs(WorkflowExecutionContext context, Activity activity)
        {
            CodeContract.Requires(context != null);
            CodeContract.Requires(activity != null);
            
            Context = context;
            Activity = activity;
        }
    }
}
