<template>
  <div class="page">
    <div class="toolbar">
      <span class="title">平台营收报表</span>
      <el-radio-group v-model="months" @change="load">
        <el-radio-button :value="6">近 6 个月</el-radio-button>
        <el-radio-button :value="12">近 12 个月</el-radio-button>
        <el-radio-button :value="24">近 24 个月</el-radio-button>
      </el-radio-group>
    </div>

    <el-row :gutter="16">
      <el-col :span="6">
        <el-card shadow="hover">
          <div class="metric-label">累计营收</div>
          <div class="metric-value">¥ {{ money(data?.totalAmount) }}</div>
          <div class="metric-sub">订单数：{{ data?.totalOrders ?? 0 }}</div>
        </el-card>
      </el-col>
      <el-col :span="6">
        <el-card shadow="hover">
          <div class="metric-label">新签营收</div>
          <div class="metric-value">¥ {{ money(data?.newCustomerAmount) }}</div>
          <div class="metric-sub">{{ newPercent }}</div>
        </el-card>
      </el-col>
      <el-col :span="6">
        <el-card shadow="hover">
          <div class="metric-label">续费营收</div>
          <div class="metric-value">¥ {{ money(data?.renewalAmount) }}</div>
          <div class="metric-sub">{{ renewalPercent }}</div>
        </el-card>
      </el-col>
      <el-col :span="6">
        <el-card shadow="hover">
          <div class="metric-label">月均营收</div>
          <div class="metric-value">¥ {{ money(monthlyAverage) }}</div>
          <div class="metric-sub">统计 {{ data?.months ?? 0 }} 个月</div>
        </el-card>
      </el-col>
    </el-row>

    <el-card style="margin-top: 16px" shadow="never">
      <template #header><span>月度营收趋势</span></template>
      <el-table :data="data?.monthlyTrend ?? []" v-loading="loading" empty-text="暂无数据">
        <el-table-column label="月份" width="120">
          <template #default="{ row }">{{ row.year }}-{{ pad(row.month) }}</template>
        </el-table-column>
        <el-table-column label="营收" min-width="320">
          <template #default="{ row }">
            <div class="bar-row">
              <div class="bar" :style="{ width: barWidth(row.amount) }" />
              <span class="bar-amount">¥ {{ money(row.amount) }}</span>
            </div>
          </template>
        </el-table-column>
        <el-table-column prop="orderCount" label="订单数" width="100" />
      </el-table>
    </el-card>

    <el-row :gutter="16" style="margin-top: 16px">
      <el-col :span="12">
        <el-card shadow="never">
          <template #header><span>按套餐</span></template>
          <el-table :data="data?.byPlan ?? []" empty-text="暂无数据">
            <el-table-column prop="name" label="套餐" min-width="140" />
            <el-table-column label="营收" min-width="120">
              <template #default="{ row }">¥ {{ money(row.amount) }}</template>
            </el-table-column>
            <el-table-column prop="orderCount" label="订单数" width="100" />
          </el-table>
        </el-card>
      </el-col>
      <el-col :span="12">
        <el-card shadow="never">
          <template #header><span>按支付渠道</span></template>
          <el-table :data="data?.byChannel ?? []" empty-text="暂无数据">
            <el-table-column label="渠道" min-width="140">
              <template #default="{ row }">{{ channelLabel(row.name) }}</template>
            </el-table-column>
            <el-table-column label="营收" min-width="120">
              <template #default="{ row }">¥ {{ money(row.amount) }}</template>
            </el-table-column>
            <el-table-column prop="orderCount" label="订单数" width="100" />
          </el-table>
        </el-card>
      </el-col>
    </el-row>
  </div>
</template>

<script setup lang="ts">
import { computed, onMounted, ref } from 'vue';
import { dashboardApi } from '@/api/modules';
import type { PlatformRevenue } from '@/api/types';

const data = ref<PlatformRevenue | null>(null);
const loading = ref(false);
const months = ref(12);

const CHANNEL_LABEL: Record<string, string> = {
  Wechat: '微信支付',
  Alipay: '支付宝',
  Offline: '线下'
};

function money(v?: number | null) {
  return ((v ?? 0) as number).toFixed(2);
}
function pad(n: number) {
  return n < 10 ? `0${n}` : `${n}`;
}
function channelLabel(name: string) {
  return CHANNEL_LABEL[name] ?? name;
}

const maxMonthly = computed(() =>
  Math.max(1, ...(data.value?.monthlyTrend ?? []).map((m) => m.amount))
);
function barWidth(amount: number) {
  return `${Math.round((amount / maxMonthly.value) * 100)}%`;
}

const monthlyAverage = computed(() => {
  const d = data.value;
  if (!d || d.months === 0) return 0;
  return d.totalAmount / d.months;
});
const newPercent = computed(() => percentOf(data.value?.newCustomerAmount));
const renewalPercent = computed(() => percentOf(data.value?.renewalAmount));
function percentOf(part?: number) {
  const total = data.value?.totalAmount ?? 0;
  if (!total || part == null) return '占比 0%';
  return `占比 ${Math.round((part / total) * 100)}%`;
}

async function load() {
  loading.value = true;
  try {
    data.value = await dashboardApi.revenue(months.value);
  } finally {
    loading.value = false;
  }
}
onMounted(load);
</script>

<style scoped>
.page { padding-bottom: 24px; }
.toolbar {
  display: flex;
  align-items: center;
  justify-content: space-between;
  margin-bottom: 16px;
}
.toolbar .title { font-weight: 600; font-size: 16px; }
.metric-label { color: var(--el-text-color-secondary); font-size: 13px; }
.metric-value { font-size: 26px; font-weight: 600; margin: 6px 0; }
.metric-sub { color: var(--el-text-color-secondary); font-size: 12px; }
.bar-row { display: flex; align-items: center; gap: 10px; }
.bar {
  height: 14px;
  min-width: 2px;
  background: linear-gradient(90deg, #409eff, #79bbff);
  border-radius: 3px;
}
.bar-amount { font-size: 13px; color: var(--el-text-color-regular); white-space: nowrap; }
</style>
