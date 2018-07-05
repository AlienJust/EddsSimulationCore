using System;
using BumizNetwork.Contracts;

namespace BumizNetwork {
  internal interface ILinkQualityStorage {
    DateTime? GetLastBadTime(ObjectAddress obj);
    void SetLastBadTime(ObjectAddress obj, DateTime? time);
  }
}