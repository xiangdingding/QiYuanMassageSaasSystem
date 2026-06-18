<template>
  <div class="qy-page ph">
    <van-nav-bar :title="title" left-text="返回" left-arrow @click-left="$router.back()" />
    <van-empty image="search" :description="`「${title}」移动端开发中`">
      <p class="ph-tip">
        该模块已在网页端（BS）/ 桌面端（CS）提供完整功能，<br />
        移动端将按迭代逐步上线。当前可先用网页端处理。
      </p>
      <van-button round type="primary" class="ph-btn" @click="$router.replace('/home')">返回首页</van-button>
    </van-empty>
  </div>
</template>

<script setup lang="ts">
import { computed } from 'vue';
import { useRoute } from 'vue-router';
import { NavBar as VanNavBar, Empty as VanEmpty, Button as VanButton } from 'vant';

const route = useRoute();
const TITLES: Record<string, string> = {
  pos: '收银台'
};
const title = computed(() => TITLES[String(route.params.key)] || '功能');
</script>

<style scoped>
.ph-tip { color: #98a2b3; font-size: 13px; line-height: 1.7; margin: 8px 0 18px; }
.ph-btn { padding-left: 30px; padding-right: 30px; }
</style>
