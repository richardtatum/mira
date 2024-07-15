using Microsoft.Extensions.DependencyInjection;
using Mira.Features.SlashCommands.Unsubscribe.Repositories;

namespace Mira.Features.SlashCommands.Unsubscribe;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddUnsubscribeSlashCommand(this IServiceCollection services)
    {
        services.AddScoped<CommandRepository>();
        services.AddScoped<QueryRepository>();
        services.AddScoped<ISlashCommand, SlashCommand>();
        services.AddScoped<ISelectable, SlashCommand>();

        return services;
    }
}