import { defineStore } from 'pinia';
import { storesApi } from '@/api/modules';
export const useAppStore = defineStore('app', {
    state: () => ({
        stores: [],
        activeStoreId: Number(localStorage.getItem('massage_saas_active_store') ?? '0') || null
    }),
    actions: {
        async loadStores(force = false) {
            if (this.stores.length > 0 && !force)
                return;
            this.stores = await storesApi.list();
            if (!this.activeStoreId && this.stores.length > 0) {
                this.setActiveStore(this.stores[0].id);
            }
        },
        setActiveStore(id) {
            this.activeStoreId = id;
            localStorage.setItem('massage_saas_active_store', String(id));
        }
    }
});
