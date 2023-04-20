//#define USE_ACTORMODEL

/* 
This controller contains 2 implementations of the TrafficControl functionality: a basic 
implementation and an actor-model based implementation.

The code for the basic implementation is in this controller. The actor-model implementation 
resides in the Vehicle actor (./Actors/VehicleActor.cs).

To switch between the two implementations, you need to use the USE_ACTORMODEL symbol at
the top of this file. If you comment the #define USE_ACTORMODEL statement, the basic 
implementation is used. Uncomment this statement to use the Actormodel implementation.
*/

namespace TrafficControlService.Controllers;

[ApiController]
[Route("")]
public class TrafficController : ControllerBase
{
    private readonly ILogger<TrafficController> _logger;
    private readonly IVehicleStateRepository _vehicleStateRepository;
    private readonly ISpeedingViolationCalculator _speedingViolationCalculator;
    private readonly string _roadId;

    public TrafficController(
        ILogger<TrafficController> logger,
        IVehicleStateRepository vehicleStateRepository,
        ISpeedingViolationCalculator speedingViolationCalculator)
    {
        _logger = logger;
        _vehicleStateRepository = vehicleStateRepository;
        _speedingViolationCalculator = speedingViolationCalculator;
        _roadId = speedingViolationCalculator.GetRoadId();
    }

#if !USE_ACTORMODEL

    [HttpPost("entrycam")]
    public async Task<ActionResult> VehicleEntryAsync(VehicleRegistered msg, [FromServices] DaprClient daprClient)
    {
        try
        {
            // log entry
            _logger.LogInformation(
                "ENTRY detected in lane {0} at {1:hh:mm:ss} of vehicle with license-number {2}.", msg.Lane,
                msg.Timestamp, msg.LicenseNumber);

            // get vehicle state
            (VehicleState state, string etag) = await _vehicleStateRepository.GetVehicleStateAsync(msg.LicenseNumber);
            if (state == default(VehicleState))
            {
                // store vehicle state
                var vehicleState = new VehicleState(msg.LicenseNumber, msg.Timestamp);
                await _vehicleStateRepository.SaveVehicleStateAsync(vehicleState, etag);
                return Ok();
            }

            // update state
            var entryState = state with { EntryTimestamp = msg.Timestamp };
            await _vehicleStateRepository.SaveVehicleStateAsync(entryState, etag);
            await HandlePossibleSpeedingViolation(entryState, daprClient);

            return Ok();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while processing ENTRY for {0}", msg.LicenseNumber);
            return StatusCode(500);
        }
    }

    [HttpPost("exitcam")]
    public async Task<ActionResult> VehicleExitAsync(VehicleRegistered msg, [FromServices] DaprClient daprClient)
    {
        try
        {
            // log exit
            _logger.LogInformation($"EXIT detected in lane {msg.Lane} at {msg.Timestamp:hh:mm:ss} " +
                                   $"of vehicle with license-number {msg.LicenseNumber}.");

            // get vehicle state
            (VehicleState state, string etag) = await _vehicleStateRepository.GetVehicleStateAsync(msg.LicenseNumber);
            if (state == default(VehicleState))
            {
                // store vehicle state
                var vehicleState = new VehicleState(msg.LicenseNumber, null,msg.Timestamp);
                await _vehicleStateRepository.SaveVehicleStateAsync(vehicleState, etag);
                return Ok();
            }

            // update state
            var exitState = state with { ExitTimestamp = msg.Timestamp };
            await _vehicleStateRepository.SaveVehicleStateAsync(exitState, etag);
            await HandlePossibleSpeedingViolation(exitState, daprClient);
            
            return Ok();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while processing EXIT for {0}", msg.LicenseNumber);
            return StatusCode(500);
        }
    }

    private async Task HandlePossibleSpeedingViolation(VehicleState state, DaprClient daprClient)
    {
        _logger.LogInformation("Handling possible speed violation for {0}", state.LicenseNumber);
        if (state.ExitTimestamp == null || state.EntryTimestamp == null)
        {
            _logger.LogError("Incorrect state: {0}", state);
            return;
        }

        // handle possible speeding violation
        int violation = _speedingViolationCalculator.DetermineSpeedingViolationInKmh(state.EntryTimestamp!.Value, state.ExitTimestamp!.Value);
        if (violation <= 0) return;

        _logger.LogInformation($"Speeding violation detected ({violation} KMh) of vehicle" +
                               $"with license-number {state.LicenseNumber}.");

        var speedingViolation = new SpeedingViolation
        {
            VehicleId = state.LicenseNumber,
            RoadId = _roadId,
            ViolationInKmh = violation,
            Timestamp = state.ExitTimestamp.Value
        };

        // publish speedingviolation (Dapr publish / subscribe)
        await daprClient.PublishEventAsync("pubsub", "speedingviolations", speedingViolation);
    }

#else

        [HttpPost("entrycam")]
        public async Task<ActionResult> VehicleEntryAsync(VehicleRegistered msg)
        {
            try
            {
                var actorId = new ActorId(msg.LicenseNumber);
                var proxy = ActorProxy.Create<IVehicleActor>(actorId, nameof(VehicleActor));
                await proxy.RegisterEntryAsync(msg);
                return Ok();
            }
            catch
            {
                return StatusCode(500);
            }
        }

        [HttpPost("exitcam")]
        public async Task<ActionResult> VehicleExitAsync(VehicleRegistered msg)
        {
            try
            {
                var actorId = new ActorId(msg.LicenseNumber);
                var proxy = ActorProxy.Create<IVehicleActor>(actorId, nameof(VehicleActor));
                await proxy.RegisterExitAsync(msg);
                return Ok();
            }
            catch
            {
                return StatusCode(500);
            }
        }

#endif

}
