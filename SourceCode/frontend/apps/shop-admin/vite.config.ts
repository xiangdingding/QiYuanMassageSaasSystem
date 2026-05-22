import { defineConfig } from 'vite';
import vue from '@vitejs/plugin-vue';
import { fileURLToPath, URL } from 'node:url';

export default defineConfig({
  plugins: [vue()],
  resolve: {
    alias: {
      '@': fileURLToPath(new URL('./src', import.meta.url))
    },
    // src/ 下既有 .ts 源也有之前 build 产生的 .js 残留，
    // Vite 默认 .js 优先于 .ts，会导致改了 .ts 但运行的还是旧 .js。
    // 这里把 .ts/.tsx 提到 .js 前面，让源码改动总是生效。
    extensions: ['.mjs', '.mts', '.ts', '.tsx', '.js', '.jsx', '.json']
  },
  server: {
    port: 5174,
    proxy: {
      '/api': {
        target: 'http://localhost:5139',
        changeOrigin: true
      }
    }
  }
});
