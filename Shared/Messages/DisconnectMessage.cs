using System.IO;
using KspConnected.Shared.Protocol;

namespace KspConnected.Shared.Messages
{
    public class DisconnectMessage
    {
        public string Reason = "";

        public byte[] ToPayload() => MessageFrame.BuildPayload(w => w.Write(Reason));

        public static DisconnectMessage FromPayload(byte[] payload) =>
            MessageFrame.ParsePayload(payload, r => new DisconnectMessage
            {
                Reason = r.ReadString(),
            });
    }
}
