namespace DiscordBot_Framework.Services;

public class PingService
{
    public string GetStatusMessage(int latency) => $"Pong! 目前延遲：`{latency} ms`";
}