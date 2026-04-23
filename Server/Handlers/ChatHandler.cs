using KspConnected.Server.Core;
using KspConnected.Shared.Messages;
using KspConnected.Shared.Protocol;

namespace KspConnected.Server.Handlers
{
    public static class ChatHandler
    {
        public static void Handle(ClientSession session, KspServer server, byte[] payload)
        {
            var msg = ChatMessage.FromPayload(payload);

            // Overwrite with server-verified identity
            msg.SenderId   = session.PlayerId;
            msg.SenderName = session.PlayerName;

            System.Console.WriteLine($"[Chat] {msg.SenderName}: {msg.Text}");

            byte[] fwd = msg.ToPayload();
            server.GetRegistry(session).Broadcast(MessageType.Chat, fwd);
        }
    }
}
