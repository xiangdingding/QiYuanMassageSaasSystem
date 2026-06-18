<template>
  <router-view />
</template>

<script setup lang="ts">
// 根组件：仅承载路由出口。Capacitor 状态栏等初始化在此可扩展。
import { onMounted } from 'vue';

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
