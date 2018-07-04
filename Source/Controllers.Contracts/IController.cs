using System;
using System.Collections.Generic;

namespace Controllers.Contracts
{
	public interface IController {
		string Name { get; }
		void GetDataInCallback(int command, IEnumerable<byte> data, Action<Exception, IEnumerable<byte>> callback);
	}
}
