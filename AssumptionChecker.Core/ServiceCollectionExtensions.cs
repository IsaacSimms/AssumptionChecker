////// handles dependency injection for .NET hosts. (VS extension, CLI, etc.) /////

using Microsoft.Extensions.DependencyInjection;

namespace AssumptionChecker.Core
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddAssumptionChecker(
            this IServiceCollection services, string baseUrl, int timeoutSeconds = 60)
        {
            // validate the base URL before registering services
            if (string.IsNullOrWhiteSpace(baseUrl))
                throw new ArgumentException("Engine base URL cannot be null or empty", nameof(baseUrl));

            // register the AssumptionCheckerHttpService with a configured HttpClient
            services.AddHttpClient<IAssumptionCheckerService, AssumptionCheckerHttpService>(client =>
            {
                client.BaseAddress = new Uri(baseUrl);                 // set the base URL for API calls
                client.Timeout = TimeSpan.FromSeconds(timeoutSeconds); // set the timeout for API calls
            });

            return services;
        }
    }
}
