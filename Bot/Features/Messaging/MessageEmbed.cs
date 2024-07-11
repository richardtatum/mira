using Discord;

namespace Mira.Features.Messaging;

public static class MessageEmbed
{
    public static Embed Live(string url, int viewers, string duration)
        => new EmbedBuilder()
            .WithTitle("Stream is online")
            .WithDescription($"{url} is currently live!")
            .WithColor(Color.Green)
            .WithFields(
                new EmbedFieldBuilder().WithName("Viewers").WithValue(viewers),
                new EmbedFieldBuilder().WithName("Duration").WithValue(duration)
            )
            .WithFooter("Started: ")
            .WithCurrentTimestamp()
            .Build();

    public static Embed Offline(string url, int viewers, string duration)
        => new EmbedBuilder()
            .WithTitle("Stream is offline")            
            .WithDescription($"{url} is now offline.")
            .WithColor(Color.Red)
            .WithFields(
                new EmbedFieldBuilder().WithName("Viewers").WithValue(viewers),
                new EmbedFieldBuilder().WithName("Duration").WithValue(duration)
            )
            .WithFooter("Ended: ")
            .WithCurrentTimestamp()
            .Build();
}