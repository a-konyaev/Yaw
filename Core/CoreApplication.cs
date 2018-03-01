using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Threading;
using Yaw.Core.Configuration;
using Yaw.Core.Diagnostics;
using Yaw.Core.Diagnostics.Default;
using Yaw.Core.Extensions;
using Yaw.Core.Utils;
using Yaw.Core.Utils.Threading;

namespace Yaw.Core
{
    /// <summary>
    /// ����������
    /// </summary>
    public class CoreApplication : ICoreApplication
    {
		/// <summary>
        /// ������� ����������
        /// </summary>
        public static ICoreApplication Instance
        {
            get;
            private set;
        }

        /// <summary>
        /// ������������ ����������
        /// </summary>
        public string Name
        {
            get;
            private set;
        }

        #region �������������

        /// <summary>
        /// �����������
        /// </summary>
        public CoreApplication()
		{
            Instance = this;

            // �������� ������ ����������
            Config = LoadConfig();
            // ������� ��� ����������
            Name = string.IsNullOrEmpty(Config.Name) ? Guid.NewGuid().ToString() : Config.Name;
            // �������������� ������
            InitLogger();
            // �������� ����������
            CreateSubsystems();
		}

        /// <summary>
        /// �������� ������������
        /// </summary>
        /// <returns></returns>
        protected virtual ApplicationConfig LoadConfig()
        {
            try
            {
                var exeConfig = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
                var config = (ApplicationConfig)exeConfig.GetSection(ApplicationConfig.SECTION_NAME);
                if (config == null)
                    throw new Exception("������ �� �������: " + ApplicationConfig.SECTION_NAME);

                return config;
            }
            catch (Exception ex)
            {
                throw new ConfigurationErrorsException("������ ��������� ������������ ����������", ex);
            }
        }

        /// <summary>
        /// ������������� �������
        /// </summary>
        private void InitLogger()
        {
            // ������� ��������� �������
            LogFileFolder = Config.LogFileFolder.Path;
            if (string.IsNullOrEmpty(LogFileFolder))
                LogFileFolder = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);

            TraceLevel = GetTraceLevelByName(Config.TraceLevelName, TraceLevel.Error);

            EventDispatcher.Init(Config.DiagnosticsConfig);
            FileWriter.Init(LogFileFolder);

            // �������� ������ � ������� ������ �� ��������� ������
            _logger = (Logger)CreateLogger(Name, TraceLevel);
        }

        /// <summary>
        /// ������� ����������
        /// </summary>
        private void CreateSubsystems()
        {
            if (Config.Subsystems.Count == 0)
                throw new ConfigurationErrorsException("������ ��������� ����");

            foreach (SubsystemConfig subsystemConfig in Config.Subsystems)
            {
                Subsystem subsystem;
                var subsystemName = subsystemConfig.SubsystemName;

                try
                {
                    // �������� ����������
                    var type = Type.GetType(subsystemConfig.SubsystemTypeName, true);
                    subsystem = (Subsystem)Activator.CreateInstance(type);
                    
                    // ������� ���
                    subsystem.Name = subsystemName;

                    // ������� � ��������� ���������
                    AddSubsystem(subsystemName, subsystem);

                    // ������� ��������� �����������
                    subsystem.TraceLevel = GetTraceLevelByName(subsystemConfig.TraceLevelName, TraceLevel);
                    subsystem.LogFileFolder = string.IsNullOrEmpty(subsystemConfig.LogFileFolder)
                        ? LogFileFolder
                        // ������� ���������� �������� ������������ ������ ��������
                        : LogFileFolder + "/" + subsystemConfig.LogFileFolder;
                    subsystem.SeparateLog = subsystemConfig.SeparateLog;

                    // ������� ���������� ����� ��� ��������
                    subsystem.DisposeOrder = subsystemConfig.DisposeOrder;

                    // ���������� �� ������� ��������� ������������ ����������
					subsystem.ConfigUpdated += SubsystemConfigUpdated;

                    Logger.LogVerbose("������� ���������� {0}", subsystemName);
                }
                catch (Exception ex)
                {
                    Logger.LogException("������ �������� ���������� {0}: {1}", ex, subsystemName, ex.Message);
                    throw new Exception("������ �������� ���������� " + subsystemName, ex);
                }
            }

            // ����������������� ����������
            foreach (SubsystemConfig subsystemConfig in Config.Subsystems)
            {
                var subsystemName = subsystemConfig.SubsystemName;
                try
                {
                    var subsystem = GetSubsystem(subsystemName);
                    subsystem.Init(subsystemConfig);
                    Logger.LogVerbose("��������� ������������� ���������� {0}", subsystemName);
                }
                catch (Exception ex)
                {
                    Logger.LogException("������ ������������� ���������� {0}: {1}", ex, subsystemName, ex.Message);
                    throw new Exception("������ ������������� ���������� " + subsystemName, ex);
                }
            }
        }

