using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Mira.Extensions;
using Mira.Features.SlashCommands;

var host = await Host.CreateDefaultBuilder()
    .ConfigureServices((_, services) =>
    {
        // Add services here.
        services.TryAddScoped<DiscordSocketClient>();
        services.AddSlashCommands();
    })
    .StartAsync();

var client = host.Services.GetRequiredService<DiscordSocketClient>();
var slashCommandBuilder = host.Services.GetRequiredService<Builder>();
var slashCommandHandler = host.Services.GetRequiredService<Handler>();

// var token = Environment.GetEnvironmentVariable("DISCORD_TOKEN");
var token = "MTI1MDQ0NzIyOTU3MjA4NzgwOA.Ga3hQi.D9KFNvJfaXKU8hwfmJbfLSS-czBJCUTgtCit_s";
await client.LoginAsync(TokenType.Bot, token);
await client.StartAsync();

client.Log += message =>
{
    Console.WriteLine(message);
    return Task.CompletedTask;
};
client.Ready += slashCommandBuilder.OnReadyAsync;
client.SlashCommandExecuted += slashCommandHandler.HandleCommandExecutedAsync;
client.InteractionCreated += slashCommandHandler.HandleInteractionCreatedAsync;

// Look into the following:
// client.SelectMenuExecuted +=

// Keep connection open
await Task.Delay(Timeout.Infinite);