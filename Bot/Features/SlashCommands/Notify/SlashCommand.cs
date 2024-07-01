using Discord;
using Discord.WebSocket;
using Mira.Interfaces;

namespace Mira.Features.SlashCommands.Notify;

public class SlashCommand : ISlashCommand, IInteractable
{
    public string Name => "notify";

    private string[] Hosts => new[] { "https://b.siobud.com", "https://stream.smoothbrain.io" };
    private string TokenOptionName => "token";
    private string UserOptionName => "user";

    private string uniqueId = "asjkdhaukjh3243424";
    private Notification[] db =>
    [
        new Notification("", id: uniqueId)
    ];

    public Task<SlashCommandProperties> BuildCommandAsync() => Task.FromResult(
        new SlashCommandBuilder()
            .WithName(Name)
            .WithDescription("Notify when a user begins streaming.")
            .AddOptions(
                new SlashCommandOptionBuilder()
                    .WithName(TokenOptionName)
                    .WithDescription("The token the stream uses. This is usually the streamers username.")
                    .WithRequired(true)
                    .WithType(ApplicationCommandOptionType.String),
                new SlashCommandOptionBuilder()
                    .WithName(UserOptionName)
                    .WithDescription("The discord user associated with this stream.")
                    .WithType(ApplicationCommandOptionType.User)
            )
            .Build());

    public async Task RespondAsync(SocketSlashCommand command)
    {
        var token = command.Data.Options.FirstOrDefault(x => x.Name == TokenOptionName)?.Value?.ToString();
        var user = command.Data.Options.FirstOrDefault(x => x.Name == UserOptionName)?.Value?.ToString();
        if (string.IsNullOrWhiteSpace(token))
        {
            // Return failure message
            return;
        }

        var notification = db.First();
        notification.Token = token;
        notification.Mention = user;

        var options = new ComponentBuilder()
            .WithSelectMenu(uniqueId,
            Hosts.Select(host => new SelectMenuOptionBuilder(host, host)).ToList())
            .Build();

        await command.RespondAsync("Select a host:", components: options, ephemeral: true);
    }


    public async Task RespondAsync(SocketInteraction interaction)
    {
        if (interaction is not SocketMessageComponent component)
        {
            return;
        }
        
        var record = db.FirstOrDefault(x => x.Id == component.Data.CustomId);
        if (record is null)
        {
            return;
        }

        await interaction.DeferAsync(ephemeral: true);

        var host = component.Data.Values.FirstOrDefault();
        record.Host = host;
        
        await interaction.ModifyOriginalResponseAsync(message =>
        {
            message.Content = $"Notification created! {record.Mention} Url: {record.Url}";
            message.Components = new ComponentBuilder().WithButton("Watch now!", style: ButtonStyle.Link, url: $"{record.Url}").Build();
        });
    }
}