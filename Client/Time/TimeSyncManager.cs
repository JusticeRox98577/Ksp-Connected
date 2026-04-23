using System;
using System.Collections.Generic;
using KspConnected.Client.Core;
using KspConnected.Shared.Messages;

namespace KspConnected.Client.Time
{
    /// <summary>
    /// Periodically syncs game universal time with the server using a simplified
    /// NTP-style two-packet exchange. Maintains a rolling average of UT offset.
    /// </summary>
    public class TimeSyncManager
    {
        private const int   SampleCount       = 8;
        private const float SyncIntervalSec   = 15f;

        private readonly ConnectionManager _conn;
        private readonly Queue<double>     _offsets = new Queue<double>();
        private float                      _timer;

        public double UTOffset { get; private set; } = 0.0;

        public TimeSyncManager(ConnectionManager conn) => _conn = conn;

        public void Update()
        {
            _timer += UnityEngine.Time.deltaTime;
            if (_timer >= SyncIntervalSec)
            {
                _timer = 0f;
                SendSync();
            }
        }

        private void SendSync()
        {
            _conn.SendTimeSync(new TimeSyncMessage
            {
                ClientUT        = Planetarium.GetUniversalTime(),
                ClientTicksSent = (long)System.Diagnostics.Stopwatch.GetTimestamp(),
            });
        }

        public void HandleReply(TimeSyncReplyMessage reply)
        {
            long rttTicks = (long)System.Diagnostics.Stopwatch.GetTimestamp() - reply.ClientTicksSent;
            double rttSeconds = (double)rttTicks / System.Diagnostics.Stopwatch.Frequency;
            double estimatedServerUT = reply.ServerUT + rttSeconds / 2.0;
            double offset = estimatedServerUT - Planetarium.GetUniversalTime();

            _offsets.Enqueue(offset);
            if (_offsets.Count > SampleCount) _offsets.Dequeue();

            double sum = 0;
            foreach (double o in _offsets) sum += o;
            UTOffset = sum / _offsets.Count;
        }

        /// <summary>Returns corrected universal time for display/comparison purposes.</summary>
        public double GetCorrectedUT() => Planetarium.GetUniversalTime() + UTOffset;
    }
}
