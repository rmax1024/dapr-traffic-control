int lanes = 1;
CameraSimulation[] cameras = new CameraSimulation[lanes];

var client = new HttpClient();
var str = await client.GetStringAsync("https://google.com");
Console.WriteLine($"google: {str}");

for (var i = 0; i < lanes; i++)
{
    int camNumber = i + 1;
    var trafficControlService = new DaprTrafficControlService(camNumber);
    cameras[i] = new CameraSimulation(camNumber, trafficControlService);
}
Parallel.ForEach(cameras, cam => cam.Start());

Task.Run(() => Thread.Sleep(Timeout.Infinite)).Wait();