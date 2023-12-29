using System.Text.Json;
using System.Text.Json.Serialization;
using Elsa.Api.Client.Contracts;
using Elsa.Api.Client.Converters;
using Elsa.Api.Client.HttpMessageHandlers;
using Elsa.Api.Client.Options;
using Elsa.Api.Client.Resources.ActivityDescriptorOptions.Contracts;
using Elsa.Api.Client.Resources.ActivityDescriptors.Contracts;
using Elsa.Api.Client.Resources.ActivityExecutions.Contracts;
using Elsa.Api.Client.Resources.Features.Contracts;
using Elsa.Api.Client.Resources.Identity.Contracts;
using Elsa.Api.Client.Resources.IncidentStrategies.Contracts;
using Elsa.Api.Client.Resources.Scripting.Contracts;
using Elsa.Api.Client.Resources.StorageDrivers.Contracts;
using Elsa.Api.Client.Resources.VariableTypes.Contracts;
using Elsa.Api.Client.Resources.WorkflowActivationStrategies.Contracts;
using Elsa.Api.Client.Resources.WorkflowDefinitions.Contracts;
using Elsa.Api.Client.Resources.WorkflowExecutionContexts.Contracts;
using Elsa.Api.Client.Resources.WorkflowInstances.Contracts;
using Elsa.Api.Client.Services;
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Polly;
using Refit;

namespace Elsa.Api.Client.Extensions;

/// <summary>
/// Provides extension methods for dependency injection.
/// </summary>
[PublicAPI]
public static class DependencyInjectionExtensions
{
    /// <summary>
    /// Adds the Elsa client to the service collection.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="baseAddress">The base address of the Elsa API.</param>
    /// <param name="apiKey">The API key to use for authentication.</param>
    /// <param name="configureHttpClient">An optional delegate that can be used to configure the HTTP client.</param>
    /// <param name="configureBuilderOptions">An optional delegate that can be used to configure the client builder options.</param>
    public static IServiceCollection AddElsaClient(this IServiceCollection services, Uri baseAddress, string apiKey, Action<IServiceProvider, HttpClient>? configureHttpClient = default, Action<ElsaClientBuilderOptions>? configureBuilderOptions = default)
    {
        services.AddScoped<ApiKeyHttpMessageHandler>();
        return services.AddElsaClient(
            options =>
            {
                options.BaseAddress = baseAddress;
                options.ApiKey = apiKey;
                options.ConfigureHttpClient = configureHttpClient;
            },
            configureBuilderOptions: options =>
            {
                options.ConfigureHttpClientBuilder = builder => builder.AddHttpMessageHandler<ApiKeyHttpMessageHandler>();
                configureBuilderOptions?.Invoke(options);
            });
    }
    
    /// <summary>
    /// Adds the Elsa client to the service collection.
    /// </summary>
    public static IServiceCollection AddElsaClient(this IServiceCollection services, Action<ElsaClientOptions>? configureOptions = default, Action<ElsaClientBuilderOptions>? configureBuilderOptions = default)
    {
        var builderOptions = new ElsaClientBuilderOptions();
        configureBuilderOptions?.Invoke(builderOptions);

        services.Configure(configureOptions ?? (_ => { }));
        services.AddScoped<IElsaClient, ElsaClient>();
        services.AddApi<IWorkflowDefinitionsApi>(builderOptions);
        services.AddApi<IWorkflowInstancesApi>(builderOptions);
        services.AddApi<IActivityDescriptorsApi>(builderOptions);
        services.AddApi<IActivityDescriptorOptionsApi>(builderOptions);
        services.AddApi<IActivityExecutionsApi>(builderOptions);
        services.AddApi<IStorageDriversApi>(builderOptions);
        services.AddApi<IVariableTypesApi>(builderOptions);
        services.AddApi<IWorkflowActivationStrategiesApi>(builderOptions);
        services.AddApi<IIncidentStrategiesApi>(builderOptions);
        services.AddApi<ILoginApi>(builderOptions);
        services.AddApi<IFeaturesApi>(builderOptions);
        services.AddApi<IJavaScriptApi>(builderOptions);
        services.AddApi<IExpressionDescriptorsApi>(builderOptions);
        services.AddApi<IWorkflowContextProviderDescriptorsApi>(builderOptions);
        return services;
    }

    /// <summary>
    /// Adds a refit client for the specified API type.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="httpClientBuilderOptions">An options object that can be used to configure the HTTP client builder.</param>
    /// <typeparam name="T">The type representing the API.</typeparam>
    public static void AddApi<T>(this IServiceCollection services, ElsaClientBuilderOptions? httpClientBuilderOptions = default) where T : class
    {
        var builder = services.AddRefitClient<T>(CreateRefitSettings, typeof(T).Name).ConfigureHttpClient(ConfigureElsaApiHttpClient);
        httpClientBuilderOptions?.ConfigureHttpClientBuilder?.Invoke(builder);
        builder.AddTransientHttpErrorPolicy(p => p.WaitAndRetryAsync(3, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt))));
    }

    /// <summary>
    /// Creates an API client for the specified API type.
    /// </summary>
    public static T CreateApi<T>(this IServiceProvider serviceProvider, Uri baseAddress) where T : class
    {
        var httpClientFactory = serviceProvider.GetRequiredService<IHttpClientFactory>();
        var httpClient = httpClientFactory.CreateClient(typeof(T).Name);
        httpClient.BaseAddress = baseAddress;
        return CreateApi<T>(serviceProvider, httpClient);
    }

    /// <summary>
    /// Creates an API client for the specified API type.
    /// </summary>
    public static T CreateApi<T>(this IServiceProvider serviceProvider, HttpClient httpClient) where T : class
    {
        return RestService.For<T>(httpClient, CreateRefitSettings(serviceProvider));
    }

    private static void ConfigureElsaApiHttpClient(IServiceProvider serviceProvider, HttpClient httpClient)
    {
        var options = serviceProvider.GetRequiredService<IOptions<ElsaClientOptions>>().Value;
        httpClient.BaseAddress = options.BaseAddress;
        options.ConfigureHttpClient?.Invoke(serviceProvider, httpClient);
    }

    /// <summary>
    /// Creates a <see cref="RefitSettings"/> instance configured for Elsa. 
    /// </summary>
    private static RefitSettings CreateRefitSettings(IServiceProvider serviceProvider)
    {
        JsonSerializerOptions serializerOptions = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        };

        serializerOptions.Converters.Add(new JsonStringEnumConverter());
        serializerOptions.Converters.Add(new VersionOptionsJsonConverter());

        var settings = new RefitSettings
        {
            ContentSerializer = new SystemTextJsonContentSerializer(serializerOptions),
        };

        return settings;
    }
}