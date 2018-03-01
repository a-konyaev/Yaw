using System;
using System.Text;
using System.Threading;
using Yaw.Core.Extensions;
using Yaw.Core.Utils.Threading;

namespace Yaw.Core.Utils.Text
{
    /// <summary>
    /// Класс обеспечивает прокрутку текста
    /// </summary>
    public sealed class RollTextMachine :
        IDisposable
    {
        /// <summary>
        /// Состояния прокрутчика
        /// </summary>
        public enum MachineState
        {
            /// <summary>
            /// Прокуртка текста запущена
            /// </summary>
            Running,
            /// <summary>
            /// Прокуртка текста остановлена
            /// </summary>
            Stopped,
        }

        /// <summary>
        /// Задержка по умолчанию при обновлении текста при прокрутки (в мсек)
        /// </summary>
        public const int DEFAULT_UPDATE_TEXT_DELAY = 250;
        /// <summary>
        /// Задержка по умолчанию при начале прокрутки текста (в мсек)
        /// </summary>
        public const int DEFAULT_START_DELAY = 500;
        /// <summary>
        /// Задержка по умолчанию при окончании прокрутки текста (в мсек)
        /// </summary>
        public const int DEFAULT_END_DELAY = 0;
        /// <summary>
        /// Кол-во пробелов, которое будет добавляться в конец прокручиваемого текста, 
        /// если текст полностью не умещается на индикаторе, 
        /// чтобы выделить один цикл прокручивания текста
        /// </summary>
        public const int ROLL_LOOP_DELIM_SPACE_COUNT = 10;
        /// <summary>
        /// Задержка по умолчанию при обновлении текста при прокрутки (в мсек)
        /// </summary>
        public int UpdateTextDelay = DEFAULT_UPDATE_TEXT_DELAY;
        /// <summary>
        /// Задержка по умолчанию при начале прокрутки текста (в мсек)
        /// </summary>
        public int StartDelay = DEFAULT_START_DELAY;
        /// <summary>
        /// Задержка по умолчанию при окончании прокрутки текста (в мсек)
        /// </summary>
        public int EndDelay = DEFAULT_END_DELAY;

        /// <summary>
        /// Событие о необходимости остановить работу потока прокрутки текста
        /// </summary>
        private readonly ManualResetEvent _stopRollTextEvent = new ManualResetEvent(false);
        /// <summary>
        /// Делегат метода, который будет вызываться для установки очередной части текста
        /// </summary>
        /// <param name="text"></param>
        public delegate bool NeedSetTextDelegate(string text);
        /// <summary>
        /// Событие "Нужно установить новый текст"
        /// </summary>
        public event NeedSetTextDelegate NeedSetText;
        /// <summary>
        /// Возбудить событие "Нужно установить новый текст"
        /// </summary>
        /// <param name="text"></param>
        private bool RaiseNeedSetText(string text)
        {
            var handler = NeedSetText;
            if (handler != null)
                return handler(text);

            return false;
        }

        /// <summary>
        /// Признак того, что объект был удален
        /// </summary>
        private bool _disposed;
        /// <summary>
        /// Максимальная длина прокручиваемой области текста
        /// </summary>
        private readonly int _maxTextLength;
        /// <summary>
        /// прокручивать ли текст, если его длина меньше максимально допустимой
        /// </summary>
        private readonly bool _rollIfLessThanMaxLen;
        /// <summary>
        /// Прокручиваемый текст, который видит пользователь
        /// </summary>
        private string _rolledText = "";
        /// <summary>
        /// Реально прокручиваемый текст, который прокручивается циклично.
        /// Это прокручиваемый текст, который видит пользователь + пробелы для разделения циклов
        /// </summary>
        private string _realRolledText = "";
        /// <summary>
        /// Текст, который нужно прокручивать
        /// </summary>
        public string RolledText
        {
            get
            {
                return _rolledText;
            }
            set
            {
                CodeContract.Requires(!string.IsNullOrEmpty(value));

                lock (s_syncRoot)
                {
                    var currentState = State;
                    if (currentState == MachineState.Running)
                        Stop();

                    _rolledText = value;

                    if (currentState == MachineState.Running)
                        Start();
                }
            }
        }

        /// <summary>
        /// Поток прокрутки текста
        /// </summary>
        private Thread _rollTextThread;
        /// <summary>
        /// Объект синхронизации
        /// </summary>
        private static readonly object s_syncRoot = new object();

        /// <summary>
        /// Текущее состояние прокрутчика
        /// </summary>
        public MachineState State
        {
            get;
            private set;
        }

        /// <summary>
        /// Конструктор
        /// </summary>
        /// <param name="maxTextLength">максимальная длина текста, который можно отобразить</param>
        /// <param name="rollIfLessThanMaxLen">прокручивать ли текст, если его длина меньше максимально допустимой</param>
        public RollTextMachine(int maxTextLength, bool rollIfLessThanMaxLen)
        {
            CodeContract.Requires(maxTextLength > 0);

            _maxTextLength = maxTextLength;
            _rollIfLessThanMaxLen = rollIfLessThanMaxLen;

            State = MachineState.Stopped;
        }

        /// <summary>
        /// Запустить прокрутку
        /// </summary>
        public void Start()
        {
            lock (s_syncRoot)
            {
                if (_disposed)
                    return;

                // если прокрутка и так уже включена
                if (State == MachineState.Running)
                    // то ничего не делаем
                    return;
                
                StopRollTextThread();
                ResetCounters();
                SetRealRolledText();

                // если есть что прокручивать
                if (_rolledText.Length > 0)
                {
                    _stopRollTextEvent.Reset();

                    // если длина текста для прокрутки больше, чем максимальная
                    if (_rolledText.Length > _maxTextLength ||
                        // или нужно прокручивать даже если длина меньше максимальной
                        _rollIfLessThanMaxLen)
                    {
                        // запускаем поток прокручивания длинного текста
                        _rollTextThread = ThreadUtils.StartBackgroundThread(RollLongTextThreadMethod);
                    }
                    else
                    {
                        // запускаем поток прокручивания короткого текста
                        _rollTextThread = ThreadUtils.StartBackgroundThread(RollShortTextThreadMethod);
                    }
                }

                State = MachineState.Running;
            }
        }

        /// <summary>
        /// Остановить прокрутку
        /// </summary>
        public void Stop()
        {
            lock (s_syncRoot)
            {
                if (_disposed)
                    return;

                StopRollTextThread();
                State = MachineState.Stopped;
            }
        }

        /// <summary>
        /// Метод прокрутки короткого текста
        /// </summary>
        private void RollShortTextThreadMethod()
        {
            try
            {
                while (!RaiseNeedSetText(_rolledText))
                {
                    if (_stopRollTextEvent.WaitOne(UpdateTextDelay, false))
                        return;
                }
            }
            catch (ThreadAbortException)
            {
            }
        }

        /// <summary>
        /// Метод прокрутки длинного текста
        /// </summary>
        private void RollLongTextThreadMethod()
        {
            try
            {
                string textPart;
                GetNextTextPart(out textPart);
                RaiseNeedSetText(textPart);
                Thread.Sleep(StartDelay);

                while (true)
                {
                    var end = GetNextTextPart(out textPart);
                    RaiseNeedSetText(textPart);

                    if (end)
                        Thread.Sleep(EndDelay);

                    if (_stopRollTextEvent.WaitOne(UpdateTextDelay, false))
                        return;
                }
            }
            catch (ThreadAbortException)
            {
            }
        }

        /// <summary>
        /// Установка реально прокручиваемого текста
        /// </summary>
        private void SetRealRolledText()
        {
            if (_rolledText.Length == 0)
            {
                _realRolledText = "";
                return;
            }

            var sb = new StringBuilder(_rolledText);

            if (_rolledText.Length <= _maxTextLength)
                // дополняем пробелами, чтобы заполнить всю длину индикатора
                sb.Append(' ', _maxTextLength - _rolledText.Length);
            else
                // добавляем пробелы, чтобы выделить один цикл прокрутки текста
                sb.Append(' ', ROLL_LOOP_DELIM_SPACE_COUNT);

            _realRolledText = sb.ToString();
        }

        /// <summary>
        /// Текущая позиция в прокручиваемой строке первого отображаемого символа
        /// </summary>
        private int _currentPosition = -1;

        /// <summary>
        /// Сбросить счетчики, кот. используются при получении очередной порции текста
        /// </summary>
        private void ResetCounters()
        {
            _currentPosition = -1;
        }

        /// <summary>
        /// Получить очередную порцию текста
        /// </summary>
        /// <returns></returns>
        private bool GetNextTextPart(out string textPart)
        {
            if (++_currentPosition == _realRolledText.Length)
                _currentPosition = 0;

            if (_currentPosition + _maxTextLength <= _realRolledText.Length)
            {
                textPart = _realRolledText.Substring(_currentPosition, _maxTextLength);
            }
            else
            {
                textPart = _realRolledText.Substring(_currentPosition, _realRolledText.Length - _currentPosition);
                textPart += _realRolledText.Substring(0, _maxTextLength - textPart.Length);
            }

            return _currentPosition + _maxTextLength == _realRolledText.Length;
        }

        /// <summary>
        /// Прервать прокрутку текста
        /// </summary>
        private void StopRollTextThread()
        {
            if (_rollTextThread != null)
            {
                _stopRollTextEvent.Set();
                if (!_rollTextThread.Join(100))
                    _rollTextThread.SafeAbort();
                _rollTextThread = null;
            }
        }

        #region IDisposable Members

        public void Dispose()
        {
            lock (s_syncRoot)
            {
                StopRollTextThread();
                _disposed = true;
            }
            GC.SuppressFinalize(this);
        }

        #endregion
    }
}
