namespace TrafficControlService.Repositories;

public interface IVehicleStateRepository
{
    Task SaveVehicleStateAsync(VehicleState vehicleState, string? etag = null);
    Task<(VehicleState Value, string ETag)> GetVehicleStateAsync(string licenseNumber);
}
