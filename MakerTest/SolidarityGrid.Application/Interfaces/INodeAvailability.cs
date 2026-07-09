namespace SolidarityGrid.Application.Interfaces
{
    public interface INodeAvailability
    {
        bool IsReady { get; }
        void MarkReady();
    }
}
