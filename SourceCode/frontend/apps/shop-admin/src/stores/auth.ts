import { defineStore } from 'pinia';
import type { UserInfo, UserRole } from '@/api/types';

const TOKEN_KEY = 'massage_saas_shop_token';
const USER_KEY = 'massage_saas_shop_user';

interface AuthState {
  token: string | null;
  user: UserInfo | null;
  expiresAt: string | null;
}

export const useAuthStore = defineStore('auth', {
  state: (): AuthState => ({
    token: localStorage.getItem(TOKEN_KEY),
    user: JSON.parse(localStorage.getItem(USER_KEY) ?? 'null'),
    expiresAt: null
  }),
  getters: {
    isAuthenticated: (s) => !!s.token,
    role: (s): UserRole | null => s.user?.role ?? null,
    isOwner: (s) => s.user?.role === 'ShopOwner',
    isManager: (s) => s.user?.role === 'StoreManager',
    isCashier: (s) => s.user?.role === 'Cashier',
    isTechnician: (s) => s.user?.role === 'Technician'
  },
  actions: {
    setSession(token: string, user: UserInfo, expiresAt: string) {
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

const ALL: UserRole[] = ['ShopOwner', 'StoreManager', 'Cashier', 'Technician'];
const SHOP_LEADERSHIP: UserRole[] = ['ShopOwner', 'StoreManager'];
const NEED_POS: UserRole[] = ['ShopOwner', 'StoreManager', 'Cashier'];

export const ROLE_GROUPS = { ALL, SHOP_LEADERSHIP, NEED_POS };

export function canSee(roles: UserRole[] | undefined, current: UserRole | null): boolean {
  if (!roles || roles.length === 0) return true;
  if (!current) return false;
  return roles.includes(current);
}
