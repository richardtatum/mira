using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Mira.Features.SlashCommands.AddHost;
using Mira.Features.SlashCommands.List;
using Mira.Features.SlashCommands.RemoveHost;
using Mira.Features.SlashCommands.Subscribe;
using Mira.Features.SlashCommands.Unsubscribe;

namespace Mira.Features.SlashCommands;

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