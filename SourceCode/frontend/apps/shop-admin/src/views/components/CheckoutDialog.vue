<template>
  <el-dialog
    :model-value="modelValue"
    @update:model-value="(v: boolean) => emit('update:modelValue', v)"
    title="结账"
    :aria-label="dialogAriaLabel"
    width="520px"
    :close-on-press-escape="true"
    :close-on-click-modal="false"
    @open="onOpen"
    @keydown.ctrl.enter.stop="onCtrlEnter"
  >
    <el-form :model="form" label-width="100px" size="large">
      <el-form-item label="应收">
        <span
          class="payable"
          :aria-label="payableAriaLabel"
        >¥ {{ payable.toFixed(2) }}</span>
        <span v-if="total !== payable" class="muted" style="margin-left: 8px" aria-hidden="true">
          （合计 ¥{{ total.toFixed(2) }}）
        </span>
      </el-form-item>
      <el-form-item label="支付方式">
        <el-radio-group
          v-model="form.payMethod"
          aria-label="支付方式，按方向键切换：现金、会员卡、微信、支付宝、银行卡"
        >
          <el-radio-button value="Cash" aria-label="现金">现金</el-radio-button>
          <el-radio-button
            value="MemberCard"
            :disabled="!hasMember"
            :aria-label="hasMember ? '会员卡' : '会员卡，未关联会员时不可选'"
          >会员卡</el-radio-button>
          <el-radio-button value="Wechat" aria-label="微信">微信</el-radio-button>
          <el-radio-button value="Alipay" aria-label="支付宝">支付宝</el-radio-button>
          <el-radio-button value="BankCard" aria-label="银行卡">银行卡</el-radio-button>
        </el-radio-group>
      </el-form-item>
      <el-form-item v-if="form.payMethod === 'MemberCard'" label="会员余额">
        <span
          :class="{ insufficient: memberBalance < payable }"
          :aria-label="memberBalanceAriaLabel"
        >¥ {{ memberBalance.toFixed(2) }}</span>
        <el-tag
          v-if="memberBalance < payable"
          type="danger"
          size="default"
          style="margin-left:8px"
          aria-hidden="true"
        >余额不足</el-tag>
      </el-form-item>
      <el-form-item v-if="form.payMethod === 'Cash'" label="实收金额">
        <el-input-number
          v-model="form.paidAmount"
          :min="payable"
          :precision="2"
          :step="10"
          style="width: 220px"
          :aria-label="cashInputAriaLabel"
        />
        <div
          v-if="form.paidAmount && form.paidAmount > payable"
          class="muted"
          style="margin-top: 6px"
          role="status"
          :aria-label="`找零 ${yuanReadable(form.paidAmount - payable)}`"
        >
          找零 ¥ {{ (form.paidAmount - payable).toFixed(2) }}
        </div>
      </el-form-item>
      <el-form-item label="备注">
        <el-input
          v-model="form.remark"
          type="textarea"
          :rows="2"
          placeholder="可选，留言会写到订单备注"
          aria-label="结账备注，可选"
        />
      </el-form-item>
    </el-form>
    <template #footer>
      <el-button
        size="large"
        :aria-label="'放弃结账并关闭对话框'"
        @click="emit('update:modelValue', false)"
      >取消</el-button>
      <el-button
        type="primary"
        size="large"
        :loading="loading"
        :disabled="!canSubmit"
        :aria-label="confirmAriaLabel"
        @click="submit"
      >确认结账</el-button>
    </template>
  </el-dialog>
</template>

<script setup lang="ts">
import { computed, reactive, watch } from 'vue';

const props = defineProps<{
  modelValue: boolean;
  total: number;
  payable: number;
  hasMember: boolean;
  memberBalance: number;
  loading: boolean;
}>();

const emit = defineEmits<{
  (e: 'update:modelValue', v: boolean): void;
  (e: 'submit', payload: { payMethod: string; paidAmount: number | null; remark: string | null }): void;
}>();

const form = reactive<{ payMethod: string; paidAmount: number; remark: string }>({
  payMethod: 'Cash',
  paidAmount: 0,
  remark: ''
});

watch(
  () => props.payable,
  (v) => {
    form.paidAmount = v;
  },
  { immediate: true }
);

watch(
  () => form.payMethod,
  (m) => {
    if (m !== 'Cash') form.paidAmount = props.payable;
  }
);

const canSubmit = computed(() => {
  if (form.payMethod === 'MemberCard' && props.memberBalance < props.payable) return false;
  if (form.payMethod === 'Cash' && form.paidAmount < props.payable) return false;
  return true;
});

/// 金额朗读：避免读屏把 "32.5" 念成"三十二点五"，统一成"32 元 5 角"
function yuanReadable(amount: number): string {
  const safe = Number.isFinite(amount) ? amount : 0;
  const yuan = Math.floor(safe);
  const jiao = Math.round((safe - yuan) * 10);
  return jiao === 0 ? `${yuan} 元` : `${yuan} 元 ${jiao} 角`;
}

function payMethodLabel(m: string) {
  return ({
    Cash: '现金', MemberCard: '会员卡', Wechat: '微信', Alipay: '支付宝', BankCard: '银行卡'
  } as Record<string, string>)[m] ?? m;
}

const dialogAriaLabel = computed(
  () => `结账对话框，应收 ${yuanReadable(props.payable)}，按 Esc 关闭，Ctrl 加回车确认`
);

const payableAriaLabel = computed(() => {
  const base = `应收 ${yuanReadable(props.payable)}`;
  if (props.total !== props.payable) return `${base}，合计 ${yuanReadable(props.total)}`;
  return base;
});

const memberBalanceAriaLabel = computed(() => {
  const base = `会员余额 ${yuanReadable(props.memberBalance)}`;
  if (props.memberBalance < props.payable) {
    return `${base}，不足以支付应收 ${yuanReadable(props.payable)}，请换支付方式`;
  }
  return base;
});

const cashInputAriaLabel = computed(
  () => `输入实收现金金额，最少 ${yuanReadable(props.payable)}，按上下键调整 10 元`
);

const confirmAriaLabel = computed(() => {
  if (!canSubmit.value) return '确认结账，当前条件不满足';
  const method = payMethodLabel(form.payMethod);
  const change = form.payMethod === 'Cash' && form.paidAmount > props.payable
    ? `，找零 ${yuanReadable(form.paidAmount - props.payable)}`
    : '';
  return `确认结账，支付方式 ${method}，应收 ${yuanReadable(props.payable)}${change}`;
});

function onOpen() {
  // 有会员则默认走会员卡结算（余额不足时收银员可手动改其它方式）
  form.payMethod = props.hasMember ? 'MemberCard' : 'Cash';
  form.paidAmount = props.payable;
  form.remark = '';
}

function onCtrlEnter() {
  if (canSubmit.value) submit();
}

function submit() {
  emit('submit', {
    payMethod: form.payMethod,
    paidAmount: form.payMethod === 'Cash' ? form.paidAmount : null,
    remark: form.remark || null
  });
}
</script>

<style scoped>
.payable { font-size: 24px; font-weight: 700; color: #d9534f; }
.muted { color: var(--el-text-color-secondary); font-size: 13px; }
.insufficient { color: #f56c6c; font-weight: 700; font-size: 18px; }
</style>
