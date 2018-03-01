using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Threading;
using Yaw.Core;
using Yaw.Core.Diagnostics.Default;
using Yaw.Core.Extensions;
using Yaw.Core.Utils.Collections;
using Yaw.Core.Utils.Threading;
using Yaw.Workflow.Runtime;

namespace Yaw.Workflow.ComponentModel
{
    /// <summary>
    /// Контекст выполнения экземпляра потока работ
    /// </summary>
    public class WorkflowExecutionContext : IWaitController
    {
        /// <summary>
        /// Форматтер для сериализации
        /// </summary>
        private static readonly BinaryFormatter s_formatter = new BinaryFormatter();

        /// <summary>
        /// Экземпляр потока работ
        /// </summary>
        private WorkflowInstance _workflowInstance;

        /// <summary>
        /// Схема потока работ
        /// </summary>
        public WorkflowScheme Scheme
        {
            get;
            private set;
        }
        /// <summary>
        /// Идентификатор контекста
        /// </summary>
        public Guid InstanceId
        {
            get
            {
                return _workflowInstance.InstanceId;
            }
        }
        /// <summary>
        /// Включен ли режим отслеживания состояния
        /// </summary>
        public bool Tracking
        {
            get;
            internal set;
        }
        /// <summary>
        /// Приоритет текущего выполняемого действия
        /// </summary>
        public ActivityPriority Priority
        {
            get;
            internal set;
        }

        #region События

        /// <summary>
        /// Событие "Действие начинает выполняться"
        /// </summary>
        public event EventHandler<WorkflowExecutionContextEventArgs> ActivityExecutionStarting;
        /// <summary>
        /// Событие "Выполнение действия завершено"
        /// </summary>
        public event EventHandler<WorkflowExecutionContextEventArgs> ActivityExecutionFinished;
        /// <summary>
        /// Событие "Контекст выполнения изменился"
        /// </summary>
        public event EventHandler<WorkflowExecutionContextEventArgs> ExecutionContextChanged;

        #endregion

        /// <summary>
        /// Конструктор
        /// </summary>
        /// <param name="scheme"></param>
        internal WorkflowExecutionContext(WorkflowScheme scheme)
        {
            CodeContract.Requires(scheme != null);
            Scheme = scheme;
            Tracking = true;
            Priority = ActivityPriority.Default;
        }

        /// <summary>
        /// Установить экземпляр потока работ
        /// </summary>
        /// <param name="workflowInstance"></param>
        internal void SetWorkflowInstance(WorkflowInstance workflowInstance)
        {
            CodeContract.Requires(workflowInstance != null);
            _workflowInstance = workflowInstance;

            // установим ссылку на экземпляр потока работ для обработчиков событий
            foreach (var handler in _eventHandlersDict.Values)
                handler.SetWorkflowInstance(_workflowInstance);
        }

        /// <summary>
        /// Получить сервис
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public T GetService<T>()
        {
            return _workflowInstance.Runtime.GetService<T>();
        }

        /// <summary>
        /// Ключ следующего действия по умолчанию
        /// </summary>
        public NextActivityKey DefaultNextActivityKey
        {
            get
            {
                return Scheme.DefaultNextActivityKey;
            }
        }

        #region Управление процессом выполнения потока работ

        /// <summary>
        /// Объект для синхронизации выполнения выполнения рабочего потока
        /// </summary>
        private static readonly object s_interruptExecutionSync = new object();
        /// <summary>
        /// Событие прерывания выполнения рабочего потока
        /// </summary>
        private readonly ManualResetEvent _interruptExecutionEvent = new ManualResetEvent(false);
        /// <summary>
        /// Событие завершения прерывания выполнения рабочего потока
        /// </summary>
        /// <remarks>изначально, считаем, что прерывание уже завершено</remarks>
        private readonly ManualResetEvent _interruptExecutionFinishedEvent = new ManualResetEvent(true);
        /// <summary>
        /// Действие, на которое нужно переключить выполнение потока работ
        /// </summary>
        private volatile Activity _toggleActivity;
        /// <summary>
        /// Очередь действий, на которые нужно переключить выполнение потока работ
        /// </summary>
        private readonly BlockingQueue<Activity> _toggleActivityQueue = new BlockingQueue<Activity>();

