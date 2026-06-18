<script lang="ts">
export default { name: 'StaffView' };
</script>

<script setup lang="ts">
import { computed, onMounted, reactive, ref } from 'vue';
import {
  NavBar as VanNavBar, Search as VanSearch, List as VanList, PullRefresh as VanPullRefresh,
  Empty as VanEmpty, Tag as VanTag, Button as VanButton, Popup as VanPopup, Form as VanForm,
  Field as VanField, Stepper as VanStepper, Switch as VanSwitch, Picker as VanPicker,
  Cell as VanCell, CellGroup as VanCellGroup, NoticeBar as VanNoticeBar,
  showSuccessToast, showToast, showConfirmDialog
} from 'vant';
import { staffApi, type StaffTransferDto } from '@/api/modules';
import { useAppStore } from '@/stores/app';
import { ROLE_LABELS, type Staff, type UserRole } from '@/api/types';

const appStore = useAppStore();
const rows = ref<Staff[]>([]);
const loading = ref(false);
const finished = ref(false);
const refreshing = ref(false);
const saving = ref(false);
const page = ref(1);
const pageSize = 20;
const keyword = ref('');
const roleFilter = ref('');
const showRoleFilterPicker = ref(false);

const ROLES: { v: UserRole; label: string }[] = [
  { v: 'ShopOwner', label: '店主' }, { v: 'StoreManager', label: '店长' },
  { v: 'Cashier', label: '收银员' }, { v: 'Technician', label: '技师' }
];
const roleFilterColumns = [{ text: '全部角色', value: '' }, ...ROLES.map((r) => ({ text: r.label, value: r.v }))];
function roleFilterName() { return ROLES.find((r) => r.v === roleFilter.value)?.label || '全部角色'; }
function roleLabel(r: string) { return ROLE_LABELS[r as UserRole] ?? r; }
const storeMap = computed(() => Object.fromEntries(appStore.stores.map((s) => [s.id, s.name])));
function storeName(id?: number | null) { return id ? (storeMap.value[id] ?? `#${id}`) : '—'; }

const SPECIALTY_OPTIONS = ['肩颈', '足疗', '头疗', '推拿', '按摩', '艾灸', '拔罐', '刮痧', '中医理疗', '泰式按摩', '精油 SPA', '产后修复', '小儿推拿', '盲人按摩'];

async function onLoad() {
  loading.value = true;
  try {
    const data = await staffApi.list({ page: page.value, pageSize, keyword: keyword.value.trim() || undefined, role: roleFilter.value || undefined });
    rows.value.push(...data.items);
    page.value += 1;
    if (rows.value.length >= data.total || data.items.length === 0) finished.value = true;
  } catch { finished.value = true; } finally { loading.value = false; }
}
function reset() { rows.value = []; page.value = 1; finished.value = false; }
async function onRefresh() { reset(); await onLoad(); refreshing.value = false; }
function onFilter() { reset(); onLoad(); }
function onRoleFilter({ selectedValues }: { selectedValues: string[] }) { roleFilter.value = selectedValues[0] || ''; showRoleFilterPicker.value = false; onFilter(); }

// 添加/编辑
const formOpen = ref(false);
const formMode = ref<'create' | 'edit'>('create');
const editingId = ref<number | null>(null);
const showRolePicker = ref(false);
const showStorePicker = ref(false);
const form = reactive({
  password: '', realName: '', phone: '', employeeNo: 1,
  role: 'Technician' as UserRole, storeId: null as number | null,
  isBlind: false, isActive: true, idCardNo: '', birthDate: '',
  emergencyContactName: '', emergencyContactPhone: '', hireDate: '', terminationDate: '', specialties: ''
});
const roleColumns = ROLES.map((r) => ({ text: r.label, value: r.v }));
const storeColumns = () => appStore.stores.map((s) => ({ text: s.name, value: s.id }));
function formRoleName() { return ROLES.find((r) => r.v === form.role)?.label || '选择角色'; }
const selectedSpecialties = computed(() => form.specialties ? form.specialties.split(',').map((s) => s.trim()).filter(Boolean) : []);
function toggleSpecialty(opt: string) {
  const set = new Set(selectedSpecialties.value);
  set.has(opt) ? set.delete(opt) : set.add(opt);
  form.specialties = Array.from(set).join(',');
}
function onIdCard(v: string) {
  if (!v || v.length < 14) return;
  const y = v.slice(6, 10), m = v.slice(10, 12), d = v.slice(12, 14);
  if (/^\d{4}$/.test(y) && /^\d{2}$/.test(m) && /^\d{2}$/.test(d)) {
    const mm = +m, dd = +d;
    if (mm >= 1 && mm <= 12 && dd >= 1 && dd <= 31) form.birthDate = `${y}-${m}-${d}`;
  }
}

