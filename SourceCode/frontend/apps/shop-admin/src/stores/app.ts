import { defineStore } from 'pinia';
import { storesApi } from '@/api/modules';
import type { Store } from '@/api/types';

interface AppState {
  stores: Store[];
  activeStoreId: number | null;
}

export const useAppStore = defineStore('app', {
  state: (): AppState => ({
    stores: [],
    activeStoreId: Number(localStorage.getItem('massage_saas_active_store') ?? '0') || null
  }),
  actions: {
    async loadStores(force = false) {
      if (this.stores.length > 0 && !force) return;
      this.stores = await storesApi.list();
      // 默认选中总店：店主/店长登录首次进入应看到总店全店数据，
      // 分店只有手动切换才进入。
      const stillValid =
        this.activeStoreId != null && this.stores.some((s) => s.id === this.activeStoreId);
      if (!stillValid && this.stores.length > 0) {
        const hq = this.stores.find((s) => s.isHeadquarters) ?? this.stores[0];
        this.setActiveStore(hq.id);
      }
    },
    setActiveStore(id: number) {
      this.activeStoreId = id;
      localStorage.setItem('massage_saas_active_store', String(id));
    },
    reset() {
      this.stores = [];
      this.activeStoreId = null;
      localStorage.removeItem('massage_saas_active_store');
    }
  }
});
