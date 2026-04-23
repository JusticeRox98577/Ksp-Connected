using KspConnected.Server.Core;
using KspConnected.Shared.Messages;
using KspConnected.Shared.Protocol;

namespace KspConnected.Server.Handlers
{
    public static class VesselUpdateHandler
    {
        public static void Handle(ClientSession session, KspServer server, byte[] payload)
        {
            var msg = VesselUpdateMessage.FromPayload(payload);

            // Stamp with the authoritative player ID from the session
            msg.State.PlayerId   = session.PlayerId;
            msg.State.PlayerName = session.PlayerName;

            server.Registry.UpdateVessel(session.PlayerId, msg.State);

            // Forward to all other players
            byte[] fwd = msg.ToPayload();
            server.Registry.Broadcast(MessageType.VesselUpdate, fwd, excludePlayerId: session.PlayerId);
        }
    }
}
