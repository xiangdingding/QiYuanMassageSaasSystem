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
const LEADERSHIP: UserRole[] = ['ShopOwner', 'StoreManager'];
const OWNER: UserRole[] = ['ShopOwner'];
const TECH: UserRole[] = ['Technician'];

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
      // POS 角色业务页
      { path: 'pos', component: () => import('@/views/PosView.vue'), meta: { title: '收银台', roles: POS } },
      { path: 'orders', component: () => import('@/views/OrdersView.vue'), meta: { title: '订单流水', roles: POS } },
      { path: 'appointments', component: () => import('@/views/AppointmentsView.vue'), meta: { title: '预约', roles: POS } },
      { path: 'rooms', component: () => import('@/views/RoomsView.vue'), meta: { title: '房间', roles: POS } },
      { path: 'day-close', component: () => import('@/views/DayCloseView.vue'), meta: { title: '日结/交班', roles: POS } },
      { path: 'vouchers', component: () => import('@/views/VouchersView.vue'), meta: { title: '优惠券', roles: POS } },
      { path: 'inventory', component: () => import('@/views/InventoryView.vue'), meta: { title: '物耗库存', roles: POS } },
      { path: 'reviews', component: () => import('@/views/ReviewsView.vue'), meta: { title: '服务评价', roles: POS } },
      { path: 'complaints', component: () => import('@/views/ComplaintsView.vue'), meta: { title: '投诉处理', roles: POS } },
      { path: 'reports', component: () => import('@/views/ReportsView.vue'), meta: { title: '报表', roles: POS } },
      { path: 'services', component: () => import('@/views/ServicesView.vue'), meta: { title: '服务项目', roles: LEADERSHIP } },
      { path: 'member-types', component: () => import('@/views/MemberTypesView.vue'), meta: { title: '会员类型', roles: LEADERSHIP } },
      { path: 'staff', component: () => import('@/views/StaffView.vue'), meta: { title: '员工管理', roles: LEADERSHIP } },
      { path: 'schedules', component: () => import('@/views/SchedulesView.vue'), meta: { title: '排班与请假', roles: LEADERSHIP } },
      { path: 'commissions', component: () => import('@/views/CommissionsView.vue'), meta: { title: '提成规则', roles: LEADERSHIP } },
      { path: 'payroll', component: () => import('@/views/PayrollView.vue'), meta: { title: '工资结算', roles: LEADERSHIP } },
      { path: 'stores', component: () => import('@/views/StoresView.vue'), meta: { title: '门店管理', roles: OWNER } },
      { path: 'subscription', component: () => import('@/views/SubscriptionView.vue'), meta: { title: '服务订阅', roles: OWNER } },
      // 技师自助页
      { path: 'me/performance', component: () => import('@/views/MyPerformanceView.vue'), meta: { title: '我的业绩', roles: TECH } },
      { path: 'me/payroll', component: () => import('@/views/MyPayrollView.vue'), meta: { title: '我的工资', roles: TECH } },
      { path: 'me/reviews', component: () => import('@/views/MyReviewsView.vue'), meta: { title: '我的评价', roles: TECH } },
      // 仍占位（按迭代补全）：收银台等
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
