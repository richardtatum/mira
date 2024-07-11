using Microsoft.Extensions.DependencyInjection;
using Mira.Features.SlashCommands.AddHost.Repositories;
using Mira.Interfaces;

namespace Mira.Features.SlashCommands.AddHost;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddAddHostSlashCommand(this IServiceCollection services)
    {
        services.AddScoped<CommandRepository>();
        services.AddScoped<QueryRepository>();
        services.AddScoped<ISlashCommand, SlashCommand>();

        return services;
    }
}