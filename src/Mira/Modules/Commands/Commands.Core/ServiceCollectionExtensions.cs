using Commands.Core.AddHost;
using Commands.Core.List;
using Commands.Core.RemoveHost;
using Commands.Core.Subscribe;
using Commands.Core.Unsubscribe;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Commands.Core;

public static class ServiceCollectionExtensions
{
    public static void AddSlashCommands(this IServiceCollection services)
    {
        services
            .AddAddHostSlashCommand()
            .AddRemoveHostCommand()
            .AddSubscribeSlashCommand()
            .AddUnsubscribeSlashCommand()
            .AddListSlashCommand();

        services.TryAddTransient<Builder>();
        services.TryAddTransient<Handler>();
    }
}