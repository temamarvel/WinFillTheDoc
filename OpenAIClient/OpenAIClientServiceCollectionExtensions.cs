using FillTheDoc.OpenAIClient.Abstractions;
using FillTheDoc.OpenAIClient.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace FillTheDoc.OpenAIClient;

public static class OpenAIClientServiceCollectionExtensions {
    public static IServiceCollection AddOpenAIClient(
        this IServiceCollection services,
        Action<OpenAIClientOptions> configureOptions,
        Func<IServiceProvider, IOpenAIApiKeyProvider> apiKeyProviderFactory) {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configureOptions);
        ArgumentNullException.ThrowIfNull(apiKeyProviderFactory);

        var options = new OpenAIClientOptions();
        configureOptions(options);

        services.AddSingleton(options);
        services.AddSingleton<IOpenAIApiKeyProvider>(apiKeyProviderFactory);
        services.AddHttpClient<IOpenAIClient, OpenAIClient>((serviceProvider, client) =>
        {
            var resolvedOptions = serviceProvider.GetRequiredService<OpenAIClientOptions>();
            client.BaseAddress = resolvedOptions.BaseUrl;
            client.Timeout = resolvedOptions.Timeout;

            if (!string.IsNullOrWhiteSpace(resolvedOptions.UserAgent)) {
                client.DefaultRequestHeaders.UserAgent.ParseAdd(resolvedOptions.UserAgent);
            }
        });

        return services;
    }
}