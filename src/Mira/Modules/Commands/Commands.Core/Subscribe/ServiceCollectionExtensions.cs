using Commands.Core.Subscribe.Repositories;
using Microsoft.Extensions.DependencyInjection;

namespace Commands.Core.Subscribe;

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