        /// <summary>
        /// ���������� ������� ����������� �� ��������
        /// </summary>
        /// <param name="traceLevelName">�������� ������ �����������</param>
        /// <param name="defaultTraceLevel">������� ����������� �� ���������</param>
        /// <returns></returns>
        private static TraceLevel GetTraceLevelByName(string traceLevelName, TraceLevel defaultTraceLevel)
        {
            if (string.IsNullOrEmpty(traceLevelName))
                return defaultTraceLevel;

            try
            {
                return (TraceLevel)Enum.Parse(typeof(TraceLevel), traceLevelName);
            }
            catch
            {
                throw new Exception(string.Format("����������� ����� ������� �����������: '{0}'", traceLevelName));
            }
        }

        #endregion

        #region ������������

		/// <summary>
		/// ���������� ������� ��������� ������������ ����������
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void SubsystemConfigUpdated(object sender, ConfigUpdatedEventArgs e)
		{
			// ������� ����������
			var subsystem = (ISubsystem)sender;

			// ���������� ����� ������������
			subsystem.ApplyNewConfig(Config.Subsystems[subsystem.Name]);

			// ������� ������� �������� ������� ��� ����� ����������
			ConfigUpdated.RaiseEvent(this, e);
		}

        /// <summary>
        /// ������� ������������ ����������
        /// </summary>
        public ApplicationConfig Config
        {
            get;
            private set;
        }

        /// <summary>
        /// ��������� ����� ������������ ����������
        /// </summary>
        /// <param name="newConfig">����� ������ ����������</param>
        /// <param name="force">����� �� ��������� ����� ������, ���� ���� �� �� ���������� �� �������</param>
        /// <returns>
        /// true - ������������ ���������, 
        /// false - ������������ �� ���������, �.�. ����� �� ���������� �� �������
        /// </returns>
        public bool ApplyNewConfig(ApplicationConfig newConfig, bool force)
        {
            CodeContract.Requires(newConfig != null);

            // ���� �� ����� ��������� ������ � ����� ������ � 
            // ����� ������ �� ���������� �� ��������
            if (!force && Config.Equals(newConfig))
                // �� ��������� ��� �� �����
                return false;

            Logger.LogVerbose("���������� ����� ������������...");

            // ������� ������ �� �����
            Config = newConfig;

            // �������� ����� ������� ���������
            foreach (SubsystemConfig subsystemConfig in Config.Subsystems)
            {
                var subsystemName = subsystemConfig.SubsystemName;
                try
                {
                    var subsystem = GetSubsystem(subsystemName);
                    subsystem.ApplyNewConfig(subsystemConfig);
                    Logger.LogVerbose("��������� ����������������� ���������� {0}", subsystemName);
                }
                catch (Exception ex)
                {
                    Logger.LogException("������ ����������������� ���������� {0}: {1}", ex, subsystemName, ex.Message);
                    throw new Exception("������ ����������������� ���������� " + subsystemName, ex);
                }
            }

            return true;
        }

		/// <summary>
		/// ������� ��������� ������������ ����������
		/// </summary>
		public event EventHandler<ConfigUpdatedEventArgs> ConfigUpdated;

        #endregion

        #region �����������

        /// <summary>
        /// ������� ����������� ����������
        /// </summary>
        public TraceLevel TraceLevel
        {
            get;
            private set;
        }

        /// <summary>
        /// �����, � ������� ����� ����������� ���-����� ����������
        /// </summary>
        public string LogFileFolder
        {
            get;
            private set;
        }

        /// <summary>
        /// ����� ����������
        /// </summary>
        Logger _logger;
        /// <summary>
        /// ������ ����������
        /// </summary>
        public ILogger Logger
        {
			get { return _logger; }
        }

