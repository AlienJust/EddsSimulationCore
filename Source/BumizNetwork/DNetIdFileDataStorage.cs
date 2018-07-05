using System.Collections.Generic;
using System.Globalization;
using System.IO;
using BumizNetwork.Contracts;

namespace BumizNetwork {
  internal class DNetIdsFileDataStorage : IDNetIdsStorage {
    private readonly string _subDirectoryName;
    private readonly Dictionary<NetIdRetrieveType, Dictionary<uint, ushort>> _addressMap;

    public DNetIdsFileDataStorage(string subDirectoryName) {
      _subDirectoryName = subDirectoryName;
      _addressMap = new Dictionary<NetIdRetrieveType, Dictionary<uint, ushort>>();
    }

    private Dictionary<uint, ushort> GetTypedAddressMap(NetIdRetrieveType addressationType) {
      if (_addressMap.ContainsKey(addressationType)) {
        return _addressMap[addressationType];
      }

      // То есть при первом запуске вообще неизвестна такой тип адресации, нужно добавить в набор
      var wayedAddressMap = new Dictionary<uint, ushort>();
      _addressMap.Add(addressationType, wayedAddressMap);
      return wayedAddressMap;
    }

    public ushort GetDNetId(ObjectAddress address) {
      var wayedAddressMap = GetTypedAddressMap(address.Way);

      if (wayedAddressMap.ContainsKey(address.Value)) {
        return wayedAddressMap[address.Value];
      }

      // если адреса нету в коллекции - попытаемся прочитать его из файла:
      var filename = GetStoredFilePath(address);
      if (File.Exists(filename)) {
        var fileText = File.ReadAllText(filename);
        var dNetId = ushort.Parse(fileText);
        wayedAddressMap.Add(address.Value, dNetId);
        return dNetId;
      }

      throw new FileNotFoundException("Не удалось найти файл " + filename);
    }

    public void SetDNetId(ObjectAddress address, ushort dNetId) {
      try {
        var storedDnetId = GetDNetId(address);
        if (storedDnetId != dNetId) {
          // на данный момент (т.к. не выскочило исключение) - элемент в карте для dnetId же имеется, то всё просто:
          GetTypedAddressMap(address.Way)[address.Value] = dNetId;
          WriteDNetIdToFile(address, dNetId);
        }

        // если элементы совпадают - ничего не нужно делать (иначе оверхёд на перезаписи содержимого файла)
      }
      catch {
        // на данный момент в карте address-dnetId нету элемента
        GetTypedAddressMap(address.Way).Add(address.Value, dNetId);
        WriteDNetIdToFile(address, dNetId);
      }
    }

    public void RemoveDNetId(ObjectAddress address) {
      if (_addressMap.ContainsKey(address.Way)) {
        if (_addressMap[address.Way].ContainsKey(address.Value)) {
          _addressMap[address.Way].Remove(address.Value);
        }
      }
    }

    private void WriteDNetIdToFile(ObjectAddress address, ushort dNetId) {
      var filename = GetStoredFilePath(address);
      File.WriteAllText(filename, dNetId.ToString(CultureInfo.InvariantCulture));
    }

    private string GetStoredFilePath(ObjectAddress address) {
      return Path.Combine(_subDirectoryName,
        address.GetObjAddrTypeName() + "_" + address.Value.ToString("d10") + ".txt");
    }
  }
}