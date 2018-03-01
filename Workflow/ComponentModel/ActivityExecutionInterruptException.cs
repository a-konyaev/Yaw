namespace Yaw.Workflow.ComponentModel
{
    /// <summary>
    /// Исключение, возникающее при остановке или прерывании выполнения действия
    /// </summary>
    public sealed class ActivityExecutionInterruptException : ActivityExecutionException
    {
        public ActivityExecutionInterruptException(Activity activity, WorkflowExecutionContext context)
            : base("Прерывание выполнения действия", activity, context)
        {
        }
    }
}
