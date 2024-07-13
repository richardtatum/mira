using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Mira.Data;
using Mira.Features.Messaging;
using Mira.Features.Polling;
using Mira.Features.SlashCommands;

var host = Host.CreateDefaultBuilder()
    .ConfigureServices((_, services) =>
    {
        // Add services here.
        services.TryAddScoped<DiscordSocketClient>();
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

var client = host.Services.GetRequiredService<DiscordSocketClient>();
var slashCommandBuilder = host.Services.GetRequiredService<Builder>();
var slashCommandHandler = host.Services.GetRequiredService<Handler>();

// TODO: Dispose of this development token
var token = "MTI1MDQ0NzIyOTU3MjA4NzgwOA.Ga3hQi.D9KFNvJfaXKU8hwfmJbfLSS-czBJCUTgtCit_s";
await client.LoginAsync(TokenType.Bot, token);
await client.StartAsync();

//TODO: Update to logging service
client.Log += message =>
{
    Console.WriteLine(message);
    return Task.CompletedTask;
};
client.Ready += slashCommandBuilder.OnReadyAsync;
client.Ready += () => host.StartAsync();
client.SlashCommandExecuted += slashCommandHandler.HandleCommandExecutedAsync;
client.SelectMenuExecuted += slashCommandHandler.HandleSelectMenuExecutedAsync;

try
{
    await Task.Delay(Timeout.Infinite, cancellationTokenSource.Token);
}
catch (TaskCanceledException)
{
    Console.WriteLine("[MIRA] Cancellation token fired, application shutting down.");
}
