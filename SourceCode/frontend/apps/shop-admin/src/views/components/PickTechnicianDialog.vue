<template>
  <el-dialog
    :model-value="modelValue"
    @update:model-value="(v: boolean) => emit('update:modelValue', v)"
    :title="`指派技师：${service?.name ?? ''}`"
    :aria-label="`为 ${service?.name ?? ''} 指派技师对话框`"
    width="460px"
    @close="reset"
  >
    <el-form :model="form" label-width="100px" v-if="service" size="large">
      <el-form-item label="单价">
        <span
          class="price"
          :aria-label="`单价 ${yuanReadable(unit)}，时长 ${service.durationMinutes} 分钟`"
        >¥ {{ unit.toFixed(2) }}</span>
        <span class="muted" style="margin-left: 8px" aria-hidden="true">{{ service.durationMinutes }} 分钟</span>
      </el-form-item>
      <el-form-item label="技师">
        <el-select
          v-model="form.technicianId"
          filterable
          placeholder="搜索工号或姓名，回车选中"
          style="width: 100%"
          :aria-label="`选择技师，输入工号或姓名搜索`"
        >
          <el-option
            v-for="t in technicians"
            :key="t.id"
            :label="`${t.employeeNo ?? '-'} · ${t.realName ?? t.username}`"
            :value="t.id"
          />
        </el-select>
      </el-form-item>
      <el-form-item label="上钟方式">
        <el-select
          v-model="form.source"
          style="width: 100%"
          aria-label="上钟方式，二选一：轮钟为前台叫号下一位；点钟为客人指定技师"
        >
          <el-option label="轮钟（叫号）" value="Rotation" />
          <el-option label="点钟（客人指定）" value="Designation" />
        </el-select>
      </el-form-item>
      <el-form-item label="数量">
        <el-input-number
          v-model="form.quantity"
          :min="1"
          :max="10"
          aria-label="数量，最少 1 最多 10"
        />
      </el-form-item>
    </el-form>
    <template #footer>
      <el-button size="large" :aria-label="'放弃指派并关闭对话框'" @click="emit('update:modelValue', false)">取消</el-button>
      <el-button
        type="primary"
        size="large"
        :disabled="!form.technicianId"
        :aria-label="confirmAriaLabel"
        @click="confirm"
      >加入订单</el-button>
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
  (e: 'confirm', payload: { technicianId: number; quantity: number; source: 'Rotation' | 'Designation' }): void;
}>();

/// 上钟方式默认轮钟；店员可在弹窗里改，也可加入购物车后在订单行的下拉里改
const form = reactive<{
  technicianId: number | null;
  quantity: number;
  source: 'Rotation' | 'Designation';
}>({ technicianId: null, quantity: 1, source: 'Rotation' });

const unit = computed(() => {
  if (!props.service) return 0;
  return props.isMember ? props.service.memberPrice : props.service.price;
});

/// 金额朗读：避免读屏把 "32.5" 念成 "三十二点五"，给出 "32 元 5 角" 风格
function yuanReadable(amount: number): string {
  const safe = Number.isFinite(amount) ? amount : 0;
  const yuan = Math.floor(safe);
  const jiao = Math.round((safe - yuan) * 10);
  return jiao === 0 ? `${yuan} 元` : `${yuan} 元 ${jiao} 角`;
}

const confirmAriaLabel = computed(() => {
  if (!props.service) return '加入订单';
  const tech = props.technicians.find((t) => t.id === form.technicianId);
  const techDesc = tech ? `${tech.employeeNo ?? '-'} 号 ${tech.realName ?? tech.username}` : '未选技师';
  const sourceDesc = form.source === 'Rotation' ? '轮钟' : '点钟';
  return `把 ${props.service.name} 加入订单，技师 ${techDesc}，上钟方式 ${sourceDesc}，数量 ${form.quantity}`;
});

function reset() {
  form.technicianId = null;
  form.quantity = 1;
  form.source = 'Rotation';
}
function confirm() {
  if (form.technicianId == null) return;
  emit('confirm', { technicianId: form.technicianId, quantity: form.quantity, source: form.source });
  reset();
}
</script>

<style scoped>
.price { font-weight: 700; font-size: 18px; color: #d9534f; }
.muted { color: var(--el-text-color-secondary); font-size: 13px; }
</style>
