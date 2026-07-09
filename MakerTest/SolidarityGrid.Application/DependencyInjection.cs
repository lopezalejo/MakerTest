using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SolidarityGrid.Application.Interfaces;
using SolidarityGrid.Application.Options;
using SolidarityGrid.Application.Payments;

namespace SolidarityGrid.Application
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddApplication(this IServiceCollection services, IConfiguration configuration)
        {
            services.Configure<PaymentProcessingOptions>(configuration.GetSection(PaymentProcessingOptions.SectionName));
            services.PostConfigure<PaymentProcessingOptions>(options =>
            {
                options.NodeId = configuration["NODE_ID"]
                    ?? configuration[$"{PaymentProcessingOptions.SectionName}:NodeId"]
                    ?? options.NodeId;
            });

            services.AddSingleton<PaymentProcessingService>();
            services.AddSingleton<IPaymentProcessingService>(sp => sp.GetRequiredService<PaymentProcessingService>());
            services.AddScoped<AcceptPaymentHandler>();
            services.AddScoped<GetPaymentStatusHandler>();

            return services;
        }
    }

}