async function nextEmployeeNo(): Promise<number> {
  try { const d = await staffApi.list({ page: 1, pageSize: 1000 }); return d.items.reduce((m, s) => (s.employeeNo && s.employeeNo > m ? s.employeeNo : m), 0) + 1; }
  catch { return 1; }
}
async function openCreate() {
  formMode.value = 'create';
  editingId.value = null;
  const next = await nextEmployeeNo();
  Object.assign(form, { password: '', realName: '', phone: '', employeeNo: next, role: 'Technician', storeId: appStore.activeStoreId, isBlind: false, isActive: true, idCardNo: '', birthDate: '', emergencyContactName: '', emergencyContactPhone: '', hireDate: '', terminationDate: '', specialties: '' });
  formOpen.value = true;
}
function openEdit(row: Staff) {
  formMode.value = 'edit';
  editingId.value = row.id;
  Object.assign(form, {
    password: '', realName: row.realName ?? '', phone: row.phone ?? '', employeeNo: row.employeeNo ?? 1,
    role: row.role, storeId: row.storeId ?? null, isBlind: row.isBlind, isActive: row.isActive,
    idCardNo: row.idCardNo ?? '', birthDate: row.birthDate ?? '', emergencyContactName: row.emergencyContactName ?? '',
    emergencyContactPhone: row.emergencyContactPhone ?? '', hireDate: row.hireDate ?? '', terminationDate: row.terminationDate ?? '', specialties: row.specialties ?? ''
  });
  formOpen.value = true;
}
function onRolePicked({ selectedValues }: { selectedValues: UserRole[] }) { form.role = selectedValues[0]; showRolePicker.value = false; }
function onStorePicked({ selectedValues }: { selectedValues: number[] }) { form.storeId = selectedValues[0]; showStorePicker.value = false; }

async function save() {
  if (!/^\d{11}$/.test(form.phone)) { showToast('请输入 11 位手机号'); return; }
  if (!form.realName.trim()) { showToast('请输入姓名'); return; }
  if (!/^\d{17}[\dXx]$/.test(form.idCardNo)) { showToast('身份证号格式不正确'); return; }
  if (formMode.value === 'create' && form.password.length < 6) { showToast('初始密码至少 6 位'); return; }
  if (!form.storeId) { showToast('请选择门店'); return; }
  saving.value = true;
  try {
    const base = {
      realName: form.realName || null, phone: form.phone || null, employeeNo: form.employeeNo,
      role: form.role, storeId: form.storeId, isBlind: form.isBlind,
      idCardNo: form.idCardNo || null, birthDate: form.birthDate || null,
      emergencyContactName: form.emergencyContactName || null, emergencyContactPhone: form.emergencyContactPhone || null,
      hireDate: form.hireDate || null, terminationDate: form.terminationDate || null, specialties: form.specialties || null
    };
    if (formMode.value === 'create') {
      await staffApi.create({ username: form.phone, password: form.password, ...base });
    } else if (editingId.value != null) {
      await staffApi.update(editingId.value, { ...base, isActive: form.isActive });
    }
    showSuccessToast('已保存');
    formOpen.value = false;
    onFilter();
  } catch { /* */ } finally { saving.value = false; }
}

// 重置密码
const pwdOpen = ref(false);
const pwdTarget = ref<Staff | null>(null);
const newPassword = ref('');
function openResetPwd(row: Staff) { pwdTarget.value = row; newPassword.value = ''; pwdOpen.value = true; }
async function doResetPwd() {
  if (!pwdTarget.value) return;
  if (newPassword.value.length < 6) { showToast('密码至少 6 位'); return; }
  saving.value = true;
  try { await staffApi.resetPassword(pwdTarget.value.id, newPassword.value); showSuccessToast('密码已重置'); pwdOpen.value = false; }
  catch { /* */ } finally { saving.value = false; }
}

