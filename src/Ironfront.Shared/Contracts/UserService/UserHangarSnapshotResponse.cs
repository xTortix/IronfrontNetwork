using Ironfront.Shared.Contracts.Hangar;

namespace Ironfront.Shared.Contracts.UserService;

public sealed record UserHangarSnapshotResponse(
    Guid UserId,
    Guid ActiveDeckId,
    IReadOnlyList<HangarVehicleResponse> OwnedVehicles,
    IReadOnlyList<HangarDeckResponse> Decks);