using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Mira;
using Mira.Data;
using Mira.Features.Messaging;
using Mira.Features.Polling;
using Mira.Features.SlashCommands;

var host = Host.CreateDefaultBuilder()
    .ConfigureServices((_, services) =>
    {
        services.AddScoped<LoggingService>();
        services.AddSingleton(new DiscordSocketClient(new DiscordSocketConfig
        {
            GatewayIntents = GatewayIntents.Guilds
        }));
        services.AddSingleton<DbContext>();

        services.AddHttpClient();
        services.AddSlashCommands();
        services.AddPollingService();
        services.AddMessagingService();
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

var token = configuration.GetValue<string>("Discord:Token");
await client.LoginAsync(TokenType.Bot, token);
await client.StartAsync();

client.Log += loggingService.LogAsync;
client.Ready += commandBuilder.OnReadyAsync;
client.Ready += () => host.StartAsync();
client.SlashCommandExecuted += commandHandler.HandleCommandExecutedAsync;
client.SelectMenuExecuted += commandHandler.HandleSelectMenuExecutedAsync;

try
{
    await Task.Delay(Timeout.Infinite, cancellationTokenSource.Token);
}
catch (TaskCanceledException)
{
    Console.WriteLine("[MIRA] Cancellation token fired, application shutting down.");
}
