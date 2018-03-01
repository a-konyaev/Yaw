using System;

namespace Yaw.Workflow.ComponentModel
{
    /// <summary>
    /// Действие-ссылка на другое действие
    /// </summary>
    [Serializable]
    public class ReferenceActivity : Activity
    {
        public Activity ActivityForExecute
        {
            get;
            set;
        }

        /// <summary>
        /// Конструктор
        /// </summary>
        public ReferenceActivity()
        {
            ExecutionMethodCaller = new ActivityExecutionMethodCaller("ExecuteReferencedActivity", this);
        }

        internal NextActivityKey ExecuteReferencedActivity(
            WorkflowExecutionContext context, ActivityParameterDictionary parameters)
        {
            return ActivityForExecute.Execute(context, parameters);
        }
    }
}
