namespace Ironfront.UserService.Application.Exceptions;

public sealed class UserHangarNotFoundException : Exception
{
    public UserHangarNotFoundException(Guid userId)
    : base($"No game profile exists for user '{userId}'.")
    {
        UserId = userId;
    }

    public Guid UserId { get; }
}
