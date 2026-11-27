using Discord;

namespace DiscordBot_Framework.Core;

public static class ErrorHandler
{
    /// <summary>
    /// 接收 DiscordSocketClient 的日誌與事件處理器內部例外
    /// </summary>
    public static Task OnClientLogAsync(LogMessage log)
    {
        var timestamp = DateTime.Now.ToString("HH:mm:ss");
        Console.WriteLine($"[{timestamp}] [{log.Severity}] [{log.Source}] {log.Message}");

        if (log.Exception is not null)
        {
            WriteErrorDetail(log.Exception, $"Discord.Net ({log.Source})");
        }

        return Task.CompletedTask;
    }

    /// <summary>
    /// 包裝非同步事件邏輯，捕捉業務程式碼中的例外
    /// </summary>
    public static async Task SafeExecuteAsync(Func<Task> action, string context)
    {
        try
        {
            await action();
        }
        catch (Exception ex)
        {
            WriteErrorDetail(ex, context);
        }
    }

    /// <summary>
    /// 處理啟動失敗或未觀察到的致命例外
    /// </summary>
    public static void HandleFatal(Exception? ex, string context)
    {
        if (ex is null) return;
        WriteErrorDetail(ex, $"FATAL - {context}");
    }

    private static void WriteErrorDetail(Exception ex, string context)
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine($"=== 錯誤捕捉 [{context}] ===");
        Console.WriteLine($"類型: {ex.GetType().Name}");
        Console.WriteLine($"訊息: {ex.Message}");
        Console.WriteLine($"堆疊:\n{ex.StackTrace}");
        Console.ResetColor();

        // 若未來要寫入檔案 Log，可統一在這裡擴充
    }
}