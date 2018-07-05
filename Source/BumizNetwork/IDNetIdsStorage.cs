using BumizNetwork.Contracts;

namespace BumizNetwork {
  interface IDNetIdsStorage {
    ushort GetDNetId(ObjectAddress address);
    void SetDNetId(ObjectAddress address, ushort dNetId);
    void RemoveDNetId(ObjectAddress address);
  }
}