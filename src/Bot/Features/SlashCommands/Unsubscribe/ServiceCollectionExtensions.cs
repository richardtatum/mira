using Microsoft.Extensions.DependencyInjection;
using Mira.Features.SlashCommands.Unsubscribe.Repositories;

namespace Mira.Features.SlashCommands.Unsubscribe;

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