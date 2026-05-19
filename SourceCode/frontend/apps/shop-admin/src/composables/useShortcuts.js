import { onMounted, onUnmounted } from 'vue';
export function useShortcuts(handlers) {
    function handle(e) {
        // 输入控件中不抢键（除了 F2/F5 这种明确的功能键）
        const target = e.target;
        const inEditable = !!target && (target.tagName === 'INPUT' ||
            target.tagName === 'TEXTAREA' ||
            target.isContentEditable);
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
        if (inEditable)
            return;
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
