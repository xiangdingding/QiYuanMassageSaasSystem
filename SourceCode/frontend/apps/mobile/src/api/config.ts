// API 服务器地址解析。
// - 浏览器 dev：留空，走 Vite proxy 的相对 /api。
// - 打包进 App（Capacitor）：必须是绝对地址（http(s)://host:port），
//   优先用「我的 > 服务器地址」里运行时设置的值，其次用构建期 VITE_API_BASE。
const API_BASE_KEY = 'massage_saas_mobile_api_base';

export function getApiBase(): string {
  const override = localStorage.getItem(API_BASE_KEY);
  if (override && override.trim()) return stripTrailingSlash(override.trim());
  const env = import.meta.env.VITE_API_BASE;
  if (env && env.trim()) return stripTrailingSlash(env.trim());
  return '';
}

export function setApiBase(url: string | null) {
  if (!url || !url.trim()) {
    localStorage.removeItem(API_BASE_KEY);
  } else {
    localStorage.setItem(API_BASE_KEY, stripTrailingSlash(url.trim()));
  }
}

/** axios 用的 baseURL：始终以 /api 结尾。 */
export function apiBaseURL(): string {
  return `${getApiBase()}/api`;
}

function stripTrailingSlash(s: string): string {
  return s.replace(/\/+$/, '');
}
