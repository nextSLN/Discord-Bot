using Microsoft.AspNetCore.SignalR;

public class BotHub : Hub
{
    private readonly Bot _bot;

    public BotHub(Bot bot)
    {
        _bot = bot;
    }

    public async Task UpdateStatus()
    {
        await Clients.All.SendAsync("ReceiveStatus", new
        {
            IsConnected = _bot.IsConnected,
            Latency = _bot.GetLatency(),
            ServerCount = _bot.GetServerCount(),
            UserCount = _bot.GetUserCount()
        });
    }
}
