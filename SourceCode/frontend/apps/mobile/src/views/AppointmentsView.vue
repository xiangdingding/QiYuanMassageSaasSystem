<script lang="ts">
export default { name: 'AppointmentsView' };
</script>

<script setup lang="ts">
import { onMounted, ref } from 'vue';
import {
  NavBar as VanNavBar, Tabs as VanTabs, Tab as VanTab, PullRefresh as VanPullRefresh,
  Empty as VanEmpty, Tag as VanTag, Button as VanButton, Popup as VanPopup, Icon as VanIcon,
  Form as VanForm, Field as VanField, Stepper as VanStepper, Picker as VanPicker,
  CellGroup as VanCellGroup, showSuccessToast, showConfirmDialog, showToast
} from 'vant';
import { appointmentsApi, servicesApi, staffApi } from '@/api/modules';
import { useAppStore } from '@/stores/app';
import { APPOINTMENT_STATUS_LABELS, type Appointment, type AppointmentStatus, type ServiceItem, type Staff } from '@/api/types';

const appStore = useAppStore();

const tabIndex = ref(0);
const tabs: { label: string; value: AppointmentStatus | '' }[] = [
  { label: '待确认', value: 'Pending' },
  { label: '已确认', value: 'Confirmed' },
  { label: '已到店', value: 'Arrived' },
  { label: '全部', value: '' }
];

const list = ref<Appointment[]>([]);
const loading = ref(false);
const refreshing = ref(false);
const acting = ref(false);

function statusLabel(s: AppointmentStatus) { return APPOINTMENT_STATUS_LABELS[s] ?? s; }
function statusType(s: AppointmentStatus) {
  switch (s) {
    case 'Pending': return 'warning';
    case 'Confirmed': return 'primary';
    case 'Arrived': return 'success';
    case 'Cancelled': return 'danger';
    case 'NoShow': return 'default';
    default: return 'default';
  }
}
function fmtTime(s: string) { return s.slice(0, 16).replace('T', ' '); }

async function load() {
  if (!appStore.activeStoreId) return;
  loading.value = true;
  try {
    const res = await appointmentsApi.list({
      storeId: appStore.activeStoreId,
      status: tabs[tabIndex.value].value || undefined,
      page: 1,
      pageSize: 50
    });
    list.value = res.items;
  } catch {
    /* 拦截器已提示 */
  } finally {
    loading.value = false;
    refreshing.value = false;
  }
}

function onTab() { load(); }

async function confirm(a: Appointment) {
  acting.value = true;
  try { await appointmentsApi.confirm(a.id); showSuccessToast('已确认'); await load(); }
  catch { /* */ } finally { acting.value = false; }
}
async function arrive(a: Appointment) {
  acting.value = true;
  try { await appointmentsApi.arrive(a.id); showSuccessToast('已到店'); await load(); }
  catch { /* */ } finally { acting.value = false; }
}
async function cancel(a: Appointment) {
  try { await showConfirmDialog({ title: '取消预约', message: `取消 ${a.customerName} 的预约？` }); }
  catch { return; }
  acting.value = true;
  try { await appointmentsApi.cancel(a.id); showToast('已取消'); await load(); }
  catch { /* */ } finally { acting.value = false; }
}

// ------- 新增 / 修改 / 再次预约 -------
const showAdd = ref(false);
const submitting = ref(false);
const editingId = ref<number | null>(null); // 非空 = 修改模式，提交走 update
const services = ref<ServiceItem[]>([]);
const technicians = ref<Staff[]>([]);
const form = ref({
  customerName: '',
  customerPhone: '',
  expectedArriveAt: '',
  partySize: 1,
  serviceId: null as number | null,
  preferredTechnicianId: null as number | null,
  remark: ''
});
const formTitle = () => (editingId.value ? '修改预约' : '新增预约');

const showServicePicker = ref(false);
const showTechPicker = ref(false);
// Vant Picker 的 value 不接受 null，用 0 作「不指定」哨兵，确认时再转回 null。
const serviceColumns = () => [{ text: '不指定', value: 0 }, ...services.value.map((s) => ({ text: s.name, value: s.id }))];
const techColumns = () => [{ text: '不指定', value: 0 }, ...technicians.value.map((t) => ({ text: `${t.realName || t.username}${t.employeeNo ? ' #' + t.employeeNo : ''}`, value: t.id }))];
const serviceName = () => services.value.find((s) => s.id === form.value.serviceId)?.name || '不指定';
const techName = () => {
  const t = technicians.value.find((x) => x.id === form.value.preferredTechnicianId);
  return t ? (t.realName || t.username) : '不指定';
};

