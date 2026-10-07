namespace Ironfront.Shared.Contracts;

public sealed class ServiceStatusDto
{
    public string ServiceName { get; init; } = "";
    public string Status { get; init; } = "";
    public DateTime ServerTimeUtc { get; init; }
    public string Environment { get; init; } = "";
}