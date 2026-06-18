<script lang="ts">
export default { name: 'MemberTypesView' };
</script>

<script setup lang="ts">
import { onMounted, reactive, ref } from 'vue';
import {
  NavBar as VanNavBar, Tabs as VanTabs, Tab as VanTab, PullRefresh as VanPullRefresh,
  Empty as VanEmpty, Tag as VanTag, Button as VanButton, Popup as VanPopup,
  Form as VanForm, Field as VanField, Stepper as VanStepper, Switch as VanSwitch,
  Picker as VanPicker, CellGroup as VanCellGroup,
  showSuccessToast, showToast, showConfirmDialog
} from 'vant';
import { memberTypesApi, servicesApi, type MemberType, type MemberTypeKind } from '@/api/modules';
import type { ServiceItem } from '@/api/types';

const rows = ref<MemberType[]>([]);
const services = ref<ServiceItem[]>([]);
const loading = ref(false);
const refreshing = ref(false);
const saving = ref(false);
const tabIndex = ref(0);
const kinds: { label: string; value: '' | MemberTypeKind }[] = [
  { label: '全部', value: '' },
  { label: '充值卡', value: 'StoredValue' },
  { label: '计次卡', value: 'CountBased' }
];

const formOpen = ref(false);
const formMode = ref<'create' | 'edit'>('create');
const editingId = ref<number | null>(null);
const showServicePicker = ref(false);
const form = reactive({
  code: '', name: '', sort: 0, kind: 'StoredValue' as MemberTypeKind,
  serviceItemId: null as number | null,
  minRechargeAmount: 1000, minPurchaseCount: 10,
  discount: 1, bonusAmount: 0, bonusCount: 0,
  validDays: 365, isActive: true, remark: ''
});

function fmt(n?: number | null) { return (n ?? 0).toFixed(2); }
const serviceColumns = () => services.value.map((s) => ({ text: `${s.name}（${s.durationMinutes}分·¥${s.price.toFixed(0)}）`, value: s.id }));
const serviceName = () => services.value.find((s) => s.id === form.serviceItemId)?.name || '选择服务项目';

async function reload() {
  loading.value = true;
  try { rows.value = await memberTypesApi.list(true, kinds[tabIndex.value].value || undefined); }
  catch { /* */ } finally { loading.value = false; refreshing.value = false; }
}

function openCreate() {
  formMode.value = 'create';
  editingId.value = null;
  const maxSort = rows.value.reduce((m, r) => Math.max(m, r.sort ?? 0), 0);
  Object.assign(form, {
    code: '', name: '', sort: maxSort + 1, kind: 'StoredValue', serviceItemId: null,
    minRechargeAmount: 1000, minPurchaseCount: 10, discount: 1, bonusAmount: 0, bonusCount: 0,
    validDays: 365, isActive: true, remark: ''
  });
  formOpen.value = true;
}
function openEdit(row: MemberType) {
  formMode.value = 'edit';
  editingId.value = row.id;
  Object.assign(form, {
    code: row.code, name: row.name, sort: row.sort, kind: row.kind,
    serviceItemId: row.serviceItemId ?? null, minRechargeAmount: row.minRechargeAmount ?? 0,
    minPurchaseCount: row.minPurchaseCount ?? 1, discount: row.discount,
    bonusAmount: row.bonusAmount ?? 0, bonusCount: row.bonusCount ?? 0,
    validDays: row.validDays ?? 0, isActive: row.isActive, remark: row.remark ?? ''
  });
  formOpen.value = true;
}
function onServiceConfirm({ selectedValues }: { selectedValues: number[] }) {
  form.serviceItemId = selectedValues[0] ?? null;
  showServicePicker.value = false;
}

