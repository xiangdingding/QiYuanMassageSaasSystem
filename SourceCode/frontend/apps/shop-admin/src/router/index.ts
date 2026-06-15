import { createRouter, createWebHistory, type RouteRecordRaw } from 'vue-router';
import { canSee, useAuthStore } from '@/stores/auth';
import type { UserRole } from '@/api/types';

declare module 'vue-router' {
  interface RouteMeta {
    title?: string;
    public?: boolean;
    roles?: UserRole[];
    icon?: string;
    menu?: boolean;
  }
}

const ALL: UserRole[] = ['ShopOwner', 'StoreManager', 'Cashier', 'Technician'];
const POS: UserRole[] = ['ShopOwner', 'StoreManager', 'Cashier'];
const LEAD: UserRole[] = ['ShopOwner', 'StoreManager'];

const routes: RouteRecordRaw[] = [
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
      // 落地页按角色解析：技师等无权访问 /pos 的角色不能硬跳 /pos，
      // 否则与下方守卫的越权回退会形成 / → /pos → / 的重定向死循环（切换账号卡死）。
      { path: '', redirect: () => firstAllowedPath(useAuthStore().role) },

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
      { path: 'member-types', component: () => import('@/views/MemberTypesView.vue'),
        meta: { title: '会员类型', icon: 'Postcard', menu: true, roles: LEAD } },
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
        meta: { title: '服务订阅', icon: 'CreditCard', menu: true, roles: ['ShopOwner'] } }
    ]
  },
  { path: '/:pathMatch(.*)*', redirect: '/' }
];

// 当前角色能访问的第一个菜单页路径；找不到则回登录页。
// 用于落地页解析与越权回退，避免重定向到会再次重定向的 '/'。
function firstAllowedPath(role: UserRole | null): string {
  if (!role) return '/login';
  const layout = routes.find((r) => r.path === '/');
  const hit = (layout?.children ?? []).find((c) => c.meta?.menu && canSee(c.meta?.roles, role));
  return hit ? '/' + (hit.path ?? '') : '/login';
}

export const router = createRouter({
  history: createWebHistory(),
  routes
});

router.beforeEach((to) => {
  const auth = useAuthStore();
  if (!to.meta.public && !auth.isAuthenticated) {
    return { path: '/login', query: { redirect: to.fullPath } };
  }
  if ((to.path === '/login' || to.path === '/register') && auth.isAuthenticated) {
    return { path: firstAllowedPath(auth.role) };
  }
  if (to.meta.roles && !canSee(to.meta.roles, auth.role)) {
    // 跳到该角色合法首页（具体路径），不要回 '/'——否则会再次触发重定向循环。
    const home = firstAllowedPath(auth.role);
    return home === to.path ? false : { path: home };
  }
  return true;
});

// 路由切换后：更新文档标题（屏幕阅读器会朗读），把焦点放回 #main-content
router.afterEach((to) => {
  const title = (to.meta.title as string | undefined) ?? '';
  document.title = title ? `${title} - 按摩店管理` : '按摩店管理';
  // 等 router-view 渲染完
  setTimeout(() => {
    const main = document.getElementById('main-content');
    if (main) main.focus();
  }, 50);
});
