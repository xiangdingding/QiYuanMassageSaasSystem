<template>
  <div class="page">
    <el-row :gutter="16">
      <el-col :span="6">
        <el-card shadow="hover">
          <div class="metric-label">租户总数</div>
          <div class="metric-value">{{ data?.totalTenants ?? '-' }}</div>
          <div class="metric-sub">活跃 {{ data?.activeTenants ?? 0 }}</div>
        </el-card>
      </el-col>
      <el-col :span="6">
        <el-card shadow="hover">
          <div class="metric-label">30 天内到期</div>
          <div class="metric-value warn">{{ data?.expiringIn30Days ?? '-' }}</div>
          <div class="metric-sub">7 天内：{{ data?.expiringIn7Days ?? 0 }}</div>
        </el-card>
      </el-col>
      <el-col :span="6">
        <el-card shadow="hover">
          <div class="metric-label">最近 30 天收入</div>
          <div class="metric-value">¥ {{ formatMoney(data?.revenueLast30Days) }}</div>
          <div class="metric-sub">订单数：{{ data?.paidOrdersLast30Days ?? 0 }}</div>
        </el-card>
      </el-col>
      <el-col :span="6">
        <el-card shadow="hover">
          <div class="metric-label">本年度收入</div>
          <div class="metric-value">¥ {{ formatMoney(data?.revenueThisYear) }}</div>
          <div class="metric-sub">已停用 {{ data?.disabledTenants ?? 0 }} / 已过期 {{ data?.expiredTenants ?? 0 }}</div>
        </el-card>
      </el-col>
    </el-row>

    <el-card style="margin-top: 16px" shadow="never">
      <template #header><span>最近订阅</span></template>
      <el-table :data="data?.recentSubscriptions ?? []" v-loading="loading" empty-text="暂无数据">
        <el-table-column prop="tenantName" label="租户" min-width="160" />
        <el-table-column prop="planName" label="套餐" min-width="120" />
        <el-table-column label="金额" min-width="100">
          <template #default="{ row }">¥ {{ formatMoney(row.amount) }}</template>
        </el-table-column>
        <el-table-column prop="source" label="来源" min-width="120">
          <template #default="{ row }">
            <el-tag :type="row.source === 'OnlinePayment' ? 'success' : 'info'">
              {{ row.source === 'OnlinePayment' ? '在线支付' : '线下激活' }}
            </el-tag>
          </template>
        </el-table-column>
        <el-table-column label="时间" min-width="160">
          <template #default="{ row }">{{ formatTime(row.createdAt) }}</template>
        </el-table-column>
      </el-table>
    </el-card>
  </div>
</template>

<script setup lang="ts">
import { onMounted, ref } from 'vue';
import { dashboardApi } from '@/api/modules';
import type { PlatformDashboard } from '@/api/types';

const data = ref<PlatformDashboard | null>(null);
const loading = ref(false);

function formatMoney(v?: number | null) {
  return ((v ?? 0) as number).toFixed(2);
}
function formatTime(v: string) {
  return new Date(v).toLocaleString('zh-CN');
}

async function load() {
  loading.value = true;
  try {
    data.value = await dashboardApi.platform();
  } finally {
    loading.value = false;
  }
}
onMounted(load);
</script>

<style scoped>
.page { padding-bottom: 24px; }
.metric-label { color: var(--el-text-color-secondary); font-size: 13px; }
.metric-value { font-size: 28px; font-weight: 600; margin: 6px 0; }
.metric-value.warn { color: #e6a23c; }
.metric-sub { color: var(--el-text-color-secondary); font-size: 12px; }
</style>
