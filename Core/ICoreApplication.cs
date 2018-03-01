using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using Yaw.Core.Diagnostics;
using Yaw.Core.Configuration;

namespace Yaw.Core
{
    /// <summary>
    /// Интерфейс приложения
    /// </summary>
    public interface ICoreApplication : ILoggerContainer
    {
        /// <summary>
        /// Наименование приложения
        /// </summary>
        string Name { get; }

        #region Конфигурация

        /// <summary>
        /// Текущая конфигурация приложения
        /// </summary>
        ApplicationConfig Config { get; }

        /// <summary>
        /// Применить новую конфигурацию приложения
        /// </summary>
        /// <param name="newConfig">новый конфиг приложения</param>
        /// <param name="force">нужно ли применить новый конфиг, даже если он не отличается от старого</param>
        /// <returns>
        /// true - конфигурация применена, 
        /// false - конфигурация не применена, т.к. новая не отличается от текущей
        /// </returns>
        bool ApplyNewConfig(ApplicationConfig newConfig, bool force);

		/// <summary>
		/// Событие изменения конфигурации подсистемы
		/// </summary>
		event EventHandler<ConfigUpdatedEventArgs> ConfigUpdated;

        #endregion

        #region Логирование

        /// <summary>
        /// Уровень трассировки приложения
        /// </summary>
        TraceLevel TraceLevel { get; }

        /// <summary>
        /// Папка, в которой будут создаваться лог-файлы приложения
        /// </summary>
        string LogFileFolder { get; }

        /// <summary>
        /// Создает новый логгер с заданным именем и уровнем трассировки,
        /// который будет писать в отдельные файлы в заданной папке
        /// </summary>
        /// <param name="loggerName">имя логгера</param>
        /// <param name="traceLevel">уровень трассировки</param>
        /// <returns></returns>
        ILogger CreateLogger(string loggerName, TraceLevel traceLevel);

        /// <summary>
        /// Включен ли логер
        /// </summary>
        bool LoggerEnabled { get; set; }

        #endregion

        #region Подсистемы

        /// <summary>
        /// Найти подсистему, которая реализует заданный интерфейс
        /// </summary>
        /// <typeparam name="T">запрашиваемый интерфейс</typeparam>
        /// <returns>
        /// null - подсистема, реализующая заданный интерфейс, не найдена
        /// ссылка на подсистему - первая найденная подсистема, реализующая заданный интерфейс
        /// </returns>
        T FindSubsystemImplementsInterface<T>();

        /// <summary>
        /// Найти подсистему, которая реализует заданный интерфейс
        /// </summary>
        /// <typeparam name="T">запрашиваемый интерфейс</typeparam>
        /// <returns>
        /// ссылка на подсистему - первая найденная подсистема, реализующая заданный интерфейс
        /// </returns>
        /// <exception cref="System.Exception">подсистема, реализующая заданный интерфейс, не найдена</exception>
        T FindSubsystemImplementsInterfaceOrThrow<T>();

        /// <summary>
        /// Найти все подсистемы, которые реализует заданный интерфейс
        /// </summary>
        /// <typeparam name="T">запрашиваемый интерфейс</typeparam>
        /// <returns>список найденных подсистем</returns>
        IEnumerable<T> FindAllSubsystemsImplementsInterface<T>();

        /// <summary>
        /// Возвращает подсистему по ее интерфейсу, в случае не нахождения генерирует исключение
        /// </summary>
        /// <typeparam name="T">Интерфейс запрашиваемой подсистемы</typeparam>
        /// <returns></returns>
        T GetSubsystemOrThrow<T>() where T : ISubsystem;

        /// <summary>
        /// Возвращает подсистему по ее интерфейсу, в случае не нахождения генерирует исключение с заданным текстом.
        /// </summary>
        /// <typeparam name="T">Интерфейс запрашиваемой подсистемы</typeparam>
        /// <param name="errorMsg"></param>
        /// <returns></returns>
        T GetSubsystemOrThrow<T>(string errorMsg) where T : ISubsystem;

        /// <summary>
        /// read-only список всех подсистем
        /// </summary>
        IEnumerable<ISubsystem> Subsystems { get; }

        /// <summary>
        /// Возвращает runtime-описатель подсистемы по ее наименованию
        /// </summary>
        /// <param name="sName">Наименование подсистемы</param>
        /// <exception cref="ArgumentNullException">Если наименование не задано</exception>
        /// <exception cref="ArgumentException">Если наименование не задано</exception>
        /// <returns>Экземпляр или null, если подсистема с заданным наименованием не найдена</returns>
        ISubsystem GetSubsystem(String sName);

        /// <summary>
        /// Возвращает runtime-описатель подсистемы по ее наименованию 
        /// с приведением к конкретному типу
        /// </summary>
        /// <param name="sName">Наименование подсистемы</param>
        /// <typeparam name="T">Тип экземпляра</typeparam>
        /// <returns>Экземпляр или null</returns>
        /// <exception cref="ArgumentNullException">Если наименование не задано</exception>
        /// <exception cref="ArgumentException">Если наименование не задано</exception>
        /// <exception cref="ArgumentException">Если экземпляр подсистемы, 
        /// найденный по наименованию не может быть приведен к типу <typeparamref name="T"/></exception>
        T GetSubsystem<T>(String sName) where T : ISubsystem;

        /// <summary>
        /// Возвращает первый попавшийся неименованный runtime-описатель подсистемы,
        /// если он может быть приведен к переданному generic типу (<typeparamref name="T"/>).
        /// </summary>
        /// <remarks>
        /// При поиске анализируются только неименованные подсистемы.
        /// </remarks>
        /// <typeparam name="T">Тип экземпляра</typeparam>
        /// <returns>Экземпляр или null</returns>
        T GetSubsystem<T>() where T : ISubsystem;

        /// <summary>
        /// Возвращает все подсистемы являющиеся или наследующие тип <typeparamref name="T"/>.
        /// </summary>
        /// <typeparam name="T">Тип подсистемы.</typeparam>
        /// <returns>Коллекция найденный подсистем вместе с их наименованиями, 
        /// под которыми они были добавлены в приложение.</returns>
        List<KeyValuePair<String, T>> GetSubsystems<T>() where T : ISubsystem;

        #endregion

		#region Версия приложения

		/// <summary>
		/// Версия приложения
		/// </summary>
		Version ApplicationVersion { get; }

		#endregion

        #region Завершение работы

        /// <summary>
        /// Событие завершения работы приложения
        /// </summary>
        WaitHandle ExitEvent { get; }

		/// <summary>
		/// Событие выхода из приложения
		/// </summary>
		event EventHandler<ApplicationExitEventArgs> Exited;

        /// <summary>
        /// Ожидает, когда приложение завершит работу
        /// </summary>
        void WaitForExit();

        /// <summary>
        /// Завершить работу приложения
        /// </summary>
        /// <param name="exitType">как завершить работу приложения</param>
        void Exit(ApplicationExitType exitType);

        #endregion
    }
}
