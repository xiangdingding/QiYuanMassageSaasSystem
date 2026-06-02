<template>
  <div class="page">
    <el-card shadow="never">
      <div class="toolbar">
        <span class="title">排班与请假</span>
        <div class="spacer" />
        <el-button :icon="Refresh" @click="reload">刷新</el-button>
      </div>

      <el-tabs v-model="tab" class="page-tabs" @tab-change="reload">
        <el-tab-pane label="排班" name="schedules">
          <div class="sub-toolbar">
            <el-date-picker v-model="range" type="daterange" range-separator="-" start-placeholder="开始" end-placeholder="结束" />
            <el-button type="primary" :icon="Plus" @click="openSchedule">新增排班</el-button>
          </div>
          <div class="table-wrap">
          <el-table :data="schedules" v-loading="loading" stripe height="100%">
            <el-table-column prop="workDate" label="日期" width="120">
              <template #default="{ row }">{{ formatDate(row.workDate) }}</template>
            </el-table-column>
            <el-table-column prop="userName" label="员工" width="120" />
            <el-table-column label="班次" width="160">
              <template #default="{ row }">{{ row.startTime }} - {{ row.endTime }}</template>
            </el-table-column>
            <el-table-column prop="remark" label="备注" min-width="160" />
            <el-table-column label="操作" :width="$actCol(120)">
              <template #default="{ row }">
                <el-button size="small" type="danger" @click="removeSchedule(row.id)">删除</el-button>
              </template>
            </el-table-column>
          </el-table>
          </div>
        </el-tab-pane>

        <el-tab-pane label="请假审批" name="leaves">
          <div class="table-wrap">
          <el-table :data="leaves" v-loading="loading" stripe height="100%">
            <el-table-column prop="userName" label="员工" width="120" />
            <el-table-column prop="type" label="类型" width="100">
              <template #default="{ row }">{{ leaveTypeLabel(row.type) }}</template>
            </el-table-column>
            <el-table-column label="日期" width="220">
              <template #default="{ row }">{{ formatDate(row.fromDate) }} - {{ formatDate(row.toDate) }}</template>
            </el-table-column>
            <el-table-column prop="reason" label="原因" min-width="180" />
            <el-table-column prop="status" label="状态" width="100">
              <template #default="{ row }">{{ leaveStatusLabel(row.status) }}</template>
            </el-table-column>
            <el-table-column prop="approverName" label="审批人" width="100" />
            <el-table-column label="操作" :width="$actCol(180)" fixed="right">
              <template #default="{ row }">
                <el-button size="small" type="success" :disabled="row.status !== 'Pending'" @click="approve(row, true)">同意</el-button>
                <el-button size="small" type="danger" :disabled="row.status !== 'Pending'" @click="approve(row, false)">驳回</el-button>
              </template>
            </el-table-column>
          </el-table>
          </div>
        </el-tab-pane>
      </el-tabs>
    </el-card>

    <el-dialog v-model="schedOpen" title="新增排班" width="420px">
      <el-form :model="sched" label-width="80px">
        <el-form-item label="员工" required>
          <el-select v-model="sched.userId" filterable placeholder="选择员工" style="width:100%">
            <el-option v-for="s in staffList" :key="s.id" :label="`${s.realName || s.username}（${s.role}）`" :value="s.id" />
          </el-select>
        </el-form-item>
        <el-form-item label="日期" required><el-date-picker v-model="sched.workDate" type="date" style="width:100%" /></el-form-item>
        <el-form-item label="开始" required><el-time-picker v-model="sched.startTime" format="HH:mm" value-format="HH:mm" style="width:100%" /></el-form-item>
        <el-form-item label="结束" required><el-time-picker v-model="sched.endTime" format="HH:mm" value-format="HH:mm" style="width:100%" /></el-form-item>
        <el-form-item label="备注"><el-input v-model="sched.remark" maxlength="200" /></el-form-item>
      </el-form>
      <template #footer>
        <el-button @click="schedOpen = false">取消</el-button>
        <el-button type="primary" :loading="saving" @click="saveSchedule">保存</el-button>
      </template>
    </el-dialog>
  </div>
</template>

<script setup lang="ts">
import { onMounted, reactive, ref } from 'vue';
import { ElMessage } from 'element-plus';
import { Plus, Refresh } from '@element-plus/icons-vue';
import {
  schedulesApi, staffApi,
  type StaffScheduleDto, type LeaveRequestDto
} from '@/api/modules';
import { useAppStore } from '@/stores/app';
import type { Staff } from '@/api/types';
import { leaveStatusLabel, leaveTypeLabel } from '@/utils/enumLabels';

const appStore = useAppStore();

const tab = ref<'schedules' | 'leaves'>('schedules');
const schedules = ref<StaffScheduleDto[]>([]);
const leaves = ref<LeaveRequestDto[]>([]);
const loading = ref(false);
const staffList = ref<Staff[]>([]);
const range = ref<[Date, Date] | null>(null);

const schedOpen = ref(false);
const saving = ref(false);
const sched = reactive({
  userId: 0 as number, workDate: null as Date | null,
  startTime: '09:00', endTime: '18:00', remark: ''
});

function formatDate(s: string) {
  return s ? new Date(s).toISOString().slice(0, 10) : '';
}

async function reload() {
  loading.value = true;
  try {
    if (tab.value === 'schedules') {
      if (!appStore.activeStoreId) return;
      const from = range.value?.[0]?.toISOString();
      const to = range.value?.[1]?.toISOString();
      schedules.value = await schedulesApi.list(appStore.activeStoreId, from, to);
    } else {
      leaves.value = await schedulesApi.leaves();
    }
  } finally {
    loading.value = false;
  }
}

function openSchedule() {
  Object.assign(sched, { userId: 0, workDate: new Date(), startTime: '09:00', endTime: '18:00', remark: '' });
  schedOpen.value = true;
}

async function saveSchedule() {
  if (!sched.userId || !sched.workDate) { ElMessage.warning('员工与日期必填'); return; }
  saving.value = true;
  try {
    await schedulesApi.create({
      storeId: appStore.activeStoreId,
      userId: sched.userId,
      workDate: (sched.workDate as Date).toISOString(),
      startTime: sched.startTime,
      endTime: sched.endTime,
      remark: sched.remark || null
    });
    schedOpen.value = false;
    ElMessage.success('已保存');
    await reload();
  } finally {
    saving.value = false;
  }
}

async function removeSchedule(id: number) {
  await schedulesApi.remove(id);
  ElMessage.success('已删除');
  await reload();
}

async function approve(row: LeaveRequestDto, approve: boolean) {
  await schedulesApi.approveLeave(row.id, approve);
  ElMessage.success(approve ? '已同意' : '已驳回');
  await reload();
}

onMounted(async () => {
  const data = await staffApi.list({ page: 1, pageSize: 100 });
  staffList.value = data.items;
  await reload();
});
</script>

<style scoped>
.toolbar { display: flex; gap: 12px; align-items: center; }
.toolbar .title { font-weight: 600; font-size: 16px; }
.spacer { flex: 1; }
.sub-toolbar { display: flex; gap: 12px; flex: 0 0 auto; padding-bottom: 8px; }
.page-tabs { flex: 1 1 auto; display: flex; flex-direction: column; min-height: 0; margin-top: 8px; }
.page-tabs :deep(.el-tabs__content) { flex: 1 1 auto; min-height: 0; overflow: hidden; }
.page-tabs :deep(.el-tab-pane) { height: 100%; display: flex; flex-direction: column; }
</style>
