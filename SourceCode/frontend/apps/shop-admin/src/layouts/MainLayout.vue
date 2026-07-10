<template>
  <el-container class="layout">
    <a href="#main-content" class="skip-link" aria-label="跳到主要内容">跳到主要内容</a>
    <el-aside width="220px" class="aside" role="navigation" aria-label="主导航">
      <div class="brand">
        <el-icon class="brand-icon"><OfficeBuilding /></el-icon>
        <span class="brand-text">{{ activeStoreName || '按摩店' }}</span>
      </div>
      <el-menu
        :default-active="route.path"
        router
        class="menu"
        background-color="#1d2b3a"
        text-color="#c7d0d9"
        active-text-color="#ffffff"
      >
        <template v-for="item in visibleMenu" :key="item.path">
          <el-menu-item :index="item.path" :aria-label="item.title + shortcutAria(item.path)">
            <el-icon v-if="item.icon"><component :is="iconCmp(item.icon)" /></el-icon>
            <span>{{ item.title }}</span>
            <span v-if="SHORTCUT_LABELS[item.path]" class="menu-shortcut">{{ SHORTCUT_LABELS[item.path] }}</span>
          </el-menu-item>
        </template>
      </el-menu>
    </el-aside>
    <el-container>
      <el-header class="header" role="banner">
        <div class="header-left">
          <h1 class="page-title" aria-live="polite">{{ pageTitle }}</h1>
          <el-tag v-if="subStore.status === 'Trial' && !subStore.expired" type="info" size="small">
            试用中{{ subStore.daysToExpire != null ? `，剩 ${subStore.daysToExpire} 天` : '' }}
          </el-tag>
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
            aria-label="切换门店"
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
                <el-dropdown-item command="help" aria-label="打开使用帮助，快捷键 F1">使用帮助（F1）</el-dropdown-item>
              <el-dropdown-item command="profile">个人设置</el-dropdown-item>
                <el-dropdown-item command="downloadCs" aria-label="下载电脑客户端安装程序（桌面版）">下载电脑版</el-dropdown-item>
                <el-dropdown-item command="toggleA11y" :aria-label="prefs.isA11y ? '关闭无障碍模式，回到正常显示' : '切换到无障碍模式（字号放大、焦点加粗、读屏优化）'">
                  {{ prefs.isA11y ? '关闭无障碍模式' : '开启无障碍模式' }}
                </el-dropdown-item>
                <el-dropdown-item command="logout" divided>退出登录</el-dropdown-item>
              </el-dropdown-menu>
            </template>
          </el-dropdown>
        </div>
      </el-header>
      <el-main class="main" role="main" id="main-content" tabindex="-1">
        <router-view v-slot="{ Component }">
          <keep-alive :include="['PosView']">
            <component :is="Component" />
          </keep-alive>
        </router-view>
      </el-main>
    </el-container>
    <ProfileDialog v-model="profileVisible" />

    <el-drawer
      v-model="helpVisible"
      title="使用帮助"
      size="46%"
      :append-to-body="true"
    >
      <div class="help-toolbar">
        <el-checkbox v-model="helpA11y" aria-label="切换无障碍版本说明书">无障碍版本</el-checkbox>
      </div>
      <pre class="manual-text" :class="{ a11y: helpA11y }" tabindex="0" aria-label="使用说明书内容">{{ helpText }}</pre>
    </el-drawer>
  </el-container>
</template>

<script setup lang="ts">
import { computed, onMounted, onUnmounted, reactive, ref } from 'vue';
import { useRoute, useRouter } from 'vue-router';
import {
  AlarmClock,
  Avatar,
  Box,
  Calendar,
  CreditCard,
  DataAnalysis,
  Discount,
  Goods,
  House,
  List,
  Money,
  OfficeBuilding,
  Postcard,
  StarFilled,
  TakeawayBox,
  Tickets,
  TrendCharts,
  User,
  UserFilled,
  Wallet,
  WarnTriangleFilled
} from '@element-plus/icons-vue';
import { canSee, useAuthStore } from '@/stores/auth';
import { useAppStore } from '@/stores/app';
import { usePrefsStore } from '@/stores/prefs';
import { helpApi, subscriptionsApi } from '@/api/modules';
import type { PlatformManual, UserRole } from '@/api/types';
import ProfileDialog from '@/views/components/ProfileDialog.vue';
import { CS_DOWNLOAD_URL } from '@/config';

const profileVisible = ref(false);

