<script lang="ts">
export default { name: 'PayrollView' };
</script>

<script setup lang="ts">
import { computed, onMounted, reactive, ref } from 'vue';
import {
  NavBar as VanNavBar, Tabs as VanTabs, Tab as VanTab, PullRefresh as VanPullRefresh,
  Empty as VanEmpty, Tag as VanTag, Button as VanButton, Popup as VanPopup, Form as VanForm,
  Field as VanField, Stepper as VanStepper, Cell as VanCell, CellGroup as VanCellGroup,
  showSuccessToast, showToast, showConfirmDialog
} from 'vant';
import {
  payrollApi, staffApi,
  type PayrollPeriodDto, type PayrollPeriodDetailDto, type PayrollItemDto, type SalaryProfileDto
} from '@/api/modules';
import { useAppStore } from '@/stores/app';
import type { Staff } from '@/api/types';

const appStore = useAppStore();
const tabIndex = ref(0);
const periods = ref<PayrollPeriodDto[]>([]);
const profiles = ref<SalaryProfileDto[]>([]);
const loading = ref(false);
const refreshing = ref(false);
const saving = ref(false);
const generating = ref(false);

function thisMonth() { const d = new Date(); return `${d.getFullYear()}-${String(d.getMonth() + 1).padStart(2, '0')}`; }
const genMonth = ref(thisMonth());

function fmt(n?: number | null) { return (n ?? 0).toFixed(2); }
function statusLabel(s: string) { return ({ Draft: '草稿', Locked: '已封盘', Paid: '已发放' } as Record<string, string>)[s] ?? s; }
function statusType(s: string) { return s === 'Draft' ? 'primary' : s === 'Locked' ? 'warning' : 'success'; }

async function loadPeriods() {
  if (!appStore.activeStoreId) return;
  loading.value = true;
  try { periods.value = await payrollApi.periods(appStore.activeStoreId); }
  catch { /* */ } finally { loading.value = false; refreshing.value = false; }
}
async function loadProfiles() {
  if (!appStore.activeStoreId) return;
  loading.value = true;
  try {
    const staff = (await staffApi.list({ storeId: appStore.activeStoreId, page: 1, pageSize: 200 })).items;
    const map = new Map<number, SalaryProfileDto>((await payrollApi.profiles(appStore.activeStoreId)).map((p) => [p.userId, p]));
    profiles.value = staff.map((u: Staff) => map.get(u.id) ?? ({ userId: u.id, userName: u.realName ?? u.username, baseMonthly: 0, overtimeHourRate: 0, attendanceBonusAmount: 0, requiredAttendanceDays: 0, remark: null }));
  } catch { /* */ } finally { loading.value = false; refreshing.value = false; }
}
function reload() { tabIndex.value === 0 ? loadPeriods() : loadProfiles(); }

async function generate() {
  if (!appStore.activeStoreId) return;
  const [y, m] = genMonth.value.split('-').map(Number);
  generating.value = true;
  try { await payrollApi.generate(appStore.activeStoreId, y, m, null); showSuccessToast('已生成草稿'); await loadPeriods(); }
  catch { /* */ } finally { generating.value = false; }
}
async function lockPeriod(row: PayrollPeriodDto) {
  try { await showConfirmDialog({ title: '封盘', message: `封盘后 ${row.year}-${row.month} 工资单不可再修改。确认？` }); } catch { return; }
  try { await payrollApi.lock(row.id); showSuccessToast('已封盘'); loadPeriods(); } catch { /* */ }
}
async function markPaid(row: PayrollPeriodDto) {
  try { await showConfirmDialog({ title: '标记发放', message: `确认 ${row.year}-${row.month} 已发放？` }); } catch { return; }
  try { await payrollApi.markPaid(row.id); showSuccessToast('已标记发放'); loadPeriods(); } catch { /* */ }
}
async function removeDraft(row: PayrollPeriodDto) {
  try { await showConfirmDialog({ title: '删除草稿', message: `删除 ${row.year}-${row.month} 工资单草稿？` }); } catch { return; }
  try { await payrollApi.removeDraft(row.id); showSuccessToast('已删除'); loadPeriods(); } catch { /* */ }
}

// 详情
const detailOpen = ref(false);
const detail = ref<PayrollPeriodDetailDto | null>(null);
const expandedId = ref<number | null>(null);
async function openDetail(row: PayrollPeriodDto) {
  detail.value = await payrollApi.period(row.id);
  expandedId.value = null;
  detailOpen.value = true;
}
const isDraft = computed(() => detail.value?.period.status === 'Draft');
function mergeItem(u: PayrollItemDto) {
  if (!detail.value) return;
  const i = detail.value.items.findIndex((x) => x.id === u.id);
  if (i >= 0) detail.value.items.splice(i, 1, u);
  detail.value.period.totalAmount = detail.value.items.reduce((a, b) => a + b.netTotal, 0);
}

