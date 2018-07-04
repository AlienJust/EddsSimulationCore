using System.Collections.Generic;

namespace Controllers.Bumiz {
	internal interface IDataSender {
		void SendData(IEnumerable<byte> data);
	}
}