        /// <summary>
        /// Запустить выполнение
        /// </summary>
        internal void StartExecution()
        {
            // открываем очередь для приема действий, на которые нужно выполнять переключение выполнения
            _toggleActivityQueue.Clear();
            _toggleActivityQueue.Open();

            // запускаем поток обработки переключения выполнения
            ThreadUtils.StartBackgroundThread(InterruptExecutionThread);
        }

        /// <summary>
        /// Остановить выполнения
        /// </summary>
        internal void StopExecution()
        {
            //CoreApplication.Instance.Logger.LogInfo("StopExecution: call");
            // закрываем очередь для приема действий-переключений
            _toggleActivityQueue.Close();

            //CoreApplication.Instance.Logger.LogInfo("StopExecution: interrupt to ExitActivity");
            // прерываем выполнение
            InterruptExecution(Scheme.ExitActivity);
        }

        /// <summary>
        /// Переключить выполнение на действие
        /// </summary>
        /// <remarks>выполнение текущего действия прерывается и продолжается с заданного действия</remarks>
        /// <param name="toggleActivity">Действие, на которое нужно переключить выполнение потока работ</param>
        internal void ToggleExecutionToActivity(Activity toggleActivity)
        {
            //CoreApplication.Instance.Logger.LogInfo("ToggleExecutionToActivity: tan = " + toggleActivity.Name);
            // помещаем действие в очередь на переключение
            _toggleActivityQueue.TryEnqueue(toggleActivity);
        }

        /// <summary>
        /// Метод потока, который выполняет обработку переключений выполнения потока работ на другое действие
        /// </summary>
        private void InterruptExecutionThread()
        {
            //CoreApplication.Instance.Logger.LogInfo("InterruptExecutionThread started");

            // достаем действия из очереди до тех пор, пока очередь не закроют
            Activity toggleActivity;
            while (_toggleActivityQueue.TryDequeue(out toggleActivity))
            {
                // прерываем выполнение
                InterruptExecution(toggleActivity);
            }
        }

        /// <summary>
        /// Прерывает выполнение потока работ
        /// </summary>
        /// <param name="toggleActivity">
        /// действие, к выполнению которого нужно перейти после того,
        /// как прерывание будет произведено</param>
        private void InterruptExecution(Activity toggleActivity)
        {
            //CoreApplication.Instance.Logger.LogInfo("InterruptExecution: tan = " + toggleActivity.Name);

            while (true)
            {
                //CoreApplication.Instance.Logger.LogInfo("InterruptExecution: wait...");
                // ждем, когда завершиться предыдущее прерывание, если оно выполняется в данный момент
                _interruptExecutionFinishedEvent.WaitOne();

                // завершилось => начинаем новый процесс прерывания:
                // получаем блокировку, внутри которой еще раз проверяем, что другое прерывание 
                // уже завершилось, и только тогда начинаем "наше" прерывание

                lock (s_interruptExecutionSync)
                {
                    // если "другое" прерывание успело запуститься раньше, чем "наше"
                    if (!_interruptExecutionFinishedEvent.WaitOne(0))
                    {
                        //CoreApplication.Instance.Logger.LogInfo("InterruptExecution: go to wait");
                        // снова идем ждать в начало цикла
                        continue;
                    }

                    // ОК - "другое" прерывание сейчас не выполняется => запускаем "наше"
                    _toggleActivity = toggleActivity;

                    // если приоритет действия-прерывания не ниже приоритета контекста
                    if (_toggleActivity.Priority >= Priority)
                    {
                        //CoreApplication.Instance.Logger.LogInfo("InterruptExecution: start interruption");
                        // значит запускаем прерывание немедленно 
                        // => сбросим событие "прерывание завершено" и выставим "прерывать выполнение"
                        _interruptExecutionFinishedEvent.Reset();
                        _interruptExecutionEvent.Set();
                    }
                    else
                    {
                        // Не будем запускать прерывание прямо сейчас, но в _toggleActivity
                        // будет хранится действие, на которое нужно переключиться.
                        // Это позволит попробовать выполнить прерывание позже, когда
                        // приоритет контекста перестанет быть выше действия-прерывания 
                        // (см. CheckNeedInterrupt).
                        // Но если "придет" новое прерывание, у которого приоритет будет достаточным
                        // для запуска, то данное прерывание будет утеряно (это не ошибка - это by design).
                        //CoreApplication.Instance.Logger.LogInfo(
                        //    "InterruptExecution: delay interruption (ta.Priority={0}; Priority={1}; context = [{2}])",
                        //    _toggleActivity.Priority.Value, Priority.Value, ToString());
                    }

                    return;
                }
            }
        }

