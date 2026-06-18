import type { CapacitorConfig } from '@capacitor/cli';

const config: CapacitorConfig = {
  appId: 'com.qiyuan.massage.mobile',
  appName: '齐源按摩',
  webDir: 'dist',
  // 远程调试 / 直连后端时可临时开启 server.url 指向 dev server 或线上站点。
  // 正式打包走本地 webDir，由 src/api/config.ts 的 apiBase 指向真实 API 服务器。
  // server: { url: 'http://192.168.1.100:5175', cleartext: true },
  android: {
    allowMixedContent: true
  },
  ios: {
    contentInset: 'always'
  },
  plugins: {
    // 原生 HTTP：让 webview 里的 fetch/XHR(含 axios) 走原生网络，
    // 直连 http://IP:端口 时绕过浏览器 CORS 与明文限制，连局域网后端更稳。
    CapacitorHttp: {
      enabled: true
    }
  }
};

export default config;
