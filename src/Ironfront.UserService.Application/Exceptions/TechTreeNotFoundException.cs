namespace Ironfront.UserService.Application.Exceptions;

public sealed class TechTreeNotFoundException : Exception
{
    public TechTreeNotFoundException(string techTreeId)
        : base($"Tech tree '{techTreeId}' does not exist in the active catalog.")
    {
        TechTreeId = techTreeId;
    }

    public string TechTreeId { get; }
}