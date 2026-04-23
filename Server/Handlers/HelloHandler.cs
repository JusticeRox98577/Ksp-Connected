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
                Reject(session, server,
                    $"Protocol mismatch. Server: v{ProtocolConstants.Version}, Client: v{hello.ProtocolVersion}");
                return;
            }

            string name = hello.PlayerName?.Trim() ?? "";
            if (string.IsNullOrEmpty(name)) name = "Kerbal_" + new Random().Next(1000, 9999);

            if (server.Config.RelayMode)
            {
                HandleRelay(session, server, hello, name);
                return;
            }

            // ── direct mode ───────────────────────────────────────────────────
            int playerId = server.Registry.Register(name, session);
            session.SetIdentity(playerId, name);
            Console.WriteLine($"[Hello] Player '{name}' joined as #{playerId}.");

            Accept(session, server, playerId, roomCode: "");

            var reg = server.Registry;
            session.Send(MessageType.PlayerList, reg.BuildPlayerList().ToPayload());
            reg.Broadcast(MessageType.PlayerList, reg.BuildPlayerList().ToPayload());
            SendWelcome(session, server, reg, name, playerId);
        }

        // ── relay mode ────────────────────────────────────────────────────────

        private static void HandleRelay(ClientSession session, KspServer server,
                                        HelloMessage hello, string name)
        {
            if (hello.IsRelayHost)
            {
                string code = server.RelayRooms.CreateRoom(name, session);
                Accept(session, server, session.PlayerId, roomCode: code);

                var reg = server.RelayRooms.GetRoomRegistry(code);
                session.Send(MessageType.PlayerList, reg.BuildPlayerList().ToPayload());
            }
            else if (!string.IsNullOrWhiteSpace(hello.RoomCode))
            {
                string code = hello.RoomCode.Trim().ToUpperInvariant();
                var (ok, error) = server.RelayRooms.JoinRoom(code, name, session);
                if (!ok) { Reject(session, server, error); return; }

                Accept(session, server, session.PlayerId, roomCode: code);

                var reg = server.RelayRooms.GetRoomRegistry(code);
                session.Send(MessageType.PlayerList, reg.BuildPlayerList().ToPayload());
                reg.Broadcast(MessageType.PlayerList, reg.BuildPlayerList().ToPayload());
                SendWelcome(session, server, reg, name, session.PlayerId);
            }
            else
            {
                Reject(session, server,
                    "Server is in relay mode. Set IsRelayHost=true to create a room, " +
                    "or provide a RoomCode to join an existing one.");
            }
        }

        // ── helpers ───────────────────────────────────────────────────────────

        private static void Accept(ClientSession session, KspServer server, int playerId, string roomCode)
        {
            session.Send(MessageType.HelloAck, new HelloAckMessage
            {
                AssignedPlayerId = playerId,
                ServerName       = server.Config.ServerName,
                Accepted         = true,
                RoomCode         = roomCode,
            }.ToPayload());
        }

        private static void Reject(ClientSession session, KspServer server, string reason)
        {
            session.Send(MessageType.HelloAck, new HelloAckMessage
            {
                AssignedPlayerId = -1,
                ServerName       = server.Config.ServerName,
                Accepted         = false,
                RejectReason     = reason,
            }.ToPayload());
            session.Close(reason);
        }

        private static void SendWelcome(ClientSession session, KspServer server,
                                        PlayerRegistry reg, string name, int playerId)
        {
            if (!string.IsNullOrWhiteSpace(server.Config.WelcomeMessage))
                session.Send(MessageType.Chat, new ChatMessage
                {
                    SenderId = -1, SenderName = "Server",
                    Text = server.Config.WelcomeMessage,
                }.ToPayload());

            reg.Broadcast(MessageType.Chat, new ChatMessage
            {
                SenderId = -1, SenderName = "Server",
                Text = $"{name} joined the session.",
            }.ToPayload(), excludePlayerId: playerId);
        }
    }
}
