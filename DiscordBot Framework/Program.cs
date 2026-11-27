using DiscordBot_Framework.Core;
using DiscordBot_Framework.Properties;
public class discordBotFramework
{
    public static async Task Main(string[] args)
    {
        // 攔截未被 await 或背景執行緒拋出的未處理例外
        AppDomain.CurrentDomain.UnhandledException += (_, e) =>
        ErrorHandler.HandleFatal(e.ExceptionObject as Exception, "AppDomain.UnhandledException");

        TaskScheduler.UnobservedTaskException += (_, e) =>
        {
            ErrorHandler.HandleFatal(e.Exception, "TaskScheduler.UnobservedTaskException");
            e.SetObserved(); // 防止整個處理程序直接崩潰
        };

        try
        {
            var token = APIs.Default.DISCORD_BOT_TOKEN?.Trim();
            if (string.IsNullOrWhiteSpace(token))
                throw new InvalidOperationException("找不到 DISCORD_BOT_TOKEN。");

            await using var bot = new BotHost();
            await bot.RunAsync(token);
        }
        catch (Exception ex)
        {
            ErrorHandler.HandleFatal(ex, "Main Startup");
        }
        finally
        {
            Console.WriteLine("按任意鍵退出...");
            Console.ReadKey();
        }
    }
}