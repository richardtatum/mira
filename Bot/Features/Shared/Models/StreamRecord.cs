namespace Mira.Features.Shared.Models;

public class StreamRecord
{
    public int Id { get; set; }
    public int NotificationId { get; set; }
    public StreamStatus Status { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime? EndTime { get; set; }
}