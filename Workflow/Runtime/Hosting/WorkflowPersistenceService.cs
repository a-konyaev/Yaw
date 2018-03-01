using System;
using Yaw.Workflow.ComponentModel;

namespace Yaw.Workflow.Runtime.Hosting
{
    /// <summary>
    /// Базовый сервис постоянства потока работ
    /// </summary>
    /// <remarks>
    /// Используется для того, чтобы сохранять и восстанавливать состояние 
    /// экземпляра потока работ в хранилище данных.
    /// </remarks>
    public abstract class WorkflowPersistenceService : WorkflowRuntimeService
    {
        public abstract WorkflowExecutionContext LoadWorkflowInstanceState(Guid instanceId);
        public abstract void SaveWorkflowInstanceState(WorkflowExecutionContext context);
    }
}
