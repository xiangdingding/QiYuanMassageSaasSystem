import { defineConfig } from 'vite';
import vue from '@vitejs/plugin-vue';
import { fileURLToPath, URL } from 'node:url';

// 移动端工程。dev 下走 proxy 连本地后端；打包进 App 后由运行时 apiBase 指向真实服务器。
export default defineConfig({
  plugins: [vue()],
  resolve: {
    alias: {
      '@': fileURLToPath(new URL('./src', import.meta.url))
    }
  },
  server: {
    port: 5175,
    proxy: {
      '/api': {
        target: 'http://localhost:5139',
        changeOrigin: true
      }
    }
  }
});
