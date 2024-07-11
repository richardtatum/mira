using Microsoft.Extensions.DependencyInjection;
using Mira.Features.SlashCommands.Subscribe.Repositories;
using Mira.Interfaces;

namespace Mira.Features.SlashCommands.Subscribe;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddSubscribeSlashCommand(this IServiceCollection services)
    {
        services.AddScoped<CommandRepository>();
        services.AddScoped<QueryRepository>();
        services.AddScoped<ISlashCommand, SlashCommand>();
        services.AddScoped<ISelectable, SlashCommand>();

        return services;
    }
}