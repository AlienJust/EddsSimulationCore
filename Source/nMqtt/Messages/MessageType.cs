using System;

namespace nMqtt.Messages {
  /// <summary>
  /// MQTT message type
  /// </summary>
  [Flags]
  public enum MessageType : byte {
    /// <summary>
    /// 发起连接
    /// </summary>
    Connect = 1,

    /// <summary>
    /// 连接回执
    /// </summary>
    Connack = 2,

    /// <summary>
    /// 发布消息
    /// </summary>
    Publish = 3,

    /// <summary>
    /// 发布回执
    /// </summary>
    Puback = 4,

    /// <summary>
    /// QoS2消息回执
    /// </summary>
    Pubrec = 5,

    /// <summary>
    /// QoS2消息释放
    /// </summary>
    Pubrel = 6,

    /// <summary>
    /// QoS2消息完成
    /// </summary>
    Pubcomp = 7,

    /// <summary>
    /// 订阅主题
    /// </summary>
    Subscribe = 8,

    /// <summary>
    /// 订阅回执
    /// </summary>
    Suback = 9,

    /// <summary>
    /// 取消订阅
    /// </summary>
    Unsubscribe = 10,

    /// <summary>
    /// 取消订阅回执
    /// </summary>
    Unsuback = 11,

    /// <summary>
    /// PING请求
    /// </summary>
    Pingreq = 12,

    /// <summary>
    /// PING响应
    /// </summary>
    Pingresp = 13,

    /// <summary>
    /// 断开连接
    /// </summary>
    Disconnect = 14
  }
}