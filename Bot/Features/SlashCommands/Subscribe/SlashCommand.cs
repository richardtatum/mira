using System.Net;
using Discord;
using Discord.WebSocket;
using Mira.Features.SlashCommands.Subscribe.Models;
using Mira.Features.SlashCommands.Subscribe.Repositories;
using Mira.Interfaces;

namespace Mira.Features.SlashCommands.Subscribe;

public class SlashCommand(QueryRepository queryRepository, CommandRepository commandRepository)
    : ISlashCommand, ISelectable
{
    public string Name => "subscribe";
    private const string CustomId = "subscribe";
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
        var streamKey = command.Data.Options.First(x => x.Name == StreamKeyOptionName).Value.ToString();
        if (string.IsNullOrWhiteSpace(streamKey))
        {
            await command.RespondAsync($"`{streamKey}` is an invalid stream key. Please double check and try again.");
            return;
        }
        
        // Sanitise the streamKey
        streamKey = WebUtility.UrlEncode(streamKey);

        var guildId = command.GuildId;
        if (guildId is null)
        {
            // Failure message
            return;
        }

        await command.DeferAsync(ephemeral: true);

        var hosts = await queryRepository.GetHostsAsync(guildId.Value);
        if (hosts.Length == 0)
        {
            await command.FollowupAsync(
                "No hosts found! Please use the `/add-host` command to add a valid BroadcastBox host before subscribing.");
            return;
        }

        var hostOptions = hosts
            .Select(host =>
                new SelectMenuOptionBuilder(host.Url, host.Id.ToString(),
                    $"Monitored URL: {host.Url}/{streamKey}"))
            .ToList();

        var component = new ComponentBuilder()
            .WithSelectMenu($"{CustomId}-{streamKey}", hostOptions, $"Where will `{streamKey}` stream?")
            .Build();

        await command.FollowupAsync("Select a host:", components: component);
    }

    public bool HandlesComponent(SocketMessageComponent component) =>
        component.Data.CustomId.Split("-").FirstOrDefault() == CustomId;

    public async Task RespondAsync(SocketMessageComponent component)
    {
        
        var streamKey = component.Data.CustomId.Split("-").LastOrDefault();
        if (string.IsNullOrWhiteSpace(streamKey))
        {
            // Log error
            return;
        }
        
        var channelId = component.ChannelId;
        if (channelId is null)
        {
            // Failure
            return;
        }

        await component.DeferAsync(ephemeral: true);

        if (!int.TryParse(component.Data.Values.FirstOrDefault(), out var hostId))
        {
            return;
        }

        var host = await queryRepository.GetHostAsync(hostId);
        if (host is null)
        {
            // Error
            return;
        }

        var keyExists = await queryRepository.HostStreamKeyExistsAsync(hostId, streamKey);
        if (keyExists)
        {
            await component.FollowupAsync(
                "This streamkey is already registered for this host. Please use `/list` to see all currently registered keys and hosts.");
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
            await component.FollowupAsync("Failed to add new subscription. Please try again.");
            return;
        }
        
        var url = $"{host.Url}/{streamKey}";

        await component.ModifyOriginalResponseAsync(message =>
        {
            message.Content = $"Subscription requested for `{url}`";
            message.Components = new ComponentBuilder().Build();
        });

        await component.InteractionChannel.SendMessageAsync(
            $"New subscription added for `{url}`! Notifications will be sent to this channel when the stream goes live.");
    }
}