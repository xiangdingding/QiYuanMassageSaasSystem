<template>
  <div class="page">
    <el-card shadow="never">
      <div class="toolbar">
        <el-select v-model="query.status" placeholder="全部状态" clearable style="width: 140px">
          <el-option label="待确认" value="Pending" />
          <el-option label="已确认" value="Confirmed" />
          <el-option label="已到店" value="Arrived" />
          <el-option label="已完成" value="Completed" />
          <el-option label="已取消" value="Cancelled" />
          <el-option label="未到店" value="NoShow" />
        </el-select>
        <el-date-picker
          v-model="dateRange"
          type="daterange"
          range-separator="至"
          start-placeholder="开始日期"
          end-placeholder="结束日期"
          format="YYYY-MM-DD"
          value-format="YYYY-MM-DD"
        />
        <el-button type="primary" @click="reload">查询</el-button>
        <el-button @click="resetQuery">重置</el-button>
      </div>

      <el-table :data="rows" v-loading="loading" stripe style="margin-top: 12px">
        <el-table-column label="到店时间" width="170">
          <template #default="{ row }">{{ dayjs(row.expectedArriveAt).format('YYYY-MM-DD HH:mm') }}</template>
        </el-table-column>
        <el-table-column prop="customerName" label="姓名" width="120" />
        <el-table-column prop="customerPhone" label="电话" width="130" />
        <el-table-column label="人数" width="60" prop="partySize" />
        <el-table-column label="服务" min-width="120" prop="serviceName" />
        <el-table-column label="指定技师" width="140" prop="preferredTechnicianName" />
        <el-table-column label="状态" width="100">
          <template #default="{ row }">
            <el-tag :type="statusType(row.status)">{{ statusLabel(row.status) }}</el-tag>
          </template>
        </el-table-column>
        <el-table-column label="备注" min-width="160" prop="remark" show-overflow-tooltip />
        <el-table-column label="操作" width="220" fixed="right">
          <template #default="{ row }">
            <el-button v-if="row.status === 'Pending'" size="small" type="primary" @click="confirm(row)">
              确认
            </el-button>
            <el-button v-if="row.status === 'Pending' || row.status === 'Confirmed'" size="small" type="success" @click="arrive(row)">
              到店
            </el-button>
            <el-button
              v-if="row.status === 'Pending' || row.status === 'Confirmed'"
              size="small"
              type="danger"
              @click="cancel(row)"
            >
              取消
            </el-button>
          </template>
        </el-table-column>
      </el-table>

      <div class="pager">
        <el-pagination
          v-model:current-page="query.page"
          v-model:page-size="query.pageSize"
          :total="total"
          layout="total, sizes, prev, pager, next, jumper"
          :page-sizes="[20, 50, 100]"
          @change="reload"
        />
      </div>
    </el-card>
  </div>
</template>

<script setup lang="ts">
import { onMounted, reactive, ref, watch } from 'vue';
import { ElMessage, ElMessageBox } from 'element-plus';
import dayjs from 'dayjs';
import { appointmentsApi } from '@/api/modules';
import { useAppStore } from '@/stores/app';
import type { Appointment, AppointmentStatus } from '@/api/types';

const appStore = useAppStore();

const query = reactive<{ page: number; pageSize: number; status: AppointmentStatus | ''; storeId?: number }>({
  page: 1, pageSize: 20, status: ''
});
const dateRange = ref<[string, string] | null>(null);
const rows = ref<Appointment[]>([]);
const total = ref(0);
const loading = ref(false);

const STATUS_LABELS: Record<AppointmentStatus, string> = {
  Pending: '待确认', Confirmed: '已确认', Arrived: '已到店',
  Completed: '已完成', Cancelled: '已取消', NoShow: '未到店'
};
const STATUS_TYPES: Record<AppointmentStatus, string> = {
  Pending: 'warning', Confirmed: 'primary', Arrived: 'success',
  Completed: '', Cancelled: 'danger', NoShow: 'info'
};
function statusLabel(s: AppointmentStatus) { return STATUS_LABELS[s] ?? s; }
function statusType(s: AppointmentStatus) { return STATUS_TYPES[s] ?? ''; }

async function reload() {
  if (!appStore.activeStoreId) return;
  loading.value = true;
  try {
    const r = await appointmentsApi.list({
      storeId: appStore.activeStoreId,
      status: query.status || undefined,
      from: dateRange.value?.[0],
      to: dateRange.value?.[1] ? dayjs(dateRange.value[1]).add(1, 'day').format('YYYY-MM-DD') : undefined,
      page: query.page,
      pageSize: query.pageSize
    });
    rows.value = r.items;
    total.value = r.total;
  } finally {
    loading.value = false;
  }
}

function resetQuery() {
  query.status = '';
  dateRange.value = null;
  query.page = 1;
  reload();
}

async function confirm(row: Appointment) {
  await appointmentsApi.confirm(row.id);
  ElMessage.success('已确认');
  await reload();
}

async function arrive(row: Appointment) {
  await appointmentsApi.arrive(row.id);
  ElMessage.success('已到店');
  await reload();
}

async function cancel(row: Appointment) {
  const { value } = await ElMessageBox.prompt('请输入取消原因（可选）', '取消预约', {
    inputPlaceholder: '比如：客人临时改期'
  }).catch(() => ({ value: null as string | null }));
  if (value === null) return;
  await appointmentsApi.cancel(row.id, value || null);
  ElMessage.success('已取消');
  await reload();
}

watch(() => appStore.activeStoreId, () => reload());
onMounted(async () => {
  await appStore.loadStores();
  await reload();
});
</script>

<style scoped>
.page { padding-bottom: 24px; }
.toolbar { display: flex; gap: 12px; align-items: center; flex-wrap: wrap; }
.pager { margin-top: 12px; display: flex; justify-content: flex-end; }
</style>
