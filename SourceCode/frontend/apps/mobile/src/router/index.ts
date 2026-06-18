import { createRouter, createWebHashHistory, type RouteRecordRaw } from 'vue-router';
import { canSee, useAuthStore } from '@/stores/auth';
import type { UserRole } from '@/api/types';

declare module 'vue-router' {
  interface RouteMeta {
    title?: string;
    public?: boolean;
    roles?: UserRole[];
  }
}

const ALL: UserRole[] = ['ShopOwner', 'StoreManager', 'Cashier', 'Technician'];
const POS: UserRole[] = ['ShopOwner', 'StoreManager', 'Cashier'];

const routes: RouteRecordRaw[] = [
  { path: '/login', component: () => import('@/views/LoginView.vue'), meta: { public: true, title: '登录' } },
  {
    path: '/',
    component: () => import('@/layouts/TabLayout.vue'),
    children: [
      { path: '', redirect: () => firstAllowedPath(useAuthStore().role) },
      { path: 'home', component: () => import('@/views/HomeView.vue'), meta: { title: '首页', roles: ALL } },
      { path: 'members', component: () => import('@/views/MembersView.vue'), meta: { title: '会员', roles: POS } },
      { path: 'queue', component: () => import('@/views/QueueView.vue'), meta: { title: '技师排队', roles: ALL } },
      { path: 'profile', component: () => import('@/views/ProfileView.vue'), meta: { title: '我的', roles: ALL } },
      { path: 'more/:key', component: () => import('@/views/PlaceholderView.vue'), meta: { title: '功能', roles: ALL } }
    ]
  },
  { path: '/:pathMatch(.*)*', redirect: '/' }
];

export const router = createRouter({
  history: createWebHashHistory(),
  routes
});

export function firstAllowedPath(role: UserRole | null): string {
  if (!role) return '/login';
  return '/home';
}

router.beforeEach((to) => {
  const auth = useAuthStore();
  if (to.meta.public) {
    if (to.path === '/login' && auth.isAuthenticated) return firstAllowedPath(auth.role);
    return true;
  }
  if (!auth.isAuthenticated) return '/login';
  if (to.meta.roles && !canSee(to.meta.roles, auth.role)) {
    return firstAllowedPath(auth.role);
  }
  return true;
});