// 编辑工资项
const editItemOpen = ref(false);
const editingItem = ref<PayrollItemDto | null>(null);
const editForm = reactive({ overtimeHours: 0, attendanceBonusOverride: -1, remark: '' });
function openEditItem(item: PayrollItemDto) {
  editingItem.value = item;
  editForm.overtimeHours = item.overtimeHours;
  editForm.attendanceBonusOverride = -1;
  editForm.remark = item.remark ?? '';
  editItemOpen.value = true;
}
async function saveItem() {
  if (!editingItem.value) return;
  saving.value = true;
  try { mergeItem(await payrollApi.updateItem(editingItem.value.id, editForm.overtimeHours, editForm.attendanceBonusOverride, editForm.remark || null)); editItemOpen.value = false; showSuccessToast('已保存'); }
  catch { /* */ } finally { saving.value = false; }
}

// 奖扣
const adjOpen = ref(false);
const adjTarget = ref<PayrollItemDto | null>(null);
const adjForm = reactive({ kind: 'Bonus', amount: 0, reason: '' });
function openAddAdj(item: PayrollItemDto) { adjTarget.value = item; expandedId.value = item.id; adjForm.kind = 'Bonus'; adjForm.amount = 0; adjForm.reason = ''; adjOpen.value = true; }
async function saveAdj() {
  if (!adjTarget.value) return;
  if (adjForm.amount <= 0 || !adjForm.reason.trim()) { showToast('金额与原因必填'); return; }
  saving.value = true;
  try { mergeItem(await payrollApi.addAdjustment(adjTarget.value.id, adjForm.kind, adjForm.amount, adjForm.reason.trim())); adjOpen.value = false; showSuccessToast('已新增'); }
  catch { /* */ } finally { saving.value = false; }
}
async function removeAdj(itemId: number, adjId: number) {
  try { mergeItem(await payrollApi.removeAdjustment(itemId, adjId)); showSuccessToast('已删除'); } catch { /* */ }
}

// 薪资配置
const profileOpen = ref(false);
const editingProfile = ref<SalaryProfileDto | null>(null);
const profileForm = reactive({ baseMonthly: 0, overtimeHourRate: 0, attendanceBonusAmount: 0, requiredAttendanceDays: 0, remark: '' });
function openProfile(p: SalaryProfileDto) {
  editingProfile.value = p;
  Object.assign(profileForm, { baseMonthly: p.baseMonthly, overtimeHourRate: p.overtimeHourRate, attendanceBonusAmount: p.attendanceBonusAmount, requiredAttendanceDays: p.requiredAttendanceDays, remark: p.remark ?? '' });
  profileOpen.value = true;
}
async function saveProfile() {
  if (!editingProfile.value) return;
  saving.value = true;
  try {
    await payrollApi.upsertProfile(editingProfile.value.userId, { baseMonthly: profileForm.baseMonthly, overtimeHourRate: profileForm.overtimeHourRate, attendanceBonusAmount: profileForm.attendanceBonusAmount, requiredAttendanceDays: profileForm.requiredAttendanceDays, remark: profileForm.remark || null });
    showSuccessToast('已保存');
    profileOpen.value = false;
    loadProfiles();
  } catch { /* */ } finally { saving.value = false; }
}

onMounted(async () => {
  if (!appStore.stores.length) await appStore.loadStores().catch(() => undefined);
  loadPeriods();
});
</script>

