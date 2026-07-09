using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SolidarityGrid.Application;
using SolidarityGrid.Application.Interfaces;
using SolidarityGrid.Infrastructure.Options;

namespace SolidarityGrid.Infrastructure.Mesh
{
    public sealed class HeartbeatBackgroundService : BackgroundService
    {
        private readonly MeshOptions _options;
        private readonly IPeerRegistry _peerRegistry;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<HeartbeatBackgroundService> _logger;
        private readonly HashSet<string> _loggedDownPeers = new(StringComparer.OrdinalIgnoreCase);

        public HeartbeatBackgroundService(
            IOptions<MeshOptions> options,
            IPeerRegistry peerRegistry,
            IHttpClientFactory httpClientFactory,
            ILogger<HeartbeatBackgroundService> logger)
        {
            _options = options.Value;
            _peerRegistry = peerRegistry;
            _httpClientFactory = httpClientFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            await Task.Delay(TimeSpan.FromSeconds(2), stoppingToken);

            while (!stoppingToken.IsCancellationRequested)
            {
                foreach (var peerUrl in _options.Peers)
                {
                    var peerId = ExtractPeerId(peerUrl);
                    try
                    {
                        var client = _httpClientFactory.CreateClient("mesh");
                        using var request = new HttpRequestMessage(HttpMethod.Get, $"{peerUrl.TrimEnd('/')}/internal/heartbeat");
                        request.Headers.Add("X-Peer-Token", _options.PeerToken);
                        using var response = await client.SendAsync(request, stoppingToken);

                        if (response.IsSuccessStatusCode)
                        {
                            _peerRegistry.RecordSuccess(peerId, DateTime.UtcNow);
                            _loggedDownPeers.Remove(peerId);
                        }
                        else
                        {
                            MarkPeerDown(peerId);
                        }
                    }
                    catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
                    {
                        MarkPeerDown(peerId);
                    }
                }

                await Task.Delay(_options.HeartbeatIntervalMs, stoppingToken);
            }
        }

        private void MarkPeerDown(string peerId)
        {
            _peerRegistry.MarkUnhealthy(peerId);
            if (_loggedDownPeers.Add(peerId))
                NodeLog.Warn(_logger, _options.NodeId, $"Detecté que {peerId} dejó de responder.");
        }

        private static string ExtractPeerId(string peerUrl) =>
            Uri.TryCreate(peerUrl, UriKind.Absolute, out var uri) ? uri.Host : peerUrl;
    }

}