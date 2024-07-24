using Commands.Core.AddHost.Models;
using Commands.Core.AddHost.Repositories;
using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.Logging;

namespace Commands.Core.AddHost;

public class SlashCommand(BroadcastBoxClient client, CommandRepository commandRepository, QueryRepository queryRepository, ILogger<SlashCommand> logger) : ISlashCommand
{
    public string Name => "add-host";
    private const string HostOptionName = "url";
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
            logger.LogCritical("[SLASH-COMMAND][{Name}] Failed to retrieve guildId from SocketSlashCommand. Received: {GuildId}", Name, guildId);
            return;
        }

        var hostUrl = command.Data.Options.GetValue<string>(HostOptionName);
        if (string.IsNullOrWhiteSpace(hostUrl))
        {
            logger.LogCritical("[SLASH-COMMAND][{Name}] Failed to retrieve the value from the host option. Received: {HostUrl}", Name, hostUrl);
            return;
        }

        // Discord.NET uses doubles to handle numbers, but as we are providing all possible outcomes we can just
        // cast this without too much of a worry
        var interval = (int)command.Data.Options.GetValue<double>(PollIntervalOptionName);
        if (interval == default)
        {
            // TODO: Log the originally retrieve value here, before cast
            logger.LogCritical("[SLASH-COMMAND][{Name}] Failed to retrieve the value from the interval option. Received: {interval}", Name, interval);
            return;
        }
        
        await command.DeferAsync();

        var isValidUrl = IsValidUrl(hostUrl, out var validHostUrl);
        if (!isValidUrl)
        {
            logger.LogInformation("[SLASH-COMMAND][{Name}] User provided invalid url: {Url}", Name, hostUrl);
            var invalidUrlEmbed = GenerateFailedEmbed($"URL `{hostUrl}` is invalid. Please check and try again.");
            await command.FollowupAsync(embed: invalidUrlEmbed);
            return;
        }

        var hostExists = await queryRepository.HostExistsAsync(validHostUrl!, guildId.Value);
        if (hostExists)
        {
            logger.LogInformation("[SLASH-COMMAND][{Name}] User provided url for host that already exists: {Url}", Name, hostUrl);
            var hostExistsEmbed = GenerateFailedEmbed($"Host `{validHostUrl}` already exists.");
            await command.FollowupAsync(embed: hostExistsEmbed);
            return;
        }

        var validHost = await client.IsVerifiedBroadcastBoxHostAsync(validHostUrl!);
        if (!validHost)
        {
            logger.LogInformation("[SLASH-COMMAND][{Name}] User provided url for host that we were unable to contact: {Url}", Name, hostUrl);
            var invalidHostEmbed =
                GenerateFailedEmbed(
                    $"Received a non-success response code from `{validHostUrl}/api/status`. Please check and try again.");
            await command.FollowupAsync(embed: invalidHostEmbed);
            return;
        }

        var host = new Host(validHostUrl!, interval, guildId.Value, command.User.Id);
        await commandRepository.AddHostAsync(host);
        var successEmbed =
            GenerateSuccessEmbed(
                $"Added new host `{validHostUrl}` with a polling interval of {interval} seconds.");
        await command.FollowupAsync(embed: successEmbed);
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

        validUrl = uri.GetLeftPart(UriPartial.Authority);
        return true;
    }

    private static Embed GenerateFailedEmbed(string description) =>
        new EmbedBuilder()
            .WithTitle("Failed")
            .WithDescription(description)
            .WithColor(Color.Red)
            .WithCurrentTimestamp()
            .Build();
    
    private static Embed GenerateSuccessEmbed(string description) =>
        new EmbedBuilder()
            .WithTitle("Success")
            .WithDescription(description)
            .WithColor(Color.Green)
            .WithCurrentTimestamp()
            .Build();
}