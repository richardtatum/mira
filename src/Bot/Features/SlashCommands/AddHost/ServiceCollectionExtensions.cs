using Microsoft.Extensions.DependencyInjection;
using Mira.Features.SlashCommands.AddHost.Options;
using Mira.Features.SlashCommands.AddHost.Repositories;

namespace Mira.Features.SlashCommands.AddHost;

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