        /// <summary>
        /// Проверяет, нужно ли прервать выполнение потока работ для переключения выполнения
        /// на другое действие. Если нужно, то поднимает исключение ActivityExecutionInterruptException
        /// </summary>
        private void CheckNeedInterrupt()
        {
            lock (s_interruptExecutionSync)
            {
                // если действие-прерывание не задано или задано, но его приоритет ниже приоритета контекста
                if (_toggleActivity == null || _toggleActivity.Priority < Priority)
                    // прерывать выполнение не нужно
                    return;

                // если мы сюда попали, значит:
                // 1) процесс прерывания уже был ранее запущен, но выставление события _interruptExecutionEvent
                //    в методе ToggleExecutionToActivity не привело к поднятию исключения-прерывания,
                //    т.к. его не ожидали => поднимаем исключение-прерывание сейчас
                // или
                // 2) в момент инициации прерывания в методе ToggleExecutionToActivity было определено,
                //    что приоритет контекста выше, чем приоритет действия-прерывания, поэтому не было запущено
                //    сразу же, но сейчас! приоритет контекста уже НЕ выше => запускаем процесс прерывания сейчас

                //CoreApplication.Instance.Logger.LogInfo(
                //    "CheckNeedInterrupt: start interruption; tan = " + _toggleActivity.Name);

                // сбросим событие "прерывание завершено"
                _interruptExecutionFinishedEvent.Reset();
                // выставлять событие "прерывать выполнение" смысла нет

                // поднимаем исключение-прерывание
                throw new ActivityExecutionInterruptException(CurrentExecutingActivity, this);
            }
        }

        /// <summary>
        /// Отменить прерывание выполнения
        /// </summary>
        /// <remarks>Используется для того, чтобы действие могло отловить исключение о прерывании
        /// и отменить прерывание выполнения</remarks>
        public void ResetInterrupt()
        {
            lock (s_interruptExecutionSync)
            {
                //CoreApplication.Instance.Logger.LogInfo(
                //    "ResetInterrupt: tan = " + _toggleActivity == null ? "<null>" : _toggleActivity.Name);

                _toggleActivity = null;
                _interruptExecutionEvent.Reset();
                _interruptExecutionFinishedEvent.Set();
            }
        }

        /// <summary>
        /// Выполнение действия было прервано
        /// </summary>
        /// <param name="ex">исключение о прерывании выполнения действия</param>
        internal void ActivityExecutionInterrupted(ActivityExecutionInterruptException ex)
        {
            // если действие, на которое нужно переключить выполнение не задано
            if (_toggleActivity == null)
            {
                //CoreApplication.Instance.Logger.LogInfo("ActivityExecutionInterrupted: throw ex");
                // то значит это просто прерывание выполнения => пропускаем исключение
                throw ex;
            }

            // иначе - это переключение выполнения

            // снимаем с вершины стека выполняемых действий имена действий до тех пор,
            // пока на вершине не останется действие, которое является родительским для _toggleActivity,
            // или пока стек не опустеет, если родительского действия нет

            // если стек еще не готов
            if (!IsStackCorrectForExecuteActivity(_toggleActivity))
                // то удаляем вершину со стека
                ExecutingActivitiesStackPop();
            
            // в любом случае пропускаем исключение, т.к. в итоге нам нужно вывалиться внутри составного действия
            //CoreApplication.Instance.Logger.LogInfo(
            //    "ActivityExecutionInterrupted: throw ex 2; tan = " + _toggleActivity.Name);
            throw ex;
        }