<template>
  <div class="qy-page payroll">
    <van-nav-bar :title="`工资结算 · ${appStore.activeStore?.name || ''}`" left-text="返回" left-arrow @click-left="$router.back()" />

    <van-tabs v-model:active="tabIndex" @change="reload" sticky>
      <van-tab title="工资单" />
      <van-tab title="薪资配置" />
    </van-tabs>

    <!-- 工资单 -->
    <template v-if="tabIndex === 0">
      <div class="gen card">
        <input v-model="genMonth" type="month" class="dt" />
        <van-button size="small" type="primary" :loading="generating" @click="generate">生成工资单</van-button>
      </div>
      <van-pull-refresh v-model="refreshing" @refresh="loadPeriods">
        <van-empty v-if="!loading && periods.length === 0" description="暂无工资单" />
        <div v-for="p in periods" :key="p.id" class="pd">
          <div class="pd-head">
            <span class="pd-month">{{ p.year }}-{{ String(p.month).padStart(2, '0') }}</span>
            <van-tag :type="statusType(p.status)">{{ statusLabel(p.status) }}</van-tag>
          </div>
          <div class="pd-sub">{{ p.itemCount }} 人 · 总额 <b class="qy-money">¥{{ fmt(p.totalAmount) }}</b><span v-if="p.operatorName"> · {{ p.operatorName }}</span></div>
          <div class="pd-actions">
            <van-button size="mini" type="primary" @click="openDetail(p)">查看</van-button>
            <van-button size="mini" type="warning" plain :disabled="p.status !== 'Draft'" @click="lockPeriod(p)">封盘</van-button>
            <van-button size="mini" type="success" plain :disabled="p.status !== 'Locked'" @click="markPaid(p)">已发放</van-button>
            <van-button size="mini" type="danger" plain :disabled="p.status !== 'Draft'" @click="removeDraft(p)">删除</van-button>
          </div>
        </div>
      </van-pull-refresh>
    </template>

    <!-- 薪资配置 -->
    <template v-else>
      <van-pull-refresh v-model="refreshing" @refresh="loadProfiles">
        <van-empty v-if="!loading && profiles.length === 0" description="暂无员工" />
        <div v-for="p in profiles" :key="p.userId" class="pf">
          <div class="pf-main">
            <div class="pf-name">{{ p.userName }}</div>
            <div class="pf-sub">底薪 ¥{{ fmt(p.baseMonthly) }} · 加班 ¥{{ fmt(p.overtimeHourRate) }}/时 · 满勤 ¥{{ fmt(p.attendanceBonusAmount) }}/{{ p.requiredAttendanceDays }}天</div>
          </div>
          <van-button size="mini" @click="openProfile(p)">编辑</van-button>
        </div>
      </van-pull-refresh>
    </template>

    <!-- 工资单详情 -->
    <van-popup v-model:show="detailOpen" position="bottom" round :style="{ maxHeight: '90%' }">
      <div class="sheet" v-if="detail">
        <div class="sheet-title">{{ detail.period.year }}-{{ String(detail.period.month).padStart(2, '0') }} 工资单</div>
        <div class="dt-metric">
          <van-tag :type="statusType(detail.period.status)">{{ statusLabel(detail.period.status) }}</van-tag>
          总额 <b class="qy-money">¥{{ fmt(detail.period.totalAmount) }}</b> · {{ detail.period.itemCount }} 人
        </div>
        <div v-for="it in detail.items" :key="it.id" class="pi">
          <div class="pi-head" @click="expandedId = expandedId === it.id ? null : it.id">
            <span class="pi-name">{{ it.userName }}<span v-if="it.employeeNo" class="pi-no"> #{{ it.employeeNo }}</span></span>
            <span class="pi-net qy-money">¥{{ fmt(it.netTotal) }}</span>
          </div>
          <div class="pi-break">底{{ fmt(it.baseSalary) }} · 提{{ fmt(it.commissionTotal) }} · 卡{{ fmt(it.referralCommissionTotal) }} · 加{{ fmt(it.overtimeAmount) }} · 勤{{ fmt(it.attendanceBonus) }} · 调{{ fmt(it.adjustmentTotal) }} · 小费{{ fmt(it.tipsTotal) }}</div>
          <div v-if="isDraft" class="pi-actions">
            <van-button size="mini" @click="openEditItem(it)">改</van-button>
            <van-button size="mini" plain @click="openAddAdj(it)">奖/扣</van-button>
          </div>
          <div v-if="expandedId === it.id && it.adjustments.length" class="pi-adj">
            <div v-for="a in it.adjustments" :key="a.id" class="adj-row">
              <span :class="{ neg: a.kind === 'Deduction' }">{{ a.kind === 'Bonus' ? '+' : '-' }}¥{{ fmt(a.amount) }}</span>
              <span class="adj-reason">{{ a.reason }}</span>
              <van-button v-if="isDraft" size="mini" type="danger" plain @click="removeAdj(it.id, a.id)">删</van-button>
            </div>
          </div>
        </div>
      </div>
    </van-popup>

    <!-- 编辑工资项 -->
    <van-popup v-model:show="editItemOpen" position="bottom" round>
      <div class="sheet" v-if="editingItem">
        <div class="sheet-title">编辑：{{ editingItem.userName }}</div>
        <van-cell-group inset>
          <van-field label="加班小时"><template #input><van-stepper v-model="editForm.overtimeHours" :min="0" :decimal-length="2" /></template></van-field>
          <van-field label="满勤奖覆盖" label-width="110"><template #input><van-stepper v-model="editForm.attendanceBonusOverride" :min="-1" :decimal-length="2" /></template></van-field>
          <van-cell title="" label="满勤奖填 -1 沿用配置自动计算" />
          <van-field v-model="editForm.remark" label="备注" type="textarea" rows="1" autosize placeholder="选填" />
        </van-cell-group>
        <div class="sheet-actions"><van-button block type="primary" :loading="saving" @click="saveItem">保存</van-button></div>
      </div>
    </van-popup>

    <!-- 奖扣 -->
    <van-popup v-model:show="adjOpen" position="bottom" round>
      <div class="sheet" v-if="adjTarget">
        <div class="sheet-title">奖金/扣款：{{ adjTarget.userName }}</div>
        <van-cell-group inset>
          <van-field label="类型">
            <template #input>
              <div class="seg"><button type="button" :class="{ on: adjForm.kind === 'Bonus' }" @click="adjForm.kind = 'Bonus'">奖金</button><button type="button" :class="{ on: adjForm.kind === 'Deduction' }" @click="adjForm.kind = 'Deduction'">扣款</button></div>
            </template>
          </van-field>
          <van-field label="金额"><template #input><van-stepper v-model="adjForm.amount" :min="0" :step="10" :decimal-length="2" /></template></van-field>
          <van-field v-model="adjForm.reason" label="原因" placeholder="必填" />
        </van-cell-group>
        <div class="sheet-actions"><van-button block type="primary" :loading="saving" @click="saveAdj">保存</van-button></div>
      </div>
    </van-popup>

    <!-- 薪资配置编辑 -->
    <van-popup v-model:show="profileOpen" position="bottom" round>
      <div class="sheet" v-if="editingProfile">
        <div class="sheet-title">薪资配置：{{ editingProfile.userName }}</div>
        <van-cell-group inset>
          <van-field label="月底薪"><template #input><van-stepper v-model="profileForm.baseMonthly" :min="0" :step="100" :decimal-length="2" /></template></van-field>
          <van-field label="加班时薪"><template #input><van-stepper v-model="profileForm.overtimeHourRate" :min="0" :step="5" :decimal-length="2" /></template></van-field>
          <van-field label="满勤奖额度"><template #input><van-stepper v-model="profileForm.attendanceBonusAmount" :min="0" :step="50" :decimal-length="2" /></template></van-field>
          <van-field label="满勤所需天数"><template #input><van-stepper v-model="profileForm.requiredAttendanceDays" :min="0" /></template></van-field>
          <van-field v-model="profileForm.remark" label="备注" type="textarea" rows="1" autosize placeholder="选填" />
        </van-cell-group>
        <div class="sheet-actions"><van-button block type="primary" :loading="saving" @click="saveProfile">保存</van-button></div>
      </div>
    </van-popup>
  </div>
