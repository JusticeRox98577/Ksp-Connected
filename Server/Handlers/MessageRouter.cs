using System;
using KspConnected.Server.Core;
using KspConnected.Shared.Protocol;

namespace KspConnected.Server.Handlers
{
    public static class MessageRouter
    {
        public static void Route(ClientSession session, KspServer server,
                                 MessageType type, byte[] payload)
        {
            try
            {
                switch (type)
                {
                    case MessageType.Hello:        HelloHandler.Handle(session, server, payload);        break;
                    case MessageType.VesselUpdate: VesselUpdateHandler.Handle(session, server, payload); break;
                    case MessageType.Chat:         ChatHandler.Handle(session, server, payload);         break;
                    case MessageType.TimeSync:     TimeSyncHandler.Handle(session, server, payload);     break;
                    case MessageType.VesselConfig:  VesselConfigHandler.Handle(session, server, payload);  break;
                    case MessageType.Disconnect:   DisconnectHandler.Handle(session, server, payload);   break;
                    case MessageType.Ping:
                        session.Send(MessageType.Pong, Array.Empty<byte>());
                        break;
                    default:
                        Console.WriteLine($"[Router] Unknown message 0x{(byte)type:X2} from player #{session.PlayerId}");
                        break;
                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[Router] Error handling {type} from #{session.PlayerId}: {ex.Message}");
            }
        }
    }
}
