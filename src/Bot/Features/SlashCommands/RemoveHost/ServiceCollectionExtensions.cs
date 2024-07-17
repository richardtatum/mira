using Microsoft.Extensions.DependencyInjection;
using Mira.Features.SlashCommands.RemoveHost.Repositories;

namespace Mira.Features.SlashCommands.RemoveHost;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddRemoveHostCommand(this IServiceCollection services)
    {
        services.AddTransient<ISlashCommand, SlashCommand>();
        services.AddTransient<ISelectable, SlashCommand>();
        services.AddTransient<QueryRepository>();
        services.AddTransient<CommandRepository>();

        return services;
    }
}