        /// <summary>
        /// Возвращает действие, к выполнению которого должно перейти составное действие после того, как
        /// произошло прерывание выполнения текущего действия.
        /// Если прерывание произошло в результате останова выполнения или еще рано переходить к
        /// выполнению другого действия, то метод пропускает исключение дальше
        /// </summary>
        /// <returns></returns>
        internal Activity GetToggledActivity(ActivityExecutionInterruptException ex)
        {
            // если действие, на которое нужно переключить выполнение не задано
            // или задано, но стек еще не готов
            if (_toggleActivity == null ||
                !IsStackCorrectForExecuteActivity(_toggleActivity))
            {
                //CoreApplication.Instance.Logger.LogInfo("GetToggledActivity: throw ex");
                // то пропускаем исключение
                throw ex;
            }

            var res = _toggleActivity;
            // отменим прерывание выполнения, чтобы выполнение продолжилось далее
            ResetInterrupt();

            // выставим режим отслеживания в положение, в котором он мог бы оказаться,
            // если бы выполнение потока работ дошло до действия, к выполнению которого переходим,
            // "естесственным" путем, т.е. без переключения выполнения.
            Tracking = GetTrackingBeforeExecuteActivity(res);

            //CoreApplication.Instance.Logger.LogInfo("GetToggledActivity: return = " + res.Name);
            return res;
        }

        /// <summary>
        /// Вычисляет значение включенности режима отслеживания, которое 
        /// должно быть перед началом выполнения заданного действия
        /// </summary>
        /// <param name="activity"></param>
        /// <returns></returns>
        private static bool GetTrackingBeforeExecuteActivity(Activity activity)
        {
            // если у действия есть родительские действия, 
            // то вычислим значения режима на основании его включенности у родительских действиях
            var tmp = activity;
            while (tmp.Parent != null)
            {
                if (!tmp.Parent.Tracking)
                    return false;

                tmp = tmp.Parent;
            }

            // если родительских действий нет, то вернем режим по умолчанию, 
            // иначе - режим корневого родителя
            return tmp.Equals(activity) ? true : tmp.Tracking;
        }

        #endregion

        #region Отслеживание стека выполнения действий

        /// <summary>
        /// Объект для синхронизации работы со стеком действий
        /// </summary>
        private static readonly object s_activitiesStackSync = new object();
        /// <summary>
        /// Стек имен действий, которые выполняются
        /// </summary>
        private readonly ListStack<string> _executingActivitiesStack = new ListStack<string>();
        /// <summary>
        /// Стек имен действий, выполнение которых отслеживается
        /// </summary>
        private ListStack<string> _trackingActivitiesStack = new ListStack<string>();
        /// <summary>
        /// Кол-во действий, выполнение которых уже восстановилось
        /// </summary>
        /// <remarks>
        /// Суть этого счетчика - кол-во элементов в _executingActivities от начала, начало выполнения
        /// которых мы снова зафиксировали
        /// </remarks>
        private int _restoredActivitiesCount;
        
        [NonSerialized]
        private bool _restoring;
        /// <summary>
        /// Признак того, что контекст находится в режиме восстановления работы
        /// </summary>
        public bool Restoring
        {
            get
            {
                return _restoring;
            }
        }

        /// <summary>
        /// Действие, которое в данный момент выполняется
        /// </summary>
        public Activity CurrentExecutingActivity
        {
            get
            {
                lock (s_activitiesStackSync)
                {
                    return _executingActivitiesStack.Count == 0
                               ? null
                               : Scheme.Activities[_executingActivitiesStack.Peek()];
                }
            }
        }

        /// <summary>
        /// Список всех действий, выполнение которых происходит в данный момент.
        /// </summary>
        /// <returns>Список имен выполняемых действий, упорядоченный следующим образом:
        /// корневое действие, дочернее, дочернее дочернего и т.д. Последнее в списке действие - то,
        /// которое непосредственно выполняется в данный момент</returns>
        public IEnumerable<string> CurrentExecutingActivities()
        {
            lock (s_activitiesStackSync)
            {
                // возвращаем копию стека в виде листа
                return new List<string>(_executingActivitiesStack);
            }
        }

        /// <summary>
        /// Вызывается, чтобы сообщить контексту о том, что действие начало выполняться
        /// </summary>
        /// <param name="activity"></param>
        internal void ActivityExecuting(Activity activity)
        {
            CodeContract.Requires(activity != null);

            // проверим, вдруг нужно прервать выполнение и переключиться на другое действие
            CheckNeedInterrupt();
            // начинаем отслеживание выполнения действия
            StartActivityExecutionTracking(activity);
        }

        /// <summary>
        /// Вызывается, чтобы сообщить контексту о том, что действие завершило выполнение
        /// </summary>
        /// <param name="activity"></param>
        internal void ActivityExecuted(Activity activity)
        {
            CodeContract.Requires(activity != null);

            // завершаем отслеживание выполнения действия
            EndActivityExecutionTracking(activity);
        }