async function save() {
  if (!form.code.trim()) { showToast('请输入编码'); return; }
  if (!form.name.trim()) { showToast('请输入名称'); return; }
  const isCount = form.kind === 'CountBased';
  if (isCount) {
    if (!form.serviceItemId) { showToast('计次卡必须选择服务项目'); return; }
    if (!form.minPurchaseCount || form.minPurchaseCount <= 0) { showToast('请设置最低购买次数'); return; }
  } else if (!form.minRechargeAmount || form.minRechargeAmount <= 0) {
    showToast('请设置最低充值金额'); return;
  }
  saving.value = true;
  try {
    const payload = {
      code: form.code.trim(), name: form.name.trim(), sort: form.sort, kind: form.kind,
      serviceItemId: isCount ? form.serviceItemId : null,
      minRechargeAmount: isCount ? null : form.minRechargeAmount,
      minPurchaseCount: isCount ? form.minPurchaseCount : null,
      discount: form.discount,
      bonusAmount: isCount ? null : (form.bonusAmount || 0),
      bonusCount: isCount ? (form.bonusCount || 0) : null,
      validDays: form.validDays && form.validDays > 0 ? form.validDays : null,
      isActive: form.isActive, remark: form.remark || null
    };
    if (formMode.value === 'create') await memberTypesApi.create(payload as Partial<MemberType> & { kind: MemberTypeKind });
    else if (editingId.value != null) await memberTypesApi.update(editingId.value, payload);
    showSuccessToast('已保存');
    formOpen.value = false;
    reload();
  } catch { /* */ } finally { saving.value = false; }
}

async function remove(row: MemberType) {
  try { await showConfirmDialog({ title: '删除会员类型', message: `确认删除「${row.name}」？已开出的卡不受影响，但此后不能再用该类型开卡。` }); }
  catch { return; }
  try { await memberTypesApi.remove(row.id); showSuccessToast('已删除'); reload(); } catch { /* */ }
}

onMounted(async () => {
  services.value = await servicesApi.list(false).catch(() => []);
  reload();
});
</script>

