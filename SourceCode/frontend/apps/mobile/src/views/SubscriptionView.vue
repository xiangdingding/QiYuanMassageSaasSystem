<script lang="ts">
export default { name: 'SubscriptionView' };
</script>

<script setup lang="ts">
import { computed, onMounted, onUnmounted, reactive, ref } from 'vue';
import {
  NavBar as VanNavBar, Cell as VanCell, CellGroup as VanCellGroup, Tag as VanTag,
  Button as VanButton, Stepper as VanStepper, Field as VanField, Popup as VanPopup,
  Loading as VanLoading, NoticeBar as VanNoticeBar,
  showSuccessToast
} from 'vant';
import { subscriptionsApi, type SubscriptionPlan, type SubscriptionPaymentResp } from '@/api/modules';
import type { PlatformSubscriptionSetting, SubscriptionStatus } from '@/api/types';

const status = ref<SubscriptionStatus | null>(null);
const loading = ref(false);
const plans = ref<SubscriptionPlan[]>([]);
const setting = ref<PlatformSubscriptionSetting | null>(null);
const noticeLines = computed(() => (setting.value?.expiryNotice ?? '').split('\n').filter((l) => l.trim()));

const payForm = reactive<{ planId: number | null; years: number; channel: 'Wechat' | 'Alipay' }>({ planId: null, years: 1, channel: 'Wechat' });
const creating = ref(false);
const payOpen = ref(false);
const payOrder = ref<SubscriptionPaymentResp | null>(null);
const polling = ref(false);
let pollTimer: ReturnType<typeof setInterval> | null = null;

function fmt(n?: number | null) { return (n ?? 0).toFixed(2); }
const statusLabel = computed(() => ({ Active: '活跃', Trial: '试用中', Expired: '已过期', Disabled: '已停用' } as Record<string, string>)[status.value?.status ?? ''] ?? (status.value?.status ?? '—'));
type TagKind = 'success' | 'primary' | 'warning' | 'danger' | 'default';
const statusType = computed<TagKind>(() => ({ Active: 'success', Trial: 'primary', Expired: 'warning', Disabled: 'danger' } as Record<string, TagKind>)[status.value?.status ?? ''] ?? 'default');
const selectedPlan = computed(() => plans.value.find((p) => p.id === payForm.planId) ?? null);
const totalAmount = computed(() => selectedPlan.value ? selectedPlan.value.annualPrice * payForm.years : 0);
const payStatusLabel = computed(() => ({ Pending: '待支付', Paid: '已支付', Failed: '已失败', Refunded: '已退款' } as Record<string, string>)[payOrder.value?.status ?? ''] ?? (payOrder.value?.status ?? '—'));

async function loadAll() {
  loading.value = true;
  try {
    const [s, p, cfg] = await Promise.all([
      subscriptionsApi.me(), subscriptionsApi.plans(), subscriptionsApi.platformSetting().catch(() => null)
    ]);
    status.value = s; plans.value = p; setting.value = cfg;
    if (payForm.planId == null) payForm.planId = p.find((x) => x.id === s.currentPlanId)?.id ?? p[0]?.id ?? null;
  } catch { /* */ } finally { loading.value = false; }
}

async function createPayment() {
  if (!status.value || !payForm.planId) return;
  creating.value = true;
  payOpen.value = true;
  payOrder.value = null;
  try {
    payOrder.value = await subscriptionsApi.pay({ tenantId: status.value.tenantId, planId: payForm.planId, years: payForm.years, channel: payForm.channel });
    startPolling();
  } catch { payOpen.value = false; } finally { creating.value = false; }
}
function startPolling() { stopPolling(); pollTimer = setInterval(checkOnce, 3000); }
function stopPolling() { if (pollTimer) { clearInterval(pollTimer); pollTimer = null; } }
async function checkOnce() {
  if (!payOrder.value) return;
  polling.value = true;
  try {
    const cur = await subscriptionsApi.payStatus(payOrder.value.orderNo);
    payOrder.value = { ...payOrder.value, status: cur.status };
    if (cur.status === 'Paid') { stopPolling(); showSuccessToast('支付成功'); await loadAll(); }
    else if (cur.status === 'Failed' || cur.status === 'Refunded') stopPolling();
  } catch { /* */ } finally { polling.value = false; }
}

onMounted(loadAll);
onUnmounted(stopPolling);
</script>

<template>
  <div class="qy-page subscription">
    <van-nav-bar title="服务订阅" left-text="返回" left-arrow @click-left="$router.back()" />

    <div v-if="loading && !status" class="loading"><van-loading>加载中…</van-loading></div>

    <template v-else-if="status">
      <van-cell-group inset title="订阅与到期">
        <van-cell title="状态"><template #value><van-tag :type="statusType">{{ statusLabel }}</van-tag></template></van-cell>
        <van-cell title="当前套餐" :value="status.currentPlanName ?? '—'" />
        <van-cell title="到期时间" :value="status.expireAt ? status.expireAt.slice(0, 10) : '—'" />
        <van-cell title="距离到期">
          <template #value>
            <span v-if="status.daysToExpire == null">—</span>
            <van-tag v-else-if="status.daysToExpire <= 0" type="danger">已过期 {{ -status.daysToExpire }} 天</van-tag>
            <van-tag v-else-if="status.daysToExpire <= 7" type="danger">{{ status.daysToExpire }} 天</van-tag>
            <van-tag v-else-if="status.daysToExpire <= 30" type="warning">{{ status.daysToExpire }} 天</van-tag>
            <van-tag v-else type="success">{{ status.daysToExpire }} 天</van-tag>
          </template>
        </van-cell>
      </van-cell-group>

      <div v-if="noticeLines.length || setting?.contactPhone" class="notice card">
        <p v-for="(l, i) in noticeLines" :key="i" class="notice-line">{{ l }}</p>
        <p v-if="setting?.contactPhone || setting?.contactWechat" class="notice-contact">
          客服电话：<b>{{ setting?.contactPhone || '—' }}</b> · 微信：<b>{{ setting?.contactWechat || '—' }}</b>
        </p>
      </div>

      <p class="qy-section-title">购买 / 续费</p>
      <div class="plans">
        <button v-for="p in plans" :key="p.id" class="plan" :class="{ on: payForm.planId === p.id }" @click="payForm.planId = p.id">
          <div class="p-head">
            <span class="p-name">{{ p.name }}</span>
            <van-tag v-if="status.currentPlanId === p.id" type="primary">当前</van-tag>
          </div>
          <div class="p-price">¥{{ Math.round(p.annualPrice) }}<span>/年</span></div>
          <div class="p-spec">最多 {{ p.maxStores }} 店 · {{ p.maxStaff }} 员工</div>
        </button>
      </div>
      <van-empty v-if="!plans.length" description="暂无可购套餐，请联系平台方" />

      <van-cell-group v-if="plans.length" inset>
        <van-field label="购买年限"><template #input><van-stepper v-model="payForm.years" :min="1" :max="10" /></template></van-field>
        <van-field label="支付渠道">
          <template #input>
            <div class="seg">
              <button type="button" :class="{ on: payForm.channel === 'Wechat' }" @click="payForm.channel = 'Wechat'">微信支付</button>
              <button type="button" :class="{ on: payForm.channel === 'Alipay' }" @click="payForm.channel = 'Alipay'">支付宝</button>
            </div>
          </template>
        </van-field>
        <van-cell title="应付金额"><template #value><b class="qy-money pay-amt">¥{{ fmt(totalAmount) }}</b></template></van-cell>
      </van-cell-group>

      <div v-if="plans.length" class="submit">
        <van-button block type="primary" :loading="creating" :disabled="!payForm.planId" @click="createPayment">发起续费</van-button>
      </div>
    </template>

    <!-- 支付单 -->
    <van-popup v-model:show="payOpen" position="bottom" round :close-on-click-overlay="!polling">
      <div class="sheet">
        <div class="sheet-title">{{ payOrder ? `请在${payForm.channel === 'Wechat' ? '微信' : '支付宝'}完成支付` : '创建支付单…' }}</div>
        <div v-if="payOrder" class="pay">
          <van-cell-group inset>
            <van-cell title="订单号" :value="payOrder.orderNo" />
            <van-cell title="金额" :value="`¥ ${fmt(payOrder.amount)}`" />
            <van-cell title="状态" :value="payStatusLabel" />
          </van-cell-group>
          <van-notice-bar v-if="payOrder.status === 'Pending'" wrapable :scrollable="false" text="已生成支付单，在第三方完成支付后此处会自动刷新。" />
          <van-notice-bar v-else-if="payOrder.status === 'Paid'" wrapable :scrollable="false" color="#16a34a" background="#eaf7ee" text="支付成功，到期时间已延长。" />
          <van-notice-bar v-else-if="payOrder.status === 'Failed'" wrapable :scrollable="false" color="#ee4d4d" background="#fdeaea" text="支付失败，请重试。" />
          <div v-if="payOrder.payUrl" class="payurl">支付链接：{{ payOrder.payUrl }}</div>
        </div>
        <div v-else class="loading"><van-loading /></div>
        <div class="sheet-actions">
          <van-button v-if="payOrder?.status === 'Pending'" block plain :loading="polling" @click="checkOnce">我已支付，立即查询</van-button>
          <van-button block type="primary" style="margin-top:8px" @click="payOpen = false">关闭</van-button>
        </div>
      </div>
    </van-popup>
  </div>
</template>

<style scoped>
.loading { padding: 50px 0; text-align: center; }
.card { background: #fff; margin: 12px; border-radius: 12px; padding: 14px; }
.notice-line { margin: 4px 0; color: #475569; font-size: 13px; line-height: 1.7; }
.notice-contact { margin: 10px 0 0; color: #9a3412; font-size: 13px; }
.plans { display: grid; grid-template-columns: repeat(2, 1fr); gap: 10px; padding: 0 12px; }
.plan { text-align: left; background: #fff; border: 1.5px solid #e3e7ec; border-radius: 12px; padding: 14px; }
.plan.on { border-color: var(--qy-brand); box-shadow: 0 4px 14px -8px rgba(14,122,107,.4); }
.p-head { display: flex; align-items: center; justify-content: space-between; }
.p-name { font-weight: 600; font-size: 15px; }
.p-price { color: var(--qy-brand); font-size: 24px; font-weight: 800; margin: 8px 0 6px; }
.p-price span { font-size: 12px; color: #98a2b3; font-weight: 400; margin-left: 2px; }
.p-spec { color: #98a2b3; font-size: 12px; }
.pay-amt { color: #ee4d4d; font-size: 18px; }
.submit { padding: 16px; }
.seg { display: flex; gap: 8px; width: 100%; }
.seg button { flex: 1; border: 1px solid #d6dbe2; background: #fff; color: #4b5563; border-radius: 8px; padding: 6px 0; font-size: 14px; }
.seg button.on { background: var(--qy-brand); color: #fff; border-color: var(--qy-brand); }
.sheet { padding: 16px 0 24px; }
.sheet-title { text-align: center; font-size: 16px; font-weight: 700; margin-bottom: 12px; padding: 0 16px; }
.sheet-actions { padding: 16px 16px 0; }
.payurl { word-break: break-all; font-size: 12px; color: #6b7280; padding: 10px 28px 0; }
</style>
