namespace Ironfront.Shared.Contracts.Auth;

public sealed class LogoutResponse
{
    public DateTime LoggedOutAtUtc { get; init; }
}