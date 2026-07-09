using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SolidarityGrid.Application.Interfaces;
using SolidarityGrid.Application.Interfaces.Repository;
using SolidarityGrid.Infrastructure.Database;
using SolidarityGrid.Infrastructure.Mesh;
using SolidarityGrid.Infrastructure.Options;
using SolidarityGrid.Infrastructure.Regional;
using SolidarityGrid.Infrastructure.Repository;

namespace SolidarityGrid.Infrastructure
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
        {
            services.Configure<MeshOptions>(configuration.GetSection(MeshOptions.SectionName));
            services.Configure<DatabaseOptions>(configuration.GetSection(DatabaseOptions.SectionName));
            services.Configure<RegionalOptions>(configuration.GetSection(RegionalOptions.SectionName));

            services.PostConfigure<MeshOptions>(options =>
            {
                options.NodeId = configuration["NODE_ID"]
                    ?? configuration[$"{MeshOptions.SectionName}:NodeId"]
                    ?? options.NodeId;
                options.PeerToken = configuration["PEER_TOKEN"]
                    ?? configuration[$"{MeshOptions.SectionName}:PeerToken"]
                    ?? options.PeerToken;

                var peers = configuration["PEERS"];
                if (!string.IsNullOrWhiteSpace(peers))
                    options.Peers = peers.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            });

            services.PostConfigure<DatabaseOptions>(options =>
            {
                options.SolidarityGrid = configuration.GetConnectionString("SolidarityGrid")
                    ?? configuration["ConnectionStrings:SolidarityGrid"]
                    ?? options.SolidarityGrid;
            });

            services.PostConfigure<RegionalOptions>(options =>
            {
                options.TimeZoneId = configuration["TZ"]
                    ?? configuration[$"{RegionalOptions.SectionName}:TimeZoneId"]
                    ?? options.TimeZoneId;
                options.Culture = configuration[$"{RegionalOptions.SectionName}:Culture"]
                    ?? configuration["CULTURE"]
                    ?? options.Culture;
            });

            services.AddDbContext<SolidaryGridDBContext>((sp, builder) =>
            {
                var database = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<DatabaseOptions>>().Value;
                builder.UseSqlServer(database.SolidarityGrid);
            });

            services.AddSingleton<INodeAvailability, NodeAvailability>();
            services.AddSingleton<IPeerRegistry, PeerRegistry>();
            services.AddSingleton<IRegionalClock, RegionalClock>();
            services.AddScoped<IPaymentRepository, PaymentRepository>();
            services.AddHttpClient("mesh", client => client.Timeout = TimeSpan.FromSeconds(2));
            services.AddHostedService<HeartbeatBackgroundService>();
            services.AddHostedService<ReconciliationBackgroundService>();

            return services;
        }
    }
}
