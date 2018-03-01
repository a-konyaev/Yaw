using System;

namespace Yaw.Workflow.ComponentModel
{
    /// <summary>
    /// Действие, которое освобождает эксклюзивную блокировку с заданным именем
    /// </summary>
    [Serializable]
    public class MonitorExitActivity : MonitorActivity
    {
        public MonitorExitActivity()
        {
            ExecutionMethodCaller = new ActivityExecutionMethodCaller("MonitorExit", this);
        }

        internal NextActivityKey MonitorExit(
            WorkflowExecutionContext context, ActivityParameterDictionary parameters)
        {
            context.MonitorExit(LockName);
            return context.DefaultNextActivityKey;
        }
    }
}
