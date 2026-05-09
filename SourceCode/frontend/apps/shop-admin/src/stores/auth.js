import { defineStore } from 'pinia';
const TOKEN_KEY = 'massage_saas_shop_token';
const USER_KEY = 'massage_saas_shop_user';
export const useAuthStore = defineStore('auth', {
    state: () => ({
        token: localStorage.getItem(TOKEN_KEY),
        user: JSON.parse(localStorage.getItem(USER_KEY) ?? 'null'),
        expiresAt: null
    }),
    getters: {
        isAuthenticated: (s) => !!s.token,
        role: (s) => s.user?.role ?? null,
        isOwner: (s) => s.user?.role === 'ShopOwner',
        isManager: (s) => s.user?.role === 'StoreManager',
        isCashier: (s) => s.user?.role === 'Cashier',
        isTechnician: (s) => s.user?.role === 'Technician'
    },
    actions: {
        setSession(token, user, expiresAt) {
            this.token = token;
            this.user = user;
            this.expiresAt = expiresAt;
            localStorage.setItem(TOKEN_KEY, token);
            localStorage.setItem(USER_KEY, JSON.stringify(user));
        },
        logout() {
            this.token = null;
            this.user = null;
            this.expiresAt = null;
            localStorage.removeItem(TOKEN_KEY);
            localStorage.removeItem(USER_KEY);
        }
    }
});
const ALL = ['ShopOwner', 'StoreManager', 'Cashier', 'Technician'];
const SHOP_LEADERSHIP = ['ShopOwner', 'StoreManager'];
const NEED_POS = ['ShopOwner', 'StoreManager', 'Cashier'];
export const ROLE_GROUPS = { ALL, SHOP_LEADERSHIP, NEED_POS };
export function canSee(roles, current) {
    if (!roles || roles.length === 0)
        return true;
    if (!current)
        return false;
    return roles.includes(current);
}
