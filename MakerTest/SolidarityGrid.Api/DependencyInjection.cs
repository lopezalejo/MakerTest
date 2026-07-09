using SolidarityGrid.Api.Options;

namespace SolidarityGrid.Api
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddApiServices(this IServiceCollection services, IConfiguration configuration)
        {
            services.Configure<ApiAuthOptions>(configuration.GetSection(ApiAuthOptions.SectionName));
            services.PostConfigure<ApiAuthOptions>(options =>
            {
                options.ApiKey = configuration["API_KEY"]
                    ?? configuration[$"{ApiAuthOptions.SectionName}:ApiKey"]
                    ?? options.ApiKey;
            });

            return services;
        }
    }

}
