using Microsoft.Extensions.DependencyInjection;
using Mira.Features.SlashCommands.List.Repositories;

namespace Mira.Features.SlashCommands.List;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddListSlashCommand(this IServiceCollection services)
    {
        services.AddTransient<ISlashCommand, SlashCommand>();
        services.AddTransient<QueryRepository>();

        return services;
    }
}