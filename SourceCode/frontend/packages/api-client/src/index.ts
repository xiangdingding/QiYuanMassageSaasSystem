// P1 阶段用 openapi-typescript 或 orval 从 /swagger/v1/swagger.json 生成
// 这里先放手写 axios 基础客户端占位

import axios, { AxiosInstance } from 'axios';

export function createApiClient(baseURL: string, token?: () => string | null): AxiosInstance {
  const instance = axios.create({ baseURL, timeout: 15000 });

  instance.interceptors.request.use((config) => {
    const t = token?.();
    if (t) {
      config.headers = config.headers ?? {};
      (config.headers as Record<string, string>).Authorization = `Bearer ${t}`;
    }
    return config;
  });

  return instance;
}
