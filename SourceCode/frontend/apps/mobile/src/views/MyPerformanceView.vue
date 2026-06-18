<script lang="ts">
export default { name: 'MyPerformanceView' };
</script>

<script setup lang="ts">
import { onMounted, ref } from 'vue';
import {
  NavBar as VanNavBar, PullRefresh as VanPullRefresh, Loading as VanLoading
} from 'vant';
import { reportsApi, type MyPerformance } from '@/api/modules';

const data = ref<MyPerformance | null>(null);
const loading = ref(false);
const refreshing = ref(false);

function fmt(n?: number | null): string {
  return (n ?? 0).toLocaleString('zh-CN', { minimumFractionDigits: 2, maximumFractionDigits: 2 });
}

async function load() {
  loading.value = true;
  try {
    data.value = await reportsApi.myPerformance();
  } catch {
    /* 错误已由 http 拦截器提示 */
  } finally {
    loading.value = false;
    refreshing.value = false;
  }
}

onMounted(load);
</script>

<template>
  <div class="qy-page perf">
    <van-nav-bar title="我的业绩" left-text="返回" left-arrow @click-left="$router.back()" />

    <van-pull-refresh v-model="refreshing" @refresh="load">
      <div v-if="loading && !data" class="loading"><van-loading>加载中…</van-loading></div>

      <template v-else>
        <div class="card today">
          <p class="card-title">今日</p>
          <div class="big">
            <span class="big-money qy-money">¥ {{ fmt(data?.todayAmount) }}</span>
            <span class="big-sub">业绩</span>
          </div>
          <div class="grid2">
            <div><b class="qy-money">¥{{ fmt(data?.todayCommission) }}</b><span>提成</span></div>
            <div><b>{{ data?.todayRoundCount ?? 0 }}</b><span>钟数</span></div>
          </div>
        </div>

        <div class="card month">
          <p class="card-title">本月</p>
          <div class="big">
            <span class="big-money qy-money">¥ {{ fmt(data?.monthAmount) }}</span>
            <span class="big-sub">业绩</span>
          </div>
          <div class="grid2">
            <div><b class="qy-money">¥{{ fmt(data?.monthCommission) }}</b><span>提成</span></div>
            <div><b>{{ data?.monthRoundCount ?? 0 }}</b><span>钟数</span></div>
          </div>
        </div>

        <p class="tip">业绩按门店营业日切日口径统计，仅含已完成订单。</p>
      </template>
    </van-pull-refresh>
  </div>
</template>

<style scoped>
.loading { padding: 60px 0; text-align: center; }
.card { background: #fff; margin: 12px; border-radius: 14px; padding: 18px; box-shadow: 0 6px 18px -12px rgba(16,42,67,.25); }
.card-title { margin: 0 0 12px; color: #6b7280; font-size: 14px; }
.big { display: flex; align-items: baseline; gap: 10px; margin-bottom: 16px; }
.big-money { font-size: 30px; font-weight: 800; color: #16324a; }
.big-sub { color: #98a2b3; font-size: 13px; }
.grid2 { display: grid; grid-template-columns: repeat(2, 1fr); gap: 8px; }
.grid2 div { background: #f5f7f9; border-radius: 10px; padding: 12px 6px; text-align: center; }
.grid2 b { display: block; font-size: 18px; color: var(--qy-brand); }
.grid2 span { font-size: 12px; color: #98a2b3; }
.tip { color: #b0b8c4; font-size: 12px; text-align: center; padding: 6px 24px 20px; line-height: 1.6; }
</style>
