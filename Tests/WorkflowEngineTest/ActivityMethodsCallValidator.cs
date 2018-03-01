using Yaw.Workflow.ComponentModel;

namespace Yaw.Tests.WorkflowEngineTest
{
    /// <summary>
    /// Вспомогательный класс, содержащий методы, который используются при выполнении действия
    /// </summary>
    public class ActivityMethodsCallValidator
    {
        public void Reset()
        {
            PassedParameters = null;
            InitializeCallCount = 0;
            UninitializeCallCount = 0;
            ExecuteCallCount = 0;
        }

        public ActivityParameterDictionary PassedParameters { get; private set; }

        #region Initialize

        public const string INITIALIZE_METHODNAME = "Initialize";

        public int InitializeCallCount { get; private set; }

        public void Initialize(WorkflowExecutionContext context)
        {
            InitializeCallCount++;
        }

        #endregion

        #region Uninitialize

        public const string UNINITIALIZE_METHODNAME = "Uninitialize";

        public int UninitializeCallCount { get; private set; }

        public void Uninitialize(WorkflowExecutionContext context)
        {
            UninitializeCallCount++;
        }

        #endregion

        #region Execute

        public const string EXECUTE_METHODNAME = "Execute";
        public static NextActivityKey TestNextActivityKey = new NextActivityKey("Test");

        public int ExecuteCallCount { get; private set; }

        public NextActivityKey Execute(
            WorkflowExecutionContext context, ActivityParameterDictionary parameters)
        {
            ExecuteCallCount++;
            PassedParameters = parameters;
            return TestNextActivityKey;
        }

        #endregion
    }
}
