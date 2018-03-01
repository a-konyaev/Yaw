using System;
using System.Diagnostics;
using Yaw.Core.Diagnostics;
using Yaw.Core.Configuration;

namespace Yaw.Core
{
    /// <summary>
    /// »ыQерфейс, БEторыБEдоБEыZ реализьAыватБEвсБEБEдсистеБE
    /// </summary>
    public interface ISubsystem : ILoggerContainer, IDisposable
    {
        /// <summary>
        /// ѕриБEжеыGБE БEБEтороБEвхьCит БEдсистеБE
        /// </summary>
        ICoreApplication Application { get; set; }

        /// <summary>
        /// »ыGциализацБE БEдсистеБE
        /// </summary>
        /// <param name="config"></param>
        void Init(SubsystemConfig config);

        /// <summary>
        /// ѕриБEыGть ыMвую БEыSигурацБE БEдсистеБE
        /// </summary>
        /// <param name="newConfig">ыMвыБEБEыSиг-эБEБEыQ БEъьстройкамБEБEдсистеБE</param>
        void ApplyNewConfig(SubsystemConfig newConfig);

		/// <summary>
		/// —ь@ытие изБEыDыG€ БEыSигурации БEдсистеБE
		/// </summary>
		event EventHandler<ConfigUpdatedEventArgs> ConfigUpdated;

        /// <summary>
        /// »БE БEдсистеБE
        /// </summary>
        string Name { get; }

        #region ЋьBирьAание

        /// <summary>
        /// ”ровеы[ трассировкБEБEдсистеБE
        /// </summary>
        TraceLevel TraceLevel { get; }

        /// <summary>
        /// ѕапБE, БEБEтороБEбудуБEсоздаватБE€ БEБEфайлБEБEдсистеБE
        /// </summary>
        string LogFileFolder { get; }

        /// <summary>
        /// ѕризнак ыDь@ходиБEстБEБEсать БEги БEъCдеБEыZБEфайл
        /// </summary>
        bool SeparateLog
        {
            get;
            set;
        }

        /// <summary>
        /// ќсвь@ьEдает БEггер БEдсистеБE
        /// </summary>
        void DisposeLogger();

        #endregion

        /// <summary>
        /// ѕъA€дкьAый ыMБEБEБEБEудалении БEдсистеБE
        /// </summary>
        int DisposeOrder { get; }
    }
}
