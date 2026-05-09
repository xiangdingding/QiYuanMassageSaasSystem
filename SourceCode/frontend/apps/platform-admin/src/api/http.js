import axios from 'axios';
import { ElMessage } from 'element-plus';
import { useAuthStore } from '@/stores/auth';
let instance = null;
export function http() {
    if (instance)
        return instance;
    instance = axios.create({ baseURL: '/api', timeout: 15000 });
    instance.interceptors.request.use((config) => {
        const auth = useAuthStore();
        if (auth.token) {
            config.headers = config.headers ?? {};
            config.headers.Authorization = `Bearer ${auth.token}`;
        }
        return config;
    });
    instance.interceptors.response.use((resp) => resp, (err) => {
        const status = err.response?.status;
        const msg = err.response?.data?.message || err.message;
        if (status === 401) {
            const auth = useAuthStore();
            auth.logout();
            ElMessage.error('登录已过期，请重新登录');
            if (location.pathname !== '/login')
                location.href = '/login';
        }
        else if (status === 403) {
            ElMessage.error(msg || '无权限');
        }
        else if (status && status >= 500) {
            ElMessage.error(msg || '服务器异常');
        }
        else if (msg) {
            ElMessage.error(msg);
        }
        return Promise.reject(err);
    });
    return instance;
}
