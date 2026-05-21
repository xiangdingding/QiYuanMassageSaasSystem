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
        path: '/register',
        component: () => import('@/views/RegisterView.vue'),
        meta: { public: true }
    },
    {
        path: '/',
        component: () => import('@/layouts/MainLayout.vue'),
        children: [
            { path: '', redirect: '/pos' },
            { path: 'pos', component: () => import('@/views/PosView.vue'),
                meta: { title: '收银台', icon: 'Tickets', menu: true, roles: POS } },
            { path: 'appointments', component: () => import('@/views/AppointmentsView.vue'),
                meta: { title: '预约管理', icon: 'Calendar', menu: true, roles: POS } },
            { path: 'orders', component: () => import('@/views/OrdersView.vue'),
                meta: { title: '订单流水', icon: 'List', menu: true, roles: POS } },
            { path: 'rooms', component: () => import('@/views/RoomsView.vue'),
                meta: { title: '房间管理', icon: 'House', menu: true, roles: POS } },
            { path: 'members', component: () => import('@/views/MembersView.vue'),
                meta: { title: '会员管理', icon: 'User', menu: true, roles: POS } },
            { path: 'queue', component: () => import('@/views/QueueView.vue'),
                meta: { title: '技师排队', icon: 'Avatar', menu: true, roles: ALL } },
            { path: 'reports', component: () => import('@/views/ReportsView.vue'),
                meta: { title: '日报与业绩', icon: 'TrendCharts', menu: true, roles: POS } },
            { path: 'day-close', component: () => import('@/views/DayCloseView.vue'),
                meta: { title: '日结/交班', icon: 'Wallet', menu: true, roles: POS } },
            { path: 'services', component: () => import('@/views/ServicesView.vue'),
                meta: { title: '服务项目', icon: 'Goods', menu: true, roles: LEAD } },
            { path: 'member-packages', component: () => import('@/views/MemberPackagesView.vue'),
                meta: { title: '会员套餐', icon: 'CreditCard', menu: true, roles: POS } },
            { path: 'vouchers', component: () => import('@/views/VouchersView.vue'),
                meta: { title: '优惠券', icon: 'Discount', menu: true, roles: POS } },
            { path: 'inventory', component: () => import('@/views/InventoryView.vue'),
                meta: { title: '物耗库存', icon: 'Box', menu: true, roles: POS } },
            { path: 'reviews', component: () => import('@/views/ReviewsView.vue'),
                meta: { title: '服务评价', icon: 'StarFilled', menu: true, roles: POS } },
            { path: 'complaints', component: () => import('@/views/ComplaintsView.vue'),
                meta: { title: '投诉处理', icon: 'WarnTriangleFilled', menu: true, roles: POS } },
            { path: 'schedules', component: () => import('@/views/SchedulesView.vue'),
                meta: { title: '排班与请假', icon: 'AlarmClock', menu: true, roles: LEAD } },
            { path: 'commissions', component: () => import('@/views/CommissionsView.vue'),
                meta: { title: '提成规则', icon: 'Money', menu: true, roles: LEAD } },
            { path: 'payroll', component: () => import('@/views/PayrollView.vue'),
                meta: { title: '工资结算', icon: 'TakeawayBox', menu: true, roles: LEAD } },
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
    if ((to.path === '/login' || to.path === '/register') && auth.isAuthenticated)
        return { path: '/' };
    if (to.meta.roles && !canSee(to.meta.roles, auth.role)) {
        return { path: '/' };
    }
    return true;
});
// 路由切换后：更新文档标题（屏幕阅读器会朗读），把焦点放回 #main-content
router.afterEach((to) => {
    const title = to.meta.title ?? '';
    document.title = title ? `${title} - 按摩店管理` : '按摩店管理';
    // 等 router-view 渲染完
    setTimeout(() => {
        const main = document.getElementById('main-content');
        if (main)
            main.focus();
    }, 50);
});
