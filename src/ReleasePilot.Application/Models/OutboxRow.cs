namespace ReleasePilot.Application.Models;

public sealed record OutboxRow(
    Guid id,
    Guid aggregate_id,
    string event_type,
    string payload,
    DateTime occurred_on);