using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using KspConnected.Shared.Data;

namespace KspConnected.Client.Sync
{
    /// <summary>
    /// Thread-safe store of the latest vessel state for every remote player.
    /// UI components and sync senders read from this without locking.
    /// </summary>
    public class RemoteVesselStore
    {
        private readonly ConcurrentDictionary<int, VesselState> _states =
            new ConcurrentDictionary<int, VesselState>();

        public void Update(VesselState state) =>
            _states[state.PlayerId] = state;

        public void Remove(int playerId) =>
            _states.TryRemove(playerId, out _);

        public void Clear() => _states.Clear();

        public bool TryGet(int playerId, out VesselState state) =>
            _states.TryGetValue(playerId, out state);

        public ICollection<VesselState> All() => _states.Values;
    }
}
