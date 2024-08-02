using Discord;

namespace Messaging.Core;

public static class MessageEmbed
{
    public static Embed Live(string url, int viewers, TimeSpan duration, string? playing, string? imageName = null)
    {
        var fields = new List<EmbedFieldBuilder>
        {
            // Add an empty line between the description and the first field
            new EmbedFieldBuilder().WithName("\u200B").WithValue("\u200B")
        };

        // Add the playing status first
        if (playing is not null)
        {
            fields.Add(new EmbedFieldBuilder().WithName("Currently Playing").WithValue(playing));
        }

        // Set viewers and duration as inline
        fields.AddRange([
            new EmbedFieldBuilder().WithName("Duration").WithValue(duration.ToString(@"hh\:mm")).WithIsInline(true),
            new EmbedFieldBuilder().WithName("Viewers").WithValue(viewers).WithIsInline(true)
        ]);

        var builder = new EmbedBuilder()
            .WithTitle("Stream Online")
            .WithDescription($"{url} is live!")
            .WithColor(Color.Green)
            .WithFields(fields)
            .WithFooter("Last Updated: ")
            .WithCurrentTimestamp();

        if (imageName is not null)
        {
            builder.WithImageUrl($"attachment://{imageName}");
        }

        return builder.Build();
    }


    public static Embed Offline(string url, TimeSpan duration, string? playing, string? imageName = null)
    {
        var fields = new List<EmbedFieldBuilder>
        {
            // Add an empty line between the description and the first field
            new EmbedFieldBuilder().WithName("\u200B").WithValue("\u200B")
        };

        // Add the playing status first
        if (playing is not null)
        {
            fields.Add(new EmbedFieldBuilder().WithName("Previously Playing").WithValue(playing));
        }

        fields.AddRange([
            new EmbedFieldBuilder().WithName("Duration").WithValue(duration.ToString(@"hh\:mm")).WithIsInline(true)
        ]);

        var builder = new EmbedBuilder()
            .WithTitle("Stream Offline")
            .WithDescription($"{url} is offline.")
            .WithColor(Color.Red)
            .WithFields(fields)
            .WithFooter("Ended: ")
            .WithCurrentTimestamp();
        
        if (imageName is not null)
        {
            builder.WithImageUrl($"attachment://{imageName}");
        }

        return builder.Build();
    }
}