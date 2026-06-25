// 移动端（安卓）升级：检测最新版 → 应用内下载 APK（带进度）→ 唤起系统安装器。
// 检测走匿名接口、静默失败；下载用 @capacitor/filesystem，安装用 @capacitor-community/file-opener。
import axios from 'axios';
import { apiBaseURL } from '@/api/config';

export interface AppVersionCheckResult {
  hasUpdate: boolean;
  forceUpdate: boolean;
  latestVersion: string;
  currentVersion?: string | null;
  downloadUrl: string;
  changelog?: string | null;
  fileSizeBytes?: number | null;
  sha256?: string | null;
}

/** 检测安卓端最新版本。静默失败（返回 null），不打扰用户。 */
export async function checkAppUpdate(currentVersion: string): Promise<AppVersionCheckResult | null> {
  try {
    // 用裸 axios 而非全局 http()，避免检测失败时弹出网络错误 Toast
    const res = await axios.get<AppVersionCheckResult>(`${apiBaseURL()}/app-versions/check`, {
      params: { platform: 'Android', version: currentVersion },
      timeout: 8000
    });
    return res.data;
  } catch {
    return null;
  }
}

/** 取当前安装的 App 版本号（原生 versionName）；非原生环境返回 '0.0.0'。 */
export async function getCurrentVersion(): Promise<string> {
  try {
    const { App } = await import('@capacitor/app');
    const info = await App.getInfo();
    return info.version || '0.0.0';
  } catch {
    return '0.0.0';
  }
}

/**
 * 下载 APK 到缓存目录并唤起系统安装器。
 * onProgress 回调 0~1 的比例（无 Content-Length 时可能不回调）。
 */
export async function downloadAndInstallApk(
  url: string,
  version: string,
  onProgress: (ratio: number) => void
): Promise<void> {
  const { Filesystem, Directory } = await import('@capacitor/filesystem');
  const { FileOpener } = await import('@capacitor-community/file-opener');

  const fileName = `massage-update-${version}.apk`;

  // 进度监听：downloadFile 配 progress:true 时按字节回调
  const listener = await Filesystem.addListener('progress', (status: { bytes: number; contentLength: number }) => {
    if (status?.contentLength > 0) onProgress(Math.min(1, status.bytes / status.contentLength));
  });

  try {
    await Filesystem.downloadFile({
      url,
      path: fileName,
      directory: Directory.Cache,
      progress: true
    });

    const { uri } = await Filesystem.getUri({ path: fileName, directory: Directory.Cache });

    // contentType 为 APK MIME，系统据此唤起包安装器
    await FileOpener.open({
      filePath: uri,
      contentType: 'application/vnd.android.package-archive'
    });
  } finally {
    await listener.remove();
  }
}
