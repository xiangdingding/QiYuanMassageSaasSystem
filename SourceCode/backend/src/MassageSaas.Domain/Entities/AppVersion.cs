using MassageSaas.Domain.Common;

namespace MassageSaas.Domain.Entities;

/// <summary>
/// 客户端应用版本（CS 桌面端 / 安卓移动端）。平台端发布、客户端启动时检测升级。
/// 属平台级数据，<b>不做租户隔离</b>：所有租户共用同一套发行版本。
/// 检测时按平台取「启用中、版本号最大」的一条作为最新版；客户端版本低于
/// <see cref="MinSupportedVersion"/> 则强制更新。
/// </summary>
public class AppVersion : BaseEntity
{
    /// <summary>目标平台（CS / 安卓）。</summary>
    public AppPlatform Platform { get; set; }

    /// <summary>版本号，点分语义化，如 1.2.0。</summary>
    public string Version { get; set; } = string.Empty;

    /// <summary>
    /// 最低支持版本：客户端当前版本低于此值则强制更新（必须更新才能继续用）。
    /// 留空表示该版本不强制，仅提示可更新。
    /// </summary>
    public string? MinSupportedVersion { get; set; }

    /// <summary>安装包下载地址（CS 为 setup.exe，安卓为 .apk）。</summary>
    public string DownloadUrl { get; set; } = string.Empty;

    /// <summary>更新日志（本次更新内容，向用户展示）。</summary>
    public string? Changelog { get; set; }

    /// <summary>安装包大小（字节，用于下载进度展示，可空）。</summary>
    public long? FileSizeBytes { get; set; }

    /// <summary>安装包 SHA256（小写十六进制，用于下载后完整性校验，可空）。</summary>
    public string? Sha256 { get; set; }

    /// <summary>是否启用：仅启用中的版本参与检测，便于下线问题包。</summary>
    public bool IsActive { get; set; } = true;

    /// <summary>发布人（平台账号）Id。</summary>
    public long? PublishedByUserId { get; set; }
    public User? PublishedByUser { get; set; }
}
