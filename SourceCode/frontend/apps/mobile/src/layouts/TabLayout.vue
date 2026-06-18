<template>
  <div class="tab-layout">
    <div class="tab-content">
      <router-view v-slot="{ Component }">
        <keep-alive :include="['MembersView', 'QueueView']">
          <component :is="Component" />
        </keep-alive>
      </router-view>
    </div>

    <van-tabbar route safe-area-inset-bottom>
      <van-tabbar-item to="/home" icon="wap-home-o">首页</van-tabbar-item>
      <van-tabbar-item v-if="showMembers" to="/members" icon="friends-o">会员</van-tabbar-item>
      <van-tabbar-item to="/queue" icon="exchange">排队</van-tabbar-item>
      <van-tabbar-item to="/profile" icon="user-o">我的</van-tabbar-item>
    </van-tabbar>
  </div>
</template>

<script setup lang="ts">
import { computed } from 'vue';
import { Tabbar as VanTabbar, TabbarItem as VanTabbarItem } from 'vant';
import { useAuthStore } from '@/stores/auth';

const auth = useAuthStore();
// 会员菜单仅 POS 角色（店主/店长/收银员）可见，与 BS 端权限一致；技师不显示。
const showMembers = computed(() => auth.role !== 'Technician');
</script>

<style scoped>
.tab-layout { min-height: 100%; }
.tab-content { min-height: 100%; }
</style>