async function ensureLookups() {
  if (!services.value.length) services.value = await servicesApi.list().catch(() => []);
  if (!technicians.value.length && appStore.activeStoreId) {
    technicians.value = await staffApi.list({ role: 'Technician', storeId: appStore.activeStoreId, pageSize: 200 })
      .then((r) => r.items).catch(() => []);
  }
}

async function openAdd() {
  editingId.value = null;
  form.value = { customerName: '', customerPhone: '', expectedArriveAt: defaultArrive(), partySize: 1, serviceId: null, preferredTechnicianId: null, remark: '' };
  showAdd.value = true;
  await ensureLookups();
}

// 修改未确认（Pending）的预约：信息全数预填，到店时间保留原值
async function openEdit(a: Appointment) {
  if (a.status !== 'Pending') { showToast('仅未确认的预约可修改'); return; }
  editingId.value = a.id;
  form.value = {
    customerName: a.customerName,
    customerPhone: a.customerPhone,
    expectedArriveAt: toLocalInput(a.expectedArriveAt),
    partySize: a.partySize || 1,
    serviceId: a.serviceId ?? null,
    preferredTechnicianId: a.preferredTechnicianId ?? null,
    remark: a.remark ?? ''
  };
  showAdd.value = true;
  await ensureLookups();
}

// 再次预约：基于已取消单，复用顾客信息，到店时间给个默认值由前台再调
async function openRebook(a: Appointment) {
  editingId.value = null;
  form.value = {
    customerName: a.customerName,
    customerPhone: a.customerPhone,
    expectedArriveAt: defaultArrive(),
    partySize: a.partySize || 1,
    serviceId: a.serviceId ?? null,
    preferredTechnicianId: a.preferredTechnicianId ?? null,
    remark: a.remark ?? ''
  };
  showAdd.value = true;
  await ensureLookups();
}

function defaultArrive(): string {
  const d = new Date(Date.now() + 60 * 60 * 1000);
  d.setMinutes(0, 0, 0);
  return toLocalInput(d.toISOString());
}
// ISO 时间 → 本地 datetime-local 格式 yyyy-MM-ddTHH:mm
function toLocalInput(iso: string): string {
  const d = new Date(iso);
  const pad = (n: number) => String(n).padStart(2, '0');
  return `${d.getFullYear()}-${pad(d.getMonth() + 1)}-${pad(d.getDate())}T${pad(d.getHours())}:${pad(d.getMinutes())}`;
}

function onServiceConfirm({ selectedValues }: { selectedValues: number[] }) {
  form.value.serviceId = selectedValues[0] || null;
  showServicePicker.value = false;
}
function onTechConfirm({ selectedValues }: { selectedValues: number[] }) {
  form.value.preferredTechnicianId = selectedValues[0] || null;
  showTechPicker.value = false;
}

async function submit() {
  if (!appStore.activeStoreId) return;
  if (!form.value.customerName.trim()) { showToast('请填写顾客姓名'); return; }
  if (!form.value.customerPhone.trim()) { showToast('请填写联系电话'); return; }
  if (!form.value.expectedArriveAt) { showToast('请选择到店时间'); return; }
  submitting.value = true;
  const body = {
    customerName: form.value.customerName.trim(),
    customerPhone: form.value.customerPhone.trim(),
    expectedArriveAt: new Date(form.value.expectedArriveAt).toISOString(),
    partySize: form.value.partySize,
    serviceId: form.value.serviceId,
    preferredTechnicianId: form.value.preferredTechnicianId,
    remark: form.value.remark.trim() || null
  };
  try {
    if (editingId.value) {
      await appointmentsApi.update(editingId.value, body);
      showSuccessToast('已保存修改');
    } else {
      await appointmentsApi.create({ storeId: appStore.activeStoreId, ...body });
      showSuccessToast('已登记，待确认');
      tabIndex.value = 0;
    }
    showAdd.value = false;
    await load();
  } catch {
    /* 拦截器已提示 */
  } finally {
    submitting.value = false;
  }
}

onMounted(async () => {
  if (!appStore.stores.length) await appStore.loadStores().catch(() => undefined);
  load();
});
</script>

