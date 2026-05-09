/// <reference types="vite/client" />

declare module '*.vue' {
  import type { DefineComponent } from 'vue';
  const component: DefineComponent<{}, {}, any>;
  export default component;
}

declare module 'element-plus/dist/locale/zh-cn.mjs' {
  const locale: import('element-plus/es/locale').Language;
  export default locale;
}
