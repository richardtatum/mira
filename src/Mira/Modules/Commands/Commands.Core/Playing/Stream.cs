namespace Commands.Core.Playing;

public class Stream
{
    public int Id { get; set; }
    public string HostUrl { get; set; } = null!;
    public string Key { get; set; } = null !;
    public string Url => $"{HostUrl}/{Key}";
}