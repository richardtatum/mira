using Commands.Core.List.Repositories;
using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.Logging;

namespace Commands.Core.List;

public class SlashCommand(QueryRepository queryRepository, ILogger<SlashCommand> logger) : ISlashCommand
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
            logger.LogCritical("[SLASH-COMMAND][{Name}] Failed to retrieve guildId from SocketSlashCommand. Received: {GuildId}", Name, guildId);
            return;
        }

        await command.DeferAsync(ephemeral: true);

        var subscriptions = await queryRepository.GetSubscriptionsAsync(guildId.Value);
        if (subscriptions.Length == 0)
        {
            var noHostEmbed = GenerateEmbed("No hosts or subscriptions found. Please use `/add-host` to register a new one.");
            await command.FollowupAsync(embed: noHostEmbed);
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

        var embed = GenerateEmbed("Below are the hosts and associated stream keys subscribed to from this server:", fields);
        await command.FollowupAsync(embed: embed);
    }

    private static Embed GenerateEmbed(string description, IEnumerable<EmbedFieldBuilder>? fields = null)
    {
        var embed = new EmbedBuilder()
            .WithTitle("Hosts & Subscriptions")
            .WithDescription(description)
            .WithColor(Color.Blue)
            .WithCurrentTimestamp();

        var embedFieldBuilders = fields as EmbedFieldBuilder[] ?? fields?.ToArray();
        if (embedFieldBuilders is not null && embedFieldBuilders.Length != 0)
        {
            embed.WithFields(embedFieldBuilders);
        }

        return embed.Build();
    }
}