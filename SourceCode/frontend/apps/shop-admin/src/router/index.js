import { createRouter, createWebHistory } from 'vue-router';
import { canSee, useAuthStore } from '@/stores/auth';
const ALL = ['ShopOwner', 'StoreManager', 'Cashier', 'Technician'];
const POS = ['ShopOwner', 'StoreManager', 'Cashier'];
const LEAD = ['ShopOwner', 'StoreManager'];
const routes = [
    {
        path: '/login',
        component: () => import('@/views/LoginView.vue'),
        meta: { public: true }
    },
    {
        path: '/',
        component: () => import('@/layouts/MainLayout.vue'),
        children: [
            { path: '', redirect: '/pos' },
            { path: 'pos', component: () => import('@/views/PosView.vue'),
                meta: { title: '收银台', icon: 'Tickets', menu: true, roles: POS } },
            { path: 'orders', component: () => import('@/views/OrdersView.vue'),
                meta: { title: '订单流水', icon: 'List', menu: true, roles: POS } },
            { path: 'members', component: () => import('@/views/MembersView.vue'),
                meta: { title: '会员管理', icon: 'User', menu: true, roles: POS } },
            { path: 'queue', component: () => import('@/views/QueueView.vue'),
                meta: { title: '技师排队', icon: 'Avatar', menu: true, roles: ALL } },
            { path: 'reports', component: () => import('@/views/ReportsView.vue'),
                meta: { title: '日报与业绩', icon: 'TrendCharts', menu: true, roles: POS } },
            { path: 'services', component: () => import('@/views/ServicesView.vue'),
                meta: { title: '服务项目', icon: 'Goods', menu: true, roles: LEAD } },
            { path: 'commissions', component: () => import('@/views/CommissionsView.vue'),
                meta: { title: '提成规则', icon: 'Money', menu: true, roles: LEAD } },
            { path: 'staff', component: () => import('@/views/StaffView.vue'),
                meta: { title: '员工管理', icon: 'UserFilled', menu: true, roles: LEAD } },
            { path: 'stores', component: () => import('@/views/StoresView.vue'),
                meta: { title: '门店管理', icon: 'OfficeBuilding', menu: true, roles: ['ShopOwner'] } },
            { path: 'subscription', component: () => import('@/views/SubscriptionView.vue'),
                meta: { title: '订阅状态', icon: 'CreditCard', menu: true, roles: ['ShopOwner'] } }
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
    if (to.path === '/login' && auth.isAuthenticated)
        return { path: '/' };
    if (to.meta.roles && !canSee(to.meta.roles, auth.role)) {
        return { path: '/' };
    }
    return true;
});
