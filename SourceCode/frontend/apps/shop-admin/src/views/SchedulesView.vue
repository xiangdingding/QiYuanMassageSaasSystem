<template>
  <div class="page">
    <el-card shadow="never">
      <div class="toolbar">
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
          <div class="sub-toolbar">
            <el-select v-model="leaveFilter.userId" clearable filterable placeholder="员工" style="width:140px" @change="reload">
              <el-option v-for="s in staffList" :key="s.id" :label="s.realName || s.username" :value="s.id" />
            </el-select>
            <el-select v-model="leaveFilter.type" clearable placeholder="类型" style="width:120px" @change="reload">
              <el-option label="病假" value="Sick" />
              <el-option label="事假" value="Personal" />
              <el-option label="年假" value="Annual" />
              <el-option label="培训" value="Training" />
            </el-select>
            <el-select v-model="leaveFilter.status" clearable placeholder="状态" style="width:120px" @change="reload">
              <el-option label="待审批" value="Pending" />
              <el-option label="已通过" value="Approved" />
              <el-option label="已驳回" value="Rejected" />
              <el-option label="已撤销" value="Cancelled" />
            </el-select>
            <el-date-picker v-model="leaveFilter.range" type="daterange" value-format="YYYY-MM-DD"
                            range-separator="-" start-placeholder="开始" end-placeholder="结束"
                            style="width:240px" @change="reload" />
            <el-button @click="resetLeaveFilter">重置</el-button>
            <div class="spacer" />
            <el-button type="primary" :icon="Plus" @click="openLeave">登记请假</el-button>
          </div>
          <div class="table-wrap">
          <el-table :data="leaves" v-loading="loading" stripe height="100%">
            <el-table-column prop="userName" label="员工" width="120" />
            <el-table-column prop="type" label="类型" width="100">
              <template #default="{ row }">{{ leaveTypeLabel(row.type) }}</template>
            </el-table-column>
            <el-table-column label="日期" width="200">
              <template #default="{ row }">{{ formatDate(row.fromDate) }} - {{ formatDate(row.toDate) }}</template>
            </el-table-column>
            <el-table-column label="天数" width="80">
              <template #default="{ row }">{{ leaveDaysOf(row) }} 天</template>
            </el-table-column>
            <el-table-column prop="reason" label="原因" min-width="160" />
            <el-table-column prop="status" label="状态" width="100">
              <template #default="{ row }">{{ leaveStatusLabel(row.status) }}</template>
            </el-table-column>
            <el-table-column prop="approverName" label="审批人" width="100" />
            <el-table-column label="操作" :width="$actCol(230)" fixed="right">
              <template #default="{ row }">
                <el-button size="small" @click="openDetail(row)">详情</el-button>
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
            <el-option v-for="s in staffList" :key="s.id" :label="`${s.realName || s.username}（${userRoleLabel(s.role)}）`" :value="s.id" />
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

    <el-dialog v-model="leaveOpen" title="登记请假" width="460px">
      <el-form :model="leaveForm" label-width="80px">
        <el-form-item label="员工" required>
          <el-select v-model="leaveForm.userId" filterable placeholder="选择员工" style="width:100%">
            <el-option v-for="s in staffList" :key="s.id" :label="`${s.realName || s.username}（${userRoleLabel(s.role)}）`" :value="s.id" />
          </el-select>
        </el-form-item>
        <el-form-item label="类型" required>
          <el-select v-model="leaveForm.type" style="width:100%">
            <el-option label="病假" value="Sick" />
            <el-option label="事假" value="Personal" />
            <el-option label="年假" value="Annual" />
          </el-select>
        </el-form-item>
        <el-form-item label="日期" required>
          <el-date-picker v-model="leaveForm.range" type="daterange" value-format="YYYY-MM-DD"
                          range-separator="至" start-placeholder="开始" end-placeholder="结束" style="width:100%" />
        </el-form-item>
        <el-form-item label="时段">
          <div class="half-row">
            <span class="half-label">开始</span>
            <el-radio-group v-model="leaveForm.startHalf">
              <el-radio-button value="Morning">上午</el-radio-button>
              <el-radio-button value="Afternoon">下午</el-radio-button>
            </el-radio-group>
            <span class="half-label">结束</span>
            <el-radio-group v-model="leaveForm.endHalf">
              <el-radio-button value="Morning">上午</el-radio-button>
              <el-radio-button value="Afternoon">下午</el-radio-button>
            </el-radio-group>
          </div>
        </el-form-item>
        <el-form-item label="天数">
          <el-tag v-if="leaveDays > 0" type="success">共 {{ leaveDays }} 天</el-tag>
          <el-tag v-else type="danger">时长无效（同日不能从下午请到上午）</el-tag>
        </el-form-item>
        <el-form-item label="事由">
          <el-input v-model="leaveForm.reason" type="textarea" :rows="3" maxlength="200" show-word-limit placeholder="可填写请假原因" />
        </el-form-item>
      </el-form>
      <template #footer>
        <el-button @click="leaveOpen = false">取消</el-button>
        <el-button type="primary" :loading="savingLeave" @click="submitLeave">提交</el-button>
      </template>
    </el-dialog>

    <el-dialog v-model="detailOpen" title="请假详情" width="520px">
      <el-descriptions v-if="detailRow" :column="1" border>
        <el-descriptions-item label="员工">{{ detailRow.userName }}</el-descriptions-item>
        <el-descriptions-item label="类型">{{ leaveTypeLabel(detailRow.type) }}</el-descriptions-item>
        <el-descriptions-item label="开始">{{ formatDate(detailRow.fromDate) }} {{ halfLabel(detailRow.startHalf) }}</el-descriptions-item>
        <el-descriptions-item label="结束">{{ formatDate(detailRow.toDate) }} {{ halfLabel(detailRow.endHalf) }}</el-descriptions-item>
        <el-descriptions-item label="天数">{{ leaveDaysOf(detailRow) }} 天</el-descriptions-item>
        <el-descriptions-item label="事由">{{ detailRow.reason || '—' }}</el-descriptions-item>
        <el-descriptions-item label="状态">{{ leaveStatusLabel(detailRow.status) }}</el-descriptions-item>
        <el-descriptions-item label="登记时间">{{ formatTime(detailRow.createdAt) }}</el-descriptions-item>
        <el-descriptions-item label="审批人">{{ detailRow.approverName || '—' }}</el-descriptions-item>
        <el-descriptions-item label="审批时间">{{ detailRow.approvedAt ? formatTime(detailRow.approvedAt) : '—' }}</el-descriptions-item>
      </el-descriptions>
      <template #footer>
        <el-button @click="detailOpen = false">关闭</el-button>
      </template>
    </el-dialog>
  </div>
</template>

<script setup lang="ts">
import { computed, onMounted, reactive, ref } from 'vue';
import dayjs from 'dayjs';
import { ElMessage } from 'element-plus';
import { Plus, Refresh } from '@element-plus/icons-vue';
import {
  schedulesApi, staffApi,
  type StaffScheduleDto, type LeaveRequestDto
} from '@/api/modules';
import { useAppStore } from '@/stores/app';
import type { Staff } from '@/api/types';
import { leaveStatusLabel, leaveTypeLabel, userRoleLabel } from '@/utils/enumLabels';

const appStore = useAppStore();

const tab = ref<'schedules' | 'leaves'>('schedules');
const schedules = ref<StaffScheduleDto[]>([]);
const leaves = ref<LeaveRequestDto[]>([]);
const loading = ref(false);
const staffList = ref<Staff[]>([]);
const range = ref<[Date, Date] | null>(null);

// 请假审批过滤（员工 / 类型 / 状态 / 日期区间）
const leaveFilter = reactive({
  userId: undefined as number | undefined,
  status: '' as string,
  type: '' as string,
  range: null as [string, string] | null
});
function resetLeaveFilter() {
  leaveFilter.userId = undefined;
  leaveFilter.status = '';
  leaveFilter.type = '';
  leaveFilter.range = null;
  reload();
}

const schedOpen = ref(false);
const saving = ref(false);
const sched = reactive({
  userId: 0 as number, workDate: null as Date | null,
  startTime: '09:00', endTime: '18:00', remark: ''
});

// —— 登记请假 ——
const leaveOpen = ref(false);
const savingLeave = ref(false);
const leaveForm = reactive({
  userId: 0 as number, type: 'Personal',
  range: null as [string, string] | null,
  startHalf: 'Morning', endHalf: 'Afternoon', reason: ''
});

/// 折算请假天数（上午=整天边界，下午起首日扣0.5，上午止末日扣0.5）
const leaveDays = computed(() => {
  if (!leaveForm.range || leaveForm.range.length !== 2) return 0;
  const from = dayjs(leaveForm.range[0]);
  const to = dayjs(leaveForm.range[1]);
  if (to.isBefore(from, 'day')) return 0;
  let d = to.diff(from, 'day') + 1;
  if (leaveForm.startHalf === 'Afternoon') d -= 0.5;
  if (leaveForm.endHalf === 'Morning') d -= 0.5;
  return d < 0 ? 0 : d;
});

function formatDate(s: string) {
  return s ? new Date(s).toISOString().slice(0, 10) : '';
}
/// 纯日期发给后端时取“当天中午”的无偏移串：服务端按北京时间-8h 存储，12:00 仍是当天，避免被存成前一天
function beijingNoon(d: string | Date): string {
  return dayjs(d).format('YYYY-MM-DD') + 'T12:00:00';
}
function formatTime(s: string) {
  return s ? dayjs(s).format('YYYY-MM-DD HH:mm:ss') : '';
}
function halfLabel(h: string) {
  return h === 'Morning' ? '上午' : h === 'Afternoon' ? '下午' : '';
}

// —— 请假详情 ——
const detailOpen = ref(false);
const detailRow = ref<LeaveRequestDto | null>(null);
function openDetail(row: LeaveRequestDto) {
  detailRow.value = row;
  detailOpen.value = true;
}

/// 列表天数：优先用后端返回的 days；后端未返回（旧接口）时按行内日期/时段前端兜底折算
function leaveDaysOf(row: LeaveRequestDto): number {
  if (typeof row.days === 'number') return row.days;
  const from = dayjs(row.fromDate);
  const to = dayjs(row.toDate);
  if (!from.isValid() || !to.isValid() || to.isBefore(from, 'day')) return 0;
  let d = to.diff(from, 'day') + 1;
  if (row.startHalf === 'Afternoon') d -= 0.5;
  if (row.endHalf === 'Morning') d -= 0.5;
  return d > 0 ? d : 0;
}

async function reload() {
  loading.value = true;
  try {
    if (tab.value === 'schedules') {
      if (!appStore.activeStoreId) return;
      const from = range.value?.[0] ? beijingNoon(range.value[0]) : undefined;
      const to = range.value?.[1] ? beijingNoon(range.value[1]) : undefined;
      schedules.value = await schedulesApi.list(appStore.activeStoreId, from, to);
    } else {
      leaves.value = await schedulesApi.leaves({
        userId: leaveFilter.userId || undefined,
        status: leaveFilter.status || undefined,
        type: leaveFilter.type || undefined,
        from: leaveFilter.range?.[0] ? beijingNoon(leaveFilter.range[0]) : undefined,
        to: leaveFilter.range?.[1] ? beijingNoon(leaveFilter.range[1]) : undefined
      });
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
      workDate: beijingNoon(sched.workDate as Date),
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

function openLeave() {
  Object.assign(leaveForm, { userId: 0, type: 'Personal', range: null, startHalf: 'Morning', endHalf: 'Afternoon', reason: '' });
  leaveOpen.value = true;
}

async function submitLeave() {
  if (!leaveForm.userId) { ElMessage.warning('请选择员工'); return; }
  if (!leaveForm.range || leaveForm.range.length !== 2) { ElMessage.warning('请选择请假日期'); return; }
  if (leaveDays.value <= 0) { ElMessage.warning('请假时长须大于 0（同日不能从下午请到上午）'); return; }
  savingLeave.value = true;
  try {
    await schedulesApi.submitLeave({
      userId: leaveForm.userId,
      type: leaveForm.type,
      fromDate: beijingNoon(leaveForm.range[0]),
      toDate: beijingNoon(leaveForm.range[1]),
      startHalf: leaveForm.startHalf,
      endHalf: leaveForm.endHalf,
      reason: leaveForm.reason.trim() || null
    });
    leaveOpen.value = false;
    ElMessage.success('已登记请假');
    tab.value = 'leaves';
    await reload();
  } finally {
    savingLeave.value = false;
  }
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
.half-row { display: flex; align-items: center; gap: 8px; flex-wrap: wrap; }
.half-label { color: var(--el-text-color-secondary); font-size: 13px; }
.page-tabs { flex: 1 1 auto; display: flex; flex-direction: column; min-height: 0; margin-top: 8px; }
.page-tabs :deep(.el-tabs__content) { flex: 1 1 auto; min-height: 0; overflow: hidden; }
.page-tabs :deep(.el-tab-pane) { height: 100%; display: flex; flex-direction: column; }
</style>
