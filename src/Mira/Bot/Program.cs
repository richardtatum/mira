using ChangeTracking.Core.Extensions;
using Cleanup.Core;
using Commands.Core;
using Discord;
using Discord.WebSocket;
using Messaging.Core;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Mira;
using Polling.Core;
using Shared.Core;

var config = new DiscordSocketConfig
{
    GatewayIntents = GatewayIntents.Guilds
};

var host = Host.CreateDefaultBuilder()
    .ConfigureServices((_, services) =>
    {
        services.AddSingleton<LoggingService>();
        services.AddSingleton<DbContext>();
        services.AddSingleton(new DiscordSocketClient(config));
        services.AddHttpClient();
        services.AddSlashCommands();
        services.AddPollingService();
        services.AddChangeTracking();
        services.AddMessagingService();
        services.AddCleanupServices();
    })
    .Build();

{
    // Ensure the DB is initialised
    using var scope = host.Services.CreateScope();
    var context = scope.ServiceProvider.GetRequiredService<DbContext>();
    await context.InitAsync();
}

var cancellationTokenSource = new CancellationTokenSource();
host.Services
    .GetRequiredService<IHostApplicationLifetime>()
    .ApplicationStopping
    .Register(() => cancellationTokenSource.Cancel());

var configuration = host.Services.GetRequiredService<IConfiguration>();
var client = host.Services.GetRequiredService<DiscordSocketClient>();
var commandBuilder = host.Services.GetRequiredService<Builder>();
var commandHandler = host.Services.GetRequiredService<Handler>();
var loggingService = host.Services.GetRequiredService<LoggingService>();
var guildCleanup = host.Services.GetRequiredService<ICleanupService<SocketGuild>>();
var channelCleanup = host.Services.GetRequiredService<ICleanupService<SocketChannel>>();


var token = configuration.GetValue<string>("Discord:Token");
await client.LoginAsync(TokenType.Bot, token);
await client.StartAsync();

client.Log += loggingService.LogAsync;
client.Ready += commandBuilder.OnReadyAsync;
client.Ready += () => host.StartAsync();
client.SlashCommandExecuted += commandHandler.HandleCommandExecutedAsync;
client.SelectMenuExecuted += commandHandler.HandleSelectMenuExecutedAsync;

client.LeftGuild += guildCleanup.ExecuteAsync;
client.ChannelDestroyed += channelCleanup.ExecuteAsync;

try
{
    await Task.Delay(Timeout.Infinite, cancellationTokenSource.Token);
}
catch (TaskCanceledException)
{
    Console.WriteLine("[MIRA] Cancellation token fired, application shutting down.");
}
