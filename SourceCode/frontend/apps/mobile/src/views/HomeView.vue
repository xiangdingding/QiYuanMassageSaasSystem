<template>
  <div class="qy-page">
    <div class="home-head qy-brand-bg">
      <div class="head-row">
        <div class="hello">
          <p class="hi">{{ greeting }}，{{ auth.user?.realName || auth.user?.username }}</p>
          <p class="role">{{ roleLabel }}</p>
        </div>
        <div v-if="appStore.stores.length" class="store-switch" @click="showStorePicker = true">
          <van-icon name="shop-o" />
          <span class="store-name">{{ appStore.activeStore?.name || '选择门店' }}</span>
          <van-icon name="arrow-down" />
        </div>
      </div>

      <div v-if="subWarn" class="sub-warn">
        <van-icon name="warning-o" /> {{ subWarn }}
      </div>
    </div>

    <van-pull-refresh v-model="refreshing" @refresh="loadAll">
      <!-- 技师：我的上钟状态 -->
      <div v-if="auth.isTechnician" class="card my-queue">
        <div class="mq-state">
          <span class="mq-label">我的状态</span>
          <van-tag :type="stateTagType" size="large">{{ myStateLabel }}</van-tag>
        </div>
        <p class="mq-round">今日接钟 <b>{{ myQueue?.todayRoundCount ?? 0 }}</b> 钟 · 排位 {{ myQueue?.queuePosition ?? '-' }}</p>
        <div v-if="myQueue?.currentRoomNo" class="mq-current">
          当前：{{ myQueue?.currentRoomNo }} 房 · {{ myQueue?.currentServiceName || '服务中' }}
        </div>
        <div class="mq-actions">
          <van-button size="small" type="primary" :loading="acting" @click="setMyState('OnDuty')">上钟</van-button>
          <van-button size="small" :loading="acting" @click="setMyState('Resting')">休息</van-button>
          <van-button size="small" :loading="acting" @click="setMyState('OffDuty')">下班</van-button>
        </div>
      </div>

      <!-- POS 角色：今日经营 -->
      <div v-if="canSeeKpi" class="card kpi">
        <div class="kpi-title">
          <span>今日经营</span>
          <span class="kpi-date">{{ today }}</span>
        </div>
        <div class="kpi-main">
          <span class="kpi-money qy-money">¥ {{ fmt(daily?.revenue) }}</span>
          <span class="kpi-sub">营业额</span>
        </div>
        <div class="kpi-grid">
          <div><b>{{ daily?.orderCount ?? 0 }}</b><span>订单</span></div>
          <div><b class="qy-money">{{ fmt(daily?.memberRechargeAmount) }}</b><span>充值</span></div>
          <div><b class="qy-money">{{ fmt(daily?.cashAmount) }}</b><span>现金</span></div>
          <div><b class="qy-money">{{ fmt(payOnline) }}</b><span>微/支</span></div>
        </div>
      </div>

      <p class="qy-section-title">常用功能</p>
      <van-grid :column-num="4" :border="false" class="nav-grid">
        <van-grid-item
          v-for="m in navItems"
          :key="m.key"
          :icon="m.icon"
          :text="m.title"
          @click="goModule(m)"
        />
      </van-grid>
    </van-pull-refresh>

    <!-- 门店切换 -->
    <van-popup v-model:show="showStorePicker" position="bottom" round>
      <van-picker
        :columns="storeColumns"
        :model-value="storePickerValue"
        @confirm="onPickStore"
        @cancel="showStorePicker = false"
      />
    </van-popup>
  </div>
</template>

<script setup lang="ts">
import { computed, onMounted, ref } from 'vue';
import { useRouter } from 'vue-router';
import {
  Icon as VanIcon, Tag as VanTag, Button as VanButton, PullRefresh as VanPullRefresh,
  Grid as VanGrid, GridItem as VanGridItem, Popup as VanPopup, Picker as VanPicker,
  showSuccessToast
} from 'vant';
import { reportsApi, subscriptionsApi, queueApi } from '@/api/modules';
import { canSee, useAuthStore } from '@/stores/auth';
import { useAppStore } from '@/stores/app';
import { ROLE_LABELS, type DailyReport, type MyQueue, type SubscriptionStatus, type UserRole } from '@/api/types';

const router = useRouter();
const auth = useAuthStore();
const appStore = useAppStore();

const refreshing = ref(false);
const acting = ref(false);
const daily = ref<DailyReport | null>(null);
const myQueue = ref<MyQueue | null>(null);
const sub = ref<SubscriptionStatus | null>(null);
const showStorePicker = ref(false);