        /// <summary>
        /// ������� ����� ������ � �������� ������ � ������� �����������,
        /// ������� ����� ������ � ��������� ����� � �������� �����
        /// </summary>
        /// <param name="loggerName">��� �������</param>
        /// <param name="traceLevel">������� �����������</param>
        /// <returns></returns>
        public ILogger CreateLogger(string loggerName, TraceLevel traceLevel)
        {
            var logger = new Logger(loggerName, new TraceLevelFilter(traceLevel), _logger, _loggerEnabled);

            foreach (var filter in EventDispatcher.EventFilters)
            {
                logger.AddFilter(filter);
            }

            return logger;
        }

    	/// <summary>
        /// ������� ������������/������������� ������ � �������
        /// </summary>
        private readonly ManualResetEvent _loggerEnabled = new ManualResetEvent(true);

        /// <summary>
        /// ������� �� �����
        /// </summary>
        public bool LoggerEnabled
        {
            get
            {
                return _loggerEnabled.WaitOne(0);
            }
            set
            {
                if (value)
                    _loggerEnabled.Set();
                else
                    _loggerEnabled.Reset();
            }
        }

        #endregion

        #region ����������

        /// <summary>
        /// ������� ���������
        /// </summary>
        private readonly Dictionary<string, ISubsystem> _subsystems = new Dictionary<string, ISubsystem>();

        /// <summary>
        /// ����� ����������, ������� ��������� �������� ���������
        /// </summary>
        /// <typeparam name="T">������������� ���������</typeparam>
        /// <returns>
        /// null - ����������, ����������� �������� ���������, �� �������
        /// ������ �� ���������� - ������ ��������� ����������, ����������� �������� ���������
        /// </returns>
        public T FindSubsystemImplementsInterface<T>()
        {
            return (T)_subsystems.FirstOrDefault(i => i.Value is T).Value;
        }

        /// <summary>
        /// ����� ����������, ������� ��������� �������� ���������
        /// </summary>
        /// <typeparam name="T">������������� ���������</typeparam>
        /// <returns>
        /// ������ �� ���������� - ������ ��������� ����������, ����������� �������� ���������
        /// </returns>
        /// <exception cref="System.Exception">����������, ����������� �������� ���������, �� �������</exception>
        public T FindSubsystemImplementsInterfaceOrThrow<T>()
        {
            var res = FindSubsystemImplementsInterface<T>();
            if (res == null)
                throw new ArgumentException("���������� �� �������� ����������, ����������� ��������� " + typeof(T).FullName);

            return res;
        }

        /// <summary>
        /// ����� ��� ����������, ������� ��������� �������� ���������
        /// </summary>
        /// <typeparam name="T">������������� ���������</typeparam>
        /// <returns>������ ��������� ���������</returns>
        public IEnumerable<T> FindAllSubsystemsImplementsInterface<T>()
        {
            return _subsystems.Where(i => i.Value is T).Select(i => (T) i.Value);
        }

        /// <summary>
        /// ���������� ���������� �� �� ����������, � ������ �� ���������� ���������� ����������
        /// </summary>
        /// <typeparam name="T">��������� ������������� ����������</typeparam>
        /// <returns></returns>
        public T GetSubsystemOrThrow<T>() where T : ISubsystem
        {
            return GetSubsystemOrThrow<T>("���������� �� �������� ���������� " + typeof(T).FullName);
        }

        /// <summary>
        /// ���������� ���������� �� �� ����������, � ������ �� ���������� ���������� ���������� � �������� �������.
        /// </summary>
        /// <typeparam name="T">��������� ������������� ����������</typeparam>
        /// <param name="errorMsg"></param>
        /// <returns></returns>
        public T GetSubsystemOrThrow<T>(string errorMsg) where T : ISubsystem
        {
            var subsystem = GetSubsystem<T>();
            if (subsystem == null)
                throw new ArgumentException(errorMsg);

            return subsystem;
        }

        public IEnumerable<ISubsystem> Subsystems
        {
            get { return _subsystems.Values; }
        }

