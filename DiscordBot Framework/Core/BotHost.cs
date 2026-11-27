using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using DiscordBot_Framework.Services;
using System;

namespace DiscordBot_Framework.Core;

public class BotHost : IAsyncDisposable
{
    private readonly ServiceProvider _services;
    private readonly DiscordSocketClient _client;
    private readonly InteractionHandler _interactionHandler;
    private readonly CancellationTokenSource _cts = new();

    public BotHost()
    {
        var socketConfig = new DiscordSocketConfig
        {
            GatewayIntents = GatewayIntents.AllUnprivileged | GatewayIntents.MessageContent,
            MessageCacheSize = 100,
            LogLevel = LogSeverity.Info
        };

        var interactionConfig = new InteractionServiceConfig
        {
            LogLevel = LogSeverity.Info,
            DefaultRunMode = RunMode.Async // 避免耗時指令卡住 Gateway 執行緒
        };

        // 1. 建立 DI 容器，以後新增共用服務只要在這裡 AddSingleton 即可
        _services = new ServiceCollection()
            .AddSingleton(socketConfig)
            .AddSingleton<DiscordSocketClient>()
            .AddSingleton(interactionConfig)
            .AddSingleton(x => new InteractionService(x.GetRequiredService<DiscordSocketClient>(), interactionConfig))
            .AddSingleton<InteractionHandler>()
            .AddSingleton<PingService>() // 範例商業邏輯服務
            .BuildServiceProvider();

        _client = _services.GetRequiredService<DiscordSocketClient>();
        _interactionHandler = _services.GetRequiredService<InteractionHandler>();
    }

    public async Task RunAsync(string token)
    {
        Console.CancelKeyPress += (_, e) =>
        {
            e.Cancel = true;
            _cts.Cancel();
        };

        _client.Log += ErrorHandler.OnClientLogAsync;
        _client.Disconnected += ex =>
        {
            Console.WriteLine($"[Disconnected] 與 Gateway 斷線：{ex?.Message}");
            return Task.CompletedTask;
        };

        // 2. 初始化指令系統（自動掃描 Modules 資料夾下的所有模組）
        await _interactionHandler.InitializeAsync();

        await _client.LoginAsync(TokenType.Bot, token);
        await _client.StartAsync();

        try
        {
            await Task.Delay(Timeout.Infinite, _cts.Token);
        }
        catch (TaskCanceledException)
        {
            Console.WriteLine("[Shutdown] 正在關閉 Bot...");
        }
    }

    public async ValueTask DisposeAsync()
    {
        await _client.StopAsync();
        await _client.LogoutAsync();
        await _services.DisposeAsync(); // 會一併釋放容器內的 Client 與所有服務
        _cts.Dispose();
    }
}