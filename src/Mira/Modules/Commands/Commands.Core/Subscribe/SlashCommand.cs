using System.Net;
using Commands.Core.Subscribe.Models;
using Commands.Core.Subscribe.Repositories;
using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.Logging;

namespace Commands.Core.Subscribe;

public class SlashCommand(QueryRepository queryRepository, CommandRepository commandRepository, ILogger<SlashCommand> logger)
    : ISlashCommand, ISelectable
{
    public string Name => "subscribe";
    private const string CustomIdPrefix = "subscribe";
    private const char Divider = ':';
    private const string StreamKeyOptionName = "streamkey";

    public ApplicationCommandProperties BuildCommand() =>
        new SlashCommandBuilder()
            .WithName(Name)
            .WithDescription("Be notified when a user begins streaming.")
            .AddOptions(
                new SlashCommandOptionBuilder()
                    .WithName(StreamKeyOptionName)
                    .WithDescription("The key the stream uses. This is often the streamers username.")
                    .WithRequired(true)
                    .WithType(ApplicationCommandOptionType.String)
            )
            .Build();

    public async Task RespondAsync(SocketSlashCommand command)
    {
        var guildId = command.GuildId;
        if (guildId is null)
        {
            logger.LogCritical("[SLASH-COMMAND][{Name}] Failed to retrieve guildId from SocketSlashCommand. Received: {GuildId}", Name, guildId);
            return;
        }
        
        var streamKey = command.Data.Options.First(x => x.Name == StreamKeyOptionName).Value.ToString();
        if (string.IsNullOrWhiteSpace(streamKey))
        {
            var invalidStreamKeyEmbed =
                GenerateFailedEmbed($"`{streamKey}` is an invalid stream key. Please double check and try again.");
            await command.RespondAsync(embed: invalidStreamKeyEmbed, ephemeral: true);
            return;
        }
        
        // Sanitise the streamKey
        streamKey = WebUtility.UrlEncode(streamKey);

        await command.DeferAsync(ephemeral: true);

        var hosts = await queryRepository.GetHostsAsync(guildId.Value);
        if (hosts.Length == 0)
        {
            var noHostsEmbed =
                GenerateFailedEmbed(
                    "No hosts found! Please use the `/add-host` command to add a valid BroadcastBox host before subscribing.");
            await command.FollowupAsync(embed: noHostsEmbed);
            return;
        }

        var hostOptions = hosts
            .Select(host =>
                new SelectMenuOptionBuilder(host.Url, host.Id.ToString(),
                    $"Stream URL: {host.Url}/{streamKey}"))
            .ToList();

        var customId = $"{CustomIdPrefix}{Divider}{streamKey}";
        var component = new ComponentBuilder()
            .WithSelectMenu(customId, hostOptions, $"Where will `{streamKey}` stream?")
            .Build();

        await command.FollowupAsync("Select a host:", components: component);
    }

    // We could dispose of this concept and add the stream key as part of the selected value, setting the customId to just 'subscribe' like the other commands
    public bool HandlesComponent(SocketMessageComponent component) =>
        component.Data.CustomId.Split(Divider).FirstOrDefault() == CustomIdPrefix;

    public async Task RespondAsync(SocketMessageComponent component)
    {
        var channelId = component.ChannelId;
        if (channelId is null)
        {
            logger.LogCritical("[SLASH-COMMAND][{Name}] Failed to retrieve channelId from SocketMessageComponent. Received: {ChannelId}", Name, channelId);
            return;
        }
        
        var streamKey = component.Data.CustomId.Split(Divider).LastOrDefault();
        if (string.IsNullOrWhiteSpace(streamKey))
        {
            logger.LogCritical("[SLASH-COMMAND][{Name}] Failed to retrieve value from the streamKey customId. Received: {Key}", Name, streamKey);
            return;
        }

        await component.DeferAsync(ephemeral: true);

        if (!int.TryParse(component.Data.Values.FirstOrDefault(), out var hostId))
        {
            logger.LogCritical("[SLASH-COMMAND][{Name}] Failed to retrieve value from the host option. Received: {Value}", Name, component.Data.Values.FirstOrDefault());
            return;
        }

        var host = await queryRepository.GetHostAsync(hostId);
        if (host is null)
        {
            logger.LogCritical("[SLASH-COMMAND][{Name}] Failed to retrieve the host from the provided Id: {Id}", Name, hostId);
            return;
        }

        var keyExists = await queryRepository.HostStreamKeyExistsAsync(hostId, streamKey);
        if (keyExists)
        {
            logger.LogInformation("[SLASH-COMMAND][{Name}] User provided a stream key that already exists for this host. Host: {HostUrl}, Key: {Key}", Name, host.Url, streamKey);
            var keyExistsEmbed =
                GenerateFailedEmbed(
                    "This streamkey is already registered for this host. Please use `/list` to see all currently registered keys and hosts.");
            await component.FollowupAsync(embed: keyExistsEmbed);
            return;
        }
        
        var subscription = new Subscription
        {
            StreamKey = streamKey,
            HostId = hostId,
            ChannelId = channelId.Value,
            CreatedBy = component.User.Id
        };

        var success = await commandRepository.AddSubscription(subscription);
        if (!success)
        {
            var subscriptionFailedEmbed = GenerateFailedEmbed("Unable to add new subscription. Please try again.");
            await component.FollowupAsync(embed: subscriptionFailedEmbed);
            return;
        }
        
        var url = $"{host.Url}/{streamKey}";

        await component.ModifyOriginalResponseAsync(message =>
        {
            message.Content = $"Subscription requested for `{url}`";
            message.Components = new ComponentBuilder().Build();
        });

        var successEmbed =
            GenerateSuccessEmbed(
                $"New subscription added for `{url}`! \n Notifications will be sent to this channel when the stream goes live.");
        await component.InteractionChannel.SendMessageAsync(embed: successEmbed);
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