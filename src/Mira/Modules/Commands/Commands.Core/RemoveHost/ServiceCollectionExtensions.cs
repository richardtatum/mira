using Commands.Core.RemoveHost.Repositories;
using Microsoft.Extensions.DependencyInjection;

namespace Commands.Core.RemoveHost;

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