        /// <summary>
        /// ��������� ���������� � ����������
        /// </summary>
        /// <remarks>
        /// ���� ����������� ����������� ���������� (�������� <paramref name="name"/> ����� �� null),
        /// �� �������� �� ����� ����� ������ �� ������������
        /// </remarks>
        /// <param name="name">������������ ���������� ��� null</param>
        /// <param name="subsystem">��������� ����������</param>
        public void AddSubsystem(String name, ISubsystem subsystem)
        {
            CodeContract.Requires(subsystem != null);

            if (_subsystems.ContainsKey(name))
                throw new ArgumentException("���������� ��� �������� ���������� � ������������� " + name);

            _subsystems[name] = subsystem;

            // ��������� �������� �������� � ���������� - ������ �� ����������
            subsystem.Application = this;
        }

        /// <summary>
        /// �������� ���������� � �������� ������
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public ISubsystem GetSubsystem(String name)
        {
            CodeContract.Requires(!string.IsNullOrEmpty(name));

            ISubsystem subsystem = null;
            _subsystems.TryGetValue(name, out subsystem);
            return subsystem;
        }

        /// <summary>
        /// �������� ���������� � �������� ������ � ��������� ����
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="name"></param>
        /// <returns></returns>
        public T GetSubsystem<T>(String name) where T : ISubsystem
        {
            var subsystem = GetSubsystem(name);
            if (subsystem == null)
                return default(T);

            if (!(subsystem is T))
            {
                throw new ArgumentException(
                    string.Format("����������� ���������� '{0}' �� ��������� ��������� ��������� {1}",
                                  name, typeof (T).FullName));
            }

            return (T) subsystem;
        }

        /// <summary>
        /// ���������� ������ ���������� ������������� runtime-��������� ����������,
        /// ���� �� ����� ���� �������� � ����������� generic ���� (<typeparamref name="T"/>).
        /// </summary>
        /// <typeparam name="T">��� ����������</typeparam>
        /// <returns>��������� ��� null</returns>
        public T GetSubsystem<T>() where T : ISubsystem
        {
            var foundSubsystems = GetSubsystems<T>();

            if (foundSubsystems.Count > 1)
                throw new InvalidOperationException(
                    string.Format("������� ����� ����� ���������� ���� {0}", typeof(T).Name));

            if (foundSubsystems.Count == 0)
                return default(T);

            return foundSubsystems[0].Value;
        }

        /// <summary>
        /// ���������� ��� ���������� ���������� ��� ����������� ��� <typeparamref name="T"/>.
        /// </summary>
        /// <typeparam name="T">��� ����������.</typeparam>
        /// <returns>��������� ��������� ��������� ������ � �� ��������������, 
        /// ��� �������� ��� ���� ��������� � ����������.</returns>
        public List<KeyValuePair<String, T>> GetSubsystems<T>() where T : ISubsystem
        {
            var subsystemsReq = new List<KeyValuePair<string, T>>();

            foreach (var item in _subsystems)
            {
                if (item.Value is T)
                    subsystemsReq.Add(new KeyValuePair<String, T>(item.Key, (T)item.Value));
            }

            return subsystemsReq;
        }

        #endregion

		#region ������ ����������

		/// <summary>
		/// ������ ����������
		/// </summary>
        public Version ApplicationVersion
        {
            get
            {
				foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
				{
					// ���� ��� ����� �����
					if (assembly.EntryPoint == null)
                        // �� ��������� ������
						continue;
					
                    // ���� ���� ������� ����� � ������ Main � ��� �� �� vshost
					if (assembly.EntryPoint.Name == "Main" && !assembly.GetName().Name.StartsWith("vshost"))
                        // �� ��� ������ ������ => ���������� �� ������
						return assembly.GetName().Version;
				}

				// ���� ������ �� �����, �� ������ ������ ����������� ������
				return Assembly.GetExecutingAssembly().GetName().Version;
			}
        }

		#endregion

		#region ���������� ������

        /// <summary>
        /// ������ ��� ������������� ���������� ������ ����������
        /// </summary>
        private static readonly object s_exitSync = new object();
        /// <summary>
        /// �����, � ������� ����������� �������� �� ���������� ������ ����������
        /// </summary>
        private Thread _exitThread;
		/// <summary>
        /// ������� ���������� ������ ����������
        /// </summary>
        protected ManualResetEvent _exitEvent = new ManualResetEvent(false);

        /// <summary>
        /// ������� ���������� ������ ����������
        /// </summary>
        public WaitHandle ExitEvent
        {
            get
            {
                return _exitEvent;
            }
        }

        /// <summary>
        /// ������� ������ �� ����������
        /// </summary>
		public event EventHandler<ApplicationExitEventArgs> Exited;

