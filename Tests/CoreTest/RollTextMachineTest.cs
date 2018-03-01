using System.Text;
using System.Threading;
using Yaw.Core.Utils.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Yaw.Tests.CoreTest
{
    /// <summary>
    ///This is a test class for RollTextMachineTest and is intended
    ///to contain all RollTextMachineTest Unit Tests
    ///</summary>
    [TestClass()]
    public class RollTextMachineTest
    {
        private static RollTextMachine _textMachine;
        private static RollTextMachine_Accessor _accessor;
        private TestContext testContextInstance;

        public TestContext TestContext { get; set; }

        //Use ClassInitialize to run code before running the first test in the class
        [ClassInitialize]
        public static void MyClassInitialize(TestContext testContext)
        {
            _textMachine = new RollTextMachine(5, false);
            _accessor = new RollTextMachine_Accessor(new PrivateObject(_textMachine));
        }


        /// <summary>
        ///A test for RolledText
        ///</summary>
        [TestMethod]
        public void RolledTextTest()
        {
            string expected = "Тестовый текст для прокрутчика текста";
            RollTextMachine.MachineState currentState = _textMachine.State;

            // проверим что состояние не изменилось
            _textMachine.RolledText = expected;
            Assert.AreEqual(_textMachine.State, currentState);
        }

        /// <summary>
        ///A test for Stop
        ///</summary>
        [TestMethod]
        public void StopTest()
        {
            // если машинка не запущена
            if (_textMachine.State != RollTextMachine.MachineState.Running)
            {
                _textMachine.RolledText = "1234465767";
                _textMachine.Start();
            }

            // проверим что поток создался и состояние == запущено
            if (_textMachine.State != RollTextMachine.MachineState.Running &&
                _accessor._rollTextThread == null)
            {
                Assert.Fail("Не удалось провести тест т.к метод старт выполнился не корректно");
            }

            // проверяем что состояние = остановлено и поток убит
            _textMachine.Stop();
            Assert.AreEqual(_textMachine.State, RollTextMachine.MachineState.Stopped);
            Assert.IsNull(_accessor._rollTextThread, "Поток работы машины не был остановлен");
        }

        private AutoResetEvent _handle = new AutoResetEvent(false);

        /// <summary>
        ///A test for Start
        ///</summary>
        [TestMethod]
        public void StartTest()
        {
            // если вдруг наша машина не остановлена
            if (_textMachine.State == RollTextMachine.MachineState.Running)
                _textMachine.Stop();

            _textMachine.NeedSetText += TextMachine_NeedSetText;

            // установим текст
            _textMachine.RolledText = "123456";
            //Установим флаг что проверяем половину текста
            s_isTextPart = true;

            ThreadPool.QueueUserWorkItem(new WaitCallback(DoTask), _handle);

            // запускаем машину
            _textMachine.Start();

            // проверяем состояние оно должно быть Running
            Assert.AreEqual(_textMachine.State, RollTextMachine.MachineState.Running);

            // дождемся конца обработки текста
            WaitHandle.WaitAny(new WaitHandle[] { _handle });

            // теперь проверим что у при длиннне = длинне индикатора нам покажут всю строку
            s_isTextPart = false;
            _textMachine.RolledText = "12345";
            _textMachine.Start();
            // дождемся конца обработки текста
            WaitHandle.WaitAny(new WaitHandle[] { _handle });
            _textMachine.NeedSetText -= TextMachine_NeedSetText;
        }

        private void DoTask(object state)
        {
        }

        private static bool s_isTextPart;
        private bool _firstTime = true;

        private bool TextMachine_NeedSetText(string text)
        {
            //у нас текст должен быть длинной в 5 символов т.к длинна индикатора=5
            Assert.AreEqual(5, text.Length, "Длинна текста не соответсвует ожидаемой");

            //если это не часть текста а целый
            if (!s_isTextPart)
            {
                Assert.AreEqual("12345", text);
                _handle.Set();
                _textMachine.Stop();
            }
            else
            {
                //Если первый раз 
                if (_firstTime)
                {
                    Assert.AreEqual("12345", text);
                    _firstTime = false;
                }
                else
                {
                    Assert.AreEqual("23456", text);
                    _handle.Set();
                    _textMachine.Stop();
                }
            }

            return true;
        }

        /// <summary>
        ///A test for SetRealRolledText
        ///</summary>
        [TestMethod]
        [DeploymentItem("Yaw.Core.dll")]
        public void SetRealRolledTextTest()
        {
            // если вдруг наша машина не остановлена
            if (_textMachine.State == RollTextMachine.MachineState.Running)
                _textMachine.Stop();

			var expectedText = new StringBuilder("1234567");
			expectedText.Append(' ', RollTextMachine.ROLL_LOOP_DELIM_SPACE_COUNT);

            // проверяем при тексте > длинны индикатора
            _textMachine.RolledText = "1234567";
            _accessor.SetRealRolledText();
			Assert.AreEqual(expectedText.ToString(), _accessor._realRolledText);

            // проверяем при тексте > длинны индикатора
            _textMachine.RolledText = "1234";
            _accessor.SetRealRolledText();
            Assert.AreEqual("1234 ", _accessor._realRolledText);
        }

        /// <summary>
        ///A test for ResetCounters
        ///</summary>
        [TestMethod]
        [DeploymentItem("Yaw.Core.dll")]
        public void ResetCountersTest()
        {
            //если вдруг наша машина не остановлена
            if (_textMachine.State == RollTextMachine.MachineState.Running)
                _textMachine.Stop();

            //проверим что наша позиция сменилась
            _accessor._currentPosition = 5;
            _accessor.ResetCounters();
            Assert.AreEqual(-1, _accessor._currentPosition);
        }

        /// <summary>
        ///A test for RaiseNeedSetText
        ///</summary>
        [TestMethod]
        [DeploymentItem("Yaw.Core.dll")]
        public void RaiseNeedSetTextTest()
        {
            // если вдруг наша машина не остановлена
            if (_textMachine.State == RollTextMachine.MachineState.Running)
                _textMachine.Stop();

            // проверим что событие вызвалось
            _textMachine.NeedSetText += TestEventRised;
            _accessor.RaiseNeedSetText("");

            Assert.IsTrue(_rised);
            _textMachine.NeedSetText -= TestEventRised;
        }

        private bool TestEventRised(string text)
        {
            _rised = true;
            return true;
        }

        // признак того что событие вызывалось
        private bool _rised;

        /// <summary>
        ///A test for GetNextTextPart
        ///</summary>
        [TestMethod]
        [DeploymentItem("Yaw.Core.dll")]
        public void GetNextTextPartTest()
        {
            string textPart;
            string textPartExpected = "12345";
            bool expected = false;
            bool actual;

            // если вдруг наша машина не остановлена
            if (_textMachine.State == RollTextMachine.MachineState.Running)
                _textMachine.Stop();

            // установим текст
            _accessor._realRolledText = "1234567";

            // и сразу проверим что если у нас последня буква то мы переходим на первую
            _accessor._currentPosition = 6;
            actual = _accessor.GetNextTextPart(out textPart);
            Assert.AreEqual(textPartExpected, textPart);
            Assert.AreEqual(expected, actual);
            Assert.AreEqual(0, _accessor._currentPosition);

            // проверим что, если мы ближе к концу строки то нам вернутся 
            // последние буквы начиная с текущего индекса +1 и несколько первых
            _accessor._currentPosition = 4;
            textPartExpected = "67123";
            actual = _accessor.GetNextTextPart(out textPart);
            Assert.AreEqual(textPartExpected, textPart);
            Assert.AreEqual(expected, actual);

            // ну и под конец получим значение True
            _accessor._currentPosition = 1;
            textPartExpected = "34567";
            expected = true;
            actual = _accessor.GetNextTextPart(out textPart);
            Assert.AreEqual(textPartExpected, textPart);
            Assert.AreEqual(expected, actual);
        }

        /// <summary>
        ///A test for Dispose
        ///</summary>
        [TestMethod]
        public void DisposeTest()
        {
            // сохраняем ссылки
            RollTextMachine disposableMachine = _textMachine;
            RollTextMachine_Accessor disposableAccessor = _accessor;

            // создаем машинку по новой, т.к после этого теста старая не работоспособна
            _textMachine = new RollTextMachine(5, false);
            _accessor = new RollTextMachine_Accessor(new PrivateObject(_textMachine));

            // т.к Dispose убивает поток нужно сначала выполнить старт
            // если машинка не запущена
            if (disposableMachine.State != RollTextMachine.MachineState.Running)
            {
                disposableMachine.RolledText = "1234465767";
                disposableMachine.Start();
            }

            // проверим что поток создался и состояние == запущено
            if (disposableMachine.State != RollTextMachine.MachineState.Running
                && disposableAccessor._rollTextThread == null)
            {
                Assert.Fail("Не удалось провести тест т.к метод старт выполнился не корректно");
            }

            // проверим что поток обнулился, переменная _disposed = true и состояние = stoped
            disposableMachine.Dispose();
            Assert.IsNull(disposableAccessor._rollTextThread, "Поток не остановлен");
            Assert.IsTrue(disposableAccessor._disposed);
        }
    }
}