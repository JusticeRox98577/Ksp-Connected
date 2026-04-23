using KspConnected.Server.Core;
using KspConnected.Shared.Messages;
using KspConnected.Shared.Protocol;

namespace KspConnected.Server.Handlers
{
    public static class TimeSyncHandler
    {
        public static void Handle(ClientSession session, KspServer server, byte[] payload)
        {
            var req = TimeSyncMessage.FromPayload(payload);

            // The server uses wall-clock seconds since Unix epoch as an opaque
            // monotonic reference. Clients use it only to compare relative drift.
            double serverUT = (double)System.DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() / 1000.0;

            var reply = new TimeSyncReplyMessage
            {
                ClientUT        = req.ClientUT,
                ClientTicksSent = req.ClientTicksSent,
                ServerUT        = serverUT,
            };
            session.Send(MessageType.TimeSyncReply, reply.ToPayload());
        }
    }
}