        /// <summary>
        /// �������, ����� ���������� �������� ������
        /// </summary>
        public void WaitForExit()
        {
            _exitEvent.WaitOne();
        }

        /// <summary>
        /// ��������� ������ ����������
        /// </summary>
        /// <param name="exitType">��� ��������� ������ ����������</param>
        public void Exit(ApplicationExitType exitType)
        {
            lock (s_exitSync)
            {
                // ���� ��� �� ������ ��������� ����������
                if (_exitThread == null)
                {
                    // ��������� � ��������� ������ ������, ��� ������������ ���������� ����������
                    // ����� �����-������ ����������, � ���� �� ����� ��������� ���������� � ������ ������,
                    // �� ��� ������ ������ �� ���������� ���� ����������, �� ������ ����� ����� ����������
                    // � �� �� ������ ��������� ���������� ���������

                    // ps: ����� ��������� ����� �� ��� �������
                    _exitThread = new Thread(ExitThread);
                    _exitThread.Start(exitType);
                }

                // ���� ���������� ������
                WaitForExit();

                // ���� ��������� �����, ���� ���������� �� ����� �����
                Thread.Sleep(3000);
            }
        }

        /// <summary>
        /// ����� ������, ������� ��������� ����������
        /// </summary>
        /// <param name="state"></param>
        private void ExitThread(object state)
        {
            Logger.LogInfo("���������� ������ ����������...");

            // ���������� ������ ���������, ������������� �� ����������� DisposeOrder
            var sortedList = new SortedList<int, List<ISubsystem>>(_subsystems.Count);
            foreach (var subsystem in _subsystems.Values)
            {
                // ���� � ����� ���������� ������� ��� ���� �������
                if (sortedList.ContainsKey(subsystem.DisposeOrder))
                {
                    // �� ������� ���������� � ����� ������
                    sortedList[subsystem.DisposeOrder].Add(subsystem);
                }
                else
                {
                    sortedList.Add(subsystem.DisposeOrder, new List<ISubsystem> {subsystem});
                }
            }

            // ������� ���������� �� �������
            foreach (var subsystem in sortedList.SelectMany(pair => pair.Value))
            {
                Logger.LogVerbose(string.Format("������� ���������� '{0}'...", subsystem.Name));
                try
                {
                    Disposer.DisposeObject(subsystem);
                }
                catch (Exception ex)
                {
                    Logger.LogWarning(
                        string.Format("������ ��������� ���������� '{0}': {1}", subsystem.Name, ex));
                }
            }

            // ��������, ����� ���������� ������ �������� � ��� ���� ��������� �����
            Thread.Sleep(1000);

            // ������� ������� ���������
            foreach (var subsystem in sortedList.SelectMany(pair => pair.Value))
            {
                Logger.LogVerbose(string.Format("������� �������������� ���������� '{0}'...", subsystem.Name));
                try
                {
                    subsystem.DisposeLogger();
                }
                catch (Exception ex)
                {
                    Logger.LogWarning(
                        string.Format("������ ��������� �������������� ���������� '{0}': {1}", subsystem.Name, ex));
                }
            }

            var exitTypeStr = state == null ? "�� �����" : ((ApplicationExitType) state).ToString();
        	Logger.LogInfo("���������� ������ ���������� (��� ������ {0})", exitTypeStr);

            // ������� ������ ����������
            Thread.Sleep(1000);
            Disposer.DisposeObject(Logger);

			// ������� ��� ����� ����
			FileWriter.Close();

            // ��������, ��� ������ ���������
            _exitEvent.Set();

            // ��������, ����� ��� ������ ���������� ������ ���������� ������
            Thread.Sleep(1000);

            if (state == null)
                // state ����� = null, ����� ����� ���������� �� unit-�����
                // � ���� ������ ��������� ���������� ���������� �� �����
                return;

            var exitType = (ApplicationExitType) state;

            // �������, ��� ������ ���������� ���������
            Exited.RaiseEvent(this, new ApplicationExitEventArgs(exitType));

            // ���� ����� ���� ������ ���������� ����������
            if (exitType == ApplicationExitType.Stop)
                return;

            // ������� ������� � ������ �����
            var exitCode = (int) state;
            LoggingUtils.LogToConsole("exit with code: {0}", exitCode);
            Environment.Exit(exitCode);
        }

        #endregion
    }
}
