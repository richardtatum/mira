using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Mira.Features.SlashCommands;
using Mira.Interfaces;

namespace Mira.Extensions;

public static class ServiceCollectionExtensions
{
    public static void AddSlashCommands(this IServiceCollection services)
    {
        services.TryAddScoped<Builder>();
        services.TryAddScoped<Handler>();
        
        // Use reflection to obtain all instances of the ISlashCommand and register them
        var commands = Assembly
            .GetExecutingAssembly()
            .GetTypes()
            .Where(x => !x.IsAbstract && x.IsClass && x.IsAssignableTo(typeof(ISlashCommand)));

        foreach (var command in commands)
        {
            services.TryAdd(new ServiceDescriptor(typeof(ISlashCommand), command, ServiceLifetime.Scoped));
        }
        
        var selectables = Assembly
            .GetExecutingAssembly()
            .GetTypes()
            .Where(x => !x.IsAbstract && x.IsClass && x.IsAssignableTo(typeof(ISelectable)));
        
        foreach (var selectable in selectables)
        {
            services.TryAdd(new ServiceDescriptor(typeof(ISelectable), selectable, ServiceLifetime.Scoped));
        }
    }
}