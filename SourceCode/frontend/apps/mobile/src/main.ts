import { createApp } from 'vue';
import { createPinia } from 'pinia';
import App from './App.vue';
import { router } from './router';

// Vant 按需引入：组件在各 .vue 里 import，这里只引全局样式与基础样式重置。
import 'vant/lib/index.css';
import './styles/global.css';

const app = createApp(App);
app.use(createPinia());
app.use(router);

// http 拦截器里 401 时派发 auth:unauthorized，这里统一跳登录（避免在 store 里耦合 router）。
window.addEventListener('auth:unauthorized', () => {
  if (router.currentRoute.value.path !== '/login') router.replace('/login');
});

app.mount('#app');
