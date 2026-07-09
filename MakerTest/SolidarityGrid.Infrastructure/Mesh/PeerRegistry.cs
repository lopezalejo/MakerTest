using SolidarityGrid.Application.Interfaces;
using System.Collections.Concurrent;

namespace SolidarityGrid.Infrastructure.Mesh
{
    public sealed class PeerRegistry : IPeerRegistry
    {
        private readonly ConcurrentDictionary<string, DateTime> _lastSeenUtc = new(StringComparer.OrdinalIgnoreCase);
        private readonly ConcurrentDictionary<string, bool> _healthy = new(StringComparer.OrdinalIgnoreCase);

        public void RecordSuccess(string peerId, DateTime seenAtUtc)
        {
            _lastSeenUtc[peerId] = seenAtUtc;
            _healthy[peerId] = true;
        }

        public void MarkUnhealthy(string peerId) => _healthy[peerId] = false;

        public bool IsConsideredDead(string peerId, int timeoutMs)
        {
            if (_healthy.TryGetValue(peerId, out var healthy) && !healthy)
                return true;

            if (!_lastSeenUtc.TryGetValue(peerId, out var lastSeen))
                return false;

            return DateTime.UtcNow - lastSeen > TimeSpan.FromMilliseconds(timeoutMs);
        }

        public IReadOnlyDictionary<string, bool> Snapshot() =>
            _healthy.ToDictionary(static x => x.Key, static x => x.Value);
    }
}
