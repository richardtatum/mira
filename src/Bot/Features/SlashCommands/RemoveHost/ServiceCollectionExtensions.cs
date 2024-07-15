using Microsoft.Extensions.DependencyInjection;
using Mira.Features.SlashCommands.RemoveHost.Repositories;

namespace Mira.Features.SlashCommands.RemoveHost;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddRemoveHostCommand(this IServiceCollection services)
    {
        services.AddScoped<ISlashCommand, SlashCommand>();
        services.AddScoped<ISelectable, SlashCommand>();
        services.AddScoped<QueryRepository>();
        services.AddScoped<CommandRepository>();

        return services;
    }
}