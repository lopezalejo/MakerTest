using Microsoft.Extensions.Logging;

namespace SolidarityGrid.Application;

public static class NodeLog
{
    public static void Info(ILogger logger, string nodeId, string message) =>
        logger.LogInformation("[{NodeId}]: {Message}", nodeId, message);

    public static void Warn(ILogger logger, string nodeId, string message) =>
        logger.LogWarning("[{NodeId}]: {Message}", nodeId, message);
}

