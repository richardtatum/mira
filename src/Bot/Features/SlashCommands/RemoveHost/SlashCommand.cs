using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.Logging;
using Mira.Features.SlashCommands.RemoveHost.Repositories;

namespace Mira.Features.SlashCommands.RemoveHost;

public class SlashCommand(QueryRepository queryRepository, CommandRepository commandRepository, ILogger<SlashCommand> logger) : ISlashCommand, ISelectable
{
    public string Name => "remove-host";
    public const string CustomId = "remote-host";

    public ApplicationCommandProperties BuildCommand() =>
        new SlashCommandBuilder()
            .WithName(Name)
            .WithDescription("Remove any existing BroadcastBox hosts and associated subscriptions.")
            .Build();

    public async Task RespondAsync(SocketSlashCommand command)
    {
        var guildId = command.GuildId;
        if (guildId is null)
        {
            logger.LogCritical("[SLASH-COMMAND][{Name}] Failed to retrieve guildId from SocketSlashCommand. Received: {GuildId}", Name, guildId);
            return;
        }

        await command.DeferAsync(ephemeral: true);

        var hosts = await queryRepository.GetHostsAsync(guildId.Value);
        if (hosts.Length == 0)
        {
            await command.FollowupAsync("No hosts found for this server.");
            return;
        }

        var options = hosts
            .Select(host =>
                new SelectMenuOptionBuilder(host.Url, host.Id.ToString(), $"{host.SubscriptionCount} subscription(s)"))
            .ToList();

        var component = new ComponentBuilder()
            .WithSelectMenu(CustomId, options)
            .Build();

        await command.FollowupAsync("Select a host to remove:", components: component);
    }

    public bool HandlesComponent(SocketMessageComponent component) => component.Data.CustomId == CustomId;

    // TODO: Add a confirmation modal which asks the user to enter the host url and submit delete
    // TODO: Restrict removal to those who added it or an admin
    public async Task RespondAsync(SocketMessageComponent component)
    {
        var guildId = component.GuildId;
        if (guildId is null)
        {
            logger.LogCritical("[SLASH-COMMAND][{Name}] Failed to retrieve guildId from SocketMessageComponent. Received: {GuildId}", Name, guildId);
            return;
        }
        
        if (!int.TryParse(component.Data.Values.FirstOrDefault(), out var hostId))
        {
            logger.LogCritical("[SLASH-COMMAND][{Name}] Failed to retrieve hostId value from options. Received: {Value}", Name, component.Data.Values.FirstOrDefault());
            return;
        }
        
        await component.DeferAsync(ephemeral: true);

        // TODO: Use this to acquire the createdBy and see if it matches
        var host = await queryRepository.GetHostAsync(hostId);
        if (host is null)
        {
            logger.LogCritical("[SLASH-COMMAND][{Name}] Failed to retrieve host from hostId: {HostId}", Name, hostId);
            var invalidHostEmbed = GenerateFailedEmbed("Failed to retrieve host. Please try again.");
            await component.FollowupAsync(embed: invalidHostEmbed);
            return;
        }

        var success = await commandRepository.DeleteHostAsync(hostId);
        if (!success)
        {
            logger.LogCritical("[SLASH-COMMAND][{Name}] Failed to delete host {HostUrl}", Name, host.Url);
            var failedToDeleteEmbed = GenerateFailedEmbed("Failed to remove host. Please try again.");
            await component.FollowupAsync(embed: failedToDeleteEmbed);
            return;
        }

        await component.ModifyOriginalResponseAsync(message =>
        {
            message.Content = $"Request made to remove host `{host.Url}`";
            message.Components = new ComponentBuilder().Build();
        });

        var successEmbed =
            GenerateSuccessEmbed($"Host `{host.Url}` has been removed along with all relevant subscriptions.");
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