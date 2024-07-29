using System.Net;
using Commands.Core.Playing.Repositories;
using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.Logging;
using Shared.Core;

namespace Commands.Core.Playing;

public class SlashCommand(QueryRepository queryRepository, CommandRepository commandRepository, ILogger<SlashCommand> logger)
    : ISlashCommand, ISelectable
{
    public string Name => "playing";
    private const string PlayingOptionName = "playing";
    private const char Divider = ':';

    public ApplicationCommandProperties BuildCommand() => 
        new SlashCommandBuilder()
            .WithName(Name)
            .WithDescription("Set what is currently being played by a live stream.")
            .AddOptions(
                new SlashCommandOptionBuilder()
                    .WithName(PlayingOptionName)
                    .WithDescription("What is being played right now?")
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
        
        var playing = command.Data.Options.First(x => x.Name == PlayingOptionName).Value.ToString();
        if (string.IsNullOrWhiteSpace(playing))
        {
            var invalidStreamKeyEmbed =
                EmbedResponse.Failed($"`{playing}` is an invalid string. Please double check and try again.");
            await command.RespondAsync(embed: invalidStreamKeyEmbed, ephemeral: true);
            return;
        }

        // This prevents any issues with values in the string that match the divider
        var encodedPlaying = WebUtility.UrlEncode(playing);
        await command.DeferAsync(ephemeral: true);

        var liveStreams = await queryRepository.GetLiveStreamsAsync(guildId.Value);
        if (liveStreams.Length == 0)
        {
            var noLiveStreamsEmbed = EmbedResponse.Failed("There are currently no live streams.");
            await command.FollowupAsync(embed: noLiveStreamsEmbed);
        }

        var streamOptions = liveStreams
            .Select(stream => new SelectMenuOptionBuilder(stream.Url, $"{stream.Id}"))
            .ToList();
        
        var customId = $"{Name}{Divider}{encodedPlaying}";
        var component = new ComponentBuilder()
            .WithSelectMenu(customId, streamOptions, $"Which stream is playing `{playing}`?")
            .Build();

        await command.FollowupAsync("Select a stream:", components: component);
    }

    public bool HandlesComponent(SocketMessageComponent component)
        => component.Data.CustomId.Split(Divider).FirstOrDefault() == Name;
    
    public async Task RespondAsync(SocketMessageComponent component)
    {
        var channelId = component.ChannelId;
        if (channelId is null)
        {
            logger.LogCritical("[SLASH-COMMAND][{Name}] Failed to retrieve channelId from SocketMessageComponent. Received: {ChannelId}", Name, channelId);
            return;
        }
        
        var encodedPlaying = component.Data.CustomId.Split(Divider).LastOrDefault();
        if (string.IsNullOrWhiteSpace(encodedPlaying))
        {
            logger.LogCritical("[SLASH-COMMAND][{Name}] Failed to retrieve the encoded playing value from the customId. Received: {Playing}", Name, encodedPlaying);
            return;
        }
        
        var playing = WebUtility.UrlDecode(encodedPlaying);
        await component.DeferAsync(ephemeral: true);
        
        if (!int.TryParse(component.Data.Values.FirstOrDefault(), out var streamId))
        {
            logger.LogCritical("[SLASH-COMMAND][{Name}] Failed to retrieve value from the stream option. Received: {Value}", Name, component.Data.Values.FirstOrDefault());
            return;
        }

        var stream = await queryRepository.GetStreamAsync(streamId);
        if (stream is null)
        {
            logger.LogCritical("[SLASH-COMMAND][{Name}] Failed to retrieve the stream from the provided Id: {Id}", Name, streamId);
            var failedToFindStreamEmbed =
                EmbedResponse.Failed("Failed to find the requested stream. Please try again.");
            await component.FollowupAsync(embed: failedToFindStreamEmbed, ephemeral: true);
            return;
        }

        // TODO: Consider the fact that this allows any user on any server where the subscription is active to override all servers `playing` status
        var updated = await commandRepository.SetPlayingAsync(stream.Key, playing);
        if (!updated)
        {
            logger.LogCritical("[SLASH-COMMAND][{Name}] Failed to update the stream with the new playing value. StreamId: {Id}, Playing value: {Playing}", Name, streamId, playing);
            var failedToUpdateStreamEmbed =
                EmbedResponse.Failed("Failed to update the requested stream with the new playing value. Please try again.");
            await component.FollowupAsync(embed: failedToUpdateStreamEmbed, ephemeral: true);
            return;
        }
        
        // Clear the original component
        await component.ModifyOriginalResponseAsync(message =>
        {
            message.Content = $"Update requested for `{stream.Url}`";
            message.Components = new ComponentBuilder().Build();
        });

        var successEmbed = EmbedResponse.Success($"`{stream.Url}` is now playing: `{playing}` \n\n This change will be reflected on the next update.");
        await component.FollowupAsync(embed: successEmbed, ephemeral: true);
    }
}