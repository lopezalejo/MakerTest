namespace SolidarityGrid.Infrastructure
{
    public sealed class NodeAvailability : Application.Interfaces.INodeAvailability
    {
        private volatile bool _ready;
        public bool IsReady => _ready;
        public void MarkReady() => _ready = true;
    }
}
