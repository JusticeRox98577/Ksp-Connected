namespace KspConnected.Shared.Protocol
{
    public static class ProtocolConstants
    {
        public const int Version = 1;
        public const int DefaultPort = 7654;
        public const int MaxPayloadBytes = 1024 * 1024; // 1 MB
        public const int FrameHeaderSize = 5;            // 4-byte length + 1-byte type
        public const int HeartbeatIntervalSeconds = 10;
        public const int VesselUpdateIntervalMs = 500;   // minimum send interval per vessel
    }
}
