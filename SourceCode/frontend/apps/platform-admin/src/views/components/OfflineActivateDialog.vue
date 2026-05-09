<template>
  <el-dialog
    :model-value="modelValue"
    @update:model-value="(v: boolean) => emit('update:modelValue', v)"
    :title="`线下续费/激活：${tenant?.name ?? ''}`"
    width="480px"
    @close="reset"
  >
    <el-form :model="form" :rules="rules" ref="formRef" label-width="120px">
      <el-form-item label="套餐" prop="planId">
        <el-select v-model="form.planId" placeholder="请选择" style="width: 100%">
          <el-option
            v-for="p in plans"
            :key="p.id"
            :label="`${p.name} - ¥${p.annualPrice}/年`"
            :value="p.id"
          />
        </el-select>
      </el-form-item>
      <el-form-item label="购买年限" prop="years">
        <el-input-number v-model="form.years" :min="1" :max="10" />
      </el-form-item>
      <el-form-item label="实收金额" prop="amountReceived">
        <el-input-number v-model="form.amountReceived" :min="0" :precision="2" :step="100" style="width: 200px" />
        <span style="margin-left: 8px; color: var(--el-text-color-secondary)">元</span>
      </el-form-item>
      <el-form-item label="备注">
        <el-input v-model="form.remark" type="textarea" :rows="2" placeholder="如：已收到银行转账 12800 元" />
      </el-form-item>
    </el-form>
    <template #footer>
      <el-button @click="emit('update:modelValue', false)">取消</el-button>
      <el-button type="primary" :loading="loading" @click="submit">确认激活</el-button>
    </template>
  </el-dialog>
</template>

<script setup lang="ts">
import { reactive, ref, watch } from 'vue';
import { ElMessage, type FormInstance, type FormRules } from 'element-plus';
import { subscriptionsApi } from '@/api/modules';
import type { Plan, TenantSummary } from '@/api/types';

const props = defineProps<{
  modelValue: boolean;
  tenant: TenantSummary | null;
  plans: Plan[];
}>();
const emit = defineEmits<{
  (e: 'update:modelValue', v: boolean): void;
  (e: 'activated'): void;
}>();

const formRef = ref<FormInstance>();
const loading = ref(false);

const form = reactive({
  planId: null as number | null,
  years: 1,
  amountReceived: 0,
  remark: ''
});

const rules: FormRules = {
  planId: [{ required: true, message: '请选择套餐', trigger: 'change' }],
  years: [{ required: true, message: '请填写年限', trigger: 'blur' }],
  amountReceived: [{ required: true, message: '请填写实收金额', trigger: 'blur' }]
};

watch(
  () => [form.planId, form.years] as const,
  ([pid, years]) => {
    if (pid != null && form.amountReceived === 0) {
      const plan = props.plans.find((p) => p.id === pid);
      if (plan) form.amountReceived = +(plan.annualPrice * years).toFixed(2);
    }
  }
);

watch(
  () => props.modelValue,
  (v) => {
    if (v && props.tenant?.currentPlanId) {
      form.planId = props.tenant.currentPlanId;
    }
  }
);

function reset() {
  form.planId = null;
  form.years = 1;
  form.amountReceived = 0;
  form.remark = '';
}

async function submit() {
  if (!formRef.value || !props.tenant) return;
  const ok = await formRef.value.validate().catch(() => false);
  if (!ok) return;
  loading.value = true;
  try {
    await subscriptionsApi.activateOffline({
      tenantId: props.tenant.id,
      planId: form.planId!,
      years: form.years,
      amountReceived: form.amountReceived,
      remark: form.remark || null
    });
    ElMessage.success('激活成功');
    emit('activated');
    emit('update:modelValue', false);
  } finally {
    loading.value = false;
  }
}
</script>
