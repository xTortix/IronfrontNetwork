namespace Ironfront.UserService.Application.Exceptions;

public sealed class UserProfileNotFoundException : Exception
{
    public UserProfileNotFoundException(Guid userId)
        : base($"No user profile exists for user '{userId}'.")
    {
        UserId = userId;
    }

    public Guid UserId { get; }
}