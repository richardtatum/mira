using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Mira.Interfaces;

namespace Mira.Features.SlashCommands;

public static class ServiceCollectionExtensions
{
    public static void AddSlashCommands(this IServiceCollection services)
    {
        services.TryAddScoped<Builder>();
        services.TryAddScoped<Handler>();
        
        // Use reflection to obtain all instances of the ISlashCommand and ISelectable and register them
        var commands = Assembly
            .GetExecutingAssembly()
            .GetTypes()
            .Where(x => x is { IsAbstract: false, IsClass: true } && x.IsAssignableTo(typeof(ISlashCommand)))
            .Select(type => ServiceDescriptor.Scoped(typeof(ISlashCommand), type))
            .ToArray();
        
        var selectables = Assembly
            .GetExecutingAssembly()
            .GetTypes()
            .Where(x => x is { IsAbstract: false, IsClass: true } && x.IsAssignableTo(typeof(ISelectable)))
            .Select(type => ServiceDescriptor.Scoped(typeof(ISelectable), type))
            .ToArray();
        
        services.TryAddEnumerable(commands);
        services.TryAddEnumerable(selectables);
    }
}