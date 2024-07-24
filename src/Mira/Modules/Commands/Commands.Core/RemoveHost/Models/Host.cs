namespace Commands.Core.RemoveHost.Models;

public class Host
{
    public int Id { get; set; }
    public string Url { get; set; } = null!;
    public ulong CreatedBy { get; set; }
}