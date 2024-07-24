using Commands.Core.AddHost.Options;
using Commands.Core.AddHost.Repositories;
using Microsoft.Extensions.DependencyInjection;

namespace Commands.Core.AddHost;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddAddHostSlashCommand(this IServiceCollection services)
    {
        services.AddTransient<CommandRepository>();
        services.AddTransient<QueryRepository>();
        services.AddTransient<ISlashCommand, SlashCommand>();
        services.AddTransient<BroadcastBoxClient>();
        
        services
            .AddOptions<PollingOptions>()
            .BindConfiguration(nameof(PollingOptions));

        return services;
    }
}