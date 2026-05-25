<template>
  <el-dialog
    :model-value="modelValue"
    @update:model-value="(v: boolean) => emit('update:modelValue', v)"
    title="结账"
    width="460px"
    aria-label="结账对话框，按 Esc 关闭，Ctrl+回车确认"
    :close-on-press-escape="true"
    :close-on-click-modal="false"
    @open="onOpen"
    @keydown.ctrl.enter.stop="onCtrlEnter"
  >
    <el-form :model="form" label-width="90px">
      <el-form-item label="应收">
        <span class="payable">¥ {{ payable.toFixed(2) }}</span>
        <span v-if="total !== payable" class="muted" style="margin-left: 8px">
          （合计 ¥{{ total.toFixed(2) }}）
        </span>
      </el-form-item>
      <el-form-item label="支付方式">
        <el-radio-group v-model="form.payMethod">
          <el-radio-button value="Cash">现金</el-radio-button>
          <el-radio-button value="MemberCard" :disabled="!hasMember">会员卡</el-radio-button>
          <el-radio-button value="Wechat">微信</el-radio-button>
          <el-radio-button value="Alipay">支付宝</el-radio-button>
          <el-radio-button value="BankCard">银行卡</el-radio-button>
        </el-radio-group>
      </el-form-item>
      <el-form-item v-if="form.payMethod === 'MemberCard'" label="会员余额">
        <span :class="{ insufficient: memberBalance < payable }">
          ¥ {{ memberBalance.toFixed(2) }}
        </span>
        <span v-if="memberBalance < payable" class="muted" style="margin-left: 8px">
          余额不足
        </span>
      </el-form-item>
      <el-form-item v-if="form.payMethod === 'Cash'" label="实收金额">
        <el-input-number v-model="form.paidAmount" :min="payable" :precision="2" :step="10" style="width: 200px" />
        <div v-if="form.paidAmount && form.paidAmount > payable" class="muted" style="margin-top: 6px">
          找零 ¥ {{ (form.paidAmount - payable).toFixed(2) }}
        </div>
      </el-form-item>
      <el-form-item label="备注">
        <el-input v-model="form.remark" type="textarea" :rows="2" placeholder="可选" />
      </el-form-item>
    </el-form>
    <template #footer>
      <el-button @click="emit('update:modelValue', false)">取消</el-button>
      <el-button
        type="primary"
        :loading="loading"
        :disabled="!canSubmit"
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

function onOpen() {
  // 有会员则默认走会员卡结算（余额不足时收银员可手动改其它方式）
  form.payMethod = props.hasMember ? 'MemberCard' : 'Cash';
  form.paidAmount = props.payable;
  form.remark = '';
  // 弹窗打开后把焦点放到第一个 RadioButton（el-dialog 自带焦点陷阱，Tab 不会跑出去）
  // Element Plus 已自动 focus 到 dialog 内首个可聚焦元素，不再额外处理
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
.payable { font-size: 22px; font-weight: 700; color: #d9534f; }
.muted { color: var(--el-text-color-secondary); font-size: 13px; }
.insufficient { color: #f56c6c; font-weight: 600; }
</style>
