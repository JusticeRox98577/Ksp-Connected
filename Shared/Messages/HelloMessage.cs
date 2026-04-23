using KspConnected.Shared.Protocol;

namespace KspConnected.Shared.Messages
{
    public class HelloMessage
    {
        public int    ProtocolVersion = ProtocolConstants.Version;
        public string PlayerName      = "";
        public bool   IsRelayHost     = false;  // true = create a new relay room
        public string RoomCode        = "";     // non-empty = join this relay room

        public byte[] ToPayload() => MessageFrame.BuildPayload(w =>
        {
            w.Write(ProtocolVersion);
            w.Write(PlayerName);
            w.Write(IsRelayHost);
            w.Write(RoomCode);
        });

        public static HelloMessage FromPayload(byte[] payload) =>
            MessageFrame.ParsePayload(payload, r => new HelloMessage
            {
                ProtocolVersion = r.ReadInt32(),
                PlayerName      = r.ReadString(),
                IsRelayHost     = r.ReadBoolean(),
                RoomCode        = r.ReadString(),
            });
    }
}
