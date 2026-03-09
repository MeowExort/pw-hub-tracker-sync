using System.Text.Json;

namespace Pw.Hub.Tracker.Sync.Web.Models;

public record EventDto(string EventType, JsonElement Json);

public record EventEntity
{
    public long Id { get; init; }
    public required string EventType { get; init; }
    public required string Payload { get; init; }
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
}
