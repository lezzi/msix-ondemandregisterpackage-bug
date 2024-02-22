namespace WindowsService;

public class Worker : BackgroundService
{
    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        return Task.Delay(int.MaxValue, stoppingToken);
    }
}