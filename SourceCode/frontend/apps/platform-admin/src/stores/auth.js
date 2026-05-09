import { defineStore } from 'pinia';
const TOKEN_KEY = 'massage_saas_platform_token';
const USER_KEY = 'massage_saas_platform_user';
export const useAuthStore = defineStore('auth', {
    state: () => ({
        token: localStorage.getItem(TOKEN_KEY),
        user: JSON.parse(localStorage.getItem(USER_KEY) ?? 'null'),
        expiresAt: null
    }),
    getters: {
        isAuthenticated: (s) => !!s.token,
        isPlatformAdmin: (s) => s.user?.role === 'PlatformAdmin'
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
