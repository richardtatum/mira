using Discord;
using Discord.WebSocket;
using Mira.Features.SlashCommands.RemoveHost.Repositories;
using Mira.Interfaces;

namespace Mira.Features.SlashCommands.RemoveHost;

public class SlashCommand(QueryRepository queryRepository, CommandRepository commandRepository) : ISlashCommand, ISelectable
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
            // Failure message
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
        if (!int.TryParse(component.Data.Values.FirstOrDefault(), out var hostId))
        {
            // Failure message
            return;
        }
        
        var guildId = component.GuildId;
        if (guildId is null)
        {
            // Failure message
            return;
        }
        
        await component.DeferAsync(ephemeral: true);

        // TODO: Use this to acquire the createdBy and see if it matches
        var host = await queryRepository.GetHostAsync(hostId);
        if (host is null)
        {
            // Failure message
            return;
        }

        var success = await commandRepository.DeleteHostAsync(hostId);
        if (!success)
        {
            // Log and return failure message
            return;
        }

        await component.ModifyOriginalResponseAsync(message =>
        {
            message.Content = $"Request made to remove host `{host.Url}`";
            message.Components = new ComponentBuilder().Build();
        });

        await component.InteractionChannel.SendMessageAsync(
            $"Host `{host.Url}` has been deleted along with all relevant subscriptions."
        );
    }
}