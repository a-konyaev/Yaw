using System;
using System.Text;
using System.Threading;
using Yaw.Core.Extensions;
using Yaw.Core.Utils.Threading;

namespace Yaw.Core.Utils.Text
{
    /// <summary>
    /// ����� ������������ ��������� ������
    /// </summary>
    public sealed class RollTextMachine :
        IDisposable
    {
        /// <summary>
        /// ��������� �����������
        /// </summary>
        public enum MachineState
        {
            /// <summary>
            /// ��������� ������ ��������
            /// </summary>
            Running,
            /// <summary>
            /// ��������� ������ �����������
            /// </summary>
            Stopped,
        }

        /// <summary>
        /// �������� �� ��������� ��� ���������� ������ ��� ��������� (� ����)
        /// </summary>
        public const int DEFAULT_UPDATE_TEXT_DELAY = 250;
        /// <summary>
        /// �������� �� ��������� ��� ������ ��������� ������ (� ����)
        /// </summary>
        public const int DEFAULT_START_DELAY = 500;
        /// <summary>
        /// �������� �� ��������� ��� ��������� ��������� ������ (� ����)
        /// </summary>
        public const int DEFAULT_END_DELAY = 0;
        /// <summary>
        /// ���-�� ��������, ������� ����� ����������� � ����� ��������������� ������, 
        /// ���� ����� ��������� �� ��������� �� ����������, 
        /// ����� �������� ���� ���� ������������� ������
        /// </summary>
        public const int ROLL_LOOP_DELIM_SPACE_COUNT = 10;
        /// <summary>
        /// �������� �� ��������� ��� ���������� ������ ��� ��������� (� ����)
        /// </summary>
        public int UpdateTextDelay = DEFAULT_UPDATE_TEXT_DELAY;
        /// <summary>
        /// �������� �� ��������� ��� ������ ��������� ������ (� ����)
        /// </summary>
        public int StartDelay = DEFAULT_START_DELAY;
        /// <summary>
        /// �������� �� ��������� ��� ��������� ��������� ������ (� ����)
        /// </summary>
        public int EndDelay = DEFAULT_END_DELAY;

        /// <summary>
        /// ������� � ������������� ���������� ������ ������ ��������� ������
        /// </summary>
        private readonly ManualResetEvent _stopRollTextEvent = new ManualResetEvent(false);
        /// <summary>
        /// ������� ������, ������� ����� ���������� ��� ��������� ��������� ����� ������
        /// </summary>
        /// <param name="text"></param>
        public delegate bool NeedSetTextDelegate(string text);
        /// <summary>
        /// ������� "����� ���������� ����� �����"
        /// </summary>
        public event NeedSetTextDelegate NeedSetText;
        /// <summary>
        /// ��������� ������� "����� ���������� ����� �����"
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
        /// ������� ����, ��� ������ ��� ������
        /// </summary>
        private bool _disposed;
        /// <summary>
        /// ������������ ����� �������������� ������� ������
        /// </summary>
        private readonly int _maxTextLength;
        /// <summary>
        /// ������������ �� �����, ���� ��� ����� ������ ����������� ����������
        /// </summary>
        private readonly bool _rollIfLessThanMaxLen;
        /// <summary>
        /// �������������� �����, ������� ����� ������������
        /// </summary>
        private string _rolledText = "";
        /// <summary>
        /// ������� �������������� �����, ������� �������������� ��������.
        /// ��� �������������� �����, ������� ����� ������������ + ������� ��� ���������� ������
        /// </summary>
        private string _realRolledText = "";
        /// <summary>
        /// �����, ������� ����� ������������
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
        /// ����� ��������� ������
        /// </summary>
        private Thread _rollTextThread;
        /// <summary>
        /// ������ �������������
        /// </summary>
        private static readonly object s_syncRoot = new object();

        /// <summary>
        /// ������� ��������� �����������
        /// </summary>
        public MachineState State
        {
            get;
            private set;
        }

        /// <summary>
        /// �����������
        /// </summary>
        /// <param name="maxTextLength">������������ ����� ������, ������� ����� ����������</param>
        /// <param name="rollIfLessThanMaxLen">������������ �� �����, ���� ��� ����� ������ ����������� ����������</param>
        public RollTextMachine(int maxTextLength, bool rollIfLessThanMaxLen)
        {
            CodeContract.Requires(maxTextLength > 0);

            _maxTextLength = maxTextLength;
            _rollIfLessThanMaxLen = rollIfLessThanMaxLen;

            State = MachineState.Stopped;
        }

        /// <summary>
        /// ��������� ���������
        /// </summary>
        public void Start()
        {
            lock (s_syncRoot)
            {
                if (_disposed)
                    return;

                // ���� ��������� � ��� ��� ��������
                if (State == MachineState.Running)
                    // �� ������ �� ������
                    return;
                
                StopRollTextThread();
                ResetCounters();
                SetRealRolledText();

                // ���� ���� ��� ������������
                if (_rolledText.Length > 0)
                {
                    _stopRollTextEvent.Reset();

                    // ���� ����� ������ ��� ��������� ������, ��� ������������
                    if (_rolledText.Length > _maxTextLength ||
                        // ��� ����� ������������ ���� ���� ����� ������ ������������
                        _rollIfLessThanMaxLen)
                    {
                        // ��������� ����� ������������� �������� ������
                        _rollTextThread = ThreadUtils.StartBackgroundThread(RollLongTextThreadMethod);
                    }
                    else
                    {
                        // ��������� ����� ������������� ��������� ������
                        _rollTextThread = ThreadUtils.StartBackgroundThread(RollShortTextThreadMethod);
                    }
                }

                State = MachineState.Running;
            }
        }

        /// <summary>
        /// ���������� ���������
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
        /// ����� ��������� ��������� ������
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
        /// ����� ��������� �������� ������
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
        /// ��������� ������� ��������������� ������
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
                // ��������� ���������, ����� ��������� ��� ����� ����������
                sb.Append(' ', _maxTextLength - _rolledText.Length);
            else
                // ��������� �������, ����� �������� ���� ���� ��������� ������
                sb.Append(' ', ROLL_LOOP_DELIM_SPACE_COUNT);

            _realRolledText = sb.ToString();
        }

        /// <summary>
        /// ������� ������� � �������������� ������ ������� ������������� �������
        /// </summary>
        private int _currentPosition = -1;

        /// <summary>
        /// �������� ��������, ���. ������������ ��� ��������� ��������� ������ ������
        /// </summary>
        private void ResetCounters()
        {
            _currentPosition = -1;
        }

        /// <summary>
        /// �������� ��������� ������ ������
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
        /// �������� ��������� ������
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
