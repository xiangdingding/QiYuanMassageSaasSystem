<script lang="ts">
export default { name: 'DayCloseView' };
</script>

<script setup lang="ts">
import { computed, onMounted, ref, watch } from 'vue';
import {
  NavBar as VanNavBar, PullRefresh as VanPullRefresh, Empty as VanEmpty, Tag as VanTag,
  Cell as VanCell, CellGroup as VanCellGroup, Field as VanField, Stepper as VanStepper,
  Button as VanButton, NoticeBar as VanNoticeBar, Dialog,
  showSuccessToast, showConfirmDialog
} from 'vant';
import { dayClosesApi } from '@/api/modules';
import { useAppStore } from '@/stores/app';
import { useAuthStore } from '@/stores/auth';
import type { DayClose, DayClosePreview } from '@/api/types';

const appStore = useAppStore();
const auth = useAuthStore();
const canRevoke = computed(() => auth.isOwner || auth.isManager);

const businessDate = ref(todayStr());
const preview = ref<DayClosePreview | null>(null);
const actualCash = ref(0);
const remark = ref('');
const submitting = ref(false);
const history = ref<DayClose[]>([]);
const refreshing = ref(false);

function todayStr() {
  const d = new Date();
  const pad = (n: number) => String(n).padStart(2, '0');
  return `${d.getFullYear()}-${pad(d.getMonth() + 1)}-${pad(d.getDate())}`;
}
function fmt(n?: number | null) { return (n ?? 0).toFixed(2); }
const variance = computed(() => actualCash.value - (preview.value?.expectedCash ?? 0));

async function loadPreview(date?: string) {
  if (!appStore.activeStoreId) return;
  preview.value = await dayClosesApi.preview(appStore.activeStoreId, date);
  businessDate.value = preview.value.businessDate.slice(0, 10);
  actualCash.value = preview.value.expectedCash;
  remark.value = '';
}
async function loadHistory() {
  if (!appStore.activeStoreId) return;
  history.value = await dayClosesApi.history(appStore.activeStoreId).catch(() => []);
}
async function reloadAll() {
  try { await Promise.all([loadPreview(), loadHistory()]); }
  finally { refreshing.value = false; }
}

watch(businessDate, (v) => {
  if (preview.value && v !== preview.value.businessDate.slice(0, 10)) loadPreview(v);
});

async function submit() {
  if (!appStore.activeStoreId || !preview.value) return;
  if (Math.abs(variance.value) > 0.005) {
    try { await showConfirmDialog({ title: '差额确认', message: `差额 ¥${variance.value.toFixed(2)}，确定提交？` }); }
    catch { return; }
  }
  submitting.value = true;
  try {
    await dayClosesApi.submit({
      storeId: appStore.activeStoreId, businessDate: businessDate.value,
      actualCash: actualCash.value, remark: remark.value || null
    });
    showSuccessToast('日结已提交');
    await reloadAll();
  } catch { /* */ } finally { submitting.value = false; }
}

async function revoke(row: DayClose) {
  try {
    const res = await Dialog.confirm({
      title: '撤销日结',
      message: `将撤销 ${row.businessDate.slice(0, 10)} 的日结（营业额 ¥${fmt(row.revenueTotal)}）。撤销后可重新提交。`,
      confirmButtonText: '确认撤销'
    }).then(() => true).catch(() => false);
    if (!res) return;
    await dayClosesApi.revoke(row.id, '移动端撤销');
    showSuccessToast('已撤销');
    await reloadAll();
  } catch { /* */ }
}

onMounted(async () => {
  if (!appStore.stores.length) await appStore.loadStores().catch(() => undefined);
  reloadAll();
});
</script>

