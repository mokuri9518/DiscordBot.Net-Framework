# DiscordBot_Framework

基於 C# 與 [Discord.Net](https://docs.discordnet.dev/) 建構的模組化 Discord Bot 開發框架，內建相依性注入（DI）、Slash Command 自動掃描註冊、宣告式使用者權限控管，以及多層級全域錯誤捕捉機制。

---

## 核心特色

* **模組化架構（Module Pattern）**：採用 `InteractionService` 自動掃描 `Modules/` 目錄下的指令類別，新增功能只需獨立建立 `.cs` 檔案，無須修改核心啟動流程。
* **關注點分離與 DI 容器**：整合 `Microsoft.Extensions.DependencyInjection`，將互動介面（`Modules/`）與商業邏輯（`Services/`）解耦，方便維護與擴充。
* **宣告式權限攔截（`[DiscordUserAccess]`）**：透過自訂 `PreconditionAttribute` 搭配 `permissions.settings`，以白名單（User ID）方式控管指令或群組的存取權限（如 `developers`、`testers`）。
* **多層級全域錯誤捕捉**：涵蓋 `AppDomain` 未處理例外、`TaskScheduler` 背景任務例外、Discord Gateway 日誌與指令執行期錯誤攔截，並自動向使用者回傳 `Ephemeral` 錯誤提示。
* **機密設定隔離**：透過 `APIs.settings` 管理 Token 與外部 API 金鑰，並搭配版控範本（`APIs template.settings`）避免敏感資訊外洩。

---

## 專案目錄結構

```text
DiscordBot_Framework/
├── Program.cs                        # 程式進入點、頂層例外攔截與啟動控制
├── App.config                        # 應用程式設定檔（已納入 .gitignore）
├── Properties/
│   ├── APIs.settings                 # 存放 Bot Token 與 API Key（已納入 .gitignore）
│   ├── APIs template.settings        # APIs 設定檔範本
│   ├── permissions.settings          # 權限群組與 Discord User ID 白名單設定
│   └── Resources.resx
├── Core/
│   ├── BotHost.cs                    # 管理 DiscordSocketClient 生命週期與 DI 容器
│   ├── InteractionHandler.cs         # 自動註冊指令模組、分派互動事件與捕捉指令錯誤
│   ├── ErrorHandler.cs               # 集中式日誌輸出與例外格式化處理
│   └── DiscordUserAccessAttribute.cs # 自訂宣告式使用者權限驗證屬性
├── Modules/
│   └── GeneralModule.cs              # Slash Command 指令模組（繼承 InteractionModuleBase）
└── Services/
    └── General.cs                    # 商業邏輯與共用服務層
```

---

## 環境需求與套件依賴

* **.NET SDK**：.NET 8.0 或以上版本
* **NuGet 套件**：
  * `Discord.Net`
  * `Microsoft.Extensions.DependencyInjection`
  * `System.Configuration.ConfigurationManager`

---

## 快速開始

### 1. 設定 Discord Developer Portal
1. 前往 [Discord Developer Portal](https://discord.com/developers/applications) 建立或選擇應用程式。
2. 進入 **Bot** 分頁，取得 **Bot Token**。
3. 在 **Privileged Gateway Intents** 區塊中開啟 **Message Content Intent**（若需讀取訊息內容）。

### 2. 配置設定檔（`Properties/`）
1. !!! 把APIs.settings加入忽略項目(.gitignore) !!!
2. 填入以下設定值：
   * `DISCORD_BOT_TOKEN`：你的 Discord Bot Token
   * `GEMINI_API`：（選填）外部 API 金鑰
3. 開啟 `Properties/permissions.settings`，設定各權限群組對應的 Discord User ID（多組 ID 以半形逗號 `,` 分隔，類型設為 `string`）：
   * `developers`：最高權限開發者清單（預設可通行所有指令）
   * `testers`：測試人員清單

### 3. 建置與執行
```bash
dotnet restore
dotnet build
dotnet run
```
啟動成功後，終端機將顯示 `[Ready] 已登入為 <BotName>，所有指令模組載入完成。`

---

## 開發指南：如何擴充新功能

### 新增商業邏輯（`Services/`）
1. 在 `Services/` 資料夾建立新的服務類別（例如 `MyFeatureService.cs`）。
2. 開啟 `Core/BotHost.cs`，在 `ServiceCollection` 中註冊該服務：
   ```csharp
   _services = new ServiceCollection()
       // ...既有服務
       .AddSingleton<MyFeatureService>()
       .BuildServiceProvider();
   ```

### 新增斜線指令（`Modules/`）
1. 在 `Modules/` 資料夾建立 `public` 類別並繼承 `InteractionModuleBase<SocketInteractionContext>`。
2. 透過建構子注入所需的 Service，並使用 `[SlashCommand]` 與 `[DiscordUserAccess]` 定義指令與權限：

```csharp
using Discord.Interactions;
using DiscordBot_Framework.Core;
using DiscordBot_Framework.Services;

namespace DiscordBot_Framework.Modules;

[Group("dev", "開發者專用指令群組")]
[DiscordUserAccess("developers")] // 限制僅 permissions.settings 中的 developers 可使用
public class DevModule : InteractionModuleBase<SocketInteractionContext>
{
    private readonly MyFeatureService _service;

    public DevModule(MyFeatureService service)
    {
        _service = service;
    }

    [SlashCommand("status", "檢查服務狀態")]
    public async Task StatusAsync()
    {
        await RespondAsync("服務運作正常！", ephemeral: true);
    }
}
```

---

## 錯誤處理機制

所有執行期錯誤皆統一導向 `Core/ErrorHandler.cs` 處理：
* **啟動與設定錯誤**：由 `Program.cs` 的 `try-catch` 攔截並印出詳細堆疊，程式結束前會暫停等待按鍵確認，避免視窗閃退。
* **指令執行與權限攔截**：透過 `InteractionHandler.OnSlashCommandExecutedAsync` 捕捉例外與 `PreconditionResult` 失敗原因，並自動以 `Ephemeral`（僅觸發者可見）訊息回覆錯誤說明。
* **未觀察到的背景例外**：由 `AppDomain.UnhandledException` 與 `TaskScheduler.UnobservedTaskException` 全域接管，防止非同步任務未 `await` 導致處理程序崩潰。