const today = new Date().toLocaleDateString('zh-CN');
const greeting = computed(() => {
  const h = new Date().getHours();
  if (h < 6) return '凌晨好';
  if (h < 12) return '早上好';
  if (h < 14) return '中午好';
  if (h < 18) return '下午好';
  return '晚上好';
});
const roleLabel = computed(() => (auth.role ? ROLE_LABELS[auth.role] : ''));
const canSeeKpi = computed(() => auth.role !== 'Technician');

const payOnline = computed(() => (daily.value ? daily.value.wechatAmount + daily.value.alipayAmount : 0));

const myStateLabel = computed(() => {
  const map: Record<string, string> = { Idle: '空闲', OnDuty: '上钟中', Resting: '休息', OffDuty: '下班' };
  return myQueue.value ? map[myQueue.value.state] ?? myQueue.value.state : '未上钟';
});
const stateTagType = computed(() => {
  switch (myQueue.value?.state) {
    case 'OnDuty': return 'success';
    case 'Resting': return 'warning';
    case 'OffDuty': return 'default';
    default: return 'primary';
  }
});

const subWarn = computed(() => {
  if (!sub.value) return '';
  const d = sub.value.daysToExpire;
  if (sub.value.status === 'Expired') return '订阅已到期，仅支持查询，请联系平台续费';
  if (d != null && d <= 15) return `订阅将在 ${d} 天后到期，请及时续费`;
  return '';
});

interface NavItem { key: string; title: string; icon: string; path?: string; roles?: UserRole[]; }
const POS_ROLES: UserRole[] = ['ShopOwner', 'StoreManager', 'Cashier'];
const LEAD_ROLES: UserRole[] = ['ShopOwner', 'StoreManager'];
const OWNER_ROLES: UserRole[] = ['ShopOwner'];

// 与 BS 端菜单同源、按角色解析。技师走自助宫格；其余角色按 roles 过滤。
const allNav: NavItem[] = [
  { key: 'pos', title: '收银台', icon: 'cart-o', path: '/pos', roles: POS_ROLES },
  { key: 'appointments', title: '预约', icon: 'calendar-o', path: '/appointments', roles: POS_ROLES },
  { key: 'orders', title: '订单流水', icon: 'orders-o', path: '/orders', roles: POS_ROLES },
  { key: 'rooms', title: '房间', icon: 'home-o', path: '/rooms', roles: POS_ROLES },
  { key: 'members', title: '会员', icon: 'friends-o', path: '/members', roles: POS_ROLES },
  { key: 'member-types', title: '会员类型', icon: 'card', path: '/member-types', roles: LEAD_ROLES },
  { key: 'queue', title: '技师排队', icon: 'exchange', path: '/queue' },
  { key: 'reports', title: '报表', icon: 'bar-chart-o', path: '/reports', roles: POS_ROLES },
  { key: 'day-close', title: '日结交班', icon: 'balance-o', path: '/day-close', roles: POS_ROLES },
  { key: 'vouchers', title: '优惠券', icon: 'coupon-o', path: '/vouchers', roles: POS_ROLES },
  { key: 'inventory', title: '物耗库存', icon: 'goods-collect-o', path: '/inventory', roles: POS_ROLES },
  { key: 'reviews', title: '服务评价', icon: 'star-o', path: '/reviews', roles: POS_ROLES },
  { key: 'complaints', title: '投诉处理', icon: 'warning-o', path: '/complaints', roles: POS_ROLES },
  { key: 'services', title: '服务项目', icon: 'shopping-cart-o', path: '/services', roles: LEAD_ROLES },
  { key: 'staff', title: '员工管理', icon: 'manager-o', path: '/staff', roles: LEAD_ROLES },
  { key: 'schedules', title: '排班请假', icon: 'clock-o', path: '/schedules', roles: LEAD_ROLES },
  { key: 'commissions', title: '提成规则', icon: 'gold-coin-o', path: '/commissions', roles: LEAD_ROLES },
  { key: 'payroll', title: '工资结算', icon: 'balance-o', path: '/payroll', roles: LEAD_ROLES },
  { key: 'stores', title: '门店管理', icon: 'shop-o', path: '/stores', roles: OWNER_ROLES },
  { key: 'subscription', title: '服务订阅', icon: 'vip-card-o', path: '/subscription', roles: OWNER_ROLES }
];
const techNav: NavItem[] = [
  { key: 'queue', title: '技师排队', icon: 'exchange', path: '/queue' },
  { key: 'performance', title: '我的业绩', icon: 'gold-coin-o', path: '/me/performance' },
  { key: 'reviews', title: '我的评价', icon: 'star-o', path: '/me/reviews' },
  { key: 'payroll', title: '我的工资', icon: 'balance-o', path: '/me/payroll' }
];
const navItems = computed(() => {
  if (auth.isTechnician) return techNav;
  return allNav.filter((n) => canSee(n.roles, auth.role));
});

const storeColumns = computed(() =>
  appStore.stores.map((s) => ({ text: s.name, value: s.id }))
);
const storePickerValue = computed<number[]>(() =>
  appStore.activeStoreId != null ? [appStore.activeStoreId] : []
);

function fmt(n?: number | null): string {
  return (n ?? 0).toLocaleString('zh-CN', { minimumFractionDigits: 2, maximumFractionDigits: 2 });
}

function goModule(m: NavItem) {
  if (m.path) router.push(m.path);
  else router.push(`/more/${m.key}`);
}

function onPickStore({ selectedValues }: { selectedValues: (number | undefined)[] }) {
  const id = selectedValues[0];
  showStorePicker.value = false;
  if (typeof id === 'number' && id !== appStore.activeStoreId) {
    appStore.setActiveStore(id);
    loadAll();
  }
}

async function setMyState(state: string) {
  acting.value = true;
  try {
    await queueApi.setMyState(state);
    myQueue.value = await queueApi.me();
    showSuccessToast('已更新');
  } catch {
    /* ignore */
  } finally {
    acting.value = false;
  }
}

async function loadAll() {
  try {
    if (auth.isTechnician) {
      myQueue.value = await queueApi.me().catch(() => null);
    }
    if (canSeeKpi.value && appStore.activeStoreId) {
      daily.value = await reportsApi.daily(appStore.activeStoreId).catch(() => null);
    }
    if (auth.isLeadership) {
      sub.value = await subscriptionsApi.me().catch(() => null);
    }
  } finally {
    refreshing.value = false;
  }
}

onMounted(async () => {
  if (!appStore.stores.length) await appStore.loadStores().catch(() => undefined);
  loadAll();
});
</script>

<style scoped>
.home-head { padding: 18px 16px 20px; border-radius: 0 0 18px 18px; }
.head-row { display: flex; align-items: center; justify-content: space-between; }
.hello .hi { margin: 0; font-size: 18px; font-weight: 600; }
.hello .role { margin: 4px 0 0; font-size: 13px; opacity: 0.85; }
.store-switch {
  display: flex; align-items: center; gap: 4px;
  background: rgba(255, 255, 255, 0.18); padding: 6px 10px; border-radius: 16px; font-size: 13px;
}
.store-name { max-width: 110px; overflow: hidden; text-overflow: ellipsis; white-space: nowrap; }
.sub-warn {
  margin-top: 14px; background: rgba(255, 255, 255, 0.16); border-radius: 10px;
  padding: 8px 12px; font-size: 13px;
}
.card { background: #fff; margin: 12px; border-radius: 14px; padding: 16px; box-shadow: 0 6px 18px -12px rgba(16,42,67,.25); }
.my-queue { margin-top: 14px; }
.mq-state { display: flex; align-items: center; justify-content: space-between; }
.mq-label { color: #6b7280; font-size: 14px; }
.mq-round { margin: 12px 0 0; color: #4b5563; font-size: 14px; }
.mq-round b { color: var(--qy-brand); font-size: 18px; }
.mq-current { margin-top: 8px; font-size: 13px; color: #8a94a6; }
.mq-actions { display: flex; gap: 10px; margin-top: 14px; }
.mq-actions .van-button { flex: 1; }
.kpi-title { display: flex; justify-content: space-between; color: #6b7280; font-size: 14px; }
.kpi-date { color: #b0b8c4; }
.kpi-main { margin: 14px 0 16px; display: flex; align-items: baseline; gap: 10px; }
.kpi-money { font-size: 30px; font-weight: 800; color: #16324a; }
.kpi-sub { color: #98a2b3; font-size: 13px; }
.kpi-grid { display: grid; grid-template-columns: repeat(4, 1fr); gap: 8px; }
.kpi-grid div { background: #f5f7f9; border-radius: 10px; padding: 10px 6px; text-align: center; }
.kpi-grid b { display: block; font-size: 16px; color: #16324a; }
.kpi-grid span { font-size: 12px; color: #98a2b3; }
.nav-grid { margin-bottom: 8px; }
</style>
