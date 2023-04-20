namespace TrafficControlService.Models;

public record struct VehicleState(string LicenseNumber, DateTime? EntryTimestamp, DateTime? ExitTimestamp = null)
{
    public string LicenseNumber { get; init; } = LicenseNumber;
    public DateTime? EntryTimestamp { get; init; } = EntryTimestamp;
    public DateTime? ExitTimestamp { get; init; } = ExitTimestamp;
}
