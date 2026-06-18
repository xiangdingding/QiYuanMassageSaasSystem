import axios, { AxiosError, AxiosInstance } from 'axios';
import { showToast, showFailToast } from 'vant';
import { useAuthStore } from '@/stores/auth';
import { apiBaseURL } from './config';

let instance: AxiosInstance | null = null;

// 与 BS 端 http.ts 行为一致：注入 Bearer、401 登出、403 到期/无权限、5xx 异常统一提示。
// 差异：baseURL 取运行时 apiBase（移动端需绝对地址）；提示用 Vant Toast 而非 ElMessage；
// 401 跳转用 router（在 main 里通过事件接管），这里只清登录态。
export function http(): AxiosInstance {
  if (instance) return instance;
  instance = axios.create({ baseURL: apiBaseURL(), timeout: 15000 });

  instance.interceptors.request.use((config) => {
    // baseURL 可能在「服务器地址」里被改过，每次请求重新取，避免缓存旧地址。
    config.baseURL = apiBaseURL();
    const auth = useAuthStore();
    if (auth.token) {
      config.headers = config.headers ?? {};
      (config.headers as Record<string, string>).Authorization = `Bearer ${auth.token}`;
    }
    return config;
  });

  instance.interceptors.response.use(
    (resp) => resp,
    (err: AxiosError<{ code?: string; message?: string }>) => {
      const status = err.response?.status;
      const data = err.response?.data;
      const msg = data?.message || err.message;
      if (status === 401) {
        const auth = useAuthStore();
        auth.logout();
        showFailToast('登录已过期，请重新登录');
        window.dispatchEvent(new CustomEvent('auth:unauthorized'));
      } else if (status === 403 && data?.code === 'TenantExpired') {
        showFailToast(msg || '订阅已到期，无法执行写操作，请联系平台续费');
      } else if (status === 403) {
        showFailToast(msg || '无权限');
      } else if (status && status >= 500) {
        showFailToast(msg || '服务器异常');
      } else if (!err.response) {
        showFailToast('网络连接失败，请检查服务器地址');
      } else if (msg) {
        showToast(msg);
      }
      return Promise.reject(err);
    }
  );

  return instance;
}
