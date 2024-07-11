using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Mira.Features.SlashCommands.AddHost;
using Mira.Features.SlashCommands.Subscribe;
using Mira.Features.SlashCommands.Unsubscribe;
using Mira.Interfaces;

namespace Mira.Features.SlashCommands;

public static class ServiceCollectionExtensions
{
    public static void AddSlashCommands(this IServiceCollection services)
    {
        services.AddAddHostSlashCommand();
        services.AddSubscribeSlashCommand();
        services.AddUnsubscribeSlashCommand();
        
        services.TryAddScoped<Builder>();
        services.TryAddScoped<Handler>();
    }
}