namespace KspConnected.Shared.Protocol
{
    public enum MessageType : byte
    {
        Hello           = 0x01,
        HelloAck        = 0x02,
        PlayerList      = 0x03,
        VesselUpdate    = 0x04,
        Chat            = 0x05,
        TimeSync        = 0x06,
        TimeSyncReply   = 0x07,
        Disconnect      = 0x08,
        Ping            = 0x09,
        Pong            = 0x0A,
    }
}
