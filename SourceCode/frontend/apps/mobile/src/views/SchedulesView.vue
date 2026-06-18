<script lang="ts">
export default { name: 'SchedulesView' };
</script>

<script setup lang="ts">
import { computed, onMounted, reactive, ref } from 'vue';
import {
  NavBar as VanNavBar, Tabs as VanTabs, Tab as VanTab, PullRefresh as VanPullRefresh,
  Empty as VanEmpty, Tag as VanTag, Button as VanButton, Popup as VanPopup, Form as VanForm,
  Field as VanField, Picker as VanPicker, Cell as VanCell, CellGroup as VanCellGroup,
  showSuccessToast, showToast, showConfirmDialog
} from 'vant';
import { schedulesApi, staffApi, type StaffScheduleDto, type LeaveRequestDto } from '@/api/modules';
import { useAppStore } from '@/stores/app';
import type { Staff } from '@/api/types';

const appStore = useAppStore();
const tabIndex = ref(0);
const schedules = ref<StaffScheduleDto[]>([]);
const leaves = ref<LeaveRequestDto[]>([]);
const loading = ref(false);
const refreshing = ref(false);
const saving = ref(false);
const staffList = ref<Staff[]>([]);

function todayStr() { const d = new Date(); const p = (n: number) => String(n).padStart(2, '0'); return `${d.getFullYear()}-${p(d.getMonth() + 1)}-${p(d.getDate())}`; }
function leaveTypeLabel(t: string) { return ({ Sick: '病假', Personal: '事假', Annual: '年假', Training: '培训' } as Record<string, string>)[t] ?? t; }
function leaveStatusLabel(s: string) { return ({ Pending: '待审批', Approved: '已通过', Rejected: '已驳回', Cancelled: '已撤销' } as Record<string, string>)[s] ?? s; }
function leaveStatusType(s: string) { return s === 'Pending' ? 'warning' : s === 'Approved' ? 'success' : 'default'; }
function halfLabel(h: string) { return h === 'Morning' ? '上午' : h === 'Afternoon' ? '下午' : ''; }
function beijingNoon(d: string) { return d ? `${d}T12:00:00` : ''; }
function fmtDate(s: string) { return s ? s.slice(0, 10) : ''; }

async function reload() {
  loading.value = true;
  try {
    if (tabIndex.value === 0) {
      if (appStore.activeStoreId) schedules.value = await schedulesApi.list(appStore.activeStoreId);
    } else {
      leaves.value = await schedulesApi.leaves({});
    }
  } catch { /* */ } finally { loading.value = false; refreshing.value = false; }
}

// 新增排班
const schedOpen = ref(false);
const showSchedStaffPicker = ref(false);
const sched = reactive({ userId: 0, workDate: todayStr(), startTime: '09:00', endTime: '18:00', remark: '' });
const staffColumns = () => staffList.value.map((s) => ({ text: `${s.realName || s.username}`, value: s.id }));
function schedStaffName() { const s = staffList.value.find((x) => x.id === sched.userId); return s ? (s.realName || s.username) : '选择员工'; }
function openSchedule() { Object.assign(sched, { userId: 0, workDate: todayStr(), startTime: '09:00', endTime: '18:00', remark: '' }); schedOpen.value = true; }
function onSchedStaff({ selectedValues }: { selectedValues: number[] }) { sched.userId = selectedValues[0] ?? 0; showSchedStaffPicker.value = false; }
async function saveSchedule() {
  if (!sched.userId || !sched.workDate) { showToast('员工与日期必填'); return; }
  saving.value = true;
  try {
    await schedulesApi.create({ storeId: appStore.activeStoreId, userId: sched.userId, workDate: beijingNoon(sched.workDate), startTime: sched.startTime, endTime: sched.endTime, remark: sched.remark || null });
    showSuccessToast('已保存');
    schedOpen.value = false;
    reload();
  } catch { /* */ } finally { saving.value = false; }
}
async function removeSchedule(id: number) {
  try { await showConfirmDialog({ title: '删除排班', message: '确认删除这条排班？' }); } catch { return; }
  try { await schedulesApi.remove(id); showSuccessToast('已删除'); reload(); } catch { /* */ }
}

