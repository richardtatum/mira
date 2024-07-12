using Discord;
using Discord.WebSocket;
using Mira.Features.SlashCommands.List.Repositories;
using Mira.Interfaces;

namespace Mira.Features.SlashCommands.List;

public class SlashCommand(QueryRepository queryRepository) : ISlashCommand
{
    public string Name => "list";

    public ApplicationCommandProperties BuildCommand() =>
        new SlashCommandBuilder()
            .WithName(Name)
            .WithDescription("List all added hosts and subscribed keys.")
            .Build();

    public async Task RespondAsync(SocketSlashCommand command)
    {
        var guildId = command.GuildId;
        if (guildId is null)
        {
            // Failure message
            return;
        }

        await command.DeferAsync();

        var subscriptions = await queryRepository.GetSubscriptionsAsync(guildId.Value);
        if (subscriptions.Length == 0)
        {
            await command.FollowupAsync("No subscriptions or hosts found for this server.");
            return;
        }

        var fields = subscriptions
            .GroupBy(subscription => subscription.Host)
            .Select(grouping =>
            {
                var hasNoStreamKeys = grouping.FirstOrDefault()?.StreamKey is null;
                var value = hasNoStreamKeys
                    ? "- No active subscriptions"
                    : string.Join("\n", grouping.Select(group => $"- {group.StreamKey}"));

                return new EmbedFieldBuilder().WithName(grouping.Key).WithValue(value);
            })
            .ToArray();

        var embed = new EmbedBuilder()
            .WithTitle("Hosts & Subscriptions")
            .WithDescription("Below are the hosts and associated stream keys subscribed to from this server:")
            .WithColor(Color.Blue)
            .WithFields(fields)
            .WithCurrentTimestamp()
            .Build();

        await command.FollowupAsync(embed: embed);
    }
}