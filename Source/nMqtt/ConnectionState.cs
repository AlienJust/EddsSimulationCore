namespace nMqtt
{
    /// <summary>
    /// 连接状态
    /// </summary>
    public enum ConnectionState
    {
        /// <summary>
        ///     The MQTT Connection is in the process of disconnecting from the broker.
        /// </summary>
        Disconnecting,
        /// <summary>
        ///     The MQTT Connection is not currently connected to any broker.
        /// </summary>
        Disconnected,
        /// <summary>
        ///     The MQTT Connection is in the process of connecting to the broker.
        /// </summary>
        Connecting,
        /// <summary>
        ///     The MQTT Connection is currently connected to the broker.
        /// </summary>
        Connected,
        /// <summary>
        ///     The MQTT Connection is faulted and no longer communicating with the broker.
        /// </summary>
        Faulted
    }
}