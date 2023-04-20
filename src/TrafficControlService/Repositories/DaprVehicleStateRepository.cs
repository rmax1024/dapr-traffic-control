namespace TrafficControlService.Repositories;

public class DaprVehicleStateRepository : IVehicleStateRepository
{
    private const string DAPR_STORE_NAME = "statestore";
    private readonly DaprClient _daprClient;

    public DaprVehicleStateRepository(DaprClient daprClient)
    {
        _daprClient = daprClient;
    }

    public async Task SaveVehicleStateAsync(VehicleState vehicleState, string? etag = null)
    {
        var result = await _daprClient.TrySaveStateAsync(
            DAPR_STORE_NAME, vehicleState.LicenseNumber, vehicleState, etag);
        if (result) return;

        (VehicleState currentState, string currentEtag) = await _daprClient.GetStateAndETagAsync<VehicleState>(
            DAPR_STORE_NAME, vehicleState.LicenseNumber);
        if (currentState.ExitTimestamp == null) currentState = currentState with { ExitTimestamp = vehicleState.ExitTimestamp};
        if (currentState.EntryTimestamp == null) currentState = currentState with { EntryTimestamp = vehicleState.EntryTimestamp };
        await _daprClient.TrySaveStateAsync(
            DAPR_STORE_NAME, vehicleState.LicenseNumber, vehicleState, etag);
    }

    public async Task<(VehicleState Value, string ETag)> GetVehicleStateAsync(string licenseNumber)
    {
        (VehicleState value, string etag) stateEntry = await _daprClient.GetStateAndETagAsync<VehicleState>(
            DAPR_STORE_NAME, licenseNumber);
        return stateEntry;
    }
}
