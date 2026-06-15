using FillTheDoc.DaDataClient.Abstractions;
using FillTheDoc.DaDataClient.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace FillTheDoc.DaDataClient;

public static class DaDataClientServiceCollectionExtensions {
    public static IServiceCollection AddDaDataClient(
        this IServiceCollection services,
        Action<DaDataClientOptions> configureOptions,
        Func<IServiceProvider, IDaDataTokenProvider> tokenProviderFactory) {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configureOptions);
        ArgumentNullException.ThrowIfNull(tokenProviderFactory);

        var options = new DaDataClientOptions();
        configureOptions(options);
        options.Validate();

        services.AddSingleton(options);
        services.AddSingleton(tokenProviderFactory);

        services.AddHttpClient<IDaDataClient, DaDataClient>((serviceProvider, client) =>
        {
            var resolvedOptions = serviceProvider.GetRequiredService<DaDataClientOptions>();
            client.BaseAddress = resolvedOptions.BaseUrl;
            client.Timeout = Timeout.InfiniteTimeSpan;
        });

        return services;
    }
}