using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using DiscordBot_Framework.Core;
using System.Reflection;

namespace DiscordBot_Framework.Core;

public class InteractionHandler
{
    private readonly DiscordSocketClient _client;
    private readonly InteractionService _interactions;
    private readonly IServiceProvider _services;

    public InteractionHandler(
        DiscordSocketClient client,
        InteractionService interactions,
        IServiceProvider services)
    {
        _client = client;
        _interactions = interactions;
        _services = services;
    }

    public async Task InitializeAsync()
    {
        _interactions.Log += ErrorHandler.OnClientLogAsync;

        // 自動掃描目前專案中所有繼承 InteractionModuleBase 的類別
        await _interactions.AddModulesAsync(Assembly.GetEntryAssembly(), _services);

        // 當 Bot 連線就緒時，將指令註冊到 Discord
        _client.Ready += async () =>
        {
            // 開發測試階段建議用 RegisterCommandsToGuildAsync(測試伺服器ID)，更新會立即生效；
            // 正式上線再改用 RegisterCommandsGloballyAsync()
            await _interactions.RegisterCommandsGloballyAsync();
            Console.WriteLine($"[Ready] 已登入為 {_client.CurrentUser}，所有指令模組載入完成。");
        };

        // 監聽所有互動事件（斜線指令、按鈕、選單、Modal）
        _client.InteractionCreated += HandleInteractionAsync;

        // 集中捕捉指令執行期間的錯誤（例如權限不足、參數錯誤、模組內部 Exception）
        _interactions.SlashCommandExecuted += OnSlashCommandExecutedAsync;
    }

    private async Task HandleInteractionAsync(SocketInteraction interaction)
    {
        await ErrorHandler.SafeExecuteAsync(async () =>
        {
            var context = new SocketInteractionContext(_client, interaction);
            await _interactions.ExecuteCommandAsync(context, _services);
        }, nameof(_client.InteractionCreated));
    }

    private static async Task OnSlashCommandExecutedAsync(
        SlashCommandInfo command,
        IInteractionContext context,
        IResult result)
    {
        if (result.IsSuccess) return;

        // 1. 記錄詳細錯誤到後台
        if (result is ExecuteResult execResult && execResult.Exception is not null)
        {
            ErrorHandler.HandleFatal(execResult.Exception, $"SlashCommand: /{command?.Name}");
        }
        else
        {
            Console.WriteLine($"[Command Warning] /{command?.Name} 失敗：{result.ErrorReason}");
        }

        // 2. 友善回覆使用者，避免 Discord 顯示「應用程式沒有回應」
        var errorMessage = $"執行指令時發生錯誤：{result.ErrorReason}";
        if (context.Interaction.HasResponded)
            await context.Interaction.FollowupAsync(errorMessage, ephemeral: true);
        else
            await context.Interaction.RespondAsync(errorMessage, ephemeral: true);
    }
}