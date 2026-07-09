namespace SolidarityGrid.Application.Interfaces
{
    public interface IPeerRegistry
    {
        void RecordSuccess(string peerId, DateTime seenAtUtc);
        void MarkUnhealthy(string peerId);
        bool IsConsideredDead(string peerId, int timeoutMs);
        IReadOnlyDictionary<string, bool> Snapshot();
    }
}
