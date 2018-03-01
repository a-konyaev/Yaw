using System;
using System.Linq;
using Yaw.Core.Extensions;
using System.Reflection;
using Yaw.Core.Utils.Collections;
using Yaw.Workflow.ComponentModel.Compiler;

namespace Yaw.Workflow.ComponentModel
{
    /// <summary>
    /// Cоставное действие
    /// </summary>
    [Serializable]
    public class CompositeActivity : Activity
    {
        /// <summary>
        /// Действия, которые входят в данное составное действие
        /// </summary>
        public ByNameAccessDictionary<Activity> Activities
        {
            get;
            private set;
        }

        /// <summary>
        /// Имя действия, с которого нужно начать выполнение
        /// </summary>
        public string StartActivity
        {
            get;
            set;
        }

        /// <summary>
        /// Конструктор
        /// </summary>
        public CompositeActivity()
        {
            Activities = new ByNameAccessDictionary<Activity>();
            ExecutionMethodCaller = new ActivityExecutionMethodCaller("ExecuteNestedActivity", this);
        }

        /// <summary>
        /// Инициализация
        /// </summary>
        /// <param name="context"></param>
        protected override void Initialize(WorkflowExecutionContext context)
        {
            base.Initialize(context);

            // сбросим Стартовое действие, чтобы избежать ситуации, когда
            // переход к выполнению данного составного действия происходит уже не первый раз,
            // причем в предыдущий раз StartActivity было задано, а сейчас - нет
            StartActivity = null;
        }

        /// <summary>
        /// Возвращает дочернее действие данного составного действия 
        /// по локальному имени дочернего действия
        /// </summary>
        /// <param name="localChildActivityName"></param>
        /// <returns></returns>
        public Activity GetChildActivity(string localChildActivityName)
        {
            return Activities[WorkflowSchemeParser.CreateFullActivityName(localChildActivityName, this)];
        }

        /// <summary>
        /// Возвращает типизированное дочернее действие данного составного действия 
        /// по локальному имени дочернего действия
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="localChildActivityName"></param>
        /// <returns></returns>
        public T GetChildActivity<T>(string localChildActivityName) where T : Activity
        {
            return (T)GetChildActivity(localChildActivityName);
        }

        /// <summary>
        /// Инициализация св-в данного действия значениями, переданными в параметрах
        /// </summary>
        /// <param name="context"></param>
        /// <param name="parameters"></param>
        private void InitProperties(WorkflowExecutionContext context, ActivityParameterDictionary parameters)
        {
            // получим тип данного составного действия
            var type = GetType();

            foreach (var param in parameters.Values)
            {
                // получим св-во 
                PropertyInfo propInfo;
                try
                {
                    propInfo = type.GetProperty(param.Name, true, true);
                }
                catch (Exception ex)
                {
                    throw new ActivityExecutionException(
                        string.Format("Ошибка получения информации о св-ве {0}", param.Name), ex, this, context);
                }

                // получим значение
                object propValue;
                try
                {
                    propValue = param.GetValue();
                }
                catch (Exception ex)
                {
                    throw new ActivityExecutionException(
                        string.Format("Ошибка получения значения для св-ва {0}", param.Name), ex, this, context);
                }

                // установим значение
                try
                {
                    if (propValue != null)
                    {
                        // попробуем сделать приведение типа
                        try
                        {
                            propValue = propInfo.PropertyType.ConvertToType(propValue);
                        }
                        catch (Exception ex)
                        {
                            throw new InvalidCastException(
                                string.Format("Ошибка приведения значения '{0}' к типу {1}",
                                              propValue, propInfo.PropertyType.Name), ex);
                        }
                    }

                    propInfo.SetValue(this, propValue, null);
                }
                catch (Exception ex)
                {
                    throw new ActivityExecutionException(
                        string.Format("Ошибка установки значения для св-ва {0}", param.Name), ex, this, context);
                }
            }
        }

        /// <summary>
        /// Возвращает действие, с которого нужно начать выполнение
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        private Activity GetStartActivity(WorkflowExecutionContext context)
        {
            // если это восстановление выполнения
            if (context.Restoring)
                // вернем действие, с которого нужно продолжить выполнение
                return Activities[context.GetActivityNameToRestore()];

            // если не задано имя действия, с которого нужно начать выполнение
            if (StartActivity == null)
                // вернем первое действие
                return Activities.Values.First();

            // получим полное имя начального действия
            var startActivityFullName = WorkflowSchemeParser.CreateFullActivityName(StartActivity, Name);

            if (!Activities.ContainsKey(startActivityFullName))
                throw new ActivityExecutionException(
                    "Начальное действие не найдено: " + startActivityFullName, this, context);

            return Activities[startActivityFullName];
        }

        /// <summary>
        /// Возвращает следующее действие, к выполнению которого нужно перейти
        /// </summary>
        /// <param name="currentExecutingActivity"></param>
        /// <param name="nextActivityKey"></param>
        /// <returns></returns>
        private static Activity GetNextActivity(Activity currentExecutingActivity, NextActivityKey nextActivityKey)
        {
            // определяем следующее действие
            var nextActivities = currentExecutingActivity.NextActivities;

            // если задан ключ след. действия
            if (nextActivities.ContainsKey(nextActivityKey))
                return nextActivities[nextActivityKey];

            // иначе, если задан ключ по умолчанию
            if (nextActivities.ContainsKey(NextActivityKey.DefaultNextActivityKey))
                return nextActivities[NextActivityKey.DefaultNextActivityKey];

            // возвращаем просто следующее действие, т.е. то, которое идет следом за текущим
            return currentExecutingActivity.FollowingActivity;
        }

        /// <summary>
        /// Выполнение вложенных действий данного составного действия
        /// </summary>
        /// <param name="context"></param>
        /// <param name="parameters"></param>
        /// <returns></returns>
        internal NextActivityKey ExecuteNestedActivity(
            WorkflowExecutionContext context, ActivityParameterDictionary parameters)
        {
            // инициализируем св-ва экземпляра данного действия значениями, переданными в параметрах
            InitProperties(context, parameters);

            var nextActivityKey = context.DefaultNextActivityKey;

            // определим действие, с которого начнем выполнение
            var currentExecutingActivity = GetStartActivity(context);

            while (currentExecutingActivity != null)
            {
                // если текущее действие - это действие выхода из составного действия
                var returnActivity = currentExecutingActivity as ReturnActivity;
                if (returnActivity != null)
                    // то выходим
                    return returnActivity.Result;

                // выполняем действие
                try
                {
                    nextActivityKey = currentExecutingActivity.Execute(context);
                }
                // выполнение было прервано
                catch (ActivityExecutionInterruptException ex)
                {
                    // попробуем получить из контекста действие, к выполнению которого нужно перейти,
                    // если это прерывание произошло в целях переключения выполнения на другое действие
                    currentExecutingActivity = context.GetToggledActivity(ex);
                    continue;
                }
                catch (Exception ex)
                {
                    throw new ActivityExecutionException(
                        "Ошибка выполнения действия", ex, currentExecutingActivity, context);
                }

                currentExecutingActivity = GetNextActivity(currentExecutingActivity, nextActivityKey);
                if (currentExecutingActivity == null)
                    // видимо, текущее действие последнее => выходим с результатом по умолчанию
                    return context.DefaultNextActivityKey;
            }

            return nextActivityKey;
        }
    }
}
