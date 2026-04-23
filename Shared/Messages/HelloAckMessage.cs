using System.IO;
using KspConnected.Shared.Protocol;

namespace KspConnected.Shared.Messages
{
    public class HelloAckMessage
    {
        public int    AssignedPlayerId;
        public string ServerName = "KSP-Connected Server";
        public bool   Accepted   = true;
        public string RejectReason = "";  // non-empty when Accepted == false

        public byte[] ToPayload() => MessageFrame.BuildPayload(w =>
        {
            w.Write(AssignedPlayerId);
            w.Write(ServerName);
            w.Write(Accepted);
            w.Write(RejectReason);
        });

        public static HelloAckMessage FromPayload(byte[] payload) =>
            MessageFrame.ParsePayload(payload, r => new HelloAckMessage
            {
                AssignedPlayerId = r.ReadInt32(),
                ServerName       = r.ReadString(),
                Accepted         = r.ReadBoolean(),
                RejectReason     = r.ReadString(),
            });
    }
}