// 跨店调动
const transferOpen = ref(false);
const transferTarget = ref<Staff | null>(null);
const transferHistory = ref<StaffTransferDto[]>([]);
const showToStorePicker = ref(false);
const tf = reactive({ toStoreId: null as number | null, kind: 'Permanent', expectedReturnAt: '', reason: '' });
function transferStatusLabel(s: string) { return ({ InEffect: '生效中', Returned: '已归还', Cancelled: '已撤销' } as Record<string, string>)[s] ?? s; }
const toStoreColumns = () => appStore.stores.filter((s) => s.id !== transferTarget.value?.storeId).map((s) => ({ text: s.name, value: s.id }));
function toStoreName() { return appStore.stores.find((s) => s.id === tf.toStoreId)?.name || '选择调入门店'; }
async function openTransfer(row: Staff) {
  transferTarget.value = row;
  Object.assign(tf, { toStoreId: null, kind: 'Permanent', expectedReturnAt: '', reason: '' });
  transferOpen.value = true;
  transferHistory.value = await staffApi.transfers({ userId: row.id }).catch(() => []);
}
function onToStorePicked({ selectedValues }: { selectedValues: number[] }) { tf.toStoreId = selectedValues[0]; showToStorePicker.value = false; }
async function doTransfer() {
  if (!transferTarget.value) return;
  if (!tf.toStoreId) { showToast('请选择调入门店'); return; }
  if (tf.kind === 'Temporary' && !tf.expectedReturnAt) { showToast('临时借调需填预计归还日期'); return; }
  saving.value = true;
  try {
    await staffApi.transfer(transferTarget.value.id, { toStoreId: tf.toStoreId, kind: tf.kind, expectedReturnAt: tf.kind === 'Temporary' ? tf.expectedReturnAt : null, reason: tf.reason || null });
    showSuccessToast('调动完成');
    transferOpen.value = false;
    onFilter();
  } catch { /* */ } finally { saving.value = false; }
}
async function returnTransfer(row: StaffTransferDto) {
  try { await showConfirmDialog({ title: '归还借调', message: `该员工将调回 ${row.fromStoreName}。` }); } catch { return; }
  try { await staffApi.returnTransfer(row.id); showSuccessToast('已归还'); transferOpen.value = false; onFilter(); } catch { /* */ }
}

onMounted(async () => {
  if (!appStore.stores.length) await appStore.loadStores().catch(() => undefined);
  onLoad();
});
</script>

