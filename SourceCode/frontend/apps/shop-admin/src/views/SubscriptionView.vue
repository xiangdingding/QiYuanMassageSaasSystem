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
        <p>请联系平台运营方进行续费，或在到期前主动联系。</p>
      </div>
      <el-empty v-else description="暂无订阅信息" />
    </el-card>
  </div>
</template>

<script setup lang="ts">
import { computed, onMounted, ref } from 'vue';
import dayjs from 'dayjs';
import { subscriptionsApi } from '@/api/modules';
import type { SubscriptionStatus } from '@/api/types';

const status = ref<SubscriptionStatus | null>(null);
const loading = ref(false);

const statusLabel = computed(() => {
  switch (status.value?.status) {
    case 'Active': return '活跃';
    case 'Expired': return '已过期';
    case 'Disabled': return '已停用';
    default: return status.value?.status ?? '—';
  }
});
const statusType = computed(() => {
  switch (status.value?.status) {
    case 'Active': return 'success';
    case 'Expired': return 'warning';
    case 'Disabled': return 'danger';
    default: return 'info';
  }
});

onMounted(async () => {
  loading.value = true;
  try {
    status.value = await subscriptionsApi.me();
  } finally {
    loading.value = false;
  }
});
</script>

<style scoped>
.page { padding-bottom: 24px; }
</style>
