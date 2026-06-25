using MassageSaas.Application.Abstractions;
using MassageSaas.Domain.Common;
using MassageSaas.Domain.Entities;
using MassageSaas.Infrastructure.Persistence;
using MassageSaas.Shared.AppVersions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MassageSaas.Api.Controllers;

/// <summary>
/// 客户端应用版本/升级：客户端（CS、安卓）匿名检测最新版；平台端（PlatformAdmin）发布与管理。
/// 平台级数据，不做租户隔离。检测为 GET，到期租户也放行。
/// </summary>
[ApiController]
[Route("api/app-versions")]
public class AppVersionsController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    private readonly ITenantContext _tenantContext;

    public AppVersionsController(ApplicationDbContext db, ITenantContext tenantContext)
    {
        _db = db;
        _tenantContext = tenantContext;
    }

    /// <summary>客户端启动检测升级（匿名）：传平台与当前版本，返回是否有更新/是否强制。</summary>
    [HttpGet("check")]
    [AllowAnonymous]
    public async Task<ActionResult<AppVersionCheckResult>> Check(
        [FromQuery] string platform,
        [FromQuery] string? version,
        CancellationToken ct = default)
    {
        if (!Enum.TryParse<AppPlatform>(platform, ignoreCase: true, out var plat))
            return BadRequest(new { code = "InvalidPlatform", message = "无效的平台参数" });

        var current = version?.Trim() ?? string.Empty;

        // 取该平台所有启用版本，在内存里按语义化版本号选最大（DB 不便做点分比较）
        var actives = await _db.AppVersions.AsNoTracking()
            .Where(x => x.Platform == plat && x.IsActive)
            .Select(x => new { x.Version, x.MinSupportedVersion, x.DownloadUrl, x.Changelog, x.FileSizeBytes, x.Sha256 })
            .ToListAsync(ct);

        if (actives.Count == 0)
            return Ok(new AppVersionCheckResult(false, false, current, current, string.Empty, null, null, null));

        var latest = actives.OrderByDescending(x => x.Version, Comparer<string>.Create(VersionCompare.Compare)).First();

        var hasUpdate = VersionCompare.IsNewer(latest.Version, current);
        var forceUpdate = hasUpdate
            && !string.IsNullOrWhiteSpace(latest.MinSupportedVersion)
            && VersionCompare.Compare(current, latest.MinSupportedVersion) < 0;

        return Ok(new AppVersionCheckResult(
            hasUpdate, forceUpdate, latest.Version, current,
            latest.DownloadUrl, latest.Changelog, latest.FileSizeBytes, latest.Sha256));
    }

    /// <summary>平台端：版本发布列表（可按平台过滤）。</summary>
    [HttpGet]
    [Authorize(Policy = "PlatformAdmin")]
    public async Task<ActionResult<List<AppVersionDto>>> List(
        [FromQuery] string? platform = null, CancellationToken ct = default)
    {
        var query = _db.AppVersions.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(platform) && Enum.TryParse<AppPlatform>(platform, true, out var plat))
            query = query.Where(x => x.Platform == plat);

        var items = await query
            .OrderByDescending(x => x.CreatedAt)
            .Select(x => new AppVersionDto(
                x.Id, x.Platform.ToString(), x.Version, x.MinSupportedVersion, x.DownloadUrl,
                x.Changelog, x.FileSizeBytes, x.Sha256, x.IsActive,
                x.PublishedByUser != null ? (x.PublishedByUser.RealName ?? x.PublishedByUser.Username) : null,
                x.CreatedAt))
            .ToListAsync(ct);

        return Ok(items);
    }

    /// <summary>平台端：发布新版本。</summary>
    [HttpPost]
    [Authorize(Policy = "PlatformAdmin")]
    public async Task<ActionResult<AppVersionDto>> Create([FromBody] CreateAppVersionRequest req, CancellationToken ct)
    {
        if (!Enum.TryParse<AppPlatform>(req.Platform, true, out var plat))
            return BadRequest(new { code = "InvalidPlatform", message = "无效的平台" });
        if (string.IsNullOrWhiteSpace(req.Version))
            return BadRequest(new { code = "VersionRequired", message = "请填写版本号" });
        if (string.IsNullOrWhiteSpace(req.DownloadUrl))
            return BadRequest(new { code = "DownloadUrlRequired", message = "请填写下载地址" });

        var entity = new AppVersion
        {
            Platform = plat,
            Version = req.Version.Trim(),
            MinSupportedVersion = string.IsNullOrWhiteSpace(req.MinSupportedVersion) ? null : req.MinSupportedVersion.Trim(),
            DownloadUrl = req.DownloadUrl.Trim(),
            Changelog = string.IsNullOrWhiteSpace(req.Changelog) ? null : req.Changelog.Trim(),
            FileSizeBytes = req.FileSizeBytes,
            Sha256 = string.IsNullOrWhiteSpace(req.Sha256) ? null : req.Sha256.Trim().ToLowerInvariant(),
            IsActive = req.IsActive,
            PublishedByUserId = _tenantContext.UserId
        };
        _db.AppVersions.Add(entity);
        await _db.SaveChangesAsync(ct);

        return Ok(await ToDto(entity.Id, ct));
    }

    /// <summary>平台端：编辑版本。</summary>
    [HttpPut("{id:long}")]
    [Authorize(Policy = "PlatformAdmin")]
    public async Task<ActionResult<AppVersionDto>> Update(long id, [FromBody] UpdateAppVersionRequest req, CancellationToken ct)
    {
        var entity = await _db.AppVersions.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (entity is null)
            return NotFound(new { code = "NotFound", message = "版本不存在" });
        if (string.IsNullOrWhiteSpace(req.Version))
            return BadRequest(new { code = "VersionRequired", message = "请填写版本号" });
        if (string.IsNullOrWhiteSpace(req.DownloadUrl))
            return BadRequest(new { code = "DownloadUrlRequired", message = "请填写下载地址" });

        entity.Version = req.Version.Trim();
        entity.MinSupportedVersion = string.IsNullOrWhiteSpace(req.MinSupportedVersion) ? null : req.MinSupportedVersion.Trim();
        entity.DownloadUrl = req.DownloadUrl.Trim();
        entity.Changelog = string.IsNullOrWhiteSpace(req.Changelog) ? null : req.Changelog.Trim();
        entity.FileSizeBytes = req.FileSizeBytes;
        entity.Sha256 = string.IsNullOrWhiteSpace(req.Sha256) ? null : req.Sha256.Trim().ToLowerInvariant();
        entity.IsActive = req.IsActive;
        entity.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);

        return Ok(await ToDto(entity.Id, ct));
    }

    /// <summary>平台端：删除版本。</summary>
    [HttpDelete("{id:long}")]
    [Authorize(Policy = "PlatformAdmin")]
    public async Task<IActionResult> Delete(long id, CancellationToken ct)
    {
        var entity = await _db.AppVersions.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (entity is null)
            return NotFound(new { code = "NotFound", message = "版本不存在" });
        _db.AppVersions.Remove(entity);
        await _db.SaveChangesAsync(ct);
        return NoContent();
    }

    private async Task<AppVersionDto> ToDto(long id, CancellationToken ct)
    {
        return await _db.AppVersions.AsNoTracking()
            .Where(x => x.Id == id)
            .Select(x => new AppVersionDto(
                x.Id, x.Platform.ToString(), x.Version, x.MinSupportedVersion, x.DownloadUrl,
                x.Changelog, x.FileSizeBytes, x.Sha256, x.IsActive,
                x.PublishedByUser != null ? (x.PublishedByUser.RealName ?? x.PublishedByUser.Username) : null,
                x.CreatedAt))
            .FirstAsync(ct);
    }
}
