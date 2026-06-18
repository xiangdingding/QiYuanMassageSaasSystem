<script lang="ts">
export default { name: 'ReportsView' };
</script>

<script setup lang="ts">
import { computed, onMounted, ref, watch } from 'vue';
import {
  NavBar as VanNavBar, Tabs as VanTabs, Tab as VanTab, PullRefresh as VanPullRefresh,
  Empty as VanEmpty, Cell as VanCell, CellGroup as VanCellGroup, Loading as VanLoading
} from 'vant';
import {
  reportsApi,
  type MonthlyReport, type YearlyReport, type ServicePopularity,
  type CustomerFlowPoint, type MemberAnalysis, type TechnicianQuality
} from '@/api/modules';
import { useAppStore } from '@/stores/app';
import type { DailyReport, TechnicianPerformance } from '@/api/types';

const appStore = useAppStore();

const tabIndex = ref(0);
const loading = ref(false);
const refreshing = ref(false);

const daily = ref<DailyReport | null>(null);
const monthly = ref<MonthlyReport | null>(null);
const yearly = ref<YearlyReport | null>(null);
const perf = ref<TechnicianPerformance[]>([]);
const popularity = ref<ServicePopularity[]>([]);
const flow = ref<CustomerFlowPoint[]>([]);
const member = ref<MemberAnalysis | null>(null);
const quality = ref<TechnicianQuality[]>([]);

function fmt(n?: number | null): string {
  return (n ?? 0).toLocaleString('zh-CN', { minimumFractionDigits: 2, maximumFractionDigits: 2 });
}
function monthRange(): { from: string; to: string } {
  const now = new Date();
  const pad = (n: number) => String(n).padStart(2, '0');
  const first = `${now.getFullYear()}-${pad(now.getMonth() + 1)}-01`;
  const last = `${now.getFullYear()}-${pad(now.getMonth() + 1)}-${pad(new Date(now.getFullYear(), now.getMonth() + 1, 0).getDate())}`;
  return { from: first, to: last };
}
function shortDate(s: string): string {
  return s.slice(0, 10);
}

const payOnline = computed(() => (daily.value ? daily.value.wechatAmount + daily.value.alipayAmount : 0));
const yearMonthAvg = computed(() =>
  yearly.value ? yearly.value.revenue / Math.max(1, yearly.value.monthly.length) : 0
);

async function load() {
  const sid = appStore.activeStoreId;
  if (!sid) return;
  loading.value = true;
  try {
    const { from, to } = monthRange();
    switch (tabIndex.value) {
      case 0: daily.value = await reportsApi.daily(sid); break;
      case 1: monthly.value = await reportsApi.monthly(sid); break;
      case 2: yearly.value = await reportsApi.yearly(sid); break;
      case 3: perf.value = await reportsApi.technicianPerformance(sid, from, to); break;
      case 4: popularity.value = await reportsApi.servicePopularity(sid, from, to); break;
      case 5: flow.value = await reportsApi.customerFlow(sid, from, to); break;
      case 6: member.value = await reportsApi.memberAnalysis(sid); break;
      case 7: quality.value = await reportsApi.technicianQuality(sid, from, to); break;
    }
  } catch {
    /* 拦截器已提示 */
  } finally {
    loading.value = false;
    refreshing.value = false;
  }
}

watch(tabIndex, load);
onMounted(async () => {
  if (!appStore.stores.length) await appStore.loadStores().catch(() => undefined);
  load();
});
</script>

