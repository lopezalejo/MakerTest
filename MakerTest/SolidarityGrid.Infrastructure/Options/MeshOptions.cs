namespace SolidarityGrid.Infrastructure.Options
{
    public sealed class MeshOptions
    {
        public const string SectionName = "Mesh";

        public string NodeId { get; set; } = "node-a";
        public string PeerToken { get; set; } = "mesh-secret";
        public string[] Peers { get; set; } = [];
        public int HeartbeatIntervalMs { get; set; } = 1000;
        public int PeerTimeoutMs { get; set; } = 3000;
    }
}
