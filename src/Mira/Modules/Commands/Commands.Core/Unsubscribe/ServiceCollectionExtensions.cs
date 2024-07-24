using Commands.Core.Unsubscribe.Repositories;
using Microsoft.Extensions.DependencyInjection;

namespace Commands.Core.Unsubscribe;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddUnsubscribeSlashCommand(this IServiceCollection services)
    {
        services.AddTransient<CommandRepository>();
        services.AddTransient<QueryRepository>();
        services.AddTransient<ISlashCommand, SlashCommand>();
        services.AddTransient<ISelectable, SlashCommand>();

        return services;
    }
}