using Ironfront.UserService.Application.Models;

namespace Ironfront.UserService.Application;

public sealed class ResearchTargetNotAvailableException
    : InvalidOperationException
{
    public ResearchTargetNotAvailableException(
        string techTreeId,
        string nodeId,
        TechTreeNodeResearchState currentState)
        : base(
            $"Node '{nodeId}' in tech tree '{techTreeId}' " +
            $"cannot be selected as a research target because " +
            $"its current state is '{currentState}'.")
    {
        TechTreeId = techTreeId;
        NodeId = nodeId;
        CurrentState = currentState;
    }

    public string TechTreeId { get; }

    public string NodeId { get; }

    public TechTreeNodeResearchState CurrentState { get; }
}