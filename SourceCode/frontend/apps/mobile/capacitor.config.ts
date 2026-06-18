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
  }
};

export default config;
