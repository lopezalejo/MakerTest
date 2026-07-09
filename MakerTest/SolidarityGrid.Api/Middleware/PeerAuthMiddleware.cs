using Microsoft.Extensions.Options;
using SolidarityGrid.Infrastructure.Options;

namespace SolidarityGrid.Api.Middleware
{
    public sealed class PeerAuthMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly string _peerToken;

        public PeerAuthMiddleware(RequestDelegate next, IOptions<MeshOptions> options)
        {
            _next = next;
            _peerToken = options.Value.PeerToken;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            if (!context.Request.Path.StartsWithSegments("/internal"))
            {
                await _next(context);
                return;
            }

            if (!context.Request.Headers.TryGetValue("X-Peer-Token", out var token) || token != _peerToken)
            {
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                await context.Response.WriteAsync("Peer token invalid or missing.");
                return;
            }

            await _next(context);
        }
    }
}
