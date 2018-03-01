using System;

namespace Yaw.Core
{
    /// <summary>
    /// Аргументы события подсистемы с состоянием
    /// </summary>
    public class StateSubsystemEventArgs : EventArgs
    {
        /// <summary>
        /// Подсистема
        /// </summary>
        public IStateSubsystem Subsystem { get; private set; }

        /// <summary>
        /// Конструктор
        /// </summary>
        /// <param name="subsystem">подсистема</param>
        public StateSubsystemEventArgs(IStateSubsystem subsystem)
        {
            CodeContract.Requires(subsystem != null);
            Subsystem = subsystem;
        }
    }
}
