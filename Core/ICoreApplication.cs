using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using Yaw.Core.Diagnostics;
using Yaw.Core.Configuration;

namespace Yaw.Core
{
    /// <summary>
    /// ��������� ����������
    /// </summary>
    public interface ICoreApplication : ILoggerContainer
    {
        /// <summary>
        /// ������������ ����������
        /// </summary>
        string Name { get; }

        #region ������������

        /// <summary>
        /// ������� ������������ ����������
        /// </summary>
        ApplicationConfig Config { get; }

        /// <summary>
        /// ��������� ����� ������������ ����������
        /// </summary>
        /// <param name="newConfig">����� ������ ����������</param>
        /// <param name="force">����� �� ��������� ����� ������, ���� ���� �� �� ���������� �� �������</param>
        /// <returns>
        /// true - ������������ ���������, 
        /// false - ������������ �� ���������, �.�. ����� �� ���������� �� �������
        /// </returns>
        bool ApplyNewConfig(ApplicationConfig newConfig, bool force);

		/// <summary>
		/// ������� ��������� ������������ ����������
		/// </summary>
		event EventHandler<ConfigUpdatedEventArgs> ConfigUpdated;

        #endregion

        #region �����������

        /// <summary>
        /// ������� ����������� ����������
        /// </summary>
        TraceLevel TraceLevel { get; }

        /// <summary>
        /// �����, � ������� ����� ����������� ���-����� ����������
        /// </summary>
        string LogFileFolder { get; }

        /// <summary>
        /// ������� ����� ������ � �������� ������ � ������� �����������,
        /// ������� ����� ������ � ��������� ����� � �������� �����
        /// </summary>
        /// <param name="loggerName">��� �������</param>
        /// <param name="traceLevel">������� �����������</param>
        /// <returns></returns>
        ILogger CreateLogger(string loggerName, TraceLevel traceLevel);

        /// <summary>
        /// ������� �� �����
        /// </summary>
        bool LoggerEnabled { get; set; }

        #endregion

        #region ����������

        /// <summary>
        /// ����� ����������, ������� ��������� �������� ���������
        /// </summary>
        /// <typeparam name="T">������������� ���������</typeparam>
        /// <returns>
        /// null - ����������, ����������� �������� ���������, �� �������
        /// ������ �� ���������� - ������ ��������� ����������, ����������� �������� ���������
        /// </returns>
        T FindSubsystemImplementsInterface<T>();

        /// <summary>
        /// ����� ����������, ������� ��������� �������� ���������
        /// </summary>
        /// <typeparam name="T">������������� ���������</typeparam>
        /// <returns>
        /// ������ �� ���������� - ������ ��������� ����������, ����������� �������� ���������
        /// </returns>
        /// <exception cref="System.Exception">����������, ����������� �������� ���������, �� �������</exception>
        T FindSubsystemImplementsInterfaceOrThrow<T>();

        /// <summary>
        /// ����� ��� ����������, ������� ��������� �������� ���������
        /// </summary>
        /// <typeparam name="T">������������� ���������</typeparam>
        /// <returns>������ ��������� ���������</returns>
        IEnumerable<T> FindAllSubsystemsImplementsInterface<T>();

        /// <summary>
        /// ���������� ���������� �� �� ����������, � ������ �� ���������� ���������� ����������
        /// </summary>
        /// <typeparam name="T">��������� ������������� ����������</typeparam>
        /// <returns></returns>
        T GetSubsystemOrThrow<T>() where T : ISubsystem;

        /// <summary>
        /// ���������� ���������� �� �� ����������, � ������ �� ���������� ���������� ���������� � �������� �������.
        /// </summary>
        /// <typeparam name="T">��������� ������������� ����������</typeparam>
        /// <param name="errorMsg"></param>
        /// <returns></returns>
        T GetSubsystemOrThrow<T>(string errorMsg) where T : ISubsystem;

        /// <summary>
        /// read-only ������ ���� ���������
        /// </summary>
        IEnumerable<ISubsystem> Subsystems { get; }

        /// <summary>
        /// ���������� runtime-��������� ���������� �� �� ������������
        /// </summary>
        /// <param name="sName">������������ ����������</param>
        /// <exception cref="ArgumentNullException">���� ������������ �� ������</exception>
        /// <exception cref="ArgumentException">���� ������������ �� ������</exception>
        /// <returns>��������� ��� null, ���� ���������� � �������� ������������� �� �������</returns>
        ISubsystem GetSubsystem(String sName);

        /// <summary>
        /// ���������� runtime-��������� ���������� �� �� ������������ 
        /// � ����������� � ����������� ����
        /// </summary>
        /// <param name="sName">������������ ����������</param>
        /// <typeparam name="T">��� ����������</typeparam>
        /// <returns>��������� ��� null</returns>
        /// <exception cref="ArgumentNullException">���� ������������ �� ������</exception>
        /// <exception cref="ArgumentException">���� ������������ �� ������</exception>
        /// <exception cref="ArgumentException">���� ��������� ����������, 
        /// ��������� �� ������������ �� ����� ���� �������� � ���� <typeparamref name="T"/></exception>
        T GetSubsystem<T>(String sName) where T : ISubsystem;

        /// <summary>
        /// ���������� ������ ���������� ������������� runtime-��������� ����������,
        /// ���� �� ����� ���� �������� � ����������� generic ���� (<typeparamref name="T"/>).
        /// </summary>
        /// <remarks>
        /// ��� ������ ������������� ������ ������������� ����������.
        /// </remarks>
        /// <typeparam name="T">��� ����������</typeparam>
        /// <returns>��������� ��� null</returns>
        T GetSubsystem<T>() where T : ISubsystem;

        /// <summary>
        /// ���������� ��� ���������� ���������� ��� ����������� ��� <typeparamref name="T"/>.
        /// </summary>
        /// <typeparam name="T">��� ����������.</typeparam>
        /// <returns>��������� ��������� ��������� ������ � �� ��������������, 
        /// ��� �������� ��� ���� ��������� � ����������.</returns>
        List<KeyValuePair<String, T>> GetSubsystems<T>() where T : ISubsystem;

        #endregion

		#region ������ ����������

		/// <summary>
		/// ������ ����������
		/// </summary>
		Version ApplicationVersion { get; }

		#endregion

        #region ���������� ������

        /// <summary>
        /// ������� ���������� ������ ����������
        /// </summary>
        WaitHandle ExitEvent { get; }

		/// <summary>
		/// ������� ������ �� ����������
		/// </summary>
		event EventHandler<ApplicationExitEventArgs> Exited;

        /// <summary>
        /// �������, ����� ���������� �������� ������
        /// </summary>
        void WaitForExit();

        /// <summary>
        /// ��������� ������ ����������
        /// </summary>
        /// <param name="exitType">��� ��������� ������ ����������</param>
        void Exit(ApplicationExitType exitType);

        #endregion
    }
}
