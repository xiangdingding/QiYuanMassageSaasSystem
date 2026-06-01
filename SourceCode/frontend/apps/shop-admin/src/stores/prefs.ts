import { defineStore } from 'pinia';

/// 无障碍模式偏好：'a11y' 时给 <html> 加 data-a11y="a11y"，全局样式据此放大字号、加粗焦点、
/// 强制视口锁定让屏幕阅读器一次能扫完整页面。持久化到 localStorage，跨刷新保留。
export type A11yMode = 'normal' | 'a11y';
const STORAGE_KEY = 'massage_saas_a11y_mode';

interface PrefsState {
  a11yMode: A11yMode;
}

function readInitial(): A11yMode {
  try {
    const v = localStorage.getItem(STORAGE_KEY);
    return v === 'a11y' ? 'a11y' : 'normal';
  } catch {
    return 'normal';
  }
}

/// 把当前模式写到 <html data-a11y>，全局 a11y.css 据此命中选择器。
/// 单独抽出来给登录页 / 主框架两处复用。
export function applyA11yModeToDom(mode: A11yMode) {
  const root = document.documentElement;
  if (mode === 'a11y') root.setAttribute('data-a11y', 'a11y');
  else root.removeAttribute('data-a11y');
}

export const usePrefsStore = defineStore('prefs', {
  state: (): PrefsState => ({ a11yMode: readInitial() }),
  getters: {
    isA11y: (s) => s.a11yMode === 'a11y'
  },
  actions: {
    setA11yMode(m: A11yMode) {
      this.a11yMode = m;
      try { localStorage.setItem(STORAGE_KEY, m); } catch { /* 隐私模式可能拒写，忽略 */ }
      applyA11yModeToDom(m);
    },
    toggle() {
      this.setA11yMode(this.a11yMode === 'a11y' ? 'normal' : 'a11y');
    }
  }
});
