<template>
  <div class="page">
    <div class="toolbar">
      <span class="store-name">{{ activeStoreName }} · 经营数据看板</span>
      <el-button :loading="loading" @click="loadAll">
        <el-icon><Refresh /></el-icon><span style="margin-left:4px">刷新</span>
      </el-button>
      <span class="window-hint">趋势/排行统计区间：近 30 天</span>
    </div>

    <!-- 关键指标卡 -->
    <el-row :gutter="16" class="kpi-row">
      <el-col :span="4">
        <el-card class="metric" shadow="hover">
          <div class="m-label">今日营业额</div>
          <div class="m-value">¥{{ fmt(daily?.revenue) }}</div>
          <div class="m-sub">订单 {{ daily?.orderCount ?? 0 }} 单</div>
        </el-card>
      </el-col>
      <el-col :span="4">
        <el-card class="metric" shadow="hover">
          <div class="m-label">今日充值</div>
          <div class="m-value">¥{{ fmt(daily?.memberRechargeAmount) }}</div>
          <div class="m-sub">{{ daily?.memberRechargeCount ?? 0 }} 笔</div>
        </el-card>
      </el-col>
      <el-col :span="4">
        <el-card class="metric" shadow="hover">
          <div class="m-label">本月营业额</div>
          <div class="m-value">¥{{ fmt(monthly?.revenue) }}</div>
          <div class="m-sub">客单价 ¥{{ fmt(monthly?.averageOrder) }}</div>
        </el-card>
      </el-col>
      <el-col :span="4">
        <el-card class="metric" shadow="hover">
          <div class="m-label">本月钟数</div>
          <div class="m-value">{{ monthly?.roundsCount ?? 0 }}</div>
          <div class="m-sub">订单 {{ monthly?.orderCount ?? 0 }} 单</div>
        </el-card>
      </el-col>
      <el-col :span="4">
        <el-card class="metric" shadow="hover">
          <div class="m-label">会员总数</div>
          <div class="m-value">{{ member?.totalMembers ?? 0 }}</div>
          <div class="m-sub">本月新增 {{ member?.newMembersThisMonth ?? 0 }}</div>
        </el-card>
      </el-col>
      <el-col :span="4">
        <el-card class="metric" shadow="hover">
          <div class="m-label">会员复购率</div>
          <div class="m-value">{{ member?.repeatRate ?? 0 }}%</div>
          <div class="m-sub">复购 {{ member?.repeatMembers ?? 0 }} 人</div>
        </el-card>
      </el-col>
    </el-row>

    <!-- 营业额趋势 + 支付方式占比 -->
    <el-row :gutter="16" class="chart-row">
      <el-col :span="16">
        <el-card shadow="never">
          <template #header><span>本月每日营业额趋势</span></template>
          <v-chart class="chart" :option="revenueTrendOption" autoresize aria-label="本月每日营业额折线图" />
        </el-card>
      </el-col>
      <el-col :span="8">
        <el-card shadow="never">
          <template #header><span>今日支付方式占比</span></template>
          <v-chart class="chart" :option="payMethodOption" autoresize aria-label="今日支付方式占比饼图" />
        </el-card>
      </el-col>
    </el-row>

    <!-- 技师业绩 + 服务热度 -->
    <el-row :gutter="16" class="chart-row">
      <el-col :span="12">
        <el-card shadow="never">
          <template #header><span>技师业绩排行 · 近 30 天提成</span></template>
          <v-chart class="chart" :option="techPerfOption" autoresize aria-label="技师业绩排行柱状图" />
        </el-card>
      </el-col>
      <el-col :span="12">
        <el-card shadow="never">
          <template #header><span>服务热度 Top 10 · 近 30 天钟数</span></template>
          <v-chart class="chart" :option="popularityOption" autoresize aria-label="服务热度排行柱状图" />
        </el-card>
      </el-col>
    </el-row>

    <!-- 客流趋势 + 会员构成 -->
    <el-row :gutter="16" class="chart-row">
      <el-col :span="16">
        <el-card shadow="never">
          <template #header><span>客流趋势 · 近 30 天</span></template>
          <v-chart class="chart" :option="flowOption" autoresize aria-label="客流趋势折线图" />
        </el-card>
      </el-col>
      <el-col :span="8">
        <el-card shadow="never">
          <template #header><span>会员构成</span></template>
          <v-chart class="chart" :option="memberCompositionOption" autoresize aria-label="会员构成饼图" />
        </el-card>
      </el-col>
    </el-row>
  </div>
</template>

<script setup lang="ts">
import { computed, onMounted, ref, watch } from 'vue';
import dayjs from 'dayjs';
import { Refresh } from '@element-plus/icons-vue';
import VChart from 'vue-echarts';
import { use } from 'echarts/core';
import { CanvasRenderer } from 'echarts/renderers';
import { LineChart, BarChart, PieChart } from 'echarts/charts';
import {
  TitleComponent, TooltipComponent, GridComponent, LegendComponent
} from 'echarts/components';
import {
  reportsApi,
  type MonthlyReport, type ServicePopularity,
  type CustomerFlowPoint, type MemberAnalysis
} from '@/api/modules';
import type { DailyReport, TechnicianPerformance } from '@/api/types';
import { useAppStore } from '@/stores/app';

use([
  CanvasRenderer, LineChart, BarChart, PieChart,
  TitleComponent, TooltipComponent, GridComponent, LegendComponent
]);

const BRAND = '#2D6A4F';
const PALETTE = ['#2D6A4F', '#40916C', '#52B788', '#74C69D', '#95D5B2', '#B7E4C7', '#D8A657', '#E07A5F'];

const appStore = useAppStore();
const loading = ref(false);

const daily = ref<DailyReport | null>(null);
const monthly = ref<MonthlyReport | null>(null);
const member = ref<MemberAnalysis | null>(null);
const perf = ref<TechnicianPerformance[]>([]);
const popularity = ref<ServicePopularity[]>([]);
const flow = ref<CustomerFlowPoint[]>([]);

const activeStoreName = computed(
  () => appStore.stores.find((s) => s.id === appStore.activeStoreId)?.name ?? '本店'
);

function fmt(v?: number | null) {
  return (v ?? 0).toFixed(2);
}

// ---- 各图表 option ----
const revenueTrendOption = computed(() => {
  const rows = monthly.value?.daily ?? [];
  return {
    color: PALETTE,
    tooltip: { trigger: 'axis' },
    legend: { data: ['营业额', '钟数'] },
    grid: { left: 50, right: 50, top: 36, bottom: 30 },
    xAxis: { type: 'category', data: rows.map((r) => dayjs(r.day).format('MM-DD')) },
    yAxis: [
      { type: 'value', name: '营业额', axisLabel: { formatter: '¥{value}' } },
      { type: 'value', name: '钟数' }
    ],
    series: [
      { name: '营业额', type: 'line', smooth: true, areaStyle: { opacity: 0.15 },
        data: rows.map((r) => r.revenue) },
      { name: '钟数', type: 'line', smooth: true, yAxisIndex: 1,
        data: rows.map((r) => r.rounds) }
    ]
  };
});

const payMethodOption = computed(() => {
  const d = daily.value;
  const data = d
    ? [
        { name: '现金', value: d.cashAmount },
        { name: '会员卡', value: d.memberCardAmount },
        { name: '微信', value: d.wechatAmount },
        { name: '支付宝', value: d.alipayAmount },
        { name: '银行卡', value: d.bankCardAmount }
      ].filter((x) => x.value > 0)
    : [];
  return {
    color: PALETTE,
    tooltip: { trigger: 'item', formatter: '{b}: ¥{c} ({d}%)' },
    legend: { bottom: 0 },
    series: [
      {
        name: '支付方式', type: 'pie', radius: ['40%', '68%'],
        center: ['50%', '46%'],
        label: { formatter: '{b}\n{d}%' },
        data
      }
    ]
  };
});

const techPerfOption = computed(() => {
  const rows = [...perf.value].sort((a, b) => b.totalCommission - a.totalCommission).slice(0, 10);
  return {
    color: [BRAND],
    tooltip: { trigger: 'axis', axisPointer: { type: 'shadow' } },
    grid: { left: 60, right: 30, top: 24, bottom: 40 },
    xAxis: { type: 'category', data: rows.map((r) => r.technicianName), axisLabel: { interval: 0, rotate: rows.length > 5 ? 30 : 0 } },
    yAxis: { type: 'value', name: '提成', axisLabel: { formatter: '¥{value}' } },
    series: [
      { name: '提成', type: 'bar', barMaxWidth: 36,
        data: rows.map((r) => r.totalCommission) }
    ]
  };
});

const popularityOption = computed(() => {
  const rows = [...popularity.value].sort((a, b) => b.roundsCount - a.roundsCount).slice(0, 10).reverse();
  return {
    color: ['#40916C'],
    tooltip: { trigger: 'axis', axisPointer: { type: 'shadow' } },
    grid: { left: 110, right: 30, top: 16, bottom: 30 },
    xAxis: { type: 'value', name: '钟数' },
    yAxis: { type: 'category', data: rows.map((r) => r.serviceName) },
    series: [
      { name: '钟数', type: 'bar', barMaxWidth: 22,
        data: rows.map((r) => r.roundsCount) }
    ]
  };
});

const flowOption = computed(() => {
  const rows = flow.value;
  return {
    color: PALETTE,
    tooltip: { trigger: 'axis' },
    legend: { data: ['订单数', '唯一会员'] },
    grid: { left: 40, right: 30, top: 36, bottom: 30 },
    xAxis: { type: 'category', data: rows.map((r) => dayjs(r.date).format('MM-DD')) },
    yAxis: { type: 'value' },
    series: [
      { name: '订单数', type: 'line', smooth: true, areaStyle: { opacity: 0.12 },
        data: rows.map((r) => r.orderCount) },
      { name: '唯一会员', type: 'line', smooth: true,
        data: rows.map((r) => r.uniqueMembers) }
    ]
  };
});

const memberCompositionOption = computed(() => {
  const m = member.value;
  const data = m
    ? [
        { name: '活跃(30天内)', value: m.activeMembers },
        { name: '沉睡(31-90天)', value: m.dormantMembers },
        { name: '流失(>90天)', value: m.lostMembers },
        { name: '从未消费', value: m.neverConsumed }
      ].filter((x) => x.value > 0)
    : [];
  return {
    color: PALETTE,
    tooltip: { trigger: 'item', formatter: '{b}: {c} 人 ({d}%)' },
    legend: { bottom: 0 },
    series: [
      {
        name: '会员构成', type: 'pie', radius: ['40%', '68%'],
        center: ['50%', '46%'],
        label: { formatter: '{b}\n{d}%' },
        data
      }
    ]
  };
});

async function loadAll() {
  const sid = appStore.activeStoreId;
  if (!sid) return;
  loading.value = true;
  const from = `${dayjs().subtract(29, 'day').format('YYYY-MM-DD')}T00:00:00Z`;
  const to = `${dayjs().add(1, 'day').format('YYYY-MM-DD')}T00:00:00Z`;
  try {
    const [d, mo, mem, p, pop, fl] = await Promise.all([
      reportsApi.daily(sid),
      reportsApi.monthly(sid),
      reportsApi.memberAnalysis(sid),
      reportsApi.technicianPerformance(sid, from, to),
      reportsApi.servicePopularity(sid, from, to),
      reportsApi.customerFlow(sid, from, to)
    ]);
    daily.value = d;
    monthly.value = mo;
    member.value = mem;
    perf.value = p;
    popularity.value = pop;
    flow.value = fl;
  } finally {
    loading.value = false;
  }
}

watch(() => appStore.activeStoreId, () => loadAll());

onMounted(async () => {
  await appStore.loadStores();
  await loadAll();
});
</script>

<style scoped>
.page { padding-bottom: 24px; overflow-y: auto; }
.toolbar { display: flex; gap: 12px; align-items: center; margin-bottom: 16px; }
.store-name { font-size: 16px; font-weight: 600; color: #1f2937; }
.window-hint { color: var(--el-text-color-secondary); font-size: 12px; }
.kpi-row { margin-bottom: 4px; }
.chart-row { margin-top: 12px; }
.metric { text-align: center; }
.m-label { color: var(--el-text-color-secondary); font-size: 13px; }
.m-value { font-size: 22px; font-weight: 700; margin: 6px 0; color: #2d6a4f; }
.m-sub { color: var(--el-text-color-secondary); font-size: 12px; }
.chart { height: 300px; width: 100%; }
</style>
