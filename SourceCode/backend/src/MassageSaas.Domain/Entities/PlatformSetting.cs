using MassageSaas.Domain.Common;

namespace MassageSaas.Domain.Entities;

/// <summary>
/// 平台级全局配置（单行，非租户隔离）。由平台端维护，各租户端只读获取。
/// 目前承载"服务订阅"页的到期说明与客服联系方式。
/// </summary>
public class PlatformSetting : BaseEntity
{
    /// <summary>订阅到期/续费说明文案（多行）。展示在 BS/CS"服务订阅"页。</summary>
    public string? ExpiryNotice { get; set; }

    /// <summary>客服电话（支付问题 / 线下支付咨询）。</summary>
    public string? ContactPhone { get; set; }

    /// <summary>客服微信号。</summary>
    public string? ContactWechat { get; set; }

    /// <summary>CS（桌面端）· 正常模式使用说明书（多行纯文本）。F1 帮助展示，平台端维护。</summary>
    public string? CsManualNormal { get; set; }

    /// <summary>CS（桌面端）· 无障碍模式使用说明书（多行纯文本）。F1 帮助展示，平台端维护。</summary>
    public string? CsManualA11y { get; set; }

    /// <summary>BS（网页端）· 正常模式使用说明书（多行纯文本）。F1 帮助展示，平台端维护。</summary>
    public string? BsManualNormal { get; set; }

    /// <summary>BS（网页端）· 无障碍模式使用说明书（多行纯文本）。F1 帮助展示，平台端维护。</summary>
    public string? BsManualA11y { get; set; }

    /// <summary>《用户服务协议》正文（多行纯文本）。注册页勾选同意，平台端维护。</summary>
    public string? ServiceAgreement { get; set; }

    /// <summary>《隐私协议》正文（多行纯文本）。注册页勾选同意，平台端维护。</summary>
    public string? PrivacyPolicy { get; set; }
}
