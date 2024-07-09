using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Mira.Data;
using Mira.Extensions;
using Mira.Features.SlashCommands;
using Mira.Features.SlashCommands.Notify.Repositories;
using Mira.Features.StreamChecker;

var host = await Host.CreateDefaultBuilder()
    .ConfigureServices((host, services) =>
    {
        // Add services here.
        services.TryAddScoped<DiscordSocketClient>();
        services.AddSingleton<DbContext>();
        services.AddSlashCommands();

        services.AddHttpClient();

        // THESE NEED TO BE EXTENSIONS OR LOADED DYNAMICALLY
        services.AddHostedService<PeriodicStreamChecker>(); // TODO: Set this to ignore failures
        services.AddScoped<StreamNotificationService>();
        services.TryAddScoped<BroadcastBoxClient>();
        services.TryAddScoped<QueryRepository>();
        services.TryAddScoped<CommandRepository>();
        services.AddScoped<Mira.Features.StreamChecker.Repositories.QueryRepository>();
        services.AddScoped<Mira.Features.StreamChecker.Repositories.CommandRepository>();
    })
    .StartAsync();

{
    // Ensure the DB is initialised
    using var scope = host.Services.CreateScope();
    var context = scope.ServiceProvider.GetRequiredService<DbContext>();
    await context.InitAsync();
}

var client = host.Services.GetRequiredService<DiscordSocketClient>();
var slashCommandBuilder = host.Services.GetRequiredService<Mira.Features.SlashCommands.Builder>();
var slashCommandHandler = host.Services.GetRequiredService<Handler>();

// var token = Environment.GetEnvironmentVariable("DISCORD_TOKEN");
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
client.SlashCommandExecuted += slashCommandHandler.HandleCommandExecutedAsync;
client.SelectMenuExecuted += slashCommandHandler.HandleSelectMenuExecutedAsync;

// Keep connection open
await Task.Delay(Timeout.Infinite);