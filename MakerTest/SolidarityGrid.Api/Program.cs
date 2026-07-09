
using Microsoft.Extensions.Options;
using SolidarityGrid.Api;
using SolidarityGrid.Api.Middleware;
using SolidarityGrid.Application;
using SolidarityGrid.Application.Contracts;
using SolidarityGrid.Application.Interfaces;
using SolidarityGrid.Application.Options;
using SolidarityGrid.Application.Payments;
using SolidarityGrid.Infrastructure;
using SolidarityGrid.Infrastructure.Options;
using SolidarityGrid.Infrastructure.UpService;
using System.Globalization;

var builder = WebApplication.CreateBuilder(args);
builder.WebHost.UseUrls("http://0.0.0.0:8080");

builder.Services.AddApplication(builder.Configuration);
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddApiServices(builder.Configuration);

var app = builder.Build();

ConfigureRegionalDefaults(app.Services);

app.UseMiddleware<PeerAuthMiddleware>();
app.UseMiddleware<ApiKeyMiddleware>();

await DatabaseInitializer.InitializeAsync(app.Services, CancellationToken.None);

var readiness = app.Services.GetRequiredService<INodeAvailability>();
readiness.MarkReady();

var processingOptions = app.Services.GetRequiredService<IOptions<PaymentProcessingOptions>>().Value;
var regionalClock = app.Services.GetRequiredService<IRegionalClock>();
var logger = app.Services.GetRequiredService<ILoggerFactory>().CreateLogger("SolidarityGrid");
NodeLog.Info(
    logger,
    processingOptions.NodeId,
    $"Nodo listo. Zona horaria compartida: {regionalClock.TimeZoneId} ({regionalClock.NowInRegionalZone:O}).");

var lifetime = app.Services.GetRequiredService<IHostApplicationLifetime>();
lifetime.ApplicationStopping.Register(() =>
    NodeLog.Warn(logger, processingOptions.NodeId, "Graceful shutdown iniciado. Dejando de aceptar nuevo trabajo."));

app.MapGet("/health", (IOptions<PaymentProcessingOptions> options, IRegionalClock clock) =>
    Results.Ok(new
    {
        status = "alive",
        nodeId = options.Value.NodeId,
        timeZoneId = clock.TimeZoneId,
        culture = clock.Culture,
        utcNow = clock.UtcNow,
        regionalNow = clock.NowInRegionalZone
    }));

app.MapGet("/ready", (INodeAvailability nodeAvailability, IOptions<PaymentProcessingOptions> options) =>
    nodeAvailability.IsReady
        ? Results.Ok(new { status = "ready", nodeId = options.Value.NodeId })
        : Results.StatusCode(StatusCodes.Status503ServiceUnavailable));

app.MapGet("/internal/heartbeat", (IOptions<MeshOptions> options, IRegionalClock clock) =>
    Results.Ok(new
    {
        nodeId = options.Value.NodeId,
        atUtc = clock.UtcNow,
        atRegional = clock.NowInRegionalZone,
        timeZoneId = clock.TimeZoneId
    }));

app.MapGet("/internal/peers", (IPeerRegistry registry) => Results.Ok(registry.Snapshot()));

app.MapPost("/pay", async (PayRequest request, HttpContext context, AcceptPaymentHandler handler, CancellationToken cancellationToken) =>
{
    var transactionId = context.Request.Headers.TryGetValue("Idempotency-Key", out var key) && !string.IsNullOrWhiteSpace(key)
        ? key.ToString()
        : $"TX-{Guid.NewGuid():N}";

    if (request.Amount <= 0)
        return Results.BadRequest(new { error = "Amount must be greater than zero." });

    var response = await handler.HandleAsync(transactionId, request.Amount, cancellationToken);
    return Results.Accepted($"/pay/{transactionId}", response);
});

app.MapGet("/pay/{transactionId}", async (string transactionId, GetPaymentStatusHandler handler, CancellationToken cancellationToken) =>
{
    var status = await handler.HandleAsync(transactionId, cancellationToken);
    return status is null ? Results.NotFound(new { error = $"Transaction {transactionId} not found." }) : Results.Ok(status);
});

app.Run();

static void ConfigureRegionalDefaults(IServiceProvider services)
{
    var regional = services.GetRequiredService<IOptions<RegionalOptions>>().Value;
    var culture = new CultureInfo(regional.Culture);
    CultureInfo.DefaultThreadCurrentCulture = culture;
    CultureInfo.DefaultThreadCurrentUICulture = culture;
}

