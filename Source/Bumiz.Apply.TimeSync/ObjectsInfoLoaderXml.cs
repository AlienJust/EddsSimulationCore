using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

namespace Bumiz.Apply.TimeSync
{
	class ObjectsInfoLoaderXml : IObjectsInfoLoader {
		private readonly string _xmlFileName;
		public ObjectsInfoLoaderXml(string xmlFileName) {
			_xmlFileName = xmlFileName;
		}

		public IList<string> GetObjects() {
			var doc = XDocument.Load(_xmlFileName);
			return doc.Element("Objects").Elements("Object").Select(o => o.Attribute("Name").Value).ToList();
		}
	}
}