</template>

<style scoped>
.card { background: #fff; margin: 10px 12px; border-radius: 12px; padding: 12px; }
.gen { display: flex; align-items: center; gap: 10px; }
.dt { border: 1px solid #e3e7ec; border-radius: 8px; padding: 7px 10px; font-size: 14px; font-family: inherit; flex: 1; }
.pd { background: #fff; margin: 8px 12px; padding: 14px; border-radius: 12px; }
.pd-head { display: flex; align-items: center; justify-content: space-between; }
.pd-month { font-size: 16px; font-weight: 700; }
.pd-sub { margin-top: 6px; color: #6b7280; font-size: 13px; }
.pd-sub b { color: var(--qy-brand); }
.pd-actions { display: flex; gap: 8px; margin-top: 10px; flex-wrap: wrap; }
.pf { display: flex; align-items: center; justify-content: space-between; background: #fff; margin: 8px 12px; padding: 14px; border-radius: 12px; }
.pf-name { font-size: 15px; font-weight: 600; }
.pf-sub { margin-top: 6px; color: #6b7280; font-size: 12px; }
.sheet { padding: 16px 0 24px; }
.sheet-title { text-align: center; font-size: 17px; font-weight: 700; margin-bottom: 12px; }
.sheet-actions { padding: 16px 16px 0; }
.dt-metric { text-align: center; margin-bottom: 12px; color: #4b5563; }
.dt-metric b { color: var(--qy-brand); }
.pi { background: #fff; margin: 0 16px 8px; border: 1px solid #eef1f4; border-radius: 10px; padding: 12px; }
.pi-head { display: flex; align-items: center; justify-content: space-between; }
.pi-name { font-weight: 600; }
.pi-no { color: #b0b8c4; font-weight: 400; font-size: 13px; }
.pi-net { color: var(--qy-brand); font-weight: 700; }
.pi-break { margin-top: 6px; color: #98a2b3; font-size: 12px; }
.pi-actions { display: flex; gap: 8px; margin-top: 8px; }
.pi-adj { margin-top: 8px; border-top: 1px dashed #eef1f4; padding-top: 8px; }
.adj-row { display: flex; align-items: center; gap: 8px; padding: 4px 0; font-size: 13px; }
.adj-reason { flex: 1; color: #6b7280; }
.neg { color: #ee4d4d; }
.seg { display: flex; gap: 8px; width: 100%; }
.seg button { flex: 1; border: 1px solid #d6dbe2; background: #fff; color: #4b5563; border-radius: 8px; padding: 6px 0; font-size: 14px; }
.seg button.on { background: var(--qy-brand); color: #fff; border-color: var(--qy-brand); }
</style>
