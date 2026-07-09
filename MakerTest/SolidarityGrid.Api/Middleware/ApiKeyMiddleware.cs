using Microsoft.Extensions.Options;
using SolidarityGrid.Api.Options;

namespace SolidarityGrid.Api.Middleware
{
    public sealed class ApiKeyMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly string _apiKey;

        public ApiKeyMiddleware(RequestDelegate next, IOptions<ApiAuthOptions> options)
        {
            _next = next;
            _apiKey = options.Value.ApiKey;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            if (context.Request.Path.StartsWithSegments("/internal")
                || context.Request.Path.StartsWithSegments("/health")
                || context.Request.Path.StartsWithSegments("/ready"))
            {
                await _next(context);
                return;
            }

            var authorized =
                (context.Request.Headers.TryGetValue("X-Api-Key", out var apiKey) && apiKey == _apiKey)
                || (context.Request.Headers.TryGetValue("Authorization", out var auth)
                    && auth.ToString().Equals($"Bearer {_apiKey}", StringComparison.Ordinal));

            if (!authorized)
            {
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                await context.Response.WriteAsync("API key invalid or missing.");
                return;
            }

            await _next(context);
        }
    }

}
