namespace QuartzNetAspire.Contracts;

/// <summary>Message published by the outbox relay (Team A) and consumed by the order listener (Team B).</summary>
public sealed record OrderPlaced(long OrderId, decimal Amount, DateTimeOffset PlacedAt);
