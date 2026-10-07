namespace Ironfront.UserService.Application.Exceptions;

public sealed class UserDeckNotFoundException : Exception
{
    public UserDeckNotFoundException(
        Guid userId,
        Guid deckId)
        : base(
            $"Deck '{deckId}' does not exist for user '{userId}'.")
    {
        UserId = userId;
        DeckId = deckId;
    }

    public Guid UserId { get; }

    public Guid DeckId { get; }
}