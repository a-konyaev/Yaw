using System;
using System.Threading;
using Yaw.Core;
using Yaw.Core.Extensions;
using Yaw.Core.Utils.Threading;
using Yaw.Workflow.ComponentModel;

namespace Yaw.Workflow.Runtime
{
    public class WorkflowInstance
    {
        /// <summary>
        /// Идентификатор экземпляра потока работ
        /// </summary>
        public readonly Guid InstanceId;
        /// <summary>
        /// Исполняющая среда
        /// </summary>
        public readonly WorkflowRuntime Runtime;
        /// <summary>
        /// Контекст выполнения экземпляра потока работ
        /// </summary>
        public readonly WorkflowExecutionContext ExecutionContext;

        /// <summary>
        /// Конструктор
        /// </summary>
        /// <param name="instanceId"></param>
        /// <param name="runtime"></param>
        /// <param name="executionContext"></param>
        internal WorkflowInstance(Guid instanceId, WorkflowRuntime runtime, WorkflowExecutionContext executionContext)
        {
            CodeContract.Requires(instanceId != Guid.Empty);
            CodeContract.Requires(runtime != null);
            CodeContract.Requires(executionContext != null);

            InstanceId = instanceId;
            Runtime = runtime;
            ExecutionContext = executionContext;
            ExecutionContext.SetWorkflowInstance(this);
        }

        #region Выполнение потока работ

        /// <summary>
        /// Запуск выполнения данного экземпляра потока работ
        /// </summary>
        public void Start()
        {
            // запускаем поток выполнения
            ThreadUtils.StartBackgroundThread(ExecuteWorkflowMethod);
        }

        /// <summary>
        /// Прекращение работы экземпляра потока работ
        /// </summary>
        public void Stop()
        {
            ExecutionContext.StopExecution();
        }

        /// <summary>
        /// Перейти к выполнению действия
        /// </summary>
        /// <remarks>
        /// Текущее выполнение потока работ прерывается и продолжается с заданного действия
        /// </remarks>
        /// <param name="activityName">имя действия, выполнять которое нужно начать</param>
        public void GoToActivity(string activityName)
        {
            CodeContract.Requires(!string.IsNullOrEmpty(activityName));

            if (!ExecutionContext.Scheme.Activities.ContainsKey(activityName))
                throw new Exception("Действие не найдено: " + activityName);

            var activity = ExecutionContext.Scheme.Activities[activityName];
            GoToActivity(activity);
        }

        /// <summary>
        /// Перейти к выполнению действия
        /// </summary>
        /// <remarks>
        /// Текущее выполнение потока работ прерывается и продолжается с заданного действия
        /// </remarks>
        /// <param name="activity">действие, выполнять которое нужно начать</param>
        public void GoToActivity(Activity activity)
        {
            ExecutionContext.ToggleExecutionToActivity(activity);
        }

        /// <summary>
        /// Метод выполнения потока работ
        /// </summary>
        private void ExecuteWorkflowMethod()
        {
            Runtime.RaiseWorkflowStarted(this);

            var activityToExecute = ExecutionContext.Scheme.RootActivity;
            ExecutionContext.StartExecution();

            while (true)
            {
                // если текущее действие - это действие выхода из составного действия
                var returnActivity = activityToExecute as ReturnActivity;
                if (returnActivity != null)
                {
                    // то выходим
                    Runtime.RaiseWorkflowCompleted(this, returnActivity.Result);
                    return;
                }

                try
                {
                    var res = activityToExecute.Execute(ExecutionContext);
                    Runtime.RaiseWorkflowCompleted(this, res);
                }
                catch (ActivityExecutionInterruptException ex)
                {
                    try
                    {
                        activityToExecute = ExecutionContext.GetToggledActivity(ex);
                        continue;
                    }
                    catch (Exception ex2)
                    {
                        Runtime.RaiseWorkflowTerminated(this, "GetToggledActivity failed", ex2);
                    }
                }
                catch (Exception ex)
                {
                    Runtime.RaiseWorkflowTerminated(this, "Activity execution failed", ex);
                }

                return;
            }
        }

        #endregion

        #region Equals & GetHashCode

        public override bool Equals(object obj)
        {
            if (obj == null)
                return false;

            var other = obj as WorkflowInstance;
            if (other == null)
                return false;

            return other.InstanceId.Equals(InstanceId);
        }

        public override int GetHashCode()
        {
            return InstanceId.GetHashCode();
        }

        #endregion
    }
}