// 登记请假
const leaveOpen = ref(false);
const showLeaveStaffPicker = ref(false);
const showLeaveTypePicker = ref(false);
const leaveForm = reactive({ userId: 0, type: 'Personal', from: todayStr(), to: todayStr(), startHalf: 'Morning', endHalf: 'Afternoon', reason: '' });
const leaveTypeCols = [{ text: '病假', value: 'Sick' }, { text: '事假', value: 'Personal' }, { text: '年假', value: 'Annual' }];
function leaveStaffName() { const s = staffList.value.find((x) => x.id === leaveForm.userId); return s ? (s.realName || s.username) : '选择员工'; }
const leaveDays = computed(() => {
  if (!leaveForm.from || !leaveForm.to) return 0;
  const from = new Date(leaveForm.from), to = new Date(leaveForm.to);
  if (to < from) return 0;
  let d = Math.round((to.getTime() - from.getTime()) / 86400000) + 1;
  if (leaveForm.startHalf === 'Afternoon') d -= 0.5;
  if (leaveForm.endHalf === 'Morning') d -= 0.5;
  return d < 0 ? 0 : d;
});
function openLeave() { Object.assign(leaveForm, { userId: 0, type: 'Personal', from: todayStr(), to: todayStr(), startHalf: 'Morning', endHalf: 'Afternoon', reason: '' }); leaveOpen.value = true; }
function onLeaveStaff({ selectedValues }: { selectedValues: number[] }) { leaveForm.userId = selectedValues[0] ?? 0; showLeaveStaffPicker.value = false; }
function onLeaveType({ selectedValues }: { selectedValues: string[] }) { leaveForm.type = selectedValues[0]; showLeaveTypePicker.value = false; }
async function submitLeave() {
  if (!leaveForm.userId) { showToast('请选择员工'); return; }
  if (leaveDays.value <= 0) { showToast('请假时长须大于 0'); return; }
  saving.value = true;
  try {
    await schedulesApi.submitLeave({ userId: leaveForm.userId, type: leaveForm.type, fromDate: beijingNoon(leaveForm.from), toDate: beijingNoon(leaveForm.to), startHalf: leaveForm.startHalf, endHalf: leaveForm.endHalf, reason: leaveForm.reason.trim() || null });
    showSuccessToast('已登记请假');
    leaveOpen.value = false;
    tabIndex.value = 1;
    reload();
  } catch { /* */ } finally { saving.value = false; }
}
async function approve(row: LeaveRequestDto, ok: boolean) {
  try { await schedulesApi.approveLeave(row.id, ok); showSuccessToast(ok ? '已同意' : '已驳回'); reload(); } catch { /* */ }
}

onMounted(async () => {
  if (!appStore.stores.length) await appStore.loadStores().catch(() => undefined);
  staffList.value = (await staffApi.list({ page: 1, pageSize: 200 }).catch(() => null))?.items ?? [];
  reload();
});
</script>

