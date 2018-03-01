namespace Yaw.Core.Diagnostics
{
	/// <summary>
	/// ��Q������ �E�����E�E���E��E.
	/// </summary>
	public interface ILogger
	{
		/// <summary>
		/// ��@��E�G�E������� �E�E����.
		/// </summary>
		/// <param name="logEvent">������E/param>
		void Log(LoggerEvent logEvent);

        /// <summary>
        /// �������E�D�@����E��E���������A��E ������� �E ��E���E"�����B��"
        /// </summary>
        /// <param name="logEvent">������E/param>
        /// <returns>�E�����E�D�@����E��E�E�C�I�J���A��E</returns>
        bool IsAcceptedByEventType(LoggerEvent logEvent);
	}
}