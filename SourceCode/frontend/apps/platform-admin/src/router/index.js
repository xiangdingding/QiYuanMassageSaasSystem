import { createRouter, createWebHistory } from 'vue-router';
import { useAuthStore } from '@/stores/auth';
const routes = [
    {
        path: '/login',
        name: 'login',
        component: () => import('@/views/LoginView.vue'),
        meta: { public: true }
    },
    {
        path: '/',
        component: () => import('@/layouts/MainLayout.vue'),
        children: [
            { path: '', name: 'dashboard', component: () => import('@/views/DashboardView.vue'), meta: { title: '运营大盘' } },
            { path: 'revenue', name: 'revenue', component: () => import('@/views/RevenueView.vue'), meta: { title: '营收报表' } },
            { path: 'tenants', name: 'tenants', component: () => import('@/views/TenantsView.vue'), meta: { title: '按摩店租户' } },
            { path: 'plans', name: 'plans', component: () => import('@/views/PlansView.vue'), meta: { title: '套餐管理' } }
        ]
    },
    { path: '/:pathMatch(.*)*', redirect: '/' }
];
export const router = createRouter({
    history: createWebHistory(),
    routes
});
router.beforeEach((to) => {
    const auth = useAuthStore();
    if (!to.meta.public && !auth.isAuthenticated) {
        return { path: '/login', query: { redirect: to.fullPath } };
    }
    if (to.path === '/login' && auth.isAuthenticated) {
        return { path: '/' };
    }
    return true;
});