<template>
  <div class="qy-page schedules">
    <van-nav-bar title="排班与请假" left-text="返回" left-arrow @click-left="$router.back()">
      <template #right><span class="nav-add" @click="tabIndex === 0 ? openSchedule() : openLeave()">{{ tabIndex === 0 ? '排班' : '请假' }}</span></template>
    </van-nav-bar>

    <van-tabs v-model:active="tabIndex" @change="reload" sticky>
      <van-tab title="排班" />
      <van-tab title="请假审批" />
    </van-tabs>

    <van-pull-refresh v-model="refreshing" @refresh="reload">
      <template v-if="tabIndex === 0">
        <van-empty v-if="!loading && schedules.length === 0" description="暂无排班" />
        <div v-for="s in schedules" :key="s.id" class="sc">
          <div class="sc-main">
            <div class="sc-l1">{{ s.userName }} <span class="sc-date">{{ fmtDate(s.workDate) }}</span></div>
            <div class="sc-l2">{{ s.startTime }} - {{ s.endTime }}<span v-if="s.remark"> · {{ s.remark }}</span></div>
          </div>
          <van-button size="mini" type="danger" plain @click="removeSchedule(s.id)">删除</van-button>
        </div>
      </template>

      <template v-else>
        <van-empty v-if="!loading && leaves.length === 0" description="暂无请假" />
        <div v-for="l in leaves" :key="l.id" class="lv">
          <div class="lv-head">
            <span class="lv-name">{{ l.userName }} · {{ leaveTypeLabel(l.type) }}</span>
            <van-tag :type="leaveStatusType(l.status)">{{ leaveStatusLabel(l.status) }}</van-tag>
          </div>
          <div class="lv-sub">{{ fmtDate(l.fromDate) }} {{ halfLabel(l.startHalf) }} - {{ fmtDate(l.toDate) }} {{ halfLabel(l.endHalf) }} · {{ l.days }} 天</div>
          <div v-if="l.reason" class="lv-reason">{{ l.reason }}</div>
          <div v-if="l.approverName" class="lv-approver">审批人 {{ l.approverName }}</div>
          <div v-if="l.status === 'Pending'" class="lv-actions">
            <van-button size="mini" type="success" @click="approve(l, true)">同意</van-button>
            <van-button size="mini" type="danger" plain @click="approve(l, false)">驳回</van-button>
          </div>
        </div>
      </template>
    </van-pull-refresh>

    <!-- 新增排班 -->
    <van-popup v-model:show="schedOpen" position="bottom" round>
      <div class="sheet">
        <div class="sheet-title">新增排班</div>
        <van-form @submit="saveSchedule">
          <van-cell-group inset>
            <van-field label="员工" :model-value="schedStaffName()" readonly is-link @click="showSchedStaffPicker = true" />
            <van-field label="日期"><template #input><input v-model="sched.workDate" type="date" class="dt" /></template></van-field>
            <van-field label="开始"><template #input><input v-model="sched.startTime" type="time" class="dt" /></template></van-field>
            <van-field label="结束"><template #input><input v-model="sched.endTime" type="time" class="dt" /></template></van-field>
            <van-field v-model="sched.remark" label="备注" placeholder="选填" />
          </van-cell-group>
          <div class="sheet-actions"><van-button block type="primary" native-type="submit" :loading="saving">保存</van-button></div>
        </van-form>
      </div>
    </van-popup>
    <van-popup v-model:show="showSchedStaffPicker" position="bottom" round>
      <van-picker :columns="staffColumns()" @confirm="onSchedStaff" @cancel="showSchedStaffPicker = false" />
    </van-popup>

    <!-- 登记请假 -->
    <van-popup v-model:show="leaveOpen" position="bottom" round :style="{ maxHeight: '92%' }">
      <div class="sheet">
        <div class="sheet-title">登记请假</div>
        <van-cell-group inset>
          <van-field label="员工" :model-value="leaveStaffName()" readonly is-link @click="showLeaveStaffPicker = true" />
          <van-field label="类型" :model-value="leaveTypeLabel(leaveForm.type)" readonly is-link @click="showLeaveTypePicker = true" />
          <van-field label="开始日期"><template #input><input v-model="leaveForm.from" type="date" class="dt" /></template></van-field>
          <van-field label="结束日期"><template #input><input v-model="leaveForm.to" type="date" class="dt" /></template></van-field>
          <van-field label="开始时段">
            <template #input>
              <div class="seg"><button type="button" :class="{ on: leaveForm.startHalf === 'Morning' }" @click="leaveForm.startHalf = 'Morning'">上午</button><button type="button" :class="{ on: leaveForm.startHalf === 'Afternoon' }" @click="leaveForm.startHalf = 'Afternoon'">下午</button></div>
            </template>
          </van-field>
          <van-field label="结束时段">
            <template #input>
              <div class="seg"><button type="button" :class="{ on: leaveForm.endHalf === 'Morning' }" @click="leaveForm.endHalf = 'Morning'">上午</button><button type="button" :class="{ on: leaveForm.endHalf === 'Afternoon' }" @click="leaveForm.endHalf = 'Afternoon'">下午</button></div>
            </template>
          </van-field>
          <van-cell title="天数" :value="leaveDays > 0 ? `${leaveDays} 天` : '时长无效'" />
          <van-field v-model="leaveForm.reason" label="事由" type="textarea" rows="2" autosize placeholder="选填" />
        </van-cell-group>
        <div class="sheet-actions"><van-button block type="primary" :loading="saving" @click="submitLeave">提交</van-button></div>
      </div>
    </van-popup>
    <van-popup v-model:show="showLeaveStaffPicker" position="bottom" round>
      <van-picker :columns="staffColumns()" @confirm="onLeaveStaff" @cancel="showLeaveStaffPicker = false" />
    </van-popup>
    <van-popup v-model:show="showLeaveTypePicker" position="bottom" round>
      <van-picker :columns="leaveTypeCols" @confirm="onLeaveType" @cancel="showLeaveTypePicker = false" />
    </van-popup>
  </div>
</template>

<style scoped>
.nav-add { color: var(--qy-brand); font-size: 15px; }
.sc { display: flex; align-items: center; justify-content: space-between; background: #fff; margin: 8px 12px; padding: 14px; border-radius: 12px; }
.sc-l1 { font-size: 15px; font-weight: 600; }
.sc-date { color: #98a2b3; font-weight: 400; font-size: 13px; margin-left: 6px; }
.sc-l2 { margin-top: 6px; color: #6b7280; font-size: 13px; }
.lv { background: #fff; margin: 8px 12px; padding: 14px; border-radius: 12px; }
.lv-head { display: flex; align-items: center; justify-content: space-between; }
.lv-name { font-size: 15px; font-weight: 600; }
.lv-sub { margin-top: 6px; color: #6b7280; font-size: 13px; }
.lv-reason { margin-top: 4px; color: #98a2b3; font-size: 13px; }
.lv-approver { margin-top: 4px; color: #b0b8c4; font-size: 12px; }
.lv-actions { display: flex; gap: 8px; margin-top: 10px; justify-content: flex-end; }
.sheet { padding: 16px 0 24px; }
.sheet-title { text-align: center; font-size: 17px; font-weight: 700; margin-bottom: 12px; }
.sheet-actions { padding: 16px 16px 0; }
.dt { border: none; outline: none; font-size: 14px; background: transparent; font-family: inherit; width: 100%; }
.seg { display: flex; gap: 8px; width: 100%; }
.seg button { flex: 1; border: 1px solid #d6dbe2; background: #fff; color: #4b5563; border-radius: 8px; padding: 6px 0; font-size: 13px; }
.seg button.on { background: var(--qy-brand); color: #fff; border-color: var(--qy-brand); }
</style>
