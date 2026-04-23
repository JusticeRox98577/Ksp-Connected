using KspConnected.Server.Core;
using KspConnected.Shared.Messages;

namespace KspConnected.Server.Handlers
{
    public static class DisconnectHandler
    {
        public static void Handle(ClientSession session, KspServer server, byte[] payload)
        {
            var msg = DisconnectMessage.FromPayload(payload);
            session.Close("Client disconnect: " + msg.Reason);
        }
    }
}
