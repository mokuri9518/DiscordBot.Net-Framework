using Discord.Interactions;
using DiscordBot_Framework.Services;

namespace DiscordBot_Framework.Modules;

// 繼承 InteractionModuleBase<SocketInteractionContext> 就會被自動掃描註冊
public class GeneralModule : InteractionModuleBase<SocketInteractionContext>
{
    private readonly PingService _pingService;

    // 建構子自動從 DI 容器取得所需的 Service
    public GeneralModule(PingService pingService)
    {
        _pingService = pingService;
    }

    [SlashCommand("ping", "測試機器人連線延遲")]
    public async Task PingAsync()
    {
        var reply = _pingService.GetStatusMessage(Context.Client.Latency);
        await RespondAsync(reply);
    }
}