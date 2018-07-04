using System;

namespace BumizNetwork.Contracts {
	public delegate void QueueCountChangedDelegate();
	/// <summary>
	/// Интерфейс моноканала
	/// </summary>
	public interface IMonoChannel : IDisposable {
		void AddCommandToQueueAndExecuteAsync(object item);
		void AddCommandToQueueAndExecuteAsync(object item, IoPriority priority);

		byte[] AddCommandToQueueAndWaitExecution(IAddressedSendingItem item);

		Action QueueChangedCallback { get; set; }
		int QueueLength { get; }
		void ClearQueue();
	}
}