<template>
  <el-container class="layout">
    <el-aside width="220px" class="aside">
      <div class="brand">SaaS 运营平台</div>
      <el-menu
        :default-active="route.path"
        router
        class="menu"
        background-color="#1f2d3d"
        text-color="#bfcbd9"
        active-text-color="#ffd04b"
      >
        <el-menu-item index="/">
          <el-icon><DataAnalysis /></el-icon>
          <span>运营大盘</span>
        </el-menu-item>
        <el-menu-item index="/revenue">
          <el-icon><TrendCharts /></el-icon>
          <span>营收报表</span>
        </el-menu-item>
        <el-menu-item index="/tenants">
          <el-icon><OfficeBuilding /></el-icon>
          <span>按摩店租户</span>
        </el-menu-item>
        <el-menu-item index="/plans">
          <el-icon><Goods /></el-icon>
          <span>套餐管理</span>
        </el-menu-item>
      </el-menu>
    </el-aside>
    <el-container>
      <el-header class="header">
        <span>{{ pageTitle }}</span>
        <el-dropdown trigger="click" @command="onCommand">
          <span class="user">
            <el-icon><UserFilled /></el-icon>
            {{ auth.user?.realName || auth.user?.username }}
          </span>
          <template #dropdown>
            <el-dropdown-menu>
              <el-dropdown-item command="logout">退出登录</el-dropdown-item>
            </el-dropdown-menu>
          </template>
        </el-dropdown>
      </el-header>
      <el-main class="main">
        <router-view />
      </el-main>
    </el-container>
  </el-container>
</template>

<script setup lang="ts">
import { computed } from 'vue';
import { useRoute, useRouter } from 'vue-router';
import {
  DataAnalysis,
  Goods,
  OfficeBuilding,
  TrendCharts,
  UserFilled
} from '@element-plus/icons-vue';
import { useAuthStore } from '@/stores/auth';

const route = useRoute();
const router = useRouter();
const auth = useAuthStore();

const pageTitle = computed(() => (route.meta.title as string) ?? '');

function onCommand(cmd: string) {
  if (cmd === 'logout') {
    auth.logout();
    router.replace('/login');
  }
}
</script>

<style scoped>
.layout {
  min-height: 100vh;
}
.aside {
  background: #1f2d3d;
  color: #bfcbd9;
}
.brand {
  height: 60px;
  display: flex;
  align-items: center;
  justify-content: center;
  font-weight: 600;
  font-size: 16px;
  letter-spacing: 1px;
  border-bottom: 1px solid #2c3e50;
}
.menu {
  border-right: none;
}
.header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  background: #fff;
  border-bottom: 1px solid var(--el-border-color-light);
  font-weight: 500;
}
.user {
  cursor: pointer;
  display: flex;
  align-items: center;
  gap: 6px;
}
.main {
  background: #f5f7fa;
}
</style>
