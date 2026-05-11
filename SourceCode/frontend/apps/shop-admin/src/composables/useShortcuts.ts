import { onMounted, onUnmounted } from 'vue';

/**
 * 行业级全局快捷键。约定：
 *   F2  → 跳到会员搜索框
 *   F5  → 刷新当前页（默认走浏览器原生 reload，但可被覆盖）
 *   Esc → 关闭最上层弹窗（Element Plus 默认即可，预留 hook）
 *   Ctrl+Enter → 触发 primaryAction（结账等）
 *   Ctrl+S → 触发 saveAction
 *   Ctrl+N → 触发 newAction
 * 用 prevent: true 阻止浏览器默认（F5 慎用，默认不阻止）。
 */
export interface ShortcutHandlers {
  onMemberSearch?: () => void;
  onRefresh?: () => void;
  onPrimary?: () => void;
  onSave?: () => void;
  onNew?: () => void;
}

export function useShortcuts(handlers: ShortcutHandlers) {
  function handle(e: KeyboardEvent) {
    // 输入控件中不抢键（除了 F2/F5 这种明确的功能键）
    const target = e.target as HTMLElement | null;
    const inEditable = !!target && (
      target.tagName === 'INPUT' ||
      target.tagName === 'TEXTAREA' ||
      target.isContentEditable
    );

    if (e.key === 'F2' && handlers.onMemberSearch) {
      e.preventDefault();
      handlers.onMemberSearch();
      return;
    }
    if (e.key === 'F5' && handlers.onRefresh) {
      e.preventDefault();
      handlers.onRefresh();
      return;
    }
    if (inEditable) return;

    if ((e.ctrlKey || e.metaKey) && e.key === 'Enter' && handlers.onPrimary) {
      e.preventDefault();
      handlers.onPrimary();
      return;
    }
    if ((e.ctrlKey || e.metaKey) && e.key.toLowerCase() === 's' && handlers.onSave) {
      e.preventDefault();
      handlers.onSave();
      return;
    }
    if ((e.ctrlKey || e.metaKey) && e.key.toLowerCase() === 'n' && handlers.onNew) {
      e.preventDefault();
      handlers.onNew();
      return;
    }
  }

  onMounted(() => window.addEventListener('keydown', handle));
  onUnmounted(() => window.removeEventListener('keydown', handle));
}
