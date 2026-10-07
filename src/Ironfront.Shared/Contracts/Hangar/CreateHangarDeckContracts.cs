namespace Ironfront.Shared.Contracts.Hangar;

public sealed record CreateHangarDeckRequest(
    string Name,
    string Nation);

public sealed record HangarDeckCreationResponse(
    Guid DeckId,
    string Name,
    string Nation,
    bool IsActive,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc);