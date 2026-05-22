<template>
  <div class="page">
    <el-card shadow="never" v-loading="loading">
      <template #header><span>订阅与到期信息</span></template>
      <div v-if="status">
        <el-descriptions :column="2" border>
          <el-descriptions-item label="租户 ID">{{ status.tenantId }}</el-descriptions-item>
          <el-descriptions-item label="状态">
            <el-tag :type="statusType">{{ statusLabel }}</el-tag>
          </el-descriptions-item>
          <el-descriptions-item label="当前套餐">{{ status.currentPlanName ?? '—' }}</el-descriptions-item>
          <el-descriptions-item label="到期时间">
            {{ status.expireAt ? dayjs(status.expireAt).format('YYYY-MM-DD HH:mm') : '—' }}
          </el-descriptions-item>
          <el-descriptions-item label="距离到期" :span="2">
            <span v-if="status.daysToExpire == null">—</span>
            <el-tag v-else-if="status.daysToExpire <= 0" type="danger">已过期 {{ -status.daysToExpire }} 天</el-tag>
            <el-tag v-else-if="status.daysToExpire <= 7" type="danger">{{ status.daysToExpire }} 天</el-tag>
            <el-tag v-else-if="status.daysToExpire <= 30" type="warning">{{ status.daysToExpire }} 天</el-tag>
            <el-tag v-else type="success">{{ status.daysToExpire }} 天</el-tag>
          </el-descriptions-item>
        </el-descriptions>

        <el-divider>说明</el-divider>
        <p>订阅到期后，系统将进入<strong>只读</strong>模式：只能查询数据，无法新建订单、充值或编辑配置。</p>
        <p>当前到期时间之后，新购买的年限会从当前到期时间继续累加；如已过期，则从今天起算。</p>
      </div>
      <el-empty v-else description="暂无订阅信息" />
    </el-card>

    <el-card v-if="status" shadow="never" style="margin-top: 16px">
      <template #header>
        <span>购买 / 续费</span>
        <el-tag v-if="status.status === 'Trial'" type="info" size="small" style="margin-left: 8px">
          试用账号
        </el-tag>
      </template>

      <el-form
        :model="payForm"
        label-width="120px"
        :inline="false"
        v-loading="plansLoading"
        @submit.prevent
      >
        <el-form-item label="套餐">
          <el-radio-group v-model="payForm.planId" aria-label="选择订阅套餐">
            <el-radio
              v-for="p in plans"
              :key="p.id"
              :value="p.id"
              border
              style="margin: 6px 12px 6px 0; min-width: 280px"
            >
              <div class="plan-radio">
                <div class="plan-name">{{ p.name }}</div>
                <div class="plan-meta">
                  ￥{{ p.annualPrice.toFixed(0) }} / 年 ·
                  门店 {{ p.maxStores }} · 员工 {{ p.maxStaff }}
                </div>
              </div>
            </el-radio>
          </el-radio-group>
          <el-empty v-if="!plansLoading && plans.length === 0" description="暂无可购套餐，请联系平台方" :image-size="60" />
        </el-form-item>

        <el-form-item label="购买年限">
          <el-input-number v-model="payForm.years" :min="1" :max="10" controls-position="right" />
          <span style="margin-left: 12px; color: var(--el-text-color-secondary)">
            到期日将延后 {{ payForm.years }} 年
          </span>
        </el-form-item>

        <el-form-item label="支付渠道">
          <el-radio-group v-model="payForm.channel" aria-label="支付渠道">
            <el-radio value="Wechat">微信支付</el-radio>
            <el-radio value="Alipay">支付宝</el-radio>
          </el-radio-group>
        </el-form-item>

        <el-form-item label="应付金额">
          <el-tag type="warning" size="large" effect="dark">
            ￥{{ totalAmount.toFixed(2) }}
          </el-tag>
        </el-form-item>

        <el-form-item>
          <el-button
            type="primary"
            size="large"
            :loading="creating"
            :disabled="plans.length === 0 || !payForm.planId"
            @click="createPayment"
          >
            发起续费
          </el-button>
          <el-button @click="loadAll">刷新</el-button>
        </el-form-item>
      </el-form>

      <el-alert
        v-if="status.expireAt && status.daysToExpire != null && status.daysToExpire <= 30"
        type="warning"
        :closable="false"
        show-icon
        style="margin-top: 12px"
      >
        距离到期还有 {{ status.daysToExpire <= 0 ? '已逾期' : status.daysToExpire + ' 天' }}，建议尽快续费以避免影响门店使用。
      </el-alert>
    </el-card>

    <el-dialog
      v-model="payDialogVisible"
      :title="payOrder ? '请在 ' + (payForm.channel === 'Wechat' ? '微信' : '支付宝') + ' 中完成支付' : '正在创建支付单…'"
      width="460px"
      :close-on-click-modal="false"
      :show-close="!polling"
      @close="onPayDialogClose"
    >
      <div v-if="payOrder" class="pay-body">
        <p class="pay-line">订单号：<strong>{{ payOrder.orderNo }}</strong></p>
        <p class="pay-line">金额：<strong style="color: #e6a23c">￥{{ payOrder.amount.toFixed(2) }}</strong></p>
        <p class="pay-line">状态：
          <el-tag :type="payStatusType">{{ payStatusLabel }}</el-tag>
        </p>
        <el-input
          v-if="payOrder.payUrl"
          :model-value="payOrder.payUrl"
          readonly
          size="small"
          style="margin: 8px 0"
        >
          <template #prepend>支付链接</template>
        </el-input>
        <el-alert
          v-if="payOrder.status === 'Pending'"
          type="info"
          :closable="false"
          show-icon
          title="已为您生成支付单，扫码或在第三方完成支付后此处会自动刷新。"
        />
        <el-alert
          v-else-if="payOrder.status === 'Paid'"
          type="success"
          :closable="false"
          show-icon
          title="支付成功，到期时间已延长。"
        />
        <el-alert
          v-else-if="payOrder.status === 'Failed'"
          type="error"
          :closable="false"
          show-icon
          title="支付失败，请重试。"
        />
      </div>
      <div v-else>
        <el-skeleton :rows="3" animated />
      </div>

      <template #footer>
        <el-button v-if="payOrder?.status === 'Pending'" @click="checkOnce" :loading="polling">
          我已支付，立即查询
        </el-button>
        <el-button type="primary" @click="payDialogVisible = false">关闭</el-button>
      </template>
    </el-dialog>
  </div>
</template>

<script setup lang="ts">
import { computed, onMounted, onUnmounted, reactive, ref } from 'vue';
import dayjs from 'dayjs';
import { ElMessage } from 'element-plus';
import { subscriptionsApi, type SubscriptionPaymentResp, type SubscriptionPlan } from '@/api/modules';
import type { SubscriptionStatus } from '@/api/types';

const status = ref<SubscriptionStatus | null>(null);
const loading = ref(false);
const plans = ref<SubscriptionPlan[]>([]);
const plansLoading = ref(false);

const payForm = reactive<{ planId: number | null; years: number; channel: 'Wechat' | 'Alipay' }>({
  planId: null,
  years: 1,
  channel: 'Wechat'
});

const creating = ref(false);
const payDialogVisible = ref(false);
const payOrder = ref<SubscriptionPaymentResp | null>(null);
const polling = ref(false);
let pollTimer: number | null = null;

const statusLabel = computed(() => {
  switch (status.value?.status) {
    case 'Active': return '活跃';
    case 'Trial': return '试用中';
    case 'Expired': return '已过期';
    case 'Disabled': return '已停用';
    default: return status.value?.status ?? '—';
  }
});
const statusType = computed(() => {
  switch (status.value?.status) {
    case 'Active': return 'success';
    case 'Trial': return 'info';
    case 'Expired': return 'warning';
    case 'Disabled': return 'danger';
    default: return 'info';
  }
});

const selectedPlan = computed(() => plans.value.find((p) => p.id === payForm.planId) ?? null);
const totalAmount = computed(() =>
  selectedPlan.value ? selectedPlan.value.annualPrice * payForm.years : 0
);

const payStatusLabel = computed(() => {
  switch (payOrder.value?.status) {
    case 'Pending': return '待支付';
    case 'Paid': return '已支付';
    case 'Failed': return '已失败';
    case 'Refunded': return '已退款';
    default: return payOrder.value?.status ?? '—';
  }
});
const payStatusType = computed(() => {
  switch (payOrder.value?.status) {
    case 'Pending': return 'warning';
    case 'Paid': return 'success';
    case 'Failed': return 'danger';
    case 'Refunded': return 'info';
    default: return 'info';
  }
});

async function loadAll() {
  loading.value = true;
  plansLoading.value = true;
  try {
    const [s, p] = await Promise.all([
      subscriptionsApi.me(),
      subscriptionsApi.plans()
    ]);
    status.value = s;
    plans.value = p;
    if (payForm.planId == null) {
      // 默认选当前套餐；没有就选最便宜的
      const cur = p.find((x) => x.id === s.currentPlanId);
      payForm.planId = cur?.id ?? p[0]?.id ?? null;
    }
  } finally {
    loading.value = false;
    plansLoading.value = false;
  }
}

async function createPayment() {
  if (!status.value || !payForm.planId) return;
  creating.value = true;
  payDialogVisible.value = true;
  payOrder.value = null;
  try {
    const order = await subscriptionsApi.pay({
      tenantId: status.value.tenantId,
      planId: payForm.planId,
      years: payForm.years,
      channel: payForm.channel
    });
    payOrder.value = order;
    startPolling();
  } catch (e) {
    payDialogVisible.value = false;
    throw e;
  } finally {
    creating.value = false;
  }
}

function startPolling() {
  stopPolling();
  pollTimer = window.setInterval(checkOnce, 3000);
}

function stopPolling() {
  if (pollTimer != null) {
    window.clearInterval(pollTimer);
    pollTimer = null;
  }
}

async function checkOnce() {
  if (!payOrder.value) return;
  polling.value = true;
  try {
    const cur = await subscriptionsApi.payStatus(payOrder.value.orderNo);
    payOrder.value = { ...payOrder.value, status: cur.status };
    if (cur.status === 'Paid') {
      stopPolling();
      ElMessage.success('支付成功，正在刷新订阅状态');
      await loadAll();
    } else if (cur.status === 'Failed' || cur.status === 'Refunded') {
      stopPolling();
    }
  } finally {
    polling.value = false;
  }
}

function onPayDialogClose() {
  stopPolling();
}

onMounted(loadAll);
onUnmounted(stopPolling);
</script>

<style scoped>
.page { padding-bottom: 24px; }
.plan-radio { display: flex; flex-direction: column; line-height: 1.4; }
.plan-name { font-weight: 600; font-size: 14px; }
.plan-meta { font-size: 12px; color: var(--el-text-color-secondary); }
.pay-body { padding: 4px 0; }
.pay-line { margin: 6px 0; }
</style>
