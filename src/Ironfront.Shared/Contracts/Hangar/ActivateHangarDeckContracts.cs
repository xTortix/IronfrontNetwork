namespace Ironfront.Shared.Contracts.Hangar;

public sealed record HangarDeckActivationResponse(
    Guid DeckId,
    string Name,
    string Nation,
    bool IsActive,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc);