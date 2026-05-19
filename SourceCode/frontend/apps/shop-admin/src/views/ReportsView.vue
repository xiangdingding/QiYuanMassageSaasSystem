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

      <el-tab-pane label="月报" name="monthly">
        <el-card shadow="never">
          <div class="toolbar">
            <el-date-picker
              v-model="monthlyMonth"
              type="month"
              placeholder="选择月份"
              format="YYYY-MM"
              value-format="YYYY-MM"
              :clearable="false"
            />
            <el-button type="primary" @click="loadMonthly">查询</el-button>
          </div>
          <div v-if="monthly" style="margin-top: 16px">
            <el-row :gutter="16">
              <el-col :span="6">
                <el-card class="metric" shadow="hover">
                  <div class="m-label">本月营业额</div>
                  <div class="m-value">¥{{ monthly.revenue.toFixed(2) }}</div>
                  <div class="m-sub">订单 {{ monthly.orderCount }} 单</div>
                </el-card>
              </el-col>
              <el-col :span="6">
                <el-card class="metric" shadow="hover">
                  <div class="m-label">钟数合计</div>
                  <div class="m-value">{{ monthly.roundsCount }}</div>
                </el-card>
              </el-col>
              <el-col :span="6">
                <el-card class="metric" shadow="hover">
                  <div class="m-label">充值入账</div>
                  <div class="m-value">¥{{ monthly.rechargeAmount.toFixed(2) }}</div>
                </el-card>
              </el-col>
              <el-col :span="6">
                <el-card class="metric" shadow="hover">
                  <div class="m-label">客单价</div>
                  <div class="m-value">¥{{ monthly.averageOrder.toFixed(2) }}</div>
                </el-card>
              </el-col>
            </el-row>
            <el-card style="margin-top: 16px" shadow="never">
              <template #header><span>每日明细</span></template>
              <el-table :data="monthly.daily" size="small">
                <el-table-column label="日期">
                  <template #default="{ row }">{{ formatDate(row.day) }}</template>
                </el-table-column>
                <el-table-column prop="orderCount" label="订单" width="100" />
                <el-table-column label="营业额">
                  <template #default="{ row }">¥{{ row.revenue.toFixed(2) }}</template>
                </el-table-column>
                <el-table-column prop="rounds" label="钟数" width="100" />
              </el-table>
            </el-card>
          </div>
        </el-card>
      </el-tab-pane>

      <el-tab-pane label="年报" name="yearly">
        <el-card shadow="never">
          <div class="toolbar">
            <el-date-picker
              v-model="yearlyYear"
              type="year"
              placeholder="选择年份"
              format="YYYY"
              value-format="YYYY"
              :clearable="false"
            />
            <el-button type="primary" @click="loadYearly">查询</el-button>
          </div>
          <div v-if="yearly" style="margin-top: 16px">
            <el-row :gutter="16">
              <el-col :span="8">
                <el-card class="metric" shadow="hover">
                  <div class="m-label">{{ yearly.year }} 年营业额</div>
                  <div class="m-value">¥{{ yearly.revenue.toFixed(2) }}</div>
                  <div class="m-sub">订单 {{ yearly.orderCount }} 单</div>
                </el-card>
              </el-col>
              <el-col :span="8">
                <el-card class="metric" shadow="hover">
                  <div class="m-label">钟数合计</div>
                  <div class="m-value">{{ yearly.roundsCount }}</div>
                </el-card>
              </el-col>
              <el-col :span="8">
                <el-card class="metric" shadow="hover">
                  <div class="m-label">月均</div>
                  <div class="m-value">¥{{ (yearly.revenue / Math.max(1, yearly.monthly.length)).toFixed(2) }}</div>
                </el-card>
              </el-col>
            </el-row>
            <el-card style="margin-top: 16px" shadow="never">
              <template #header><span>逐月明细</span></template>
              <el-table :data="yearly.monthly" size="small">
                <el-table-column label="月份">
                  <template #default="{ row }">{{ formatMonth(row.day) }}</template>
                </el-table-column>
                <el-table-column prop="orderCount" label="订单" width="100" />
                <el-table-column label="营业额">
                  <template #default="{ row }">¥{{ row.revenue.toFixed(2) }}</template>
                </el-table-column>
                <el-table-column prop="rounds" label="钟数" width="100" />
              </el-table>
            </el-card>
          </div>
        </el-card>
      </el-tab-pane>

      <el-tab-pane label="服务热度" name="popularity">
        <el-card shadow="never">
          <div class="toolbar">
            <el-date-picker
              v-model="popRange"
              type="daterange"
              range-separator="至"
              start-placeholder="开始" end-placeholder="结束"
              format="YYYY-MM-DD" value-format="YYYY-MM-DD"
            />
            <el-button type="primary" @click="loadPopularity">查询</el-button>
          </div>
          <el-table :data="popularity" v-loading="popLoading" stripe style="margin-top:12px">
            <el-table-column prop="serviceName" label="服务" min-width="160" />
            <el-table-column prop="orderItemCount" label="下单次数" width="120" />
            <el-table-column prop="roundsCount" label="钟数合计" width="120" />
            <el-table-column label="营业额">
              <template #default="{ row }">¥{{ row.revenue.toFixed(2) }}</template>
            </el-table-column>
          </el-table>
        </el-card>
      </el-tab-pane>

      <el-tab-pane label="客流" name="flow">
        <el-card shadow="never">
          <div class="toolbar">
            <el-date-picker
              v-model="flowRange"
              type="daterange"
              range-separator="至"
              start-placeholder="开始" end-placeholder="结束"
              format="YYYY-MM-DD" value-format="YYYY-MM-DD"
            />
            <el-button type="primary" @click="loadFlow">查询</el-button>
          </div>
          <el-table :data="flow" v-loading="flowLoading" stripe style="margin-top:12px">
            <el-table-column label="日期">
              <template #default="{ row }">{{ formatDate(row.date) }}</template>
            </el-table-column>
            <el-table-column prop="orderCount" label="订单数" width="120" />
            <el-table-column prop="uniqueMembers" label="唯一会员" width="120" />
          </el-table>
        </el-card>
      </el-tab-pane>

      <el-tab-pane label="会员分析" name="memberAnalysis">
        <el-card shadow="never">
          <div class="toolbar">
            <el-button type="primary" @click="loadMemberAnalysis">刷新</el-button>
          </div>
          <div v-if="memberAnalysis" style="margin-top: 16px">
            <el-row :gutter="16">
              <el-col :span="6">
                <el-card class="metric" shadow="hover">
                  <div class="m-label">会员总数</div>
                  <div class="m-value">{{ memberAnalysis.totalMembers }}</div>
                  <div class="m-sub">本月新增 {{ memberAnalysis.newMembersThisMonth }}</div>
                </el-card>
              </el-col>
              <el-col :span="6">
                <el-card class="metric" shadow="hover">
                  <div class="m-label">活跃 · 30 天内消费</div>
                  <div class="m-value">{{ memberAnalysis.activeMembers }}</div>
                </el-card>
              </el-col>
              <el-col :span="6">
                <el-card class="metric" shadow="hover">
                  <div class="m-label">沉睡 · 31-90 天</div>
                  <div class="m-value">{{ memberAnalysis.dormantMembers }}</div>
                </el-card>
              </el-col>
              <el-col :span="6">
                <el-card class="metric" shadow="hover">
                  <div class="m-label">流失 · 超 90 天</div>
                  <div class="m-value">{{ memberAnalysis.lostMembers }}</div>
                </el-card>
              </el-col>
            </el-row>
            <el-row :gutter="16" style="margin-top: 16px">
              <el-col :span="8">
                <el-card class="metric" shadow="hover">
                  <div class="m-label">从未消费</div>
                  <div class="m-value">{{ memberAnalysis.neverConsumed }}</div>
                </el-card>
              </el-col>
              <el-col :span="8">
                <el-card class="metric" shadow="hover">
                  <div class="m-label">复购会员 · 累计 ≥2 单</div>
                  <div class="m-value">{{ memberAnalysis.repeatMembers }}</div>
                </el-card>
              </el-col>
              <el-col :span="8">
                <el-card class="metric" shadow="hover">
                  <div class="m-label">复购率</div>
                  <div class="m-value">{{ memberAnalysis.repeatRate }}%</div>
                </el-card>
              </el-col>
            </el-row>
          </div>
        </el-card>
      </el-tab-pane>

      <el-tab-pane label="服务趋势" name="serviceTrend">
        <el-card shadow="never">
          <div class="toolbar">
            <el-select v-model="trendMonths" style="width: 150px" @change="loadServiceTrend">
              <el-option :value="6" label="近 6 个月" />
              <el-option :value="12" label="近 12 个月" />
            </el-select>
            <el-button type="primary" @click="loadServiceTrend">查询</el-button>
          </div>
          <el-table :data="serviceTrend?.services ?? []" v-loading="trendLoading" stripe style="margin-top: 12px">
            <el-table-column prop="serviceName" label="服务" min-width="140" fixed />
            <el-table-column prop="totalRounds" label="总钟数" width="90" />
            <el-table-column
              v-for="(label, idx) in trendMonthHeaders"
              :key="idx"
              :label="label"
              width="88"
            >
              <template #default="{ row }">{{ row.months[idx]?.rounds ?? 0 }}</template>
            </el-table-column>
          </el-table>
        </el-card>
      </el-tab-pane>

      <el-tab-pane label="技师质量" name="quality">
        <el-card shadow="never">
          <div class="toolbar">
            <el-date-picker
              v-model="qualityRange"
              type="daterange"
              range-separator="至"
              start-placeholder="开始" end-placeholder="结束"
              format="YYYY-MM-DD" value-format="YYYY-MM-DD"
            />
            <el-button type="primary" @click="loadQuality">查询</el-button>
          </div>
          <el-table :data="quality" v-loading="qualityLoading" stripe style="margin-top: 12px">
            <el-table-column prop="employeeNo" label="工号" width="80" />
            <el-table-column prop="technicianName" label="技师" min-width="120" />
            <el-table-column prop="roundCount" label="钟数" width="100" />
            <el-table-column prop="complaintCount" label="投诉数" width="100" />
            <el-table-column label="投诉率" width="120">
              <template #default="{ row }">
                <span :style="{ color: row.complaintRate > 5 ? '#d9534f' : '#2d6a4f', fontWeight: 600 }">
                  {{ row.complaintRate }}%
                </span>
              </template>
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
import {
  reportsApi,
  type MonthlyReport, type YearlyReport,
  type ServicePopularity, type CustomerFlowPoint,
  type MemberAnalysis, type ServicePopularityTrend, type TechnicianQuality
} from '@/api/modules';
import { useAppStore } from '@/stores/app';
import type { DailyReport, TechnicianPerformance } from '@/api/types';

const appStore = useAppStore();
const tab = ref<
  'daily' | 'performance' | 'monthly' | 'yearly' | 'popularity' | 'flow'
  | 'memberAnalysis' | 'serviceTrend' | 'quality'
>('daily');

const dailyDate = ref(dayjs().format('YYYY-MM-DD'));
const daily = ref<DailyReport | null>(null);

const perfRange = ref<[string, string]>([
  dayjs().subtract(7, 'day').format('YYYY-MM-DD'),
  dayjs().format('YYYY-MM-DD')
]);
const perfRows = ref<TechnicianPerformance[]>([]);
const perfLoading = ref(false);

const monthlyMonth = ref(dayjs().format('YYYY-MM'));
const monthly = ref<MonthlyReport | null>(null);

const yearlyYear = ref(dayjs().format('YYYY'));
const yearly = ref<YearlyReport | null>(null);

const popRange = ref<[string, string]>([
  dayjs().subtract(30, 'day').format('YYYY-MM-DD'),
  dayjs().format('YYYY-MM-DD')
]);
const popularity = ref<ServicePopularity[]>([]);
const popLoading = ref(false);

const flowRange = ref<[string, string]>([
  dayjs().subtract(30, 'day').format('YYYY-MM-DD'),
  dayjs().format('YYYY-MM-DD')
]);
const flow = ref<CustomerFlowPoint[]>([]);
const flowLoading = ref(false);

const memberAnalysis = ref<MemberAnalysis | null>(null);

const trendMonths = ref(6);
const serviceTrend = ref<ServicePopularityTrend | null>(null);
const trendLoading = ref(false);

const qualityRange = ref<[string, string]>([
  dayjs().subtract(30, 'day').format('YYYY-MM-DD'),
  dayjs().format('YYYY-MM-DD')
]);
const quality = ref<TechnicianQuality[]>([]);
const qualityLoading = ref(false);

function formatDate(s: string) { return dayjs(s).format('YYYY-MM-DD'); }
function formatMonth(s: string) { return dayjs(s).format('YYYY-MM'); }

const trendMonthHeaders = computed(() => {
  const first = serviceTrend.value?.services[0];
  if (!first) return [] as string[];
  return first.months.map((m) => `${m.year}-${String(m.month).padStart(2, '0')}`);
});

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

async function loadMonthly() {
  if (!appStore.activeStoreId) return;
  const [y, m] = monthlyMonth.value.split('-').map(Number);
  monthly.value = await reportsApi.monthly(appStore.activeStoreId, y, m);
}

async function loadYearly() {
  if (!appStore.activeStoreId) return;
  yearly.value = await reportsApi.yearly(appStore.activeStoreId, Number(yearlyYear.value));
}

async function loadPopularity() {
  if (!appStore.activeStoreId) return;
  const [from, to] = popRange.value;
  popLoading.value = true;
  try {
    popularity.value = await reportsApi.servicePopularity(
      appStore.activeStoreId,
      `${from}T00:00:00Z`,
      `${dayjs(to).add(1, 'day').format('YYYY-MM-DD')}T00:00:00Z`
    );
  } finally {
    popLoading.value = false;
  }
}

async function loadFlow() {
  if (!appStore.activeStoreId) return;
  const [from, to] = flowRange.value;
  flowLoading.value = true;
  try {
    flow.value = await reportsApi.customerFlow(
      appStore.activeStoreId,
      `${from}T00:00:00Z`,
      `${dayjs(to).add(1, 'day').format('YYYY-MM-DD')}T00:00:00Z`
    );
  } finally {
    flowLoading.value = false;
  }
}

async function loadMemberAnalysis() {
  if (!appStore.activeStoreId) return;
  memberAnalysis.value = await reportsApi.memberAnalysis(appStore.activeStoreId);
}

async function loadServiceTrend() {
  if (!appStore.activeStoreId) return;
  trendLoading.value = true;
  try {
    serviceTrend.value = await reportsApi.serviceTrend(appStore.activeStoreId, trendMonths.value);
  } finally {
    trendLoading.value = false;
  }
}

async function loadQuality() {
  if (!appStore.activeStoreId) return;
  const [from, to] = qualityRange.value;
  qualityLoading.value = true;
  try {
    quality.value = await reportsApi.technicianQuality(
      appStore.activeStoreId,
      `${from}T00:00:00Z`,
      `${dayjs(to).add(1, 'day').format('YYYY-MM-DD')}T00:00:00Z`
    );
  } finally {
    qualityLoading.value = false;
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
