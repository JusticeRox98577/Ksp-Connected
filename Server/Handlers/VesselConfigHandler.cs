using KspConnected.Server.Core;
using KspConnected.Shared.Messages;
using KspConnected.Shared.Protocol;

namespace KspConnected.Server.Handlers
{
    /// <summary>
    /// Forwards a VesselConfigMessage (compressed ConfigNode) from the sender
    /// to every other connected player so they can spawn the ghost vessel.
    /// </summary>
    public static class VesselConfigHandler
    {
        public static void Handle(ClientSession session, KspServer server, byte[] payload)
        {
            var msg = VesselConfigMessage.FromPayload(payload);
            msg.PlayerId = session.PlayerId; // stamp with verified identity

            System.Console.WriteLine(
                $"[VesselConfig] Player #{session.PlayerId} sent vessel config " +
                $"({payload.Length} bytes compressed).");

            byte[] fwd = msg.ToPayload();
            server.GetRegistry(session).Broadcast(MessageType.VesselConfig, fwd, excludePlayerId: session.PlayerId);
        }
    }
}
