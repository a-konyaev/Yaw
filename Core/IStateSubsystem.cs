using System;

namespace Yaw.Core
{
    /// <summary>
    /// Интерфейс подсистемы, которая имеет состояние
    /// </summary>
    /// <remarks>используется для того, чтобы пользователь подсистемы умел
    /// сохранять и восстанавливать ее состояние</remarks>
    public interface IStateSubsystem : ISubsystem
    {
        /// <summary>
        /// Событие "Состояние подсистемы изменилось"
        /// </summary>
        event EventHandler<StateSubsystemEventArgs> StateChanged;

        /// <summary>
        /// Возбудить событие "Состояние подсистемы изменилось"
        /// </summary>
        void RaiseStateChanged();

        /// <summary>
        /// Восстановить состояние
        /// </summary>
        /// <param name="state"></param>
        void RestoreState(object state);

        /// <summary>
        /// Сбросить состояние (перевести его в начальное положение)
        /// </summary>
        /// <param name="raiseStateChangedEvent">
        /// нужно ли возбудить событие "Состояние подсистемы изменилось"
        /// после того, как состояние будет сброшено</param>
        void ResetState(bool raiseStateChangedEvent);

        /// <summary>
        /// Получить состояние подсистемы
        /// </summary>
        /// <returns></returns>
        object GetState();

        /// <summary>
        /// Получить последний кусок обновления состояние подсистемы
        /// </summary>
        /// <remarks>
        /// Актуально для подсистем, состояние которых накапливается со временем.
        /// Тогда этот метод должен возвращать последний кусок, который был добавлен к полному состоянию
        /// </remarks>
        /// <returns></returns>
        object GetStateLastUpdateChunk();

        /// <summary>
        /// Принять новое состояние
        /// </summary>
        /// <param name="newState">новое состояние</param>
        /// <returns>результат принятия нового состояния подсистемой</returns>
        SubsystemStateAcceptanceResult AcceptNewState(object newState);

        /// <summary>
        /// Сохранить состояние подсистемы в файл
        /// </summary>
        /// <param name="stateFilePath">путь к файлу</param>
        void SaveStateToFile(string stateFilePath);

        /// <summary>
        /// Восстановить состояние подсистемы из файла
        /// </summary>
        /// <param name="stateFilePath">путь к файлу, который содержит состояние</param>
        void RestoreStateFromFile(string stateFilePath);
    }
}
