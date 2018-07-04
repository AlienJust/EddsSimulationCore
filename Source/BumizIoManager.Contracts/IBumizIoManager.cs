using System;
using System.Collections.Generic;
using BumizNetwork.Contracts;
using Commands.Contracts;

namespace BumizIoManager.Contracts {
	public interface IBumizIoManager {
		bool BumizObjectExist(string objectName);
		IEnumerable<string> GetAllBumizObjectNames();

		void SendDataAsync(string name, IInteleconCommand cmd, Action<ISendResultWithAddress> callback, IoPriority priority);
	}
}