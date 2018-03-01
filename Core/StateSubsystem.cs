using System;
using System.IO;
using Yaw.Core.Extensions;

namespace Yaw.Core
{
    /// <summary>
    /// Базовый класс для подсистем, которые имеют состояние
    /// </summary>
    public abstract class StateSubsystem : Subsystem, IStateSubsystem
    {
        /// <summary>
        /// Событие "Состояние подсистемы изменилось"
        /// </summary>
        public event EventHandler<StateSubsystemEventArgs> StateChanged;

        /// <summary>
        /// Возбудить событие "Состояние подсистемы изменилось"
        /// </summary>
        public void RaiseStateChanged()
        {
            StateChanged.RaiseEvent(this, new StateSubsystemEventArgs(this));
        }

        /// <summary>
        /// Получить состояние подсистемы
        /// </summary>
        /// <returns></returns>
        public abstract object GetState();

        /// <summary>
        /// Получить последний кусок обновления состояние подсистемы
        /// </summary>
        /// <remarks>
        /// Актуально для подсистем, состояние которых накапливается со временем.
        /// Тогда этот метод должен возвращать последний кусок, который был добавлен к полному состоянию
        /// </remarks>
        /// <returns></returns>
        public abstract object GetStateLastUpdateChunk();

        /// <summary>
        /// Принять новое состояние
        /// </summary>
        /// <param name="newState">новое состояние</param>
        /// <returns>результат принятия нового состояния подсистемой</returns>
        public abstract SubsystemStateAcceptanceResult AcceptNewState(object newState);

        /// <summary>
        /// Сбросить состояние (перевести его в начальное положение)
        /// </summary>
        /// <param name="raiseStateChangedEvent">
        /// нужно ли возбудить событие "Состояние подсистемы изменилось"
        /// после того, как состояние будет сброшено</param>
        public void ResetState(bool raiseStateChangedEvent)
        {
            ResetStateInternal();

            if (raiseStateChangedEvent)
                RaiseStateChanged();
        }

        /// <summary>
        /// Сбросить состояние (перевести его в начальное положение)
        /// </summary>
        /// <remarks>реализация в классе-наследнике</remarks>
        protected abstract void ResetStateInternal();

        /// <summary>
        /// Восстановить состояние
        /// </summary>
        /// <param name="state"></param>
        public abstract void RestoreState(object state);

        /// <summary>
        /// Сохранить состояние подсистемы в файл
        /// </summary>
        /// <param name="stateFilePath">путь к файлу</param>
        public virtual void SaveStateToFile(string stateFilePath)
        {
            // получим актуальное состояние
            var state = GetState();

            // убедимся, что состояние имеет тип массива байт
            var data = state as byte[];
            if (data == null)
            {
                throw new Exception("Type of State must be 'byte[]'");
            }

            // записываем данные в файл
            File.WriteAllBytes(stateFilePath, data);
        }

        /// <summary>
        /// Восстановить состояние подсистемы из файла
        /// </summary>
        /// <param name="stateFilePath">путь к файлу, который содержит состояние</param>
        public virtual void RestoreStateFromFile(string stateFilePath)
        {
            // если файла нет
            if (!File.Exists(stateFilePath))
            {
                // сбросим состояние подсистемы
                ResetState(false);
                return;
            }

            // читаем файл
            var data = File.ReadAllBytes(stateFilePath);
            // восстанавливаем состояние
            RestoreState(data);
        }
    }
}
