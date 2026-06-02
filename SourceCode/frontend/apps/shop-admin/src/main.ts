import { createApp } from 'vue';
import { createPinia } from 'pinia';
import ElementPlus from 'element-plus';
import zhCn from 'element-plus/dist/locale/zh-cn.mjs';
import 'element-plus/dist/index.css';
import App from './App.vue';
import { router } from './router';
import { usePrefsStore } from '@/stores/prefs';

declare module 'vue' {
  interface ComponentCustomProperties {
    /** 盲人模式下加宽「操作」列宽度（普通模式原样返回） */
    $actCol: (base: number) => number;
  }
}

const app = createApp(App);
app.use(createPinia());
app.use(router);
app.use(ElementPlus, { locale: zhCn });

// 全局 $actCol：盲人模式下把「操作」列宽按 1.6× 加宽，避免按钮换行。
// 模板里直接 :width="$actCol(200)" 使用，随 a11y 模式切换响应式更新。
app.config.globalProperties.$actCol = (base: number): number => {
  const prefs = usePrefsStore();
  return prefs.isA11y ? Math.round(base * 1.6) : base;
};

app.mount('#app');
