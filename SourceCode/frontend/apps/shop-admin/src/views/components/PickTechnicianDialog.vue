<template>
  <el-dialog
    :model-value="modelValue"
    @update:model-value="(v: boolean) => emit('update:modelValue', v)"
    :title="`指派技师：${service?.name ?? ''}`"
    width="420px"
    @close="reset"
  >
    <el-form :model="form" label-width="90px" v-if="service">
      <el-form-item label="单价">
        <span class="price">¥ {{ unit.toFixed(2) }}</span>
        <span class="muted" style="margin-left: 8px">{{ service.durationMinutes }} 分钟</span>
      </el-form-item>
      <el-form-item label="技师">
        <el-select v-model="form.technicianId" filterable placeholder="搜索工号或姓名" style="width: 100%">
          <el-option
            v-for="t in technicians"
            :key="t.id"
            :label="`${t.employeeNo ?? '-'} · ${t.realName ?? t.username}`"
            :value="t.id"
          />
        </el-select>
      </el-form-item>
      <el-form-item label="数量">
        <el-input-number v-model="form.quantity" :min="1" :max="10" />
      </el-form-item>
    </el-form>
    <template #footer>
      <el-button @click="emit('update:modelValue', false)">取消</el-button>
      <el-button type="primary" :disabled="!form.technicianId" @click="confirm">加入订单</el-button>
    </template>
  </el-dialog>
</template>

<script setup lang="ts">
import { computed, reactive } from 'vue';
import type { ServiceItem, Staff } from '@/api/types';

const props = defineProps<{
  modelValue: boolean;
  service: ServiceItem | null;
  technicians: Staff[];
  isMember: boolean;
}>();
const emit = defineEmits<{
  (e: 'update:modelValue', v: boolean): void;
  (e: 'confirm', payload: { technicianId: number; quantity: number }): void;
}>();

const form = reactive<{ technicianId: number | null; quantity: number }>({ technicianId: null, quantity: 1 });

const unit = computed(() => {
  if (!props.service) return 0;
  return props.isMember ? props.service.memberPrice : props.service.price;
});

function reset() {
  form.technicianId = null;
  form.quantity = 1;
}
function confirm() {
  if (form.technicianId == null) return;
  emit('confirm', { technicianId: form.technicianId, quantity: form.quantity });
  reset();
}
</script>

<style scoped>
.price { font-weight: 600; color: #d9534f; }
.muted { color: var(--el-text-color-secondary); }
</style>
