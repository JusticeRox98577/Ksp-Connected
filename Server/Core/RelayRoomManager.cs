using System;
using System.Collections.Concurrent;
using System.Text;

namespace KspConnected.Server.Core
{
    public class RelayRoom
    {
        public string         Code;
        public PlayerRegistry Players = new PlayerRegistry();
    }

    /// <summary>
    /// Manages relay rooms so players can connect without port forwarding.
    /// One player creates a room and shares the 6-character code with friends.
    /// All traffic is routed through this server within each room.
    /// </summary>
    public class RelayRoomManager
    {
        private readonly ConcurrentDictionary<string, RelayRoom> _rooms =
            new ConcurrentDictionary<string, RelayRoom>(StringComparer.OrdinalIgnoreCase);

        // Unambiguous alphanumeric characters (no 0/O, 1/I/L)
        private const string Alphabet = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
        private static readonly Random _rng = new Random();

        private static string GenerateCode()
        {
            var sb = new StringBuilder(6);
            for (int i = 0; i < 6; i++)
                sb.Append(Alphabet[_rng.Next(Alphabet.Length)]);
            return sb.ToString();
        }

        /// <summary>Creates a new room, registers the host, and returns the room code.</summary>
        public string CreateRoom(string playerName, ClientSession session)
        {
            string code;
            RelayRoom room;
            do
            {
                code = GenerateCode();
                room = new RelayRoom { Code = code };
            }
            while (!_rooms.TryAdd(code, room));

            int playerId = room.Players.Register(playerName, session);
            session.SetIdentity(playerId, playerName);
            session.RoomCode = code;

            Console.WriteLine($"[Relay] Room '{code}' created by '{playerName}' (#{playerId}).");
            return code;
        }

        /// <summary>Adds a player to an existing room. Returns (true,"") on success or (false, error).</summary>
        public (bool ok, string error) JoinRoom(string code, string playerName, ClientSession session)
        {
            if (!_rooms.TryGetValue(code, out var room))
                return (false, $"Room '{code}' not found. Check the code and try again.");

            int playerId = room.Players.Register(playerName, session);
            session.SetIdentity(playerId, playerName);
            session.RoomCode = code;

            Console.WriteLine($"[Relay] '{playerName}' (#{playerId}) joined room '{code}'.");
            return (true, "");
        }

        /// <summary>Returns the PlayerRegistry for the given room, or null if not found.</summary>
        public PlayerRegistry GetRoomRegistry(string roomCode)
        {
            if (!string.IsNullOrEmpty(roomCode) && _rooms.TryGetValue(roomCode, out var room))
                return room.Players;
            return null;
        }

        /// <summary>Removes the room if it exists (called when last player leaves).</summary>
        public void CloseRoom(string roomCode)
        {
            if (_rooms.TryRemove(roomCode, out _))
                Console.WriteLine($"[Relay] Room '{roomCode}' closed (empty).");
        }

        public int RoomCount => _rooms.Count;
    }
}