<template>
  <div class="qy-page dayclose">
    <van-nav-bar :title="`日结 · ${appStore.activeStore?.name || ''}`" left-text="返回" left-arrow @click-left="$router.back()" />

    <van-pull-refresh v-model="refreshing" @refresh="reloadAll">
      <div class="card">
        <div class="dc-date">
          <span>营业日</span>
          <input v-model="businessDate" type="date" class="date-input" :max="todayStr()" />
        </div>
        <van-notice-bar v-if="preview?.alreadyClosed" wrapable :scrollable="false" text="该日已日结，仅供查看。" />

        <template v-if="preview">
          <div class="kpi">
            <div><b>{{ preview.orderCount }}</b><span>完成订单</span></div>
            <div><b class="qy-money">{{ fmt(preview.revenueTotal) }}</b><span>营业额</span></div>
            <div><b class="qy-money">{{ fmt(preview.expectedCash) }}</b><span>预期现金</span></div>
            <div><b class="qy-money">{{ fmt(preview.rechargeAmount) }}</b><span>充值入账</span></div>
          </div>

          <van-cell-group inset title="支付方式分布">
            <van-cell title="现金" :value="`¥ ${fmt(preview.cashAmount)}`" />
            <van-cell title="会员卡" :value="`¥ ${fmt(preview.memberCardAmount)}`" />
            <van-cell title="微信" :value="`¥ ${fmt(preview.wechatAmount)}`" />
            <van-cell title="支付宝" :value="`¥ ${fmt(preview.alipayAmount)}`" />
            <van-cell title="银行卡" :value="`¥ ${fmt(preview.bankCardAmount)}`" />
          </van-cell-group>

          <van-cell-group inset title="实收清点">
            <van-field label="实际现金">
              <template #input><van-stepper v-model="actualCash" :min="0" :step="10" :decimal-length="2" :disabled="preview.alreadyClosed" /></template>
            </van-field>
            <van-cell title="差额">
              <template #value>
                <span :class="['var', variance === 0 ? 'ok' : (variance < 0 ? 'short' : 'over')]">¥{{ variance.toFixed(2) }}</span>
              </template>
            </van-cell>
            <van-field v-model="remark" label="备注" type="textarea" rows="1" autosize :disabled="preview.alreadyClosed" placeholder="可选" />
          </van-cell-group>

          <div v-if="!preview.alreadyClosed" class="submit">
            <van-button block type="primary" :loading="submitting" @click="submit">提交日结</van-button>
          </div>
        </template>
      </div>

      <p class="qy-section-title">历史日结</p>
      <van-empty v-if="history.length === 0" description="暂无历史日结" />
      <div v-for="h in history" :key="h.id" class="hist">
        <div class="h-main">
          <div class="h-date">{{ h.businessDate.slice(0, 10) }}</div>
          <div class="h-sub">{{ h.orderCount }}单 · 营业 ¥{{ fmt(h.revenueTotal) }} · 实收 ¥{{ fmt(h.actualCash) }}</div>
          <div v-if="h.operatorName" class="h-op">操作员 {{ h.operatorName }}</div>
        </div>
        <div class="h-right">
          <van-tag :type="h.variance === 0 ? 'success' : (h.variance < 0 ? 'danger' : 'warning')">差 ¥{{ fmt(h.variance) }}</van-tag>
          <van-button v-if="canRevoke" size="mini" type="danger" plain @click="revoke(h)">撤销</van-button>
        </div>
      </div>
    </van-pull-refresh>
  </div>
</template>

<style scoped>
.card { background: #fff; margin: 10px 12px; border-radius: 12px; padding: 14px; }
.dc-date { display: flex; align-items: center; justify-content: space-between; margin-bottom: 10px; }
.dc-date span { color: #6b7280; font-size: 14px; }
.date-input { border: 1px solid #e3e7ec; border-radius: 8px; padding: 6px 10px; font-size: 14px; font-family: inherit; }
.kpi { display: grid; grid-template-columns: repeat(2, 1fr); gap: 8px; margin: 12px 0; }
.kpi div { background: #f5f7f9; border-radius: 10px; padding: 12px 6px; text-align: center; }
.kpi b { display: block; font-size: 18px; color: #16324a; }
.kpi span { font-size: 12px; color: #98a2b3; }
.var { font-weight: 700; }
.var.ok { color: #16a34a; }
.var.short { color: #ee4d4d; }
.var.over { color: #f0a020; }
.submit { padding: 14px 4px 0; }
.hist { display: flex; align-items: center; justify-content: space-between; background: #fff; margin: 8px 12px; padding: 14px; border-radius: 12px; }
.h-date { font-weight: 600; }
.h-sub { margin-top: 4px; color: #6b7280; font-size: 13px; }
.h-op { margin-top: 2px; color: #98a2b3; font-size: 12px; }
.h-right { display: flex; flex-direction: column; align-items: flex-end; gap: 8px; }
</style>
