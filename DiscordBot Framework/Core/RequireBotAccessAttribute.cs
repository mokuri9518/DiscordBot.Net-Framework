using Discord;
using Discord.Interactions;
namespace DiscordBot_Framework.Core;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
public class DiscordUserAccessAttribute : PreconditionAttribute
{
    public string RequiredPermission { get; }

    public DiscordUserAccessAttribute(string permission)
    {
        RequiredPermission = permission;
    }

    public override Task<PreconditionResult> CheckRequirementsAsync(
        IInteractionContext context,
        ICommandInfo commandInfo,
        IServiceProvider services)
    {
        var userId = context.User.Id.ToString();
        var settings = DiscordBot_Framework.Properties.Permissions.Default;

        // 1. admin 預設擁有最高權限（可通行所有指令）
        if (settings.developers?.Contains(userId) == true)
            return Task.FromResult(PreconditionResult.FromSuccess());

        // 2. 根據傳入的權限名稱對應到 Settings 欄位
        bool hasAccess = RequiredPermission.ToLowerInvariant() switch
        {
            "developer" => settings.developers?.Contains(userId) == true,
            "tester" => settings.testers?.Contains(userId) == true,
            _ => false
        };

        return Task.FromResult(hasAccess
            ? PreconditionResult.FromSuccess()
            : PreconditionResult.FromError($"權限不足：需要 `{RequiredPermission}` 權限。"));
    }
}