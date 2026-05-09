<template>
  <div class="page">
    <el-card shadow="never">
      <div class="toolbar">
        <el-input
          v-model="query.keyword"
          placeholder="搜索店名 / 联系电话"
          clearable
          style="width: 240px"
          @keyup.enter="reload"
        />
        <el-select v-model="query.status" placeholder="全部状态" clearable style="width: 140px">
          <el-option label="活跃" value="Active" />
          <el-option label="已过期" value="Expired" />
          <el-option label="已停用" value="Disabled" />
        </el-select>
        <el-button type="primary" @click="reload">查询</el-button>
        <el-button @click="resetQuery">重置</el-button>
        <el-button type="success" @click="openCreate">新建租户</el-button>
      </div>

      <el-table :data="rows" v-loading="loading" stripe style="margin-top: 12px">
        <el-table-column prop="name" label="店名" min-width="180" />
        <el-table-column prop="contactPhone" label="联系电话" min-width="120" />
        <el-table-column prop="contactName" label="联系人" min-width="100" />
        <el-table-column prop="currentPlanName" label="当前套餐" min-width="120">
          <template #default="{ row }">{{ row.currentPlanName ?? '—' }}</template>
        </el-table-column>
        <el-table-column label="状态" min-width="100">
          <template #default="{ row }">
            <el-tag :type="statusType(row.status)">{{ statusLabel(row.status) }}</el-tag>
          </template>
        </el-table-column>
        <el-table-column label="到期时间" min-width="180">
          <template #default="{ row }">
            <span v-if="!row.expireAt">—</span>
            <span v-else>
              {{ formatDate(row.expireAt) }}
              <el-tag
                v-if="row.daysToExpire != null && row.daysToExpire <= 30 && row.daysToExpire > 0"
                size="small"
                type="warning"
                effect="plain"
              >
                {{ row.daysToExpire }} 天后
              </el-tag>
            </span>
          </template>
        </el-table-column>
        <el-table-column label="操作" width="240" fixed="right">
          <template #default="{ row }">
            <el-button link type="primary" @click="openActivate(row)">续费/激活</el-button>
            <el-button
              v-if="row.status !== 'Disabled'"
              link
              type="danger"
              @click="changeStatus(row, 'Disabled')"
            >停用</el-button>
            <el-button
              v-else
              link
              type="success"
              @click="changeStatus(row, 'Active')"
            >启用</el-button>
          </template>
        </el-table-column>
      </el-table>

      <el-pagination
        style="margin-top: 12px; justify-content: flex-end; display: flex"
        :current-page="query.page"
        :page-size="query.pageSize"
        :total="total"
        :page-sizes="[10, 20, 50]"
        layout="total, sizes, prev, pager, next, jumper"
        @current-change="(p: number) => { query.page = p; reload(); }"
        @size-change="(s: number) => { query.pageSize = s; query.page = 1; reload(); }"
      />
    </el-card>

    <CreateTenantDialog v-model="createOpen" :plans="plans" @created="onCreated" />
    <OfflineActivateDialog
      v-model="activateOpen"
      :tenant="activateTarget"
      :plans="plans"
      @activated="reload"
    />
  </div>
</template>

<script setup lang="ts">
import { onMounted, reactive, ref } from 'vue';
import { ElMessage, ElMessageBox } from 'element-plus';
import { plansApi, tenantsApi } from '@/api/modules';
import type { Plan, TenantSummary } from '@/api/types';
import CreateTenantDialog from '@/views/components/CreateTenantDialog.vue';
import OfflineActivateDialog from '@/views/components/OfflineActivateDialog.vue';

const rows = ref<TenantSummary[]>([]);
const total = ref(0);
const loading = ref(false);
const plans = ref<Plan[]>([]);

const query = reactive<{ page: number; pageSize: number; keyword: string; status: string }>({
  page: 1,
  pageSize: 20,
  keyword: '',
  status: ''
});

const createOpen = ref(false);
const activateOpen = ref(false);
const activateTarget = ref<TenantSummary | null>(null);

function statusType(s: string) {
  if (s === 'Active') return 'success';
  if (s === 'Expired') return 'warning';
  return 'danger';
}
function statusLabel(s: string) {
  return { Active: '活跃', Expired: '已过期', Disabled: '已停用' }[s] ?? s;
}
function formatDate(v: string) {
  return new Date(v).toLocaleDateString('zh-CN');
}

async function reload() {
  loading.value = true;
  try {
    const data = await tenantsApi.list({
      page: query.page,
      pageSize: query.pageSize,
      keyword: query.keyword || undefined,
      status: query.status || undefined
    });
    rows.value = data.items;
    total.value = data.total;
  } finally {
    loading.value = false;
  }
}

function resetQuery() {
  query.keyword = '';
  query.status = '';
  query.page = 1;
  reload();
}

function openCreate() {
  createOpen.value = true;
}
function onCreated() {
  createOpen.value = false;
  query.page = 1;
  reload();
}
function openActivate(row: TenantSummary) {
  activateTarget.value = row;
  activateOpen.value = true;
}

async function changeStatus(row: TenantSummary, status: 'Active' | 'Disabled') {
  const verb = status === 'Disabled' ? '停用' : '启用';
  await ElMessageBox.confirm(`确认${verb}租户「${row.name}」？`, '请确认', { type: 'warning' }).catch(() => null);
  try {
    await tenantsApi.updateStatus(row.id, status);
    ElMessage.success(`已${verb}`);
    reload();
  } catch {
    /* http interceptor surfaces error */
  }
}

onMounted(async () => {
  plans.value = await plansApi.list(false).catch(() => []);
  reload();
});
</script>

<style scoped>
.page { padding-bottom: 24px; }
.toolbar { display: flex; gap: 8px; align-items: center; flex-wrap: wrap; }
</style>
