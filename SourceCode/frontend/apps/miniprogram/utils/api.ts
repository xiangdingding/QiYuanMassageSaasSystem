/**
 * 微信小程序与后端 API 通信。所有请求自动注入 Bearer Token，
 * 401 会清空缓存并跳到登录页。
 */
type Method = 'GET' | 'POST' | 'PUT' | 'DELETE' | 'PATCH';

interface RequestOptions<T> {
  method?: Method;
  data?: object;
  query?: Record<string, string | number | boolean | undefined>;
  noAuth?: boolean;
}

const buildUrl = (base: string, path: string, query?: RequestOptions<unknown>['query']): string => {
  const full = `${base}${path}`;
  if (!query) return full;
  const qs = Object.entries(query)
    .filter(([, v]) => v !== undefined && v !== null && v !== '')
    .map(([k, v]) => `${encodeURIComponent(k)}=${encodeURIComponent(String(v))}`)
    .join('&');
  return qs ? `${full}?${qs}` : full;
};

export function request<T>(path: string, opts: RequestOptions<T> = {}): Promise<T> {
  const app = getApp<IAppOption>();
  const base = app.globalData.apiBase;
  const token = wx.getStorageSync('token');

  return new Promise<T>((resolve, reject) => {
    wx.request({
      url: buildUrl(base, path, opts.query),
      method: opts.method ?? 'GET',
      data: opts.data,
      header: {
        'Content-Type': 'application/json',
        ...(opts.noAuth || !token ? {} : { Authorization: `Bearer ${token}` })
      },
      success: (res) => {
        if (res.statusCode === 401) {
          wx.removeStorageSync('token');
          wx.removeStorageSync('user');
          wx.showToast({ title: '请重新登录', icon: 'none' });
          wx.navigateTo({ url: '/pages/auth/login/login' });
          reject(new Error('Unauthorized'));
          return;
        }
        if (res.statusCode >= 200 && res.statusCode < 300) {
          resolve(res.data as T);
        } else {
          const data = res.data as { message?: string } | undefined;
          const msg = data?.message || `HTTP ${res.statusCode}`;
          wx.showToast({ title: msg, icon: 'none' });
          reject(new Error(msg));
        }
      },
      fail: (err) => {
        wx.showToast({ title: '网络异常', icon: 'none' });
        reject(err);
      }
    });
  });
}

export const api = {
  get: <T>(path: string, query?: RequestOptions<T>['query']) =>
    request<T>(path, { method: 'GET', query }),
  post: <T>(path: string, data?: object, opts?: { noAuth?: boolean; query?: RequestOptions<T>['query'] }) =>
    request<T>(path, { method: 'POST', data, noAuth: opts?.noAuth, query: opts?.query }),
  put: <T>(path: string, data?: object) => request<T>(path, { method: 'PUT', data }),
  del: <T>(path: string) => request<T>(path, { method: 'DELETE' })
};

/**
 * 把金额（元）转成屏幕阅读器友好的中文："328 元 5 角"。
 * 与 performance.ts 共享逻辑，避免每个页面重复实现。
 */
export function yuanToReadable(amount: number): string {
  const yuan = Math.floor(amount);
  const jiao = Math.round((amount - yuan) * 10);
  return jiao === 0 ? `${yuan} 元` : `${yuan} 元 ${jiao} 角`;
}
