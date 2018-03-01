using System;

namespace Yaw.Workflow.ComponentModel
{
    /// <summary>
    /// Класс предназначен для вызова метода инициализации и деинициализации действия
    /// </summary>
    [Serializable]
    internal class ActivityUnInitializeMethodCaller : MethodCaller
    {
        /// <summary>
        /// Конструктор
        /// </summary>
        /// <param name="methodName">имя метода</param>
        /// <param name="methodOwner">объект-владелец метода</param>
        public ActivityUnInitializeMethodCaller(string methodName, object methodOwner)
            : base(typeof(Action<WorkflowExecutionContext>), methodName, methodOwner)
        {
        }

        /// <summary>
        /// Вызвать метод
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        public void Call(WorkflowExecutionContext context)
        {
            Call(new object[] { context });
        }
    }
}
