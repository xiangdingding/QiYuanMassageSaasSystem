namespace MassageSaas.Shared.Settings;

/// <summary>
/// 平台级"服务订阅"配置：到期说明 + 客服联系方式。平台端维护，各端只读展示。
/// </summary>
public record PlatformSubscriptionSettingDto(
    string? ExpiryNotice,
    string? ContactPhone,
    string? ContactWechat);

public record UpdatePlatformSubscriptionSettingRequest(
    string? ExpiryNotice,
    string? ContactPhone,
    string? ContactWechat);

/// <summary>
/// 平台级用户使用说明书：CS（桌面端）/ BS（网页端）× 正常 / 无障碍 共四份。
/// 平台端维护，各端 F1 帮助按"端 + 当前显示模式"只读展示对应一份。
/// </summary>
public record PlatformManualDto(
    string CsManualNormal,
    string CsManualA11y,
    string BsManualNormal,
    string BsManualA11y);

public record UpdatePlatformManualRequest(
    string? CsManualNormal,
    string? CsManualA11y,
    string? BsManualNormal,
    string? BsManualA11y);

/// <summary>
/// 平台级注册协议：《用户服务协议》+《隐私协议》。平台端维护，注册页（匿名）只读展示并勾选同意。
/// </summary>
public record PlatformAgreementDto(
    string ServiceAgreement,
    string PrivacyPolicy);

public record UpdatePlatformAgreementRequest(
    string? ServiceAgreement,
    string? PrivacyPolicy);
