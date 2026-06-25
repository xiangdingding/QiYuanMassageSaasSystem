namespace MassageSaas.Shared.AppVersions;

/// <summary>客户端启动检测升级的返回结果（CS / 安卓共用）。</summary>
public record AppVersionCheckResult(
    bool HasUpdate,             // 服务端最新版本 > 客户端当前版本
    bool ForceUpdate,          // 客户端当前版本 < 最低支持版本，必须更新
    string LatestVersion,      // 最新版本号
    string? CurrentVersion,    // 客户端上报的当前版本（原样回传，便于排查）
    string DownloadUrl,        // 安装包地址（setup.exe / apk）
    string? Changelog,         // 更新日志
    long? FileSizeBytes,       // 安装包大小（字节，可空）
    string? Sha256);           // 安装包 SHA256（可空，下载后校验）

/// <summary>平台端版本发布列表/详情项。</summary>
public record AppVersionDto(
    long Id,
    string Platform,
    string Version,
    string? MinSupportedVersion,
    string DownloadUrl,
    string? Changelog,
    long? FileSizeBytes,
    string? Sha256,
    bool IsActive,
    string? PublishedByName,
    DateTime CreatedAt);

/// <summary>平台端新增版本发布。</summary>
public record CreateAppVersionRequest(
    string Platform,
    string Version,
    string? MinSupportedVersion,
    string DownloadUrl,
    string? Changelog,
    long? FileSizeBytes,
    string? Sha256,
    bool IsActive);

/// <summary>平台端编辑版本发布。</summary>
public record UpdateAppVersionRequest(
    string Version,
    string? MinSupportedVersion,
    string DownloadUrl,
    string? Changelog,
    long? FileSizeBytes,
    string? Sha256,
    bool IsActive);

/// <summary>
/// 点分版本号比较（如 1.2.0）。逐段按整数比较，非数字段忽略其尾部，
/// 缺省段按 0 补齐；无法解析的版本视为 0。仅做大小比较，无第三方依赖，
/// 后端与 CS 端可共用。
/// </summary>
public static class VersionCompare
{
    /// <summary>a 比 b 大返回正数、相等 0、较小负数。</summary>
    public static int Compare(string? a, string? b)
    {
        var pa = Parse(a);
        var pb = Parse(b);
        var len = Math.Max(pa.Length, pb.Length);
        for (var i = 0; i < len; i++)
        {
            var va = i < pa.Length ? pa[i] : 0;
            var vb = i < pb.Length ? pb[i] : 0;
            if (va != vb) return va.CompareTo(vb);
        }
        return 0;
    }

    /// <summary>a 是否比 b 新（严格大于）。</summary>
    public static bool IsNewer(string? a, string? b) => Compare(a, b) > 0;

    private static int[] Parse(string? v)
    {
        if (string.IsNullOrWhiteSpace(v)) return new[] { 0 };
        var parts = v.Trim().TrimStart('v', 'V').Split('.');
        var nums = new int[parts.Length];
        for (var i = 0; i < parts.Length; i++)
        {
            // 取该段开头的连续数字（容忍 "1.2.0-beta" 之类）
            var digits = new string(parts[i].TakeWhile(char.IsDigit).ToArray());
            nums[i] = int.TryParse(digits, out var n) ? n : 0;
        }
        return nums;
    }
}
