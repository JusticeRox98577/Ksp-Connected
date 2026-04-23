using System.IO;
using KspConnected.Shared.Protocol;

namespace KspConnected.Shared.Messages
{
    public class TimeSyncMessage
    {
        public double ClientUT;         // client's Planetarium.GetUniversalTime()
        public long   ClientTicksSent;  // Environment.TickCount64 snapshot for RTT calc

        public byte[] ToPayload() => MessageFrame.BuildPayload(w =>
        {
            w.Write(ClientUT);
            w.Write(ClientTicksSent);
        });

        public static TimeSyncMessage FromPayload(byte[] payload) =>
            MessageFrame.ParsePayload(payload, r => new TimeSyncMessage
            {
                ClientUT        = r.ReadDouble(),
                ClientTicksSent = r.ReadInt64(),
            });
    }

    public class TimeSyncReplyMessage
    {
        public double ClientUT;         // echoed back from request
        public long   ClientTicksSent;  // echoed back for RTT calculation
        public double ServerUT;         // server's authoritative universal time

        public byte[] ToPayload() => MessageFrame.BuildPayload(w =>
        {
            w.Write(ClientUT);
            w.Write(ClientTicksSent);
            w.Write(ServerUT);
        });

        public static TimeSyncReplyMessage FromPayload(byte[] payload) =>
            MessageFrame.ParsePayload(payload, r => new TimeSyncReplyMessage
            {
                ClientUT        = r.ReadDouble(),
                ClientTicksSent = r.ReadInt64(),
                ServerUT        = r.ReadDouble(),
            });
    }
}