<template>
  <div class="qy-page reports">
    <van-nav-bar :title="`报表 · ${appStore.activeStore?.name || ''}`" left-text="返回" left-arrow @click-left="$router.back()" />
    <van-tabs v-model:active="tabIndex" sticky>
      <van-tab title="今日" />
      <van-tab title="本月" />
      <van-tab title="年报" />
      <van-tab title="技师业绩" />
      <van-tab title="服务热度" />
      <van-tab title="客流" />
      <van-tab title="会员" />
      <van-tab title="技师质量" />
    </van-tabs>

    <van-pull-refresh v-model="refreshing" @refresh="load">
      <div v-if="loading" class="loading"><van-loading>加载中…</van-loading></div>

      <!-- 今日 -->
      <template v-else-if="tabIndex === 0">
        <div class="card">
          <div class="big">
            <span class="big-money qy-money">¥ {{ fmt(daily?.revenue) }}</span>
            <span class="big-sub">今日营业额</span>
          </div>
          <div class="grid">
            <div><b>{{ daily?.orderCount ?? 0 }}</b><span>订单</span></div>
            <div><b class="qy-money">{{ fmt(daily?.memberRechargeAmount) }}</b><span>充值</span></div>
            <div><b class="qy-money">{{ fmt(daily?.refundAmount) }}</b><span>退款</span></div>
          </div>
        </div>
        <van-cell-group inset title="收款构成">
          <van-cell title="现金" :value="`¥ ${fmt(daily?.cashAmount)}`" />
          <van-cell title="会员卡" :value="`¥ ${fmt(daily?.memberCardAmount)}`" />
          <van-cell title="微信" :value="`¥ ${fmt(daily?.wechatAmount)}`" />
          <van-cell title="支付宝" :value="`¥ ${fmt(daily?.alipayAmount)}`" />
          <van-cell title="银行卡" :value="`¥ ${fmt(daily?.bankCardAmount)}`" />
          <van-cell title="线上小计（微/支）" :value="`¥ ${fmt(payOnline)}`" />
        </van-cell-group>
      </template>

      <!-- 本月 -->
      <template v-else-if="tabIndex === 1">
        <div class="card">
          <div class="big">
            <span class="big-money qy-money">¥ {{ fmt(monthly?.revenue) }}</span>
            <span class="big-sub">本月营业额</span>
          </div>
          <div class="grid">
            <div><b>{{ monthly?.orderCount ?? 0 }}</b><span>订单</span></div>
            <div><b>{{ monthly?.roundsCount ?? 0 }}</b><span>钟数</span></div>
            <div><b class="qy-money">{{ fmt(monthly?.averageOrder) }}</b><span>客单价</span></div>
          </div>
        </div>
        <van-cell-group inset>
          <van-cell title="本月充值" :value="`¥ ${fmt(monthly?.rechargeAmount)}`" />
        </van-cell-group>
        <van-cell-group v-if="monthly?.daily?.length" inset title="每日明细">
          <van-cell
            v-for="d in monthly.daily"
            :key="d.day"
            :title="shortDate(d.day)"
            :label="`${d.orderCount} 单 · ${d.rounds} 钟`"
            :value="`¥ ${fmt(d.revenue)}`"
          />
        </van-cell-group>
      </template>

      <!-- 年报 -->
      <template v-else-if="tabIndex === 2">
        <div class="card">
          <div class="big">
            <span class="big-money qy-money">¥ {{ fmt(yearly?.revenue) }}</span>
            <span class="big-sub">{{ yearly?.year }} 年营业额</span>
          </div>
          <div class="grid">
            <div><b>{{ yearly?.orderCount ?? 0 }}</b><span>订单</span></div>
            <div><b>{{ yearly?.roundsCount ?? 0 }}</b><span>钟数</span></div>
            <div><b class="qy-money">{{ fmt(yearMonthAvg) }}</b><span>月均</span></div>
          </div>
        </div>
        <van-cell-group v-if="yearly?.monthly?.length" inset title="逐月明细">
          <van-cell
            v-for="m in yearly.monthly"
            :key="m.day"
            :title="m.day.slice(0, 7)"
            :label="`${m.orderCount} 单 · ${m.rounds} 钟`"
            :value="`¥ ${fmt(m.revenue)}`"
          />
        </van-cell-group>
      </template>

      <!-- 技师业绩 -->
      <template v-else-if="tabIndex === 3">
        <van-empty v-if="perf.length === 0" description="本月暂无技师业绩" />
        <div v-for="(t, i) in perf" :key="t.technicianId" class="perf-item">
          <div class="pf-rank" :class="{ top: i < 3 }">{{ i + 1 }}</div>
          <div class="pf-mid">
            <div class="pf-name">{{ t.technicianName }}<span v-if="t.employeeNo" class="pf-no"> #{{ t.employeeNo }}</span></div>
            <div class="pf-sub">
              {{ t.orderItemCount }} 项 · {{ Math.round(t.totalDurationMinutes / 60 * 10) / 10 }} 时
              <span v-if="t.designationRate != null"> · 指定率 {{ Math.round(t.designationRate * 100) }}%</span>
            </div>
          </div>
          <div class="pf-right">
            <b class="qy-money">¥{{ fmt(t.totalServiceAmount) }}</b>
            <span class="qy-money">提成 ¥{{ fmt(t.totalCommission) }}</span>
          </div>
        </div>
      </template>

      <!-- 服务热度 -->
      <template v-else-if="tabIndex === 4">
        <van-empty v-if="popularity.length === 0" description="本月暂无服务数据" />
        <div v-for="(s, i) in popularity" :key="s.serviceId" class="perf-item">
          <div class="pf-rank" :class="{ top: i < 3 }">{{ i + 1 }}</div>
          <div class="pf-mid">
            <div class="pf-name">{{ s.serviceName }}</div>
            <div class="pf-sub">{{ s.orderItemCount }} 次 · {{ s.roundsCount }} 钟</div>
          </div>
          <div class="pf-right">
            <b class="qy-money">¥{{ fmt(s.revenue) }}</b>
            <span>营业额</span>
          </div>
        </div>
      </template>

      <!-- 客流 -->
      <template v-else-if="tabIndex === 5">
        <van-empty v-if="flow.length === 0" description="本月暂无客流数据" />
        <van-cell-group v-else inset title="本月每日客流">
          <van-cell
            v-for="f in flow"
            :key="f.date"
            :title="shortDate(f.date)"
            :label="`唯一会员 ${f.uniqueMembers} 人`"
            :value="`${f.orderCount} 单`"
          />
        </van-cell-group>
      </template>

      <!-- 会员分析 -->
      <template v-else-if="tabIndex === 6">
        <div class="card">
          <div class="big">
            <span class="big-money">{{ member?.totalMembers ?? 0 }}</span>
            <span class="big-sub">会员总数 · 本月新增 {{ member?.newMembersThisMonth ?? 0 }}</span>
          </div>
          <div class="grid">
            <div><b>{{ member?.activeMembers ?? 0 }}</b><span>活跃·30天</span></div>
            <div><b>{{ member?.dormantMembers ?? 0 }}</b><span>沉睡·31-90天</span></div>
            <div><b>{{ member?.lostMembers ?? 0 }}</b><span>流失·90天+</span></div>
          </div>
        </div>
        <van-cell-group inset>
          <van-cell title="从未消费" :value="`${member?.neverConsumed ?? 0} 人`" />
          <van-cell title="复购会员（累计 ≥2 单）" :value="`${member?.repeatMembers ?? 0} 人`" />
          <van-cell title="复购率" :value="`${member?.repeatRate ?? 0}%`" />
        </van-cell-group>
      </template>

      <!-- 技师质量 -->
      <template v-else>
        <van-empty v-if="quality.length === 0" description="本月暂无质量数据" />
        <div v-for="t in quality" :key="t.technicianId" class="perf-item">
          <div class="pf-mid">
            <div class="pf-name">{{ t.technicianName }}<span v-if="t.employeeNo" class="pf-no"> #{{ t.employeeNo }}</span></div>
            <div class="pf-sub">{{ t.roundCount }} 钟 · 投诉 {{ t.complaintCount }} 起</div>
          </div>
          <div class="pf-right">
            <b :class="t.complaintRate > 5 ? 'bad' : 'good'">{{ t.complaintRate }}%</b>
            <span>投诉率</span>
          </div>
        </div>
      </template>
    </van-pull-refresh>
  </div>
</template>

<style scoped>
.loading { padding: 60px 0; text-align: center; }
.card { background: #fff; margin: 12px; border-radius: 14px; padding: 18px; box-shadow: 0 6px 18px -12px rgba(16,42,67,.25); }
.big { display: flex; align-items: baseline; gap: 10px; margin-bottom: 16px; flex-wrap: wrap; }
.big-money { font-size: 30px; font-weight: 800; color: #16324a; }
.big-sub { color: #98a2b3; font-size: 13px; }
.grid { display: grid; grid-template-columns: repeat(3, 1fr); gap: 8px; }
.grid div { background: #f5f7f9; border-radius: 10px; padding: 12px 6px; text-align: center; }
.grid b { display: block; font-size: 17px; color: #16324a; }
.grid span { font-size: 12px; color: #98a2b3; }
.perf-item { display: flex; align-items: center; gap: 12px; background: #fff; margin: 8px 12px; padding: 14px; border-radius: 12px; }
.pf-rank {
  width: 28px; height: 28px; border-radius: 50%; background: #eef1f4; color: #8a94a6;
  display: flex; align-items: center; justify-content: center; font-weight: 700; font-size: 14px; flex-shrink: 0;
}
.pf-rank.top { background: var(--qy-brand); color: #fff; }
.pf-mid { flex: 1; min-width: 0; }
.pf-name { font-size: 15px; font-weight: 600; }
.pf-no { color: #b0b8c4; font-weight: 400; font-size: 13px; }
.pf-sub { margin-top: 4px; color: #98a2b3; font-size: 13px; }
.pf-right { text-align: right; flex-shrink: 0; }
.pf-right b { display: block; font-size: 16px; color: var(--qy-brand); }
.pf-right b.good { color: #2d6a4f; }
.pf-right b.bad { color: #d9534f; }
.pf-right span { font-size: 12px; color: #98a2b3; }
</style>
