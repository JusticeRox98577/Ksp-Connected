using System.IO;
using KspConnected.Shared.Protocol;

namespace KspConnected.Shared.Messages
{
    public class HelloMessage
    {
        public int    ProtocolVersion = ProtocolConstants.Version;
        public string PlayerName      = "";

        public byte[] ToPayload() => MessageFrame.BuildPayload(w =>
        {
            w.Write(ProtocolVersion);
            w.Write(PlayerName);
        });

        public static HelloMessage FromPayload(byte[] payload) =>
            MessageFrame.ParsePayload(payload, r => new HelloMessage
            {
                ProtocolVersion = r.ReadInt32(),
                PlayerName      = r.ReadString(),
            });
    }
}
