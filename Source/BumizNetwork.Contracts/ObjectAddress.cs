namespace BumizNetwork.Contracts {
  /// <summary>
  /// ����� �������
  /// </summary>
  public struct ObjectAddress {
    /// <summary>
    /// ��� ���������
    /// </summary>
    public NetIdRetrieveType Way { get; }

    /// <summary>
    /// �������� ������
    /// </summary>
    public uint Value { get; }

    /// <summary>
    /// ������ ����� ����� �������
    /// </summary>
    /// <param name="way">��� ���������</param>
    /// <param name="value">�������� ������</param>
    public ObjectAddress(NetIdRetrieveType way, ushort value) {
      Value = value;
      Way = way;
    }

    public override string ToString() {
      string result;
      switch (Way) {
        case NetIdRetrieveType.InteleconAddress:
          result = "Intelecon address = ";
          break;
        case NetIdRetrieveType.SerialNumber:
          result = "Serial number = ";
          break;
        case NetIdRetrieveType.OldProtocolSerialNumber:
          result = "Old serial number = ";
          break;
        default:
          result = "�� �������������� ��� ��������� = ";
          break;
      }

      result += Value;
      return result;
    }


    public override bool Equals(object obj) {
      if (ReferenceEquals(null, obj)) return false;
      if (obj.GetType() != typeof(ObjectAddress)) return false;
      return Equals((ObjectAddress) obj);
    }

    public bool Equals(ObjectAddress other) {
      return Equals(other.Way, Way) && other.Value == Value;
    }

    public override int GetHashCode() {
      unchecked {
        return (Way.GetHashCode() * 397) ^ Value.GetHashCode();
      }
    }
  }
}