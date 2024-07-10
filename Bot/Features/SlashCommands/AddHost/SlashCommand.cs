using Discord;
using Discord.WebSocket;
using Mira.Extensions;
using Mira.Features.SlashCommands.AddHost.Repositories;
using Mira.Interfaces;

namespace Mira.Features.SlashCommands.AddHost;

public class SlashCommand(BroadcastBoxClient client, CommandRepository commandRepository) : ISlashCommand
{
    public string Name => "add-host";
    private const string HostOptionName = "host";
    private const string PollIntervalOptionName = "poll-interval";

    public ApplicationCommandProperties BuildCommand() =>
        new SlashCommandBuilder()
            .WithName(Name)
            .WithDescription("Add a new BroadcastBox host.")
            .AddOptions(
                new SlashCommandOptionBuilder()
                    .WithName(HostOptionName)
                    .WithDescription("The URL of the host you wish to add.")
                    .WithRequired(true)
                    .WithType(ApplicationCommandOptionType.String),
                new SlashCommandOptionBuilder()
                    .WithName(PollIntervalOptionName)
                    .WithDescription("The frequency to poll the status endpoint for stream updates.")
                    .WithRequired(true)
                    .AddChoice("30 seconds", 30)
                    .AddChoice("60 seconds", 60)
                    .AddChoice("90 seconds", 90)
                    .AddChoice("120 seconds", 120)
                    .WithType(ApplicationCommandOptionType.Number)
            ).Build();

    public async Task RespondAsync(SocketSlashCommand command)
    {
        var guildId = command.GuildId;
        if (guildId is null)
        {
            return;
        }

        var host = command.Data.Options.GetValue<string>(HostOptionName);
        if (string.IsNullOrWhiteSpace(host))
        {
            // Return failure message
            return;
        }

        // Discord.NET uses doubles to handle numbers, but as we are providing all possible outcomes we can just
        // cast this without too much of a worry
        var interval = (int)command.Data.Options.GetValue<double>(PollIntervalOptionName);
        if (interval == default)
        {
            // Return failure message
            return;
        }
        
        await command.DeferAsync();

        var isValidUrl = IsValidUrl(host, out var validHostUrl);
        if (!isValidUrl)
        {
            await command.FollowupAsync($"Provided URL `{host}` is invalid. Please check and try again.");
            return;
        }

        var validHost = await client.IsVerifiedBroadcastBoxHostAsync(validHostUrl!);
        if (!validHost)
        {
            await command.FollowupAsync(
            $"Failed to add host. Received a non-success response code from `{validHostUrl}/api/status`. Please check and try again.");
            return;
        }

        await commandRepository.AddHostAsync(validHostUrl!, interval, guildId.Value);
        await command.FollowupAsync($"Success! Added host `{validHostUrl} with a polling interval of {interval} seconds.`");
    }
    
    private static bool IsValidUrl(string url, out string? validUrl)
    {
        validUrl = null;
        if (!url.StartsWith("http://") && !url.StartsWith("https://"))
        {
            url = $"https://{url}";
        }
        
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
        {
            return false;
        }

        // Redundant with the above adding the scheme?
        if (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps)
        {
            return false;
        }

        // Return false if there is no TLD
        var hostParts = uri.Host.Split('.');
        if (hostParts.Length < 2 || string.IsNullOrWhiteSpace(hostParts.Last()))
        {
            return false;
        }

        validUrl = url;
        return true;
    }
}