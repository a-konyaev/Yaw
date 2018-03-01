using System;
using System.Diagnostics;
using Yaw.Core.Diagnostics;
using Yaw.Core.Configuration;

namespace Yaw.Core
{
    /// <summary>
    /// ��Q������, �E�����E��E�Z �������A����E��E�E������E
    /// </summary>
    public interface ISubsystem : ILoggerContainer, IDisposable
    {
        /// <summary>
        /// ���E���G�E �E�E����E���C�� �E������E
        /// </summary>
        ICoreApplication Application { get; set; }

        /// <summary>
        /// ��G���������E �E������E
        /// </summary>
        /// <param name="config"></param>
        void Init(SubsystemConfig config);

        /// <summary>
        /// ���E�G�� �M��� �E�S�������E �E������E
        /// </summary>
        /// <param name="newConfig">�M���E�E�S��-��E�E�Q �E����������E�E������E</param>
        void ApplyNewConfig(SubsystemConfig newConfig);

		/// <summary>
		/// ��@���� ��E�D�G� �E�S�������� �E������E
		/// </summary>
		event EventHandler<ConfigUpdatedEventArgs> ConfigUpdated;

        /// <summary>
        /// ȁE �E������E
        /// </summary>
        string Name { get; }

        #region ��B���A����

        /// <summary>
        /// ������[ ����������E�E������E
        /// </summary>
        TraceLevel TraceLevel { get; }

        /// <summary>
        /// ���E, �E�E����E����E��������E� �E�E����E�E������E
        /// </summary>
        string LogFileFolder { get; }

        /// <summary>
        /// ������� �D�@����E��E�E���� �E�� �E�C��E�Z�E����
        /// </summary>
        bool SeparateLog
        {
            get;
            set;
        }

        /// <summary>
        /// ����@�E���� �E���� �E������E
        /// </summary>
        void DisposeLogger();

        #endregion

        /// <summary>
        /// ��A����A�� �M�E�E�E�E�������� �E������E
        /// </summary>
        int DisposeOrder { get; }
    }
}
