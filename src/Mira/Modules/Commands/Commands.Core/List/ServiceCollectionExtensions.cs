using Commands.Core.List.Repositories;
using Microsoft.Extensions.DependencyInjection;

namespace Commands.Core.List;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddListSlashCommand(this IServiceCollection services)
    {
        services.AddTransient<ISlashCommand, SlashCommand>();
        services.AddTransient<QueryRepository>();

        return services;
    }
}