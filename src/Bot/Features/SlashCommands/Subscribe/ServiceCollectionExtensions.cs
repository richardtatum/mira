using Microsoft.Extensions.DependencyInjection;
using Mira.Features.SlashCommands.Subscribe.Repositories;

namespace Mira.Features.SlashCommands.Subscribe;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddSubscribeSlashCommand(this IServiceCollection services)
    {
        services.AddTransient<CommandRepository>();
        services.AddTransient<QueryRepository>();
        services.AddTransient<ISlashCommand, SlashCommand>();
        services.AddTransient<ISelectable, SlashCommand>();

        return services;
    }
}