using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;

namespace Cleanup.Core;

public static class ServiceCollectionExtensions
{
    public static void AddCleanupServices(this IServiceCollection services)
    {
        services.AddTransient<CommandRepository>();
        services.AddTransient<ICleanupService<SocketChannel>, ChannelCleanupService>();
        services.AddTransient<ICleanupService<SocketGuild>, GuildCleanupService>();
    }
}