<template>
  <div class="qy-page staff">
    <van-nav-bar title="员工管理" left-text="返回" left-arrow @click-left="$router.back()">
      <template #right><span class="nav-add" @click="openCreate">添加</span></template>
    </van-nav-bar>

    <van-search v-model="keyword" placeholder="账号 / 姓名 / 手机号" @search="onFilter" @clear="onFilter" />
    <div class="filter"><span class="f-label" @click="showRoleFilterPicker = true">角色：{{ roleFilterName() }} ▾</span></div>

    <van-pull-refresh v-model="refreshing" @refresh="onRefresh">
      <van-empty v-if="finished && rows.length === 0" description="暂无员工" />
      <van-list v-else v-model:loading="loading" :finished="finished" finished-text="没有更多了" @load="onLoad">
        <div v-for="s in rows" :key="s.id" class="sf">
          <div class="sf-l1">
            <span class="sf-no">#{{ s.employeeNo ?? '-' }}</span>
            <span class="sf-name">{{ s.realName || s.username }}</span>
            <van-tag type="primary" plain>{{ roleLabel(s.role) }}</van-tag>
            <van-tag v-if="s.isBlind" type="warning">盲人</van-tag>
            <van-tag :type="s.isActive ? 'success' : 'danger'">{{ s.isActive ? '在职' : '停职' }}</van-tag>
          </div>
          <div class="sf-l2">{{ s.phone || '无手机' }} · {{ storeName(s.storeId) }}</div>
          <div class="sf-actions">
            <van-button size="mini" @click="openEdit(s)">编辑</van-button>
            <van-button size="mini" type="warning" plain @click="openResetPwd(s)">重置密码</van-button>
            <van-button size="mini" plain @click="openTransfer(s)">跨店调动</van-button>
          </div>
        </div>
      </van-list>
    </van-pull-refresh>

    <van-popup v-model:show="showRoleFilterPicker" position="bottom" round>
      <van-picker :columns="roleFilterColumns" @confirm="onRoleFilter" @cancel="showRoleFilterPicker = false" />
    </van-popup>

    <!-- 添加/编辑 -->
    <van-popup v-model:show="formOpen" position="bottom" round :style="{ maxHeight: '94%' }">
      <div class="sheet">
        <div class="sheet-title">{{ formMode === 'create' ? '添加员工' : '编辑员工' }}</div>
        <van-form @submit="save">
          <van-cell-group inset>
            <van-field label="工号"><template #input><van-stepper v-model="form.employeeNo" :min="0" /></template></van-field>
            <van-field v-model="form.phone" label="手机号" type="tel" placeholder="11位，用作登录账号" />
            <van-field v-if="formMode === 'create'" v-model="form.password" label="初始密码" type="password" placeholder="至少6位" />
            <van-field v-model="form.realName" label="姓名" placeholder="必填" />
            <van-field v-model="form.idCardNo" label="身份证号" maxlength="18" placeholder="18位" @update:model-value="onIdCard" />
            <van-field label="出生日期"><template #input><input v-model="form.birthDate" type="date" class="dt" /></template></van-field>
            <van-field v-model="form.emergencyContactName" label="紧急联系人" placeholder="选填" />
            <van-field v-model="form.emergencyContactPhone" label="联系人电话" placeholder="选填" />
            <van-field label="入职日期"><template #input><input v-model="form.hireDate" type="date" class="dt" /></template></van-field>
            <van-field label="离职日期"><template #input><input v-model="form.terminationDate" type="date" class="dt" /></template></van-field>
            <van-field label="角色" :model-value="formRoleName()" readonly is-link @click="showRolePicker = true" />
            <van-field label="所属门店" :model-value="storeName(form.storeId)" readonly is-link @click="showStorePicker = true" />
            <van-field label="盲人技师"><template #input><van-switch v-model="form.isBlind" /></template></van-field>
            <van-field v-if="formMode === 'edit'" label="在职"><template #input><van-switch v-model="form.isActive" /></template></van-field>
          </van-cell-group>
          <div class="spec">
            <div class="spec-h">专长（可多选）</div>
            <button v-for="o in SPECIALTY_OPTIONS" :key="o" type="button" class="chip" :class="{ on: selectedSpecialties.includes(o) }" @click="toggleSpecialty(o)">{{ o }}</button>
          </div>
          <div class="sheet-actions"><van-button block type="primary" native-type="submit" :loading="saving">保存</van-button></div>
        </van-form>
      </div>
    </van-popup>
    <van-popup v-model:show="showRolePicker" position="bottom" round>
      <van-picker :columns="roleColumns" @confirm="onRolePicked" @cancel="showRolePicker = false" />
    </van-popup>
    <van-popup v-model:show="showStorePicker" position="bottom" round>
      <van-picker :columns="storeColumns()" @confirm="onStorePicked" @cancel="showStorePicker = false" />
    </van-popup>

    <!-- 重置密码 -->
    <van-popup v-model:show="pwdOpen" position="bottom" round>
      <div class="sheet">
        <div class="sheet-title">重置密码：{{ pwdTarget?.username }}</div>
        <van-cell-group inset>
          <van-field v-model="newPassword" label="新密码" type="password" placeholder="至少6位" />
        </van-cell-group>
        <div class="sheet-actions"><van-button block type="primary" :loading="saving" @click="doResetPwd">确认</van-button></div>
      </div>
    </van-popup>

    <!-- 跨店调动 -->
    <van-popup v-model:show="transferOpen" position="bottom" round :style="{ maxHeight: '90%' }">
      <div class="sheet">
        <div class="sheet-title">跨店调动：{{ transferTarget?.realName || transferTarget?.username }}</div>
        <van-cell-group inset>
          <van-cell title="当前门店" :value="storeName(transferTarget?.storeId)" />
          <van-field label="调入门店" :model-value="toStoreName()" readonly is-link @click="showToStorePicker = true" />
          <van-field label="调动类型">
            <template #input>
              <div class="seg">
                <button type="button" :class="{ on: tf.kind === 'Permanent' }" @click="tf.kind = 'Permanent'">永久调动</button>
                <button type="button" :class="{ on: tf.kind === 'Temporary' }" @click="tf.kind = 'Temporary'">临时借调</button>
              </div>
            </template>
          </van-field>
          <van-field v-if="tf.kind === 'Temporary'" label="预计归还"><template #input><input v-model="tf.expectedReturnAt" type="date" class="dt" /></template></van-field>
          <van-field v-model="tf.reason" label="原因" type="textarea" rows="1" autosize placeholder="选填" />
        </van-cell-group>
        <van-notice-bar wrapable :scrollable="false" text="调动后该员工的叫号队列会迁到新店并置为下班，需在新店重新上钟。" />

        <div v-if="transferHistory.length" class="th">
          <div class="th-h">调动历史</div>
          <div v-for="h in transferHistory" :key="h.id" class="th-row">
            <span>{{ h.fromStoreName }} → {{ h.toStoreName }} · {{ h.kind === 'Permanent' ? '永久' : '临时' }}</span>
            <span class="th-right">
              <van-tag :type="h.status === 'InEffect' ? 'success' : 'default'">{{ transferStatusLabel(h.status) }}</van-tag>
              <van-button v-if="h.kind === 'Temporary' && h.status === 'InEffect'" size="mini" type="primary" plain @click="returnTransfer(h)">归还</van-button>
            </span>
          </div>
        </div>

        <div class="sheet-actions"><van-button block type="primary" :loading="saving" @click="doTransfer">确认调动</van-button></div>
      </div>
    </van-popup>
    <van-popup v-model:show="showToStorePicker" position="bottom" round>
      <van-picker :columns="toStoreColumns()" @confirm="onToStorePicked" @cancel="showToStorePicker = false" />
    </van-popup>
  </div>
