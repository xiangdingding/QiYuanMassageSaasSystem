<template>
  <router-view />
  <!-- 安卓启动检测升级：有新版本弹窗，支持内置下载 APK 并唤起安装、强制更新拦截 -->
  <AppUpdater />
</template>

<script setup lang="ts">
// 根组件：承载路由出口与升级检测。Capacitor 状态栏等初始化在此可扩展。
import { onMounted } from 'vue';
import AppUpdater from '@/components/AppUpdater.vue';

onMounted(async () => {
  // 原生平台下设置状态栏样式（Web 端忽略）。失败不影响应用运行。
  try {
    const { Capacitor } = await import('@capacitor/core');
    if (Capacitor?.isNativePlatform?.()) {
      const { StatusBar, Style } = await import('@capacitor/status-bar');
      await StatusBar.setStyle({ style: Style.Light });
    }
  } catch {
    /* 非原生或插件未装，忽略 */
  }
});
</script>