<template>
  <div class="qy-page appts">
    <van-nav-bar title="预约" left-text="返回" left-arrow @click-left="$router.back()">
      <template #right><span class="nav-add" @click="openAdd">新增</span></template>
    </van-nav-bar>

    <van-tabs v-model:active="tabIndex" @change="onTab" sticky>
      <van-tab v-for="t in tabs" :key="t.value" :title="t.label" />
    </van-tabs>

    <van-pull-refresh v-model="refreshing" @refresh="load">
      <van-empty v-if="!loading && list.length === 0" description="暂无预约" />

      <div v-for="a in list" :key="a.id" class="ap-item">
        <div class="ap-top">
          <span class="ap-name">{{ a.customerName }} <i class="ap-party" v-if="a.partySize > 1">{{ a.partySize }}人</i></span>
          <van-tag :type="statusType(a.status)">{{ statusLabel(a.status) }}</van-tag>
        </div>
        <div class="ap-row"><van-icon name="clock-o" /> {{ fmtTime(a.expectedArriveAt) }}</div>
        <div class="ap-row"><van-icon name="phone-o" /> {{ a.customerPhone }}</div>
        <div class="ap-row" v-if="a.serviceName || a.preferredTechnicianName">
          <van-icon name="comment-o" />
          {{ a.serviceName || '不限项目' }}<span v-if="a.preferredTechnicianName"> · 指定 {{ a.preferredTechnicianName }}</span>
        </div>
        <div class="ap-row" v-if="a.remark"><van-icon name="notes-o" /> {{ a.remark }}</div>

        <div class="ap-actions" v-if="a.status === 'Pending' || a.status === 'Confirmed'">
          <van-button v-if="a.status === 'Pending'" size="small" type="primary" :loading="acting" @click="confirm(a)">确认</van-button>
          <van-button v-if="a.status === 'Pending'" size="small" plain @click="openEdit(a)">修改</van-button>
          <van-button size="small" type="success" :loading="acting" @click="arrive(a)">到店</van-button>
          <van-button size="small" :loading="acting" @click="cancel(a)">取消</van-button>
        </div>
        <div class="ap-actions" v-else-if="a.status === 'Cancelled' || a.status === 'NoShow'">
          <van-button size="small" plain @click="openRebook(a)">再次预约</van-button>
        </div>
      </div>
    </van-pull-refresh>

    <!-- 新增预约 -->
    <van-popup v-model:show="showAdd" position="bottom" round :style="{ maxHeight: '90%' }">
      <div class="add-form">
        <div class="af-title">{{ formTitle() }}</div>
        <van-form @submit="submit">
          <van-cell-group inset>
            <van-field v-model="form.customerName" label="顾客姓名" placeholder="必填" required />
            <van-field v-model="form.customerPhone" label="联系电话" type="tel" placeholder="必填" required />
            <van-field label="到店时间" required>
              <template #input>
                <input v-model="form.expectedArriveAt" type="datetime-local" class="dt-input" />
              </template>
            </van-field>
            <van-field label="人数">
              <template #input><van-stepper v-model="form.partySize" :min="1" :max="50" /></template>
            </van-field>
            <van-field label="项目" :model-value="serviceName()" readonly is-link @click="showServicePicker = true" />
            <van-field label="指定技师" :model-value="techName()" readonly is-link @click="showTechPicker = true" />
            <van-field v-model="form.remark" label="备注" type="textarea" rows="2" autosize placeholder="选填" />
          </van-cell-group>
          <div class="af-submit">
            <van-button round block type="primary" native-type="submit" :loading="submitting">{{ editingId ? '保存修改' : '登记预约' }}</van-button>
          </div>
        </van-form>
      </div>
    </van-popup>

    <van-popup v-model:show="showServicePicker" position="bottom" round>
      <van-picker :columns="serviceColumns()" @confirm="onServiceConfirm" @cancel="showServicePicker = false" />
    </van-popup>
    <van-popup v-model:show="showTechPicker" position="bottom" round>
      <van-picker :columns="techColumns()" @confirm="onTechConfirm" @cancel="showTechPicker = false" />
    </van-popup>
  </div>
</template>

<style scoped>
.nav-add { color: var(--qy-brand); font-size: 15px; }
.ap-item { background: #fff; margin: 8px 12px; padding: 14px; border-radius: 12px; }
.ap-top { display: flex; align-items: center; justify-content: space-between; }
.ap-name { font-size: 16px; font-weight: 600; }
.ap-party { font-style: normal; color: #98a2b3; font-size: 13px; font-weight: 400; margin-left: 4px; }
.ap-row { display: flex; align-items: center; gap: 6px; margin-top: 8px; color: #4b5563; font-size: 14px; }
.ap-row .van-icon { color: #b0b8c4; }
.ap-actions { display: flex; gap: 8px; margin-top: 12px; justify-content: flex-end; }
.add-form { padding: 18px 0 24px; }
.af-title { text-align: center; font-size: 17px; font-weight: 700; margin-bottom: 12px; }
.af-submit { padding: 18px 16px 0; }
.dt-input {
  border: none; outline: none; font-size: 15px; width: 100%; background: transparent;
  color: #1f2733; font-family: inherit;
}
</style>
