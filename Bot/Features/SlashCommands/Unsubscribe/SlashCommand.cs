using Discord;
using Discord.WebSocket;
using Mira.Features.SlashCommands.Unsubscribe.Repositories;
using Mira.Interfaces;

namespace Mira.Features.SlashCommands.Unsubscribe;

public class SlashCommand(QueryRepository queryRepository, CommandRepository commandRepository) : ISlashCommand, ISelectable
{
    public string Name => "unsubscribe";
    private const string CustomId = "unsubscribe";

    public Task<SlashCommandProperties> BuildCommandAsync() => Task.FromResult(
        new SlashCommandBuilder()
            .WithName(Name)
            .WithDescription("Select the stream you no longer wish to subscribe to.")
            .Build()
    );

    public async Task RespondAsync(SocketSlashCommand command)
    {
        var guildId = command.GuildId;
        if (guildId is null)
        {
            // Failure message
            return;
        }
        
        var guildSubscriptions = await queryRepository.GetSubscriptionsAsync(guildId.Value);
        if (guildSubscriptions.Length == 0)
        {
            await command.RespondAsync("No subscriptions found for this server.");
            return;
        }
        
        var unsubscribeOptions = guildSubscriptions
            .Select(subscription => new SelectMenuOptionBuilder(subscription.Url, subscription.Id.ToString()))
            .ToList();

        var component = new ComponentBuilder()
            .WithSelectMenu(CustomId, unsubscribeOptions, "Select the url you wish to unsubscribe from")
            .Build();

        await command.RespondAsync("Select a stream to unsubscribe from:", components: component, ephemeral: true);
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
            message.Content = $"Request made to unsubscribe from {subscription.Url}";
            message.Components = new ComponentBuilder().Build();
        });

        await component.InteractionChannel.SendMessageAsync(
            $"Unsubscribed from {subscription.Url}. Notifications will no longer be sent for this stream.");
    }
}