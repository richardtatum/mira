using Commands.Core.Playing.Repositories;
using Microsoft.Extensions.DependencyInjection;

namespace Commands.Core.Playing;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddPlayingSlashCommand(this IServiceCollection services)
    {
        services.AddTransient<QueryRepository>();
        services.AddTransient<CommandRepository>();
        services.AddTransient<ISlashCommand, SlashCommand>();
        services.AddTransient<ISelectable, SlashCommand>();

        return services;
    }
}