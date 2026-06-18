<script lang="ts">
export default { name: 'DashboardView' };
</script>

<script setup lang="ts">
import { computed, onMounted, ref, watch } from 'vue';
import dayjs from 'dayjs';
import {
  NavBar as VanNavBar, PullRefresh as VanPullRefresh, Loading as VanLoading
} from 'vant';
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
const refreshing = ref(false);

const daily = ref<DailyReport | null>(null);
const monthly = ref<MonthlyReport | null>(null);
const member = ref<MemberAnalysis | null>(null);
const perf = ref<TechnicianPerformance[]>([]);
const popularity = ref<ServicePopularity[]>([]);
const flow = ref<CustomerFlowPoint[]>([]);

function fmt(v?: number | null) { return (v ?? 0).toFixed(2); }

const kpis = computed(() => [
  { label: '今日营业额', value: `¥${fmt(daily.value?.revenue)}`, sub: `订单 ${daily.value?.orderCount ?? 0} 单` },
  { label: '今日充值', value: `¥${fmt(daily.value?.memberRechargeAmount)}`, sub: `${daily.value?.memberRechargeCount ?? 0} 笔` },
  { label: '本月营业额', value: `¥${fmt(monthly.value?.revenue)}`, sub: `客单价 ¥${fmt(monthly.value?.averageOrder)}` },
  { label: '本月钟数', value: `${monthly.value?.roundsCount ?? 0}`, sub: `订单 ${monthly.value?.orderCount ?? 0} 单` },
  { label: '会员总数', value: `${member.value?.totalMembers ?? 0}`, sub: `本月新增 ${member.value?.newMembersThisMonth ?? 0}` },
  { label: '会员复购率', value: `${member.value?.repeatRate ?? 0}%`, sub: `复购 ${member.value?.repeatMembers ?? 0} 人` }
]);

const revenueTrendOption = computed(() => {
  const rows = monthly.value?.daily ?? [];
  return {
    color: PALETTE,
    tooltip: { trigger: 'axis' },
    legend: { data: ['营业额', '钟数'] },
    grid: { left: 46, right: 40, top: 36, bottom: 30 },
    xAxis: { type: 'category', data: rows.map((r) => dayjs(r.day).format('MM-DD')) },
    yAxis: [
      { type: 'value', name: '营业额', axisLabel: { formatter: '¥{value}' } },
      { type: 'value', name: '钟数' }
    ],
    series: [
      { name: '营业额', type: 'line', smooth: true, areaStyle: { opacity: 0.15 }, data: rows.map((r) => r.revenue) },
      { name: '钟数', type: 'line', smooth: true, yAxisIndex: 1, data: rows.map((r) => r.rounds) }
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
    series: [{ name: '支付方式', type: 'pie', radius: ['40%', '64%'], center: ['50%', '44%'], label: { formatter: '{b}\n{d}%' }, data }]
  };
});

const techPerfOption = computed(() => {
  const rows = [...perf.value].sort((a, b) => b.totalCommission - a.totalCommission).slice(0, 10);
  return {
    color: [BRAND],
    tooltip: { trigger: 'axis', axisPointer: { type: 'shadow' } },
    grid: { left: 56, right: 20, top: 24, bottom: 50 },
    xAxis: { type: 'category', data: rows.map((r) => r.technicianName), axisLabel: { interval: 0, rotate: rows.length > 4 ? 36 : 0 } },
    yAxis: { type: 'value', name: '提成', axisLabel: { formatter: '¥{value}' } },
    series: [{ name: '提成', type: 'bar', barMaxWidth: 28, data: rows.map((r) => r.totalCommission) }]
  };
});

const popularityOption = computed(() => {
  const rows = [...popularity.value].sort((a, b) => b.roundsCount - a.roundsCount).slice(0, 10).reverse();
  return {
    color: ['#40916C'],
    tooltip: { trigger: 'axis', axisPointer: { type: 'shadow' } },
    grid: { left: 90, right: 20, top: 16, bottom: 30 },
    xAxis: { type: 'value', name: '钟数' },
    yAxis: { type: 'category', data: rows.map((r) => r.serviceName) },
    series: [{ name: '钟数', type: 'bar', barMaxWidth: 18, data: rows.map((r) => r.roundsCount) }]
  };
});

const flowOption = computed(() => {
  const rows = flow.value;
  return {
    color: PALETTE,
    tooltip: { trigger: 'axis' },
    legend: { data: ['订单数', '唯一会员'] },
    grid: { left: 36, right: 20, top: 36, bottom: 30 },
    xAxis: { type: 'category', data: rows.map((r) => dayjs(r.date).format('MM-DD')) },
    yAxis: { type: 'value' },
    series: [
      { name: '订单数', type: 'line', smooth: true, areaStyle: { opacity: 0.12 }, data: rows.map((r) => r.orderCount) },
      { name: '唯一会员', type: 'line', smooth: true, data: rows.map((r) => r.uniqueMembers) }
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
    series: [{ name: '会员构成', type: 'pie', radius: ['40%', '64%'], center: ['50%', '44%'], label: { formatter: '{b}\n{d}%' }, data }]
  };
});

async function loadAll() {
  const sid = appStore.activeStoreId;
  if (!sid) { refreshing.value = false; return; }
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
    daily.value = d; monthly.value = mo; member.value = mem;
    perf.value = p; popularity.value = pop; flow.value = fl;
  } catch {
    /* 拦截器已提示 */
  } finally {
    loading.value = false;
    refreshing.value = false;
  }
}

watch(() => appStore.activeStoreId, () => loadAll());
onMounted(async () => {
  if (!appStore.stores.length) await appStore.loadStores().catch(() => undefined);
  loadAll();
});
</script>

<template>
  <div class="qy-page dashboard">
    <van-nav-bar :title="`数据看板 · ${appStore.activeStore?.name || ''}`" left-text="返回" left-arrow @click-left="$router.back()" />

    <van-pull-refresh v-model="refreshing" @refresh="loadAll">
      <p class="hint">趋势 / 排行统计区间：近 30 天</p>

      <div class="kpi-grid">
        <div v-for="k in kpis" :key="k.label" class="kpi">
          <div class="k-label">{{ k.label }}</div>
          <div class="k-value">{{ k.value }}</div>
          <div class="k-sub">{{ k.sub }}</div>
        </div>
      </div>

      <div v-if="loading" class="loading"><van-loading>加载中…</van-loading></div>

      <template v-else>
        <div class="chart-card">
          <div class="cc-title">本月每日营业额趋势</div>
          <v-chart class="chart" :option="revenueTrendOption" autoresize />
        </div>
        <div class="chart-card">
          <div class="cc-title">今日支付方式占比</div>
          <v-chart class="chart" :option="payMethodOption" autoresize />
        </div>
        <div class="chart-card">
          <div class="cc-title">技师业绩排行 · 近 30 天提成</div>
          <v-chart class="chart" :option="techPerfOption" autoresize />
        </div>
        <div class="chart-card">
          <div class="cc-title">服务热度 Top 10 · 近 30 天钟数</div>
          <v-chart class="chart" :option="popularityOption" autoresize />
        </div>
        <div class="chart-card">
          <div class="cc-title">客流趋势 · 近 30 天</div>
          <v-chart class="chart" :option="flowOption" autoresize />
        </div>
        <div class="chart-card">
          <div class="cc-title">会员构成</div>
          <v-chart class="chart" :option="memberCompositionOption" autoresize />
        </div>
      </template>
    </van-pull-refresh>
  </div>
</template>

<style scoped>
.hint { color: #b0b8c4; font-size: 12px; text-align: center; padding: 10px 0 2px; }
.kpi-grid { display: grid; grid-template-columns: repeat(2, 1fr); gap: 10px; padding: 10px 12px; }
.kpi { background: #fff; border-radius: 12px; padding: 14px; text-align: center; box-shadow: 0 6px 18px -14px rgba(16,42,67,.3); }
.k-label { color: #98a2b3; font-size: 13px; }
.k-value { font-size: 20px; font-weight: 800; margin: 6px 0 2px; color: #2d6a4f; }
.k-sub { color: #98a2b3; font-size: 12px; }
.loading { padding: 50px 0; text-align: center; }
.chart-card { background: #fff; margin: 10px 12px; border-radius: 14px; padding: 14px 8px 10px; box-shadow: 0 6px 18px -14px rgba(16,42,67,.25); }
.cc-title { font-size: 14px; font-weight: 600; color: #16324a; padding: 0 8px 6px; }
.chart { height: 260px; width: 100%; }
</style>
