using System;
using System.Threading;
using Yaw.Workflow.ComponentModel;

namespace Yaw.Tests.WorkflowEngineTest.Activities
{
    [Serializable]
    public class TestActivity : CompositeActivity
    {
        protected override void Initialize(WorkflowExecutionContext context)
        {
            InitCallCount = 0;
            UninitCallCount = 0;
            TestValue = 0;
        }

        /// <summary>
        /// Ничего не делает
        /// </summary>
        public NextActivityKey DoNothing(
            WorkflowExecutionContext context, ActivityParameterDictionary parameters)
        {
            return context.DefaultNextActivityKey;
        }

        /// <summary>
        /// Спит заданное кол-во миллисекунд
        /// </summary>
        public NextActivityKey Sleep(
            WorkflowExecutionContext context, ActivityParameterDictionary parameters)
        {
            context.Sleep(parameters.GetParamValue<int>("ms"));
            return context.DefaultNextActivityKey;
        }

        /// <summary>
        /// Спит заданное кол-во миллисекунд, после чего генерит исключение
        /// </summary>
        public NextActivityKey SleepAndThrow(
            WorkflowExecutionContext context, ActivityParameterDictionary parameters)
        {
            context.Sleep(parameters.GetParamValue<int>("ms"));
            throw new Exception("!!!");
        }

        public int TestValue { get; set; }

        /// <summary>
        /// Задает значение св-ва TestValue
        /// </summary>
        public NextActivityKey SetTestValue(
            WorkflowExecutionContext context, ActivityParameterDictionary parameters)
        {
            TestValue = parameters.GetParamValue<int>("TestValue");
            return context.DefaultNextActivityKey;
        }

        /// <summary>
        /// Увеличивает значение св-ва TestValue на 1
        /// </summary>
        public NextActivityKey IncreaseTestValue(
            WorkflowExecutionContext context, ActivityParameterDictionary parameters)
        {
            TestValue++;
            return context.DefaultNextActivityKey;
        }

        /// <summary>
        /// Проверяет значение св-ва TestValue и, если оно равно значению параметра Expected,
        /// то возвращает Yes, иначе - No
        /// </summary>
        public NextActivityKey Test(
            WorkflowExecutionContext context, ActivityParameterDictionary parameters)
        {
            return TestValue == parameters.GetParamValue<int>("Expected")
                       ? TestNextActivityKeys.Yes
                       : TestNextActivityKeys.No;
        }

        #region Проверка инициализации/деинициализации

        public int InitCallCount;
        public void Init(WorkflowExecutionContext context)
        {
            InitCallCount++;
        }

        public int UninitCallCount;
        public void Uninit(WorkflowExecutionContext context)
        {
            UninitCallCount++;
        }

        public NextActivityKey CheckInitUninitCalled(
            WorkflowExecutionContext context, ActivityParameterDictionary parameters)
        {
            return InitCallCount == 1 && UninitCallCount == 1
                       ? TestNextActivityKeys.Yes
                       : TestNextActivityKeys.No;
        }

        #endregion

        #region Проверка передачи параметров

        public int IntProp { get { return 1; } }

        public string StrProp { get { return "1"; } }

        public object[] ArrProp { get { return new object[] { 1, "1", this }; } }

        public enum TestEnum
        {
            A
        }

        public NextActivityKey CheckProp1(
            WorkflowExecutionContext context, ActivityParameterDictionary parameters)
        {
            // проверим, что параметры переданы корректно:
            // p1=123;p2=@IntProp;p3=@Root.StrProp;p4=@R.ArrProp;p5=@@True;p6=@@False;p7=[a,b];p8=123.21:22:23.77;p9=A

            var en = parameters.GetParamValueAsEnumerable<string>("p7", new string[] {}).GetEnumerator();

            return
                parameters.GetParamValue<int>("p1") == 123 &&
                parameters.GetParamValue<int>("p2") == IntProp &&
                parameters.GetParamValue<string>("p3") == StrProp &&
                parameters.GetParamValueAsArray("p4") == ArrProp &&
                parameters.GetParamValue<bool>("p5") &&
                parameters.GetParamValue<bool>("p6") == false &&
                en.MoveNext() && en.Current == "a" && en.MoveNext() && en.Current == "b" && !en.MoveNext() && 
                parameters.GetParamValue<TimeSpan>("p8") == TimeSpan.Parse("123.21:22:23.77") &&
                parameters.GetParamValue<TestEnum>("p9") == TestEnum.A
                    ? TestNextActivityKeys.Yes
                    : TestNextActivityKeys.No;
        }

        public NextActivityKey CheckProp2(
            WorkflowExecutionContext context, ActivityParameterDictionary parameters)
        {
            return
                parameters.GetParamValue<int>("p1") == 1 &&
                parameters.GetParamValue<string>("p2") == "a"
                    ? TestNextActivityKeys.Yes
                    : TestNextActivityKeys.No;
        }

        #endregion

        #region Проверка выполнения ReferenceActivity

        public int IntPropWR { get; set; }

        public NextActivityKey CheckReferenceActivityCall(
            WorkflowExecutionContext context, ActivityParameterDictionary parameters)
        {
            var p1 = parameters.GetParamValue<int>("p1");
            if (p1 == 1)
            {
                IntPropWR = 0;
                return context.DefaultNextActivityKey;
            }

            if (p1 == 2)
            {
                if (IntPropWR != 123)
                    throw new ArgumentException("Некорректное значение св-ва IntPropWR");
                return context.DefaultNextActivityKey;
            }

            throw new ArgumentException("Некорректное значение параметра p1");
        }

        #endregion

        #region Проверка обработки событий

        public event EventHandler TestEvent;

        public NextActivityKey RaiseEvent(
            WorkflowExecutionContext context, ActivityParameterDictionary parameters)
        {
            ThreadPool.QueueUserWorkItem(
                s =>
                    {
                        Thread.Sleep(100);

                        var handler = TestEvent;
                        if (handler != null)
                            handler(null, EventArgs.Empty);
                    });
            return context.DefaultNextActivityKey;
        }

        
        #endregion
    }
}
