<template>
  <el-container class="layout">
    <el-aside width="220px" class="aside">
      <div class="brand">{{ activeStoreName || '按摩店' }}</div>
      <el-menu
        :default-active="route.path"
        router
        class="menu"
        background-color="#1f2d3d"
        text-color="#bfcbd9"
        active-text-color="#ffd04b"
      >
        <template v-for="item in visibleMenu" :key="item.path">
          <el-menu-item :index="item.path">
            <el-icon v-if="item.icon"><component :is="iconCmp(item.icon)" /></el-icon>
            <span>{{ item.title }}</span>
          </el-menu-item>
        </template>
      </el-menu>
    </el-aside>
    <el-container>
      <el-header class="header">
        <div class="header-left">
          <span class="page-title">{{ pageTitle }}</span>
          <el-tag v-if="expireWarn" type="warning" size="small">
            订阅 {{ subStore.daysToExpire }} 天后到期
          </el-tag>
          <el-tag v-if="subStore.expired" type="danger" size="small">
            订阅已到期，仅支持只读
          </el-tag>
        </div>
        <div class="header-right">
          <el-select
            v-if="appStore.stores.length > 1"
            :model-value="appStore.activeStoreId"
            size="small"
            style="width: 160px; margin-right: 12px"
            @change="(v: number) => appStore.setActiveStore(v)"
          >
            <el-option
              v-for="s in appStore.stores"
              :key="s.id"
              :label="s.name + (s.isHeadquarters ? '（总店）' : '')"
              :value="s.id"
            />
          </el-select>
          <el-dropdown trigger="click" @command="onCommand">
            <span class="user">
              <el-icon><UserFilled /></el-icon>
              {{ auth.user?.realName || auth.user?.username }}
              <el-tag size="small" effect="plain">{{ roleLabel }}</el-tag>
            </span>
            <template #dropdown>
              <el-dropdown-menu>
                <el-dropdown-item command="logout">退出登录</el-dropdown-item>
              </el-dropdown-menu>
            </template>
          </el-dropdown>
        </div>
      </el-header>
      <el-main class="main">
        <router-view />
      </el-main>
    </el-container>
  </el-container>
</template>

<script setup lang="ts">
import { computed, onMounted, reactive } from 'vue';
import { useRoute, useRouter } from 'vue-router';
import {
  Avatar,
  Calendar,
  CreditCard,
  Goods,
  House,
  List,
  Money,
  OfficeBuilding,
  Tickets,
  TrendCharts,
  User,
  UserFilled,
  Wallet
} from '@element-plus/icons-vue';
import { canSee, useAuthStore } from '@/stores/auth';
import { useAppStore } from '@/stores/app';
import { subscriptionsApi } from '@/api/modules';
import type { UserRole } from '@/api/types';

const route = useRoute();
const router = useRouter();
const auth = useAuthStore();
const appStore = useAppStore();

const subStore = reactive<{ daysToExpire: number | null; expired: boolean; status: string | null }>({
  daysToExpire: null,
  expired: false,
  status: null
});

const ROLE_LABELS: Record<UserRole, string> = {
  PlatformAdmin: '平台管理员',
  ShopOwner: '店主',
  StoreManager: '店长',
  Cashier: '收银员',
  Technician: '技师'
};
const roleLabel = computed(() => (auth.user?.role ? ROLE_LABELS[auth.user.role] : ''));

const ICONS: Record<string, any> = {
  Avatar, Calendar, CreditCard, Goods, House, List, Money, OfficeBuilding,
  Tickets, TrendCharts, User, UserFilled, Wallet
};
function iconCmp(name: string) {
  return ICONS[name] ?? Tickets;
}

interface MenuItem { path: string; title: string; icon?: string; roles?: UserRole[] }

const visibleMenu = computed<MenuItem[]>(() => {
  const layout = router.options.routes.find((r) => r.path === '/');
  if (!layout?.children) return [];
  return layout.children
    .filter((c) => c.meta?.menu)
    .map((c) => ({
      path: '/' + (c.path || ''),
      title: c.meta?.title as string,
      icon: c.meta?.icon as string,
      roles: c.meta?.roles
    }))
    .filter((m) => canSee(m.roles, auth.role));
});

const pageTitle = computed(() => (route.meta.title as string) ?? '');
const activeStoreName = computed(
  () => appStore.stores.find((s) => s.id === appStore.activeStoreId)?.name ?? ''
);
const expireWarn = computed(
  () => subStore.daysToExpire !== null && subStore.daysToExpire > 0 && subStore.daysToExpire <= 30 && !subStore.expired
);

function onCommand(cmd: string) {
  if (cmd === 'logout') {
    auth.logout();
    router.replace('/login');
  }
}

onMounted(async () => {
  await appStore.loadStores().catch(() => null);
  if (auth.user?.role === 'ShopOwner' || auth.user?.role === 'StoreManager') {
    try {
      const sub = await subscriptionsApi.me();
      subStore.daysToExpire = sub.daysToExpire ?? null;
      subStore.status = sub.status;
      subStore.expired = sub.status !== 'Active';
    } catch {
      /* ignore */
    }
  }
});
</script>

<style scoped>
.layout { min-height: 100vh; }
.aside { background: #1f2d3d; color: #bfcbd9; }
.brand {
  height: 60px;
  display: flex;
  align-items: center;
  justify-content: center;
  font-weight: 600;
  font-size: 16px;
  letter-spacing: 1px;
  border-bottom: 1px solid #2c3e50;
  padding: 0 12px;
  text-align: center;
}
.menu { border-right: none; }
.header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  background: #fff;
  border-bottom: 1px solid var(--el-border-color-light);
}
.header-left { display: flex; align-items: center; gap: 12px; }
.page-title { font-weight: 500; font-size: 16px; }
.header-right { display: flex; align-items: center; }
.user { cursor: pointer; display: flex; align-items: center; gap: 6px; }
.main { background: #f5f7fa; }
</style>
