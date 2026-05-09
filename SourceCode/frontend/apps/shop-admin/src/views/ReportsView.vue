<template>
  <div class="page">
    <el-tabs v-model="tab">
      <el-tab-pane label="日报" name="daily">
        <el-card shadow="never">
          <div class="toolbar">
            <el-date-picker
              v-model="dailyDate"
              type="date"
              placeholder="选择日期"
              format="YYYY-MM-DD"
              value-format="YYYY-MM-DD"
              :clearable="false"
            />
            <el-button type="primary" @click="loadDaily">查询</el-button>
          </div>
          <div v-if="daily" class="daily-grid">
            <el-row :gutter="16" style="margin-top: 16px">
              <el-col :span="6">
                <el-card class="metric" shadow="hover">
                  <div class="m-label">营业额</div>
                  <div class="m-value">¥{{ daily.revenue.toFixed(2) }}</div>
                  <div class="m-sub">订单 {{ daily.orderCount }} 单</div>
                </el-card>
              </el-col>
              <el-col :span="6">
                <el-card class="metric" shadow="hover">
                  <div class="m-label">退款</div>
                  <div class="m-value">¥{{ daily.refundAmount.toFixed(2) }}</div>
                  <div class="m-sub">退款 {{ daily.refundCount }} 单</div>
                </el-card>
              </el-col>
              <el-col :span="6">
                <el-card class="metric" shadow="hover">
                  <div class="m-label">充值入账</div>
                  <div class="m-value">¥{{ daily.memberRechargeAmount.toFixed(2) }}</div>
                  <div class="m-sub">充值 {{ daily.memberRechargeCount }} 笔</div>
                </el-card>
              </el-col>
              <el-col :span="6">
                <el-card class="metric" shadow="hover">
                  <div class="m-label">净收入</div>
                  <div class="m-value">¥{{ (daily.revenue - daily.refundAmount).toFixed(2) }}</div>
                  <div class="m-sub">营业额 - 退款</div>
                </el-card>
              </el-col>
            </el-row>

            <el-card style="margin-top: 16px" shadow="never">
              <template #header><span>按支付方式</span></template>
              <el-table :data="payMethodRows" size="small">
                <el-table-column prop="label" label="支付方式" />
                <el-table-column label="金额">
                  <template #default="{ row }">¥{{ row.value.toFixed(2) }}</template>
                </el-table-column>
                <el-table-column label="占比">
                  <template #default="{ row }">{{ daily.revenue > 0 ? ((row.value / daily.revenue) * 100).toFixed(1) : 0 }}%</template>
                </el-table-column>
              </el-table>
            </el-card>
          </div>
        </el-card>
      </el-tab-pane>

      <el-tab-pane label="技师业绩" name="performance">
        <el-card shadow="never">
          <div class="toolbar">
            <el-date-picker
              v-model="perfRange"
              type="daterange"
              range-separator="至"
              start-placeholder="开始日期"
              end-placeholder="结束日期"
              format="YYYY-MM-DD"
              value-format="YYYY-MM-DD"
            />
            <el-button type="primary" @click="loadPerformance">查询</el-button>
          </div>
          <el-table :data="perfRows" v-loading="perfLoading" stripe style="margin-top: 12px">
            <el-table-column prop="employeeNo" label="工号" width="80" />
            <el-table-column prop="technicianName" label="姓名" min-width="120" />
            <el-table-column label="服务次数" width="100" prop="orderItemCount" />
            <el-table-column label="服务金额">
              <template #default="{ row }">¥{{ row.totalServiceAmount.toFixed(2) }}</template>
            </el-table-column>
            <el-table-column label="提成合计">
              <template #default="{ row }"><strong style="color:#d9534f">¥{{ row.totalCommission.toFixed(2) }}</strong></template>
            </el-table-column>
            <el-table-column label="服务时长">
              <template #default="{ row }">{{ Math.round(row.totalDurationMinutes / 60 * 10) / 10 }} 小时</template>
            </el-table-column>
          </el-table>
        </el-card>
      </el-tab-pane>
    </el-tabs>
  </div>
</template>

<script setup lang="ts">
import { computed, onMounted, ref } from 'vue';
import dayjs from 'dayjs';
import { ElMessage } from 'element-plus';
import { reportsApi } from '@/api/modules';
import { useAppStore } from '@/stores/app';
import type { DailyReport, TechnicianPerformance } from '@/api/types';

const appStore = useAppStore();
const tab = ref<'daily' | 'performance'>('daily');

const dailyDate = ref(dayjs().format('YYYY-MM-DD'));
const daily = ref<DailyReport | null>(null);

const perfRange = ref<[string, string]>([
  dayjs().subtract(7, 'day').format('YYYY-MM-DD'),
  dayjs().format('YYYY-MM-DD')
]);
const perfRows = ref<TechnicianPerformance[]>([]);
const perfLoading = ref(false);

const payMethodRows = computed(() => {
  if (!daily.value) return [];
  return [
    { label: '现金', value: daily.value.cashAmount },
    { label: '会员卡', value: daily.value.memberCardAmount },
    { label: '微信', value: daily.value.wechatAmount },
    { label: '支付宝', value: daily.value.alipayAmount },
    { label: '银行卡', value: daily.value.bankCardAmount }
  ];
});

async function loadDaily() {
  if (!appStore.activeStoreId) return;
  daily.value = await reportsApi.daily(appStore.activeStoreId, dailyDate.value);
}

async function loadPerformance() {
  if (!appStore.activeStoreId) return;
  if (!perfRange.value || perfRange.value.length !== 2) {
    ElMessage.warning('请选择日期区间');
    return;
  }
  const [from, to] = perfRange.value;
  perfLoading.value = true;
  try {
    perfRows.value = await reportsApi.technicianPerformance(
      appStore.activeStoreId,
      `${from}T00:00:00Z`,
      `${dayjs(to).add(1, 'day').format('YYYY-MM-DD')}T00:00:00Z`
    );
  } finally {
    perfLoading.value = false;
  }
}

onMounted(async () => {
  await appStore.loadStores();
  await loadDaily();
});
</script>

<style scoped>
.page { padding-bottom: 24px; }
.toolbar { display: flex; gap: 8px; align-items: center; flex-wrap: wrap; }
.metric { text-align: center; }
.m-label { color: var(--el-text-color-secondary); font-size: 13px; }
.m-value { font-size: 24px; font-weight: 700; margin: 6px 0; color: #2d6a4f; }
.m-sub { color: var(--el-text-color-secondary); font-size: 12px; }
</style>
