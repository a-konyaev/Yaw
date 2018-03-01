using System;
using Yaw.Core;

namespace Yaw.Workflow.Runtime
{
    public class WorkflowTerminatedEventArgs : WorkflowEventArgs
    {
        /// <summary>
        /// Причина прерывания выполнения экземпляра потока работ
        /// </summary>
        public readonly string Reason;
        /// <summary>
        /// Исключение, которое привело к прерыванию выполнения
        /// </summary>
        public readonly Exception Exception;

        /// <summary>
        /// Конструктор
        /// </summary>
        /// <param name="wi">экземпляр потока работ, выполнение которого прерывается</param>
        /// <param name="reason">причина прерывания выполнения</param>
        /// <param name="exception">исключение</param>
        public WorkflowTerminatedEventArgs(WorkflowInstance wi, string reason, Exception exception)
            : base(wi)
        {
            CodeContract.Requires(!string.IsNullOrEmpty(reason));

            Reason = reason;
            Exception = exception;
        }
    }
}