</template>

<style scoped>
.nav-add { color: var(--qy-brand); font-size: 15px; }
.filter { padding: 8px 14px; }
.f-label { color: var(--qy-brand); font-size: 14px; }
.sf { background: #fff; margin: 8px 12px; padding: 14px; border-radius: 12px; }
.sf-l1 { display: flex; align-items: center; gap: 6px; flex-wrap: wrap; }
.sf-no { color: #b0b8c4; font-size: 13px; }
.sf-name { font-size: 15px; font-weight: 600; }
.sf-l2 { margin-top: 6px; color: #6b7280; font-size: 13px; }
.sf-actions { display: flex; gap: 8px; margin-top: 10px; }
.sheet { padding: 16px 0 24px; }
.sheet-title { text-align: center; font-size: 17px; font-weight: 700; margin-bottom: 12px; }
.sheet-actions { padding: 16px 16px 0; }
.dt { border: none; outline: none; font-size: 14px; background: transparent; font-family: inherit; width: 100%; }
.spec { margin: 12px 16px; }
.spec-h { font-weight: 600; margin-bottom: 8px; }
.chip { border: 1px solid #d6dbe2; background: #fff; color: #4b5563; border-radius: 16px; padding: 5px 12px; font-size: 13px; margin: 0 8px 8px 0; }
.chip.on { background: var(--qy-brand); color: #fff; border-color: var(--qy-brand); }
.seg { display: flex; gap: 8px; width: 100%; }
.seg button { flex: 1; border: 1px solid #d6dbe2; background: #fff; color: #4b5563; border-radius: 8px; padding: 6px 0; font-size: 14px; }
.seg button.on { background: var(--qy-brand); color: #fff; border-color: var(--qy-brand); }
.th { margin: 12px 16px 0; }
.th-h { font-weight: 600; margin-bottom: 8px; }
.th-row { display: flex; align-items: center; justify-content: space-between; padding: 8px 0; border-top: 1px solid #f1f3f5; font-size: 13px; color: #4b5563; }
.th-right { display: flex; align-items: center; gap: 8px; }
</style>
