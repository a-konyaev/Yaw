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
    /// Приложение
    /// </summary>
    public class CoreApplication : ICoreApplication
    {
		/// <summary>
        /// Инстанс приложения
        /// </summary>
        public static ICoreApplication Instance
        {
            get;
            private set;
        }

        /// <summary>
        /// Наименование приложения
        /// </summary>
        public string Name
        {
            get;
            private set;
        }

        #region Инициализация

        /// <summary>
        /// Конструктор
        /// </summary>
        public CoreApplication()
		{
            Instance = this;

            // загрузим конфиг приложения
            Config = LoadConfig();
            // получим имя приложения
            Name = string.IsNullOrEmpty(Config.Name) ? Guid.NewGuid().ToString() : Config.Name;
            // инициализируем логгер
            InitLogger();
            // создадим подсистемы
            CreateSubsystems();
		}

        /// <summary>
        /// Загрузка конфигурации
        /// </summary>
        /// <returns></returns>
        protected virtual ApplicationConfig LoadConfig()
        {
            try
            {
                var exeConfig = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
                var config = (ApplicationConfig)exeConfig.GetSection(ApplicationConfig.SECTION_NAME);
                if (config == null)
                    throw new Exception("Секция не найдена: " + ApplicationConfig.SECTION_NAME);

                return config;
            }
            catch (Exception ex)
            {
                throw new ConfigurationErrorsException("Ошибка получения конфигурации приложения", ex);
            }
        }

        /// <summary>
        /// Инициализация логгера
        /// </summary>
        private void InitLogger()
        {
            // получим параметры логгера
            LogFileFolder = Config.LogFileFolder.Path;
            if (string.IsNullOrEmpty(LogFileFolder))
                LogFileFolder = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);

            TraceLevel = GetTraceLevelByName(Config.TraceLevelName, TraceLevel.Error);

            EventDispatcher.Init(Config.DiagnosticsConfig);
            FileWriter.Init(LogFileFolder);

            // создадим логгер и получим ссылку на системный логгер
            _logger = (Logger)CreateLogger(Name, TraceLevel);
        }

        /// <summary>
        /// Создаем подсистемы
        /// </summary>
        private void CreateSubsystems()
        {
            if (Config.Subsystems.Count == 0)
                throw new ConfigurationErrorsException("Список подсистем пуст");

            foreach (SubsystemConfig subsystemConfig in Config.Subsystems)
            {
                Subsystem subsystem;
                var subsystemName = subsystemConfig.SubsystemName;

                try
                {
                    // создадим подсистему
                    var type = Type.GetType(subsystemConfig.SubsystemTypeName, true);
                    subsystem = (Subsystem)Activator.CreateInstance(type);
                    
                    // зададим имя
                    subsystem.Name = subsystemName;

                    // добавим в коллекцию подсистем
                    AddSubsystem(subsystemName, subsystem);

                    // зададим параметры логирования
                    subsystem.TraceLevel = GetTraceLevelByName(subsystemConfig.TraceLevelName, TraceLevel);
                    subsystem.LogFileFolder = string.IsNullOrEmpty(subsystemConfig.LogFileFolder)
                        ? LogFileFolder
                        // каталог подсистемы задается относительно общего каталога
                        : LogFileFolder + "/" + subsystemConfig.LogFileFolder;
                    subsystem.SeparateLog = subsystemConfig.SeparateLog;

                    // зададим порядковый номер при удалении
                    subsystem.DisposeOrder = subsystemConfig.DisposeOrder;

                    // подпишемся на событие изменения конфигурации подсистемы
					subsystem.ConfigUpdated += SubsystemConfigUpdated;

                    Logger.LogVerbose("Создана подсистема {0}", subsystemName);
                }
                catch (Exception ex)
                {
                    Logger.LogException("Ошибка создания подсистемы {0}: {1}", ex, subsystemName, ex.Message);
                    throw new Exception("Ошибка создания подсистемы " + subsystemName, ex);
                }
            }

            // проинициализируем подсистемы
            foreach (SubsystemConfig subsystemConfig in Config.Subsystems)
            {
                var subsystemName = subsystemConfig.SubsystemName;
                try
                {
                    var subsystem = GetSubsystem(subsystemName);
                    subsystem.Init(subsystemConfig);
                    Logger.LogVerbose("Выполнена инициализация подсистемы {0}", subsystemName);
                }
                catch (Exception ex)
                {
                    Logger.LogException("Ошибка инициализации подсистемы {0}: {1}", ex, subsystemName, ex.Message);
                    throw new Exception("Ошибка инициализации подсистемы " + subsystemName, ex);
                }
            }
        }

        /// <summary>
        /// Возвращает уровень трассировки по названию
        /// </summary>
        /// <param name="traceLevelName">название уровня трассировки</param>
        /// <param name="defaultTraceLevel">уровень трассировки по умолчанию</param>
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
                throw new Exception(string.Format("Некорректно задан уровень трассировки: '{0}'", traceLevelName));
            }
        }

        #endregion

        #region Конфигурация

		/// <summary>
		/// Обработчик события изменения конфигурации подсистемы
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void SubsystemConfigUpdated(object sender, ConfigUpdatedEventArgs e)
		{
			// получим подсистему
			var subsystem = (ISubsystem)sender;

			// применение новой конфигурации
			subsystem.ApplyNewConfig(Config.Subsystems[subsystem.Name]);

			// вызовем событие изменени конфига для всего приложения
			ConfigUpdated.RaiseEvent(this, e);
		}

        /// <summary>
        /// Текущая конфигурация приложения
        /// </summary>
        public ApplicationConfig Config
        {
            get;
            private set;
        }

        /// <summary>
        /// Применить новую конфигурацию приложения
        /// </summary>
        /// <param name="newConfig">новый конфиг приложения</param>
        /// <param name="force">нужно ли применить новый конфиг, даже если он не отличается от старого</param>
        /// <returns>
        /// true - конфигурация применена, 
        /// false - конфигурация не применена, т.к. новая не отличается от текущей
        /// </returns>
        public bool ApplyNewConfig(ApplicationConfig newConfig, bool force)
        {
            CodeContract.Requires(newConfig != null);

            // если не нужно применять конфиг в любом случае и 
            // новый конфиг не отличается от текущего
            if (!force && Config.Equals(newConfig))
                // то применять его не будем
                return false;

            Logger.LogVerbose("Применение новой конфигурации...");

            // заменим конфиг на новый
            Config = newConfig;

            // применим новые конфиги подсистем
            foreach (SubsystemConfig subsystemConfig in Config.Subsystems)
            {
                var subsystemName = subsystemConfig.SubsystemName;
                try
                {
                    var subsystem = GetSubsystem(subsystemName);
                    subsystem.ApplyNewConfig(subsystemConfig);
                    Logger.LogVerbose("Выполнена переинициализация подсистемы {0}", subsystemName);
                }
                catch (Exception ex)
                {
                    Logger.LogException("Ошибка переинициализации подсистемы {0}: {1}", ex, subsystemName, ex.Message);
                    throw new Exception("Ошибка переинициализации подсистемы " + subsystemName, ex);
                }
            }

            return true;
        }

		/// <summary>
		/// Событие изменения конфигурации подсистемы
		/// </summary>
		public event EventHandler<ConfigUpdatedEventArgs> ConfigUpdated;

        #endregion

        #region Логирование

        /// <summary>
        /// Уровень трассировки приложения
        /// </summary>
        public TraceLevel TraceLevel
        {
            get;
            private set;
        }

        /// <summary>
        /// Папка, в которой будут создаваться лог-файлы приложения
        /// </summary>
        public string LogFileFolder
        {
            get;
            private set;
        }

        /// <summary>
        /// логер приложения
        /// </summary>
        Logger _logger;
        /// <summary>
        /// Логгер приложения
        /// </summary>
        public ILogger Logger
        {
			get { return _logger; }
        }

        /// <summary>
        /// Создает новый логгер с заданным именем и уровнем трассировки,
        /// который будет писать в отдельные файлы в заданной папке
        /// </summary>
        /// <param name="loggerName">имя логгера</param>
        /// <param name="traceLevel">уровень трассировки</param>
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
        /// Событие приостановки/возобновления записи в журналы
        /// </summary>
        private readonly ManualResetEvent _loggerEnabled = new ManualResetEvent(true);

        /// <summary>
        /// Включен ли логер
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

        #region Подсистемы

        /// <summary>
        /// Словарь подсистем
        /// </summary>
        private readonly Dictionary<string, ISubsystem> _subsystems = new Dictionary<string, ISubsystem>();

        /// <summary>
        /// Найти подсистему, которая реализует заданный интерфейс
        /// </summary>
        /// <typeparam name="T">запрашиваемый интерфейс</typeparam>
        /// <returns>
        /// null - подсистему, реализующая заданный интерфейс, не найдена
        /// ссылка на подсистему - первая найденная подсистема, реализующая заданный интерфейс
        /// </returns>
        public T FindSubsystemImplementsInterface<T>()
        {
            return (T)_subsystems.FirstOrDefault(i => i.Value is T).Value;
        }

        /// <summary>
        /// Найти подсистему, которая реализует заданный интерфейс
        /// </summary>
        /// <typeparam name="T">запрашиваемый интерфейс</typeparam>
        /// <returns>
        /// ссылка на подсистему - первая найденная подсистема, реализующая заданный интерфейс
        /// </returns>
        /// <exception cref="System.Exception">подсистема, реализующая заданный интерфейс, не найдена</exception>
        public T FindSubsystemImplementsInterfaceOrThrow<T>()
        {
            var res = FindSubsystemImplementsInterface<T>();
            if (res == null)
                throw new ArgumentException("Приложение не содержит подсистемы, реализующей интерфейс " + typeof(T).FullName);

            return res;
        }

        /// <summary>
        /// Найти все подсистемы, которые реализует заданный интерфейс
        /// </summary>
        /// <typeparam name="T">запрашиваемый интерфейс</typeparam>
        /// <returns>список найденных подсистем</returns>
        public IEnumerable<T> FindAllSubsystemsImplementsInterface<T>()
        {
            return _subsystems.Where(i => i.Value is T).Select(i => (T) i.Value);
        }

        /// <summary>
        /// Возвращает подсистему по ее интерфейсу, в случае не нахождения генерирует исключение
        /// </summary>
        /// <typeparam name="T">Интерфейс запрашиваемой подсистемы</typeparam>
        /// <returns></returns>
        public T GetSubsystemOrThrow<T>() where T : ISubsystem
        {
            return GetSubsystemOrThrow<T>("Приложение не содержит подсистемы " + typeof(T).FullName);
        }

        /// <summary>
        /// Возвращает подсистему по ее интерфейсу, в случае не нахождения генерирует исключение с заданным текстом.
        /// </summary>
        /// <typeparam name="T">Интерфейс запрашиваемой подсистемы</typeparam>
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
        /// Добавляет подсистему в приложение
        /// </summary>
        /// <remarks>
        /// Если добавляется именованная подсистемы (параметр <paramref name="name"/> равен не null),
        /// то получить ее можно будет только по наименованию
        /// </remarks>
        /// <param name="name">Наименование подсистемы или null</param>
        /// <param name="subsystem">Экземпляр подсистемы</param>
        public void AddSubsystem(String name, ISubsystem subsystem)
        {
            CodeContract.Requires(subsystem != null);

            if (_subsystems.ContainsKey(name))
                throw new ArgumentException("Приложение уже содержит подсистему с наименованием " + name);

            _subsystems[name] = subsystem;

            // установим обратное свойство у подсистемы - ссылка на приложение
            subsystem.Application = this;
        }

        /// <summary>
        /// Получить подсистему с заданным именем
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
        /// Получить подсистему с заданным именем и заданного типа
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
                    string.Format("Запрошенная подсистема '{0}' не реализует требуемый интерфейс {1}",
                                  name, typeof (T).FullName));
            }

            return (T) subsystem;
        }

        /// <summary>
        /// Возвращает первый попавшийся неименованный runtime-описатель подсистемы,
        /// если он может быть приведен к переданному generic типу (<typeparamref name="T"/>).
        /// </summary>
        /// <typeparam name="T">Тип экземпляра</typeparam>
        /// <returns>Экземпляр или null</returns>
        public T GetSubsystem<T>() where T : ISubsystem
        {
            var foundSubsystems = GetSubsystems<T>();

            if (foundSubsystems.Count > 1)
                throw new InvalidOperationException(
                    string.Format("Найдено более одной подсистемы типа {0}", typeof(T).Name));

            if (foundSubsystems.Count == 0)
                return default(T);

            return foundSubsystems[0].Value;
        }

        /// <summary>
        /// Возвращает все подсистемы являющиеся или наследующие тип <typeparamref name="T"/>.
        /// </summary>
        /// <typeparam name="T">Тип подсистемы.</typeparam>
        /// <returns>Коллекция найденный подсистем вместе с их наименованиями, 
        /// под которыми они были добавлены в приложение.</returns>
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

		#region Версия приложения

		/// <summary>
		/// Версия приложения
		/// </summary>
        public Version ApplicationVersion
        {
            get
            {
				foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
				{
					// если нет точки входа
					if (assembly.EntryPoint == null)
                        // то продолжим искать
						continue;
					
                    // если есть входная точка с именем Main и она не от vshost
					if (assembly.EntryPoint.Name == "Main" && !assembly.GetName().Name.StartsWith("vshost"))
                        // то это нужная сборка => возвращаем ее версию
						return assembly.GetName().Version;
				}

				// если ничего не нашли, то вернем версию исполняемой сборки
				return Assembly.GetExecutingAssembly().GetName().Version;
			}
        }

		#endregion

		#region Завершение работы

        /// <summary>
        /// Объект для синхронизации завершения работы приложения
        /// </summary>
        private static readonly object s_exitSync = new object();
        /// <summary>
        /// Поток, в котором выполняются действия по завершению работы приложения
        /// </summary>
        private Thread _exitThread;
		/// <summary>
        /// Событие завершения работы приложения
        /// </summary>
        protected ManualResetEvent _exitEvent = new ManualResetEvent(false);

        /// <summary>
        /// Событие завершения работы приложения
        /// </summary>
        public WaitHandle ExitEvent
        {
            get
            {
                return _exitEvent;
            }
        }

        /// <summary>
        /// Событие выхода из приложения
        /// </summary>
		public event EventHandler<ApplicationExitEventArgs> Exited;

        /// <summary>
        /// Ожидает, когда приложение завершит работу
        /// </summary>
        public void WaitForExit()
        {
            _exitEvent.WaitOne();
        }

        /// <summary>
        /// Завершить работу приложения
        /// </summary>
        /// <param name="exitType">как завершить работу приложения</param>
        public void Exit(ApplicationExitType exitType)
        {
            lock (s_exitSync)
            {
                // если еще не начали выполнять выключение
                if (_exitThread == null)
                {
                    // запускаем в отдельном потоке потому, что инициировать выключение приложения
                    // может какая-нибудь подсистема, и если мы будем выключать приложение в данном потоке,
                    // то как только дойдем до выключения этой подсистемы, по данный поток может прерваться
                    // и мы не успеем выключить приложение полностью

                    // ps: важно запускать поток НЕ как фоновый
                    _exitThread = new Thread(ExitThread);
                    _exitThread.Start(exitType);
                }

                // ждем завершения работы
                WaitForExit();

                // ждем некоторое время, пока приложение не будет убито
                Thread.Sleep(3000);
            }
        }

        /// <summary>
        /// Метод потока, который выключает приложение
        /// </summary>
        /// <param name="state"></param>
        private void ExitThread(object state)
        {
            Logger.LogInfo("Завершение работы приложения...");

            // сформируем список подсистем, упорядоченный по возрастанию DisposeOrder
            var sortedList = new SortedList<int, List<ISubsystem>>(_subsystems.Count);
            foreach (var subsystem in _subsystems.Values)
            {
                // если с таким порядковым номером уже есть элемент
                if (sortedList.ContainsKey(subsystem.DisposeOrder))
                {
                    // то добавим подсистему к этому списку
                    sortedList[subsystem.DisposeOrder].Add(subsystem);
                }
                else
                {
                    sortedList.Add(subsystem.DisposeOrder, new List<ISubsystem> {subsystem});
                }
            }

            // удаляем подсистемы по очереди
            foreach (var subsystem in sortedList.SelectMany(pair => pair.Value))
            {
                Logger.LogVerbose(string.Format("Останов подсистемы '{0}'...", subsystem.Name));
                try
                {
                    Disposer.DisposeObject(subsystem);
                }
                catch (Exception ex)
                {
                    Logger.LogWarning(
                        string.Format("Ошибка остановки подсистемы '{0}': {1}", subsystem.Name, ex));
                }
            }

            // подождем, чтобы подсистемы успели записать в лог свое последнее слово
            Thread.Sleep(1000);

            // удаляем логгеры подсистем
            foreach (var subsystem in sortedList.SelectMany(pair => pair.Value))
            {
                Logger.LogVerbose(string.Format("Останов журналирования подсистемы '{0}'...", subsystem.Name));
                try
                {
                    subsystem.DisposeLogger();
                }
                catch (Exception ex)
                {
                    Logger.LogWarning(
                        string.Format("Ошибка остановки журналирования подсистемы '{0}': {1}", subsystem.Name, ex));
                }
            }

            var exitTypeStr = state == null ? "не задан" : ((ApplicationExitType) state).ToString();
        	Logger.LogInfo("Завершение работы приложения (тип выхода {0})", exitTypeStr);

            // удаляем логгер приложения
            Thread.Sleep(1000);
            Disposer.DisposeObject(Logger);

			// закроем все файлы лога
			FileWriter.Close();

            // сообщаем, что работа завершена
            _exitEvent.Set();

            // подождем, чтобы все успели обработать сигнал завершения работы
            Thread.Sleep(1000);

            if (state == null)
                // state будет = null, когда метод вызывается из unit-теста
                // в этом случае завершать выполнение приложение не нужно
                return;

            var exitType = (ApplicationExitType) state;

            // сообщим, что работа приложения завершена
            Exited.RaiseEvent(this, new ApplicationExitEventArgs(exitType));

            // если нужно было просто остановить приложение
            if (exitType == ApplicationExitType.Stop)
                return;

            // убиваем процесс с нужным кодом
            var exitCode = (int) state;
            LoggingUtils.LogToConsole("exit with code: {0}", exitCode);
            Environment.Exit(exitCode);
        }

        #endregion
    }
}
