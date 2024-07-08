using Discord;

namespace Mira.Features.SlashCommands.Notify;

internal static class SlashCommandBuilderExtensions
{
    internal static SlashCommandOptionBuilder AddAvailableHostsAsChoices(this SlashCommandOptionBuilder optionBuilder, string[] hosts)
    {
        foreach (var host in hosts)
        {
            optionBuilder.AddChoice(host, host);
        }

        return optionBuilder;
    }
}