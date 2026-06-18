import { defineStore } from 'pinia';
import { storesApi } from '@/api/modules';
import type { Store } from '@/api/types';

const ACTIVE_STORE_KEY = 'massage_saas_mobile_active_store';

interface AppState {
  stores: Store[];
  activeStoreId: number | null;
}

// 门店选择逻辑与 BS 端 stores/app.ts 一致：默认选总店，记忆上次选择。
export const useAppStore = defineStore('app', {
  state: (): AppState => ({
    stores: [],
    activeStoreId: Number(localStorage.getItem(ACTIVE_STORE_KEY) ?? '0') || null
  }),
  getters: {
    activeStore: (s): Store | null => s.stores.find((x) => x.id === s.activeStoreId) ?? null
  },
  actions: {
    async loadStores(force = false) {
      if (this.stores.length > 0 && !force) return;
      this.stores = await storesApi.list();
      const stillValid =
        this.activeStoreId != null && this.stores.some((s) => s.id === this.activeStoreId);
      if (!stillValid && this.stores.length > 0) {
        const hq = this.stores.find((s) => s.isHeadquarters) ?? this.stores[0];
        this.setActiveStore(hq.id);
      }
    },
    setActiveStore(id: number) {
      this.activeStoreId = id;
      localStorage.setItem(ACTIVE_STORE_KEY, String(id));
    },
    reset() {
      this.stores = [];
      this.activeStoreId = null;
      localStorage.removeItem(ACTIVE_STORE_KEY);
    }
  }
});