<template>
  <div class="qy-page mtypes">
    <van-nav-bar title="会员类型" left-text="返回" left-arrow @click-left="$router.back()">
      <template #right><span class="nav-add" @click="openCreate">新增</span></template>
    </van-nav-bar>

    <van-tabs v-model:active="tabIndex" @change="reload" sticky>
      <van-tab v-for="k in kinds" :key="k.value" :title="k.label" />
    </van-tabs>

    <van-pull-refresh v-model="refreshing" @refresh="reload">
      <van-empty v-if="!loading && rows.length === 0" description="暂无会员类型" />
      <div v-for="t in rows" :key="t.id" class="mt-item">
        <div class="mt-main">
          <div class="mt-name">
            <span class="mt-code">{{ t.code }}</span>{{ t.name }}
            <van-tag :type="t.kind === 'StoredValue' ? 'warning' : 'success'">{{ t.kind === 'StoredValue' ? '充值卡' : '计次卡' }}</van-tag>
            <van-tag v-if="!t.isActive" type="default">停用</van-tag>
          </div>
          <div class="mt-sub">
            <template v-if="t.kind === 'StoredValue'">最低 ¥{{ fmt(t.minRechargeAmount) }}<span v-if="(t.bonusAmount ?? 0) > 0"> · 送 ¥{{ fmt(t.bonusAmount) }}</span></template>
            <template v-else>{{ t.serviceItemName || '—' }} · ≥{{ t.minPurchaseCount ?? '—' }}次<span v-if="(t.bonusCount ?? 0) > 0"> · 送 {{ t.bonusCount }}次</span></template>
            <span v-if="t.discount < 1"> · {{ (t.discount * 10).toFixed(1) }}折</span>
            <span> · {{ t.validDays ? t.validDays + '天' : '永久' }}</span>
          </div>
        </div>
        <div class="mt-actions">
          <van-button size="mini" @click="openEdit(t)">编辑</van-button>
          <van-button size="mini" type="danger" plain @click="remove(t)">删除</van-button>
        </div>
      </div>
    </van-pull-refresh>

    <van-popup v-model:show="formOpen" position="bottom" round :style="{ maxHeight: '92%' }">
      <div class="sheet">
        <div class="sheet-title">{{ formMode === 'create' ? '新增会员类型' : '编辑会员类型' }}</div>
        <van-form @submit="save">
          <van-cell-group inset>
            <van-field label="类型">
              <template #input>
                <div class="seg">
                  <button type="button" :class="{ on: form.kind === 'StoredValue' }" :disabled="formMode === 'edit'" @click="form.kind = 'StoredValue'">充值卡</button>
                  <button type="button" :class="{ on: form.kind === 'CountBased' }" :disabled="formMode === 'edit'" @click="form.kind = 'CountBased'">计次卡</button>
                </div>
              </template>
            </van-field>
            <van-field v-model="form.code" label="编码" placeholder="租户内唯一" />
            <van-field v-model="form.name" label="名称" placeholder="如 黄金卡 / 100次足疗卡" />

            <template v-if="form.kind === 'CountBased'">
              <van-field label="绑定服务" :model-value="serviceName()" readonly is-link @click="showServicePicker = true" />
              <van-field label="最低次数"><template #input><van-stepper v-model="form.minPurchaseCount" :min="1" :max="9999" /></template></van-field>
              <van-field label="赠送次数"><template #input><van-stepper v-model="form.bonusCount" :min="0" :max="9999" /></template></van-field>
            </template>
            <template v-else>
              <van-field label="最低充值"><template #input><van-stepper v-model="form.minRechargeAmount" :min="1" :step="100" :decimal-length="2" /></template></van-field>
              <van-field label="赠送金额"><template #input><van-stepper v-model="form.bonusAmount" :min="0" :step="50" :decimal-length="2" /></template></van-field>
            </template>

            <van-field label="折扣(0.85=8.5折)"><template #input><van-stepper v-model="form.discount" :min="0.1" :max="1" :step="0.05" :decimal-length="2" /></template></van-field>
            <van-field label="有效天数(0=永久)"><template #input><van-stepper v-model="form.validDays" :min="0" :max="3650" /></template></van-field>
            <van-field label="排序"><template #input><van-stepper v-model="form.sort" :min="0" :max="999" /></template></van-field>
            <van-field label="启用"><template #input><van-switch v-model="form.isActive" /></template></van-field>
            <van-field v-model="form.remark" label="备注" type="textarea" rows="1" autosize placeholder="选填" />
          </van-cell-group>
          <div class="sheet-actions">
            <van-button block type="primary" native-type="submit" :loading="saving">保存</van-button>
          </div>
        </van-form>
      </div>
    </van-popup>

    <van-popup v-model:show="showServicePicker" position="bottom" round>
      <van-picker :columns="serviceColumns()" @confirm="onServiceConfirm" @cancel="showServicePicker = false" />
    </van-popup>
  </div>
</template>

<style scoped>
.nav-add { color: var(--qy-brand); font-size: 15px; }
.mt-item { background: #fff; margin: 8px 12px; padding: 14px; border-radius: 12px; }
.mt-name { font-size: 15px; font-weight: 600; display: flex; align-items: center; gap: 6px; flex-wrap: wrap; }
.mt-code { color: #b0b8c4; font-weight: 400; font-size: 13px; margin-right: 4px; }
.mt-sub { margin-top: 6px; color: #6b7280; font-size: 13px; }
.mt-actions { display: flex; gap: 8px; margin-top: 12px; justify-content: flex-end; }
.sheet { padding: 16px 0 24px; }
.sheet-title { text-align: center; font-size: 17px; font-weight: 700; margin-bottom: 12px; }
.sheet-actions { padding: 16px 16px 0; }
.seg { display: flex; gap: 8px; width: 100%; }
.seg button { flex: 1; border: 1px solid #d6dbe2; background: #fff; color: #4b5563; border-radius: 8px; padding: 6px 0; font-size: 14px; }
.seg button.on { background: var(--qy-brand); color: #fff; border-color: var(--qy-brand); }
.seg button:disabled { opacity: .5; }
</style>
