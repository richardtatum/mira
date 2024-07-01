using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Mira.Data;
using Mira.Extensions;
using Mira.Features.SlashCommands;
using Mira.Features.SlashCommands.Notify.Repositories;

var host = await Host.CreateDefaultBuilder()
    .ConfigureServices((host, services) =>
    {
        var conn = host.Configuration.GetConnectionString("Primary");
        // Add services here.
        services.TryAddScoped<DiscordSocketClient>();
        services.AddSingleton<DbContext>();
        services.AddSlashCommands();
        
        // HOW TO DO THIS DYNAMICALLY?
        services.TryAddScoped<QueryRepository>();
        services.TryAddScoped<CommandRepository>();
    })
    .StartAsync();

{
    // Ensure the DB is initialised
    using var scope = host.Services.CreateScope();
    var context = scope.ServiceProvider.GetRequiredService<DbContext>();
    await context.InitAsync();
}

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

// Keep connection open
await Task.Delay(Timeout.Infinite);