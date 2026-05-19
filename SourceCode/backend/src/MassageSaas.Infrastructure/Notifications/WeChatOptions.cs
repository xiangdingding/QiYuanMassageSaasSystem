namespace MassageSaas.Infrastructure.Notifications;

/// <summary>
/// 微信小程序订阅消息配置。绑定自 appsettings 的 "Notifications:WeChat" 节。
/// Enabled=false（默认）时用 LoggingNotificationDispatcher，不发真实消息。
/// </summary>
public class WeChatOptions
{
    public const string SectionName = "Notifications:WeChat";

    public bool Enabled { get; set; }
    public string AppId { get; set; } = string.Empty;
    public string AppSecret { get; set; } = string.Empty;

    /// <summary>订阅消息点击后跳转的小程序页面路径。</summary>
    public string? Page { get; set; }
    /// <summary>跳转小程序版本：formal（正式）/ trial（体验）/ developer（开发）。</summary>
    public string MiniProgramState { get; set; } = "formal";

    /// <summary>NotificationKind 枚举名 → 订阅消息模板 ID。未配置的类型不会下发。</summary>
    public Dictionary<string, string> Templates { get; set; } = new();

    /// <summary>是否填了 AppId/AppSecret。小程序登录（code2Session）只需要这个，不看 Enabled。</summary>
    public bool HasCredentials =>
        !string.IsNullOrWhiteSpace(AppId) && !string.IsNullOrWhiteSpace(AppSecret);

    /// <summary>是否启用订阅消息下发：需开关打开且凭证齐全。</summary>
    public bool IsConfigured => Enabled && HasCredentials;
}
