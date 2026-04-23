using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using KspConnected.Shared.Data;
using KspConnected.Shared.Messages;

namespace KspConnected.Server.Core
{
    public class PlayerRecord
    {
        public int         PlayerId;
        public string      PlayerName  = "";
        public VesselState LastVessel;
        public ClientSession Session;
    }

    public class PlayerRegistry
    {
        private readonly ConcurrentDictionary<int, PlayerRecord> _players =
            new ConcurrentDictionary<int, PlayerRecord>();
        private int _nextId = 1;

        public int Register(string playerName, ClientSession session)
        {
            int id = Interlocked.Increment(ref _nextId) - 1;
            _players[id] = new PlayerRecord
            {
                PlayerId   = id,
                PlayerName = playerName,
                Session    = session,
            };
            return id;
        }

        public bool Remove(int playerId, out PlayerRecord record) =>
            _players.TryRemove(playerId, out record);

        public void UpdateVessel(int playerId, VesselState state)
        {
            if (_players.TryGetValue(playerId, out var record))
            {
                record.LastVessel = state;
                record.PlayerName = state.PlayerName; // keep name in sync
            }
        }

        public ICollection<PlayerRecord> All() => _players.Values;

        public PlayerListMessage BuildPlayerList()
        {
            var msg = new PlayerListMessage();
            foreach (var r in _players.Values)
            {
                msg.Players.Add(new PlayerEntry
                {
                    PlayerId   = r.PlayerId,
                    PlayerName = r.PlayerName,
                    VesselName = r.LastVessel?.VesselName ?? "",
                    BodyName   = r.LastVessel?.BodyName   ?? "",
                });
            }
            return msg;
        }

        public void Broadcast(KspConnected.Shared.Protocol.MessageType type, byte[] payload,
                              int excludePlayerId = -1)
        {
            foreach (var record in _players.Values)
            {
                if (record.PlayerId == excludePlayerId) continue;
                record.Session.Send(type, payload);
            }
        }

        public int Count => _players.Count;
    }
}