        /// <summary>
        /// Удаляет элемент с вершины стека выполняемых действий и при этом,
        /// если нужно, то удаляет и соотв. элемент с вершины стека отслеж. действий
        /// </summary>
        private void ExecutingActivitiesStackPop()
        {
            lock (s_activitiesStackSync)
            {
                var popName = _executingActivitiesStack.Pop();

                // если на вершине стека отслеживаемых действий находится действие, 
                // которое удалили с вершины стека выполняемых действий
                if (_trackingActivitiesStack.Count > 0 &&
                    string.CompareOrdinal(_trackingActivitiesStack.Peek(), popName) == 0)
                    // то и с вершины стека отслеж. действий удалим его тоже
                    _trackingActivitiesStack.Pop();
            }
        }

        /// <summary>
        /// Начать отслеживать выполнение действия
        /// </summary>
        private void StartActivityExecutionTracking(Activity activity)
        {
            lock (s_activitiesStackSync)
            {
                _executingActivitiesStack.Push(activity.Name);
            }

            var eventArgs = new WorkflowExecutionContextEventArgs(this, activity);
            ActivityExecutionStarting.RaiseEvent(this, eventArgs);

            // если режим отслеживания состояния выключен
            if (!Tracking)
                // то выходим
                return;

            if (_restoring)
            {
                if (_trackingActivitiesStack[_restoredActivitiesCount] != activity.Name)
                {
                    var msg = string.Format(
                        "Ошибка при восстановлении выполнения потока работ: " +
                        "действие {0}, которое начинает выполнение, отличается от действия {1}, " +
                        "которое, согласно информации в контексте, должно начать выполнение",
                        activity.Name, _trackingActivitiesStack[_restoredActivitiesCount]);

                    throw new ActivityExecutionException(msg, activity, this);
                }

                // увеличим счетчик _restoredActivitiesCount
                _restoredActivitiesCount++;

                // если счетчик сравнялся с кол-вом элементов в стеке, значит восстановление завершилось
                if (_restoredActivitiesCount == _trackingActivitiesStack.Count)
                    _restoring = false;

                return;
            }

            _trackingActivitiesStack.Push(activity.Name);
            ExecutionContextChanged.RaiseEvent(this, eventArgs);
        }

        /// <summary>
        /// Завершить отслеживать выполнение действия
        /// </summary>
        private void EndActivityExecutionTracking(Activity activity)
        {
            lock (s_activitiesStackSync)
            {
                SafePopFromStack(_executingActivitiesStack, activity);
            }

            var eventArgs = new WorkflowExecutionContextEventArgs(this, activity);
            ActivityExecutionFinished.RaiseEvent(this, eventArgs);

            // если режим отслеживания состояния выключен
            if (!Tracking)
                // то ничего не делаем
                return;

            if (_restoring)
                return;

            SafePopFromStack(_trackingActivitiesStack, activity);
            ExecutionContextChanged.RaiseEvent(this, eventArgs);
        }

        /// <summary>
        /// Удаляет информацию о действии с вершины стека, при этом проверяет, 
        /// что на вершине именно это заданное действие
        /// </summary>
        /// <param name="stack"></param>
        /// <param name="activity"></param>
        private static void SafePopFromStack(ListStack<string> stack, Activity activity)
        {
            if (stack.Count == 0)
                throw new InvalidOperationException("Стек пуст");

            var top = stack.Peek();
            if (string.CompareOrdinal(top, activity.Name) != 0)
                throw new InvalidOperationException("На вершине стека другое действие: " + top);

            stack.Pop();
        }

        /// <summary>
        /// Возвращает имя действия, к выполнению которого нужно перейти 
        /// для восстановления выполнения потока работ
        /// </summary>
        /// <returns></returns>
        internal string GetActivityNameToRestore()
        {
            if (!_restoring)
                throw new InvalidOperationException("Контекст не находится в режиме восстановления работы");

            return _trackingActivitiesStack[_restoredActivitiesCount];
        }

