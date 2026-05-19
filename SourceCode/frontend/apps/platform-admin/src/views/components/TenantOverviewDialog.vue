<template>
  <el-dialog
    :model-value="modelValue"
    @update:model-value="(v: boolean) => emit('update:modelValue', v)"
    :title="`运营概览：${tenant?.name ?? ''}`"
    width="720px"
  >
    <div v-loading="loading">
      <template v-if="data">
        <el-row :gutter="12">
          <el-col :span="8">
            <div class="metric">
              <div class="metric-label">门店</div>
              <div class="metric-value">{{ data.activeStoreCount }} / {{ data.storeCount }}</div>
              <div class="metric-sub">启用 / 总数</div>
            </div>
          </el-col>
          <el-col :span="8">
            <div class="metric">
              <div class="metric-label">员工</div>
              <div class="metric-value">{{ data.staffCount }}</div>
              <div class="metric-sub">技师 {{ data.technicianCount }} 人</div>
            </div>
          </el-col>
          <el-col :span="8">
            <div class="metric">
              <div class="metric-label">会员</div>
              <div class="metric-value">{{ data.memberCount }}</div>
              <div class="metric-sub">累计开卡</div>
            </div>
          </el-col>
        </el-row>

        <el-row :gutter="12" style="margin-top: 12px">
          <el-col :span="8">
            <div class="metric">
              <div class="metric-label">近 7 天营业额</div>
              <div class="metric-value">¥ {{ money(data.revenue7Days) }}</div>
            </div>
          </el-col>
          <el-col :span="8">
            <div class="metric">
              <div class="metric-label">近 30 天营业额</div>
              <div class="metric-value">¥ {{ money(data.revenue30Days) }}</div>
              <div class="metric-sub">完成订单 {{ data.orderCount30Days }} 单</div>
            </div>
          </el-col>
          <el-col :span="8">
            <div class="metric">
              <div class="metric-label">订阅</div>
              <div class="metric-value sm">{{ data.currentPlanName ?? '无套餐' }}</div>
              <div class="metric-sub">{{ expireText }}</div>
            </div>
          </el-col>
        </el-row>

        <div class="section-title">近 30 天技师营收榜</div>
        <el-table :data="data.topTechnicians" empty-text="近 30 天暂无服务记录" size="small">
          <el-table-column type="index" label="#" width="50" />
          <el-table-column prop="name" label="技师" min-width="140" />
          <el-table-column prop="roundCount" label="钟数" width="100" />
          <el-table-column label="营收" min-width="120">
            <template #default="{ row }">¥ {{ money(row.revenue) }}</template>
          </el-table-column>
        </el-table>
      </template>
    </div>
    <template #footer>
      <el-button @click="emit('update:modelValue', false)">关闭</el-button>
    </template>
  </el-dialog>
</template>

<script setup lang="ts">
import { computed, ref, watch } from 'vue';
import { tenantsApi } from '@/api/modules';
import type { TenantOverview, TenantSummary } from '@/api/types';

const props = defineProps<{
  modelValue: boolean;
  tenant: TenantSummary | null;
}>();
const emit = defineEmits<{
  (e: 'update:modelValue', v: boolean): void;
}>();

const data = ref<TenantOverview | null>(null);
const loading = ref(false);

function money(v: number) {
  return v.toFixed(2);
}

const expireText = computed(() => {
  const d = data.value;
  if (!d || !d.expireAt) return '未设置到期';
  const day = new Date(d.expireAt).toLocaleDateString('zh-CN');
  if (d.daysToExpire == null) return `${day} 到期`;
  return d.daysToExpire >= 0 ? `${day} 到期（剩 ${d.daysToExpire} 天）` : `${day} 已过期`;
});

watch(
  () => props.modelValue,
  async (open) => {
    if (!open || !props.tenant) return;
    data.value = null;
    loading.value = true;
    try {
      data.value = await tenantsApi.overview(props.tenant.id);
    } finally {
      loading.value = false;
    }
  }
);
</script>

<style scoped>
.metric {
  background: var(--el-fill-color-light);
  border-radius: 6px;
  padding: 12px 14px;
}
.metric-label { color: var(--el-text-color-secondary); font-size: 13px; }
.metric-value { font-size: 24px; font-weight: 600; margin: 4px 0; }
.metric-value.sm { font-size: 17px; }
.metric-sub { color: var(--el-text-color-secondary); font-size: 12px; }
.section-title { font-weight: 600; margin: 18px 0 10px; }
</style>
