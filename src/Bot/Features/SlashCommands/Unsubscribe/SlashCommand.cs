using Discord;
using Discord.WebSocket;
using Mira.Features.SlashCommands.Unsubscribe.Repositories;

namespace Mira.Features.SlashCommands.Unsubscribe;

public class SlashCommand(QueryRepository queryRepository, CommandRepository commandRepository) : ISlashCommand, ISelectable
{
    public string Name => "unsubscribe";
    private const string CustomId = "unsubscribe";

    public ApplicationCommandProperties BuildCommand() => 
        new SlashCommandBuilder()
            .WithName(Name)
            .WithDescription("Remove a stream notification.")
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
        
        var guildSubscriptions = await queryRepository.GetSubscriptionsAsync(guildId.Value);
        if (guildSubscriptions.Length == 0)
        {
            await command.FollowupAsync("No subscriptions found for this server.");
            return;
        }
        
        var unsubscribeOptions = guildSubscriptions
            .Select(subscription => new SelectMenuOptionBuilder(subscription.Url, subscription.Id.ToString()))
            .ToList();

        var component = new ComponentBuilder()
            .WithSelectMenu(CustomId, unsubscribeOptions)
            .Build();

        await command.FollowupAsync("Select a stream to unsubscribe from:", components: component);
    }
    
    public bool HandlesComponent(SocketMessageComponent component) => component.Data.CustomId == CustomId;

    public async Task RespondAsync(SocketMessageComponent component)
    {
        if (!int.TryParse(component.Data.Values.FirstOrDefault(), out var subscriptionId))
        {
            return;
        }
        
        var guildId = component.GuildId;
        if (guildId is null)
        {
            // Failure message
            return;
        }
        
        await component.DeferAsync();

        // TODO: Use this response to obtain the createdBy and see if it matches
        var subscription = await queryRepository.GetSubscriptionAsync(subscriptionId, guildId.Value);
        if (subscription is null)
        {
            // Failure Message
            return;
        }

        var success = await commandRepository.DeleteSubscriptionAsync(subscriptionId);
        if (!success)
        {
            // Log and failure
            return;
        }

        await component.ModifyOriginalResponseAsync(message =>
        {
            message.Content = $"Request made to unsubscribe from `{subscription.Url}`";
            message.Components = new ComponentBuilder().Build();
        });

        await component.InteractionChannel.SendMessageAsync(
            $"Unsubscribed from `{subscription.Url}`. Notifications will no longer be sent for this stream.");
    }
}