        /// <summary>
        /// Проверяет, находится ли стек в состоянии, когда можно начать выполнять заданное действие
        /// </summary>
        /// <returns></returns>
        private bool IsStackCorrectForExecuteActivity(Activity nextExecutingActivity)
        {
            lock (s_activitiesStackSync)
            {
                // состояние стека годится, если
                return
                    // он пуст
                    _executingActivitiesStack.Count == 0 ||
                    // или на вершине - имя родительского состояния
                    (nextExecutingActivity.Parent != null &&
                     _executingActivitiesStack.Peek() == nextExecutingActivity.Parent.Name);
            }
        }

        #endregion

        #region IWaitController Members

        /// <summary>
        /// Приостановить выполнение действия на заданный тайм-аут
        /// </summary>
        /// <param name="timeout"></param>
        public void Sleep(TimeSpan timeout)
        {
            Sleep(Convert.ToInt32(timeout.TotalMilliseconds));
        }

        /// <summary>
        /// Приостановить выполнение действия на заданный тайм-аут
        /// </summary>
        /// <param name="millisecondsTimeout"></param>
        public void Sleep(int millisecondsTimeout)
        {
            if (_interruptExecutionEvent.WaitOne(millisecondsTimeout, false))
                // произошло прерывание выполнения
                throw new ActivityExecutionInterruptException(CurrentExecutingActivity, this);
        }

        /// <summary>
        /// Ожидать бесконечно заданное событие
        /// </summary>
        /// <param name="waitHandle"></param>
        public void WaitOne(WaitHandle waitHandle)
        {
            WaitAny(new[] { waitHandle });
        }

        /// <summary>
        /// Ожидать заданное событие в течение таймаута
        /// </summary>
        /// <param name="waitHandle"></param>
        /// <param name="timeout"></param>
        /// <returns>true - произошло событие, false - время таймаута истекло</returns>
        public bool WaitOne(WaitHandle waitHandle, TimeSpan timeout)
        {
            return WaitAny(new[] { waitHandle }, timeout) != WaitHandle.WaitTimeout;
        }

        /// <summary>
        /// Ожидать любое из заданных событий
        /// </summary>
        /// <param name="waitHandles"></param>
        /// <returns></returns>
        public int WaitAny(WaitHandle[] waitHandles)
        {
            var waitHandlesEx = new List<WaitHandle>(waitHandles) { _interruptExecutionEvent };

            var index = WaitHandle.WaitAny(waitHandlesEx.ToArray());

            // произошло прерывание выполнения
            if (index == waitHandles.Length)
                throw new ActivityExecutionInterruptException(CurrentExecutingActivity, this);

            return index;
        }

        /// <summary>
        /// Ожидать любое из заданных событий в течение заданного тайм-аута
        /// </summary>
        /// <param name="waitHandles"></param>
        /// <param name="timeout"></param>
        /// <returns></returns>
        public int WaitAny(WaitHandle[] waitHandles, TimeSpan timeout)
        {
            return WaitAny(waitHandles, Convert.ToInt32(timeout.TotalMilliseconds));
        }

        /// <summary>
        /// Ожидать любое из заданных событий в течение заданного тайм-аута
        /// </summary>
        /// <param name="waitHandles"></param>
        /// <param name="timeout"></param>
        /// <returns></returns>
        public int WaitAny(WaitHandle[] waitHandles, int timeout)
        {
            var waitHandlesEx = new List<WaitHandle>(waitHandles) { _interruptExecutionEvent };

            var index = WaitHandle.WaitAny(waitHandlesEx.ToArray(), timeout, false);

            // произошло прерывание выполнения
            if (index == waitHandles.Length)
                throw new ActivityExecutionInterruptException(CurrentExecutingActivity, this);

            return index;
        }

        /// <summary>
        /// Ожидает первое заданное событие или все другие,
        /// т.е. ожидание окончится, когда сработает событие one или когда сработают сразу все события others
        /// </summary>
        /// <param name="one">первое событие</param>
        /// <param name="others">другие события</param>
        /// <returns>индекс сработавшего события:
        /// 0 - сработало первое событие
        /// 1 - сработали все другие события
        /// WaitHandle.WaitTimeout - во время ожидания произошла ошибка</returns>
        public int WaitOneOrAllOthers(WaitHandle one, WaitHandle[] others)
        {
            var index = WaitHandleUtils.WaitOneOrTwoOrAllOthers(_interruptExecutionEvent, one, others);

            // произошло прерывание выполнения
            if (index == 0)
                throw new ActivityExecutionInterruptException(CurrentExecutingActivity, this);

            return index == WaitHandle.WaitTimeout
                       ? index
                       : (index - 1);
        }

        #endregion

        #region Подписка на события

        /// <summary>
        /// Объект синхронизации доступа к Словарю обработчиков событий
        /// </summary>
        private static readonly object s_eventHandlersDictSync = new object();
        /// <summary>
        /// Словарь обработчиков событий: [уникальное имя события, обработчик]
        /// </summary>
        private Dictionary<string, EventActivityHandler> _eventHandlersDict =
            new Dictionary<string, EventActivityHandler>();

        /// <summary>
        /// Подписаться на событие
        /// </summary>
        /// <param name="eventHolder">держатель события</param>
        /// <param name="handlerActivity">действие-обработчик</param>
        /// <param name="handlingType">тип обработки события</param>
        internal void SubscribeToEvent(
            EventHolder eventHolder, Activity handlerActivity, EventHandlingType handlingType)
        {
            lock (s_eventHandlersDictSync)
            {
                EventActivityHandler handler;

                // если обработчик еще не зарегистрирован
                if (!_eventHandlersDict.ContainsKey(eventHolder.EventName))
                {
                    // создадим его
                    handler = new EventActivityHandler(_workflowInstance);
                    _eventHandlersDict[eventHolder.EventName] = handler;
                    eventHolder.AddEventHandler(handler.Method);
                }
                else
                    // получим обработчик
                    handler = _eventHandlersDict[eventHolder.EventName];

                // добавим действие-обработчик
                handler.AddActivity(handlerActivity, handlingType);
            }
        }

        /// <summary>
        /// Отписаться от события
        /// </summary>
        /// <param name="eventHolder">держатель события</param>
        /// <param name="handlerActivity">действие-обработчик</param>
        internal void UnsubscribeFromEvent(EventHolder eventHolder, Activity handlerActivity)
        {
            lock (s_eventHandlersDictSync)
            {
                // если обработчик не зарегистрирован
                if (!_eventHandlersDict.ContainsKey(eventHolder.EventName))
                    return;

                // получим обработчик
                var handler = _eventHandlersDict[eventHolder.EventName];

                // удалим действие из обработчика
                handler.RemoveActivity(handlerActivity);

                // если действий в обработчике не осталось
                if (!handler.ContainsActivities)
                {
                    eventHolder.RemoveEventHandler(handler.Method);
                    _eventHandlersDict.Remove(eventHolder.EventName);
                }
            }
        }

        #endregion

        #region Мониторинг

        /// <summary>
        /// Включить монитор
        /// </summary>
        /// <param name="lockName">имя блокировки</param>
        internal void MonitorEnter(string lockName)
        {
            // TODO: сделать
        }

        /// <summary>
        /// Выключить монитор
        /// </summary>
        /// <param name="lockName">имя блокировки</param>
        internal void MonitorExit(string lockName)
        {
            // TODO: сделать
        }

        #endregion

        #region Сериализация

        /// <summary>
        /// Возвращает состояние контекста
        /// </summary>
        /// <returns></returns>
        public object GetState()
        {
            return new object[] { 
                Scheme, 
                _trackingActivitiesStack,
                _eventHandlersDict
            };
        }

        /// <summary>
        /// Сохраняет состояние контекста в поток
        /// </summary>
        /// <param name="context"></param>
        /// <param name="stream"></param>
        public static void Save(WorkflowExecutionContext context, Stream stream)
        {
            s_formatter.Serialize(stream, context.GetState());
        }

        /// <summary>
        /// Восстанавливает состояние контекста из потока
        /// </summary>
        /// <param name="stream"></param>
        /// <returns></returns>
        public static WorkflowExecutionContext Load(Stream stream)
        {
            var arr = (object[])s_formatter.Deserialize(stream);

            // восстановим контекст
            var context = new WorkflowExecutionContext((WorkflowScheme)arr[0])
            {
                _trackingActivitiesStack = (ListStack<string>)arr[1],
                _eventHandlersDict = (Dictionary<string, EventActivityHandler>)arr[2]
            };
            // установим признак того, что выполнение восстанавливается
            context._restoring = (context._trackingActivitiesStack.Count > 0);

            return context;
        }


        #endregion

        /// <summary>
        /// Возвращает строковое представление контекста
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            lock (s_activitiesStackSync)
            {
                return _executingActivitiesStack.ToString();
            }
        }
    }
}
