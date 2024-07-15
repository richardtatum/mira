using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.Logging;
using Mira.Features.SlashCommands.Unsubscribe.Repositories;

namespace Mira.Features.SlashCommands.Unsubscribe;

public class SlashCommand(QueryRepository queryRepository, CommandRepository commandRepository, ILogger<SlashCommand> logger) : ISlashCommand, ISelectable
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
            logger.LogCritical("[SLASH-COMMAND][{Name}] Failed to retrieve guildId from SocketSlashCommand. Received: {GuildId}", Name, guildId);
            return;
        }

        await command.DeferAsync(ephemeral: true);
        
        var subscriptions = await queryRepository.GetSubscriptionsAsync(guildId.Value);
        if (subscriptions.Length == 0)
        {
            var noSubscriptionsEmbed = GenerateFailedEmbed("No subscriptions found for this server.");
            await command.FollowupAsync(embed: noSubscriptionsEmbed);
            return;
        }
        
        var unsubscribeOptions = subscriptions
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
        var guildId = component.GuildId;
        if (guildId is null)
        {
            logger.LogCritical("[SLASH-COMMAND][{Name}] Failed to retrieve guildId from SocketMessageComponent. Received: {GuildId}", Name, guildId);
            return;
        }
        
        if (!int.TryParse(component.Data.Values.FirstOrDefault(), out var subscriptionId))
        {
            logger.LogCritical("[SLASH-COMMAND][{Name}] Failed to retrieve value of subscriptionId from options. Value: {Value}", Name, component.Data.Values.FirstOrDefault());
            return;
        }
        
        await component.DeferAsync();

        // TODO: Use this response to obtain the createdBy and see if it matches
        var subscription = await queryRepository.GetSubscriptionAsync(subscriptionId, guildId.Value);
        if (subscription is null)
        {
            logger.LogCritical("[SLASH-COMMAND][{Name}] Failed to retrieve the subscription from the provided subscriptionId: {Id}", Name, subscriptionId);
            var subscriptionNullEmbed =
                GenerateFailedEmbed("Failed to retrieve the provided subscription. Please try again.");
            await component.FollowupAsync(embed: subscriptionNullEmbed);
            return;
        }

        var success = await commandRepository.DeleteSubscriptionAsync(subscriptionId);
        if (!success)
        {
            logger.LogCritical("[SLASH-COMMAND][{Name}] Failed to delete the subscription: {Url}", Name, subscription.Url);
            var subscriptionNullEmbed =
                GenerateFailedEmbed("Failed to delete the provided subscription. Please try again.");
            await component.FollowupAsync(embed: subscriptionNullEmbed);
            return;
        }

        await component.ModifyOriginalResponseAsync(message =>
        {
            message.Content = $"Request made to unsubscribe from `{subscription.Url}`";
            message.Components = new ComponentBuilder().Build();
        });

        var successEmbed =
            GenerateSuccessEmbed(
                $"Unsubscribed from `{subscription.Url}`. Notifications will no longer be sent for this stream.");
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