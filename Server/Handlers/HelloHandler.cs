using System;
using KspConnected.Server.Core;
using KspConnected.Shared.Messages;
using KspConnected.Shared.Protocol;

namespace KspConnected.Server.Handlers
{
    public static class HelloHandler
    {
        public static void Handle(ClientSession session, KspServer server, byte[] payload)
        {
            var hello = HelloMessage.FromPayload(payload);

            if (hello.ProtocolVersion != ProtocolConstants.Version)
            {
                var reject = new HelloAckMessage
                {
                    AssignedPlayerId = -1,
                    ServerName       = server.Config.ServerName,
                    Accepted         = false,
                    RejectReason     = $"Protocol mismatch. Server: v{ProtocolConstants.Version}, Client: v{hello.ProtocolVersion}",
                };
                session.Send(MessageType.HelloAck, reject.ToPayload());
                session.Close(reject.RejectReason);
                return;
            }

            string name     = hello.PlayerName?.Trim() ?? "Unknown";
            if (string.IsNullOrEmpty(name)) name = "Kerbal_" + new Random().Next(1000, 9999);

            int playerId    = server.Registry.Register(name, session);
            session.SetIdentity(playerId, name);

            Console.WriteLine($"[Hello] Player '{name}' joined as #{playerId}.");

            // Send acceptance
            var ack = new HelloAckMessage
            {
                AssignedPlayerId = playerId,
                ServerName       = server.Config.ServerName,
                Accepted         = true,
            };
            session.Send(MessageType.HelloAck, ack.ToPayload());

            // Send current player list to new player
            session.Send(MessageType.PlayerList, server.Registry.BuildPlayerList().ToPayload());

            // Broadcast updated player list to everyone
            var listPayload = server.Registry.BuildPlayerList().ToPayload();
            server.Registry.Broadcast(MessageType.PlayerList, listPayload);

            // Send welcome chat
            if (!string.IsNullOrWhiteSpace(server.Config.WelcomeMessage))
            {
                var welcome = new ChatMessage
                {
                    SenderId   = -1,
                    SenderName = "Server",
                    Text       = server.Config.WelcomeMessage,
                };
                session.Send(MessageType.Chat, welcome.ToPayload());
            }

            // Announce join to others
            var joinMsg = new ChatMessage
            {
                SenderId   = -1,
                SenderName = "Server",
                Text       = $"{name} joined the session.",
            };
            server.Registry.Broadcast(MessageType.Chat, joinMsg.ToPayload(), excludePlayerId: playerId);
        }
    }
}
