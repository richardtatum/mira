using Discord;
using Discord.WebSocket;
using Mira.Features.SlashCommands.Notify.Repositories;
using Mira.Interfaces;

namespace Mira.Features.SlashCommands.Notify;

public class SlashCommand(QueryRepository queryRepository, CommandRepository commandRepository) : ISlashCommand, IInteractable
{
    public string Name => "notify";

    private const string StreamKeyOptionName = "streamkey";

    public Task<SlashCommandProperties> BuildCommandAsync() => Task.FromResult(
        new SlashCommandBuilder()
            .WithName(Name)
            .WithDescription("Notify when a user begins streaming.")
            .AddOptions(
                new SlashCommandOptionBuilder()
                    .WithName(StreamKeyOptionName)
                    .WithDescription("The key the stream uses. This is usually the streamers username.")
                    .WithRequired(true)
                    .WithType(ApplicationCommandOptionType.String)
            )
            .Build());

    public async Task RespondAsync(SocketSlashCommand command)
    {
        var streamKey = command.Data.Options.FirstOrDefault(x => x.Name == StreamKeyOptionName)?.Value?.ToString();
        var channel = command.ChannelId;
        var createdBy = command.User.Id;
        if (string.IsNullOrWhiteSpace(streamKey))
        {
            // Return failure message
            return;
        }

        var notification = new Notification
        {
            StreamKey = streamKey,
            Channel = channel,
            CreatedBy = createdBy
        };

        var id = await commandRepository.AddNotification(notification);
        var hosts = await queryRepository.GetHostsAsync(channel);

        var options = new ComponentBuilder()
            .WithSelectMenu(id.ToString(),
            hosts.Select(host => new SelectMenuOptionBuilder(host.Url, host.Id.ToString())).ToList())
            .Build();

        await command.RespondAsync("Select a host:", components: options, ephemeral: true);
    }


    public async Task RespondAsync(SocketInteraction interaction)
    {
        if (interaction is not SocketMessageComponent component)
        {
            return;
        }

        if (!int.TryParse(component.Data.CustomId, out var id))
        {
            // Throw error
            return;
        }
        
        await interaction.DeferAsync(ephemeral: true);
        
        var record = await queryRepository.GetNotificationAsync(id);
        if (record?.Id is null)
        {
            return;
        }

        if (!int.TryParse(component.Data.Values.FirstOrDefault(), out var hostId))
        {
            return;
        }

        var host = await queryRepository.GetHostAsync(hostId);
        var url = $"{host.Url}/{record.StreamKey}";

        await commandRepository.UpdateNotification(record.Id ?? throw new ArgumentException(), hostId);
        
        await interaction.ModifyOriginalResponseAsync(message =>
        {
            message.Content = $"Notification created! Url: {url}";
            message.Components = new ComponentBuilder().WithButton("Watch now!", style: ButtonStyle.Link, url: url).Build();
        });
    }
}