// F1 使用帮助：按当前显示模式默认展示 BS 端「正常/无障碍」版说明书，可勾选切换。
const helpVisible = ref(false);
const helpA11y = ref(false);
const manual = ref<PlatformManual | null>(null);
const helpText = computed(() => {
  if (!manual.value) return '加载中…';
  return helpA11y.value ? manual.value.bsManualA11y : manual.value.bsManualNormal;
});
async function openHelp() {
  helpA11y.value = prefs.isA11y;
  if (!manual.value) {
    try { manual.value = await helpApi.manual(); } catch { /* 加载失败时抽屉仍可开 */ }
  }
  helpVisible.value = true;
}
// 全局菜单导航快捷键，与 CS 桌面端对齐。单一事实源：路径 -> 快捷键标识；
// 键盘匹配与菜单提示都由它派生。
// 注意：浏览器保留了 Ctrl+W(关标签)、Ctrl+T(新标签)、F12(开发者工具)，网页无法拦截，
// 故这三项在 BS 端改用 Alt 组合（Alt+W / Alt+T / Alt+V），其余与 CS 完全一致。
const SHORTCUT_LABELS: Record<string, string> = {
  '/dashboard': 'Ctrl+F',
  '/pos': 'F2',
  '/appointments': 'F3',
  '/orders': 'F4',
  '/rooms': 'F5',
  '/members': 'F6',
  '/member-types': 'F7',
  '/queue': 'F8',
  '/reports': 'F9',
  '/day-close': 'F10',
  '/services': 'F11',
  '/vouchers': 'Alt+V',
  '/inventory': 'Ctrl+Q',
  '/reviews': 'Alt+W',
  '/complaints': 'Ctrl+E',
  '/schedules': 'Ctrl+R',
  '/commissions': 'Alt+T',
  '/payroll': 'Ctrl+Y',
  '/staff': 'Ctrl+U',
  '/stores': 'Ctrl+I',
  '/subscription': 'Ctrl+P'
};

function shortcutAria(path: string): string {
  return SHORTCUT_LABELS[path] ? `，快捷键 ${SHORTCUT_LABELS[path]}` : '';
}

// 把按键事件匹配到菜单路径；命中返回路径，否则 null。
// 字母键用 e.code（KeyW 等）匹配，避免 Alt 组合下 e.key 产生特殊字符导致匹配不到。
function matchShortcut(e: KeyboardEvent): string | null {
  for (const [path, label] of Object.entries(SHORTCUT_LABELS)) {
    if (label.startsWith('Ctrl+')) {
      const code = 'Key' + label.slice(5).toUpperCase();
      if (e.ctrlKey && !e.altKey && !e.metaKey && !e.shiftKey && e.code === code) return path;
    } else if (label.startsWith('Alt+')) {
      const code = 'Key' + label.slice(4).toUpperCase();
      if (e.altKey && !e.ctrlKey && !e.metaKey && !e.shiftKey && e.code === code) return path;
    } else if (!e.ctrlKey && !e.altKey && !e.metaKey && e.key === label) {
      return path;
    }
  }
  return null;
}

function onGlobalKeydown(e: KeyboardEvent) {
  if (e.key === 'F1') {
    e.preventDefault();
    openHelp();
    return;
  }
  const path = matchShortcut(e);
  // 仅当当前角色可见该菜单时才拦截浏览器默认行为并跳转；不可见则放行浏览器原生快捷键。
  if (path && visibleMenu.value.some((m) => m.path === path)) {
    e.preventDefault();
    if (route.path !== path) router.push(path);
  }
}

const route = useRoute();
const router = useRouter();
const auth = useAuthStore();
const appStore = useAppStore();
const prefs = usePrefsStore();

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
  AlarmClock, Avatar, Box, Calendar, CreditCard, DataAnalysis, Discount, Goods, House,
  List, Money, OfficeBuilding, Postcard, StarFilled, TakeawayBox, Tickets, TrendCharts,
  User, UserFilled, Wallet, WarnTriangleFilled
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
// Trial 状态走「试用中，剩 X 天」标签，不再重复弹"X 天后到期"——避免双标
const expireWarn = computed(
  () => subStore.status === 'Active' &&
    subStore.daysToExpire !== null && subStore.daysToExpire > 0 && subStore.daysToExpire <= 30 && !subStore.expired
);

function onCommand(cmd: string) {
  if (cmd === 'logout') {
    auth.logout();
    appStore.reset();
    router.replace('/login');
  } else if (cmd === 'profile') {
    profileVisible.value = true;
  } else if (cmd === 'help') {
    openHelp();
  } else if (cmd === 'downloadCs') {
    window.open(CS_DOWNLOAD_URL, '_blank');
  } else if (cmd === 'toggleA11y') {
    prefs.toggle();
  }
}

onMounted(async () => {
  window.addEventListener('keydown', onGlobalKeydown);
  await appStore.loadStores().catch(() => null);
  if (auth.user?.role === 'ShopOwner' || auth.user?.role === 'StoreManager') {
    try {
      const sub = await subscriptionsApi.me();
      subStore.daysToExpire = sub.daysToExpire ?? null;
      subStore.status = sub.status;
      // Trial 与 Active 同样视为可写；只有 Expired / Disabled 才标"只读"
      subStore.expired = sub.status === 'Expired' || sub.status === 'Disabled';
    } catch {
      /* ignore */
    }
  }
});

onUnmounted(() => window.removeEventListener('keydown', onGlobalKeydown));
</script>

<style scoped>
/* 视口锁定：左侧导航 + 顶部头条都固定，只有内容区域 .main 自己滚动；
   最外层不允许出现滚动条。 */
.layout {
  height: 100vh;
  overflow: hidden;
}

/* 跳转链接：默认隐藏，键盘聚焦时显示，方便读屏与键盘用户跳过侧边栏 */
.skip-link {
  position: absolute;
  left: -9999px;
  top: 0;
  z-index: 100;
  padding: 8px 16px;
  background: #2D6A4F;
  color: #fff;
  text-decoration: none;
  border-radius: 0 0 4px 0;
}
.skip-link:focus {
  left: 0;
  outline: 2px solid #ffd04b;
}

.aside {
  background: #1d2b3a;
  color: #c7d0d9;
  height: 100vh;
  display: flex;
  flex-direction: column;
  overflow: hidden;
  box-shadow: 1px 0 0 rgba(0, 0, 0, 0.04);
}
.brand {
  flex: 0 0 60px;
  display: flex;
  align-items: center;
  justify-content: center;
  gap: 8px;
  font-weight: 600;
  font-size: 15px;
  letter-spacing: 0.5px;
  color: #ffffff;
  padding: 0 12px;
  border-bottom: 1px solid rgba(255, 255, 255, 0.06);
}
.brand-icon {
  font-size: 20px;
  color: #5fbf8a;
}
.brand-text {
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}
.menu {
  border-right: none;
  flex: 1 1 auto;
  overflow-y: auto;
  overflow-x: hidden;
  padding: 8px 0;
}
.menu :deep(.el-menu-item) {
  height: 44px;
  line-height: 44px;
  border-radius: 6px;
  margin: 2px 10px;
  padding-left: 14px !important;
  transition: background-color .15s, color .15s;
}
.menu :deep(.el-menu-item:hover) {
  background-color: rgba(255, 255, 255, 0.06) !important;
  color: #ffffff !important;
}
.menu :deep(.el-menu-item.is-active) {
  background-color: rgba(45, 106, 79, 0.85) !important;
  color: #ffffff !important;
  font-weight: 500;
}
.menu :deep(.el-menu-item .el-icon) {
  font-size: 16px;
  margin-right: 10px;
}
/* 快捷键标识：推到菜单项右侧，弱化显示 */
.menu :deep(.el-menu-item .menu-shortcut) {
  margin-left: auto;
  padding-left: 8px;
  font-size: 11px;
  color: #7c8fa3;
}
/* 自定义滚动条更克制 */
.menu::-webkit-scrollbar { width: 6px; }
.menu::-webkit-scrollbar-thumb { background: rgba(255, 255, 255, 0.08); border-radius: 3px; }
.menu::-webkit-scrollbar-thumb:hover { background: rgba(255, 255, 255, 0.16); }

.header {
  flex: 0 0 56px;
  display: flex;
  justify-content: space-between;
  align-items: center;
  padding: 0 20px;
  background: #ffffff;
  border-bottom: 1px solid var(--el-border-color-lighter);
  box-shadow: 0 1px 2px rgba(0, 0, 0, 0.03);
  position: relative;
  z-index: 2;
}
.header-left { display: flex; align-items: center; gap: 10px; }
.page-title {
  font-weight: 600;
  font-size: 16px;
  margin: 0;
  color: #1f2937;
}
.header-right { display: flex; align-items: center; gap: 4px; }
.user {
  cursor: pointer;
  display: flex;
  align-items: center;
  gap: 8px;
  padding: 6px 10px;
  border-radius: 6px;
  transition: background-color .15s;
  color: #374151;
}
.user:hover { background: var(--el-color-primary-light-9); }
.user .el-icon { color: var(--el-color-primary); font-size: 18px; }

.help-toolbar { display: flex; justify-content: flex-end; margin-bottom: 8px; }
.manual-text {
  white-space: pre-wrap;
  word-break: break-word;
  font-family: inherit;
  font-size: 14px;
  line-height: 1.7;
  color: #374151;
  margin: 0;
}
.manual-text.a11y { font-size: 18px; line-height: 1.9; }

.main {
  --el-main-padding: 16px;
  background: #f5f7fa;
  flex: 1 1 auto;
  /* 视口锁定：.main 自己不滚，让 view 内部表格 / 列表自己出滚动条 */
  overflow: hidden;
  display: flex;
  flex-direction: column;
  min-height: 0;
}
</style>
