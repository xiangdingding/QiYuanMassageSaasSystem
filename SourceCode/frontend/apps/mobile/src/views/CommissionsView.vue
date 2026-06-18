<script lang="ts">
export default { name: 'CommissionsView' };
</script>

<script setup lang="ts">
import { computed, onMounted, reactive, ref } from 'vue';
import {
  NavBar as VanNavBar, Tabs as VanTabs, Tab as VanTab, PullRefresh as VanPullRefresh,
  Empty as VanEmpty, Tag as VanTag, Button as VanButton, Popup as VanPopup, Form as VanForm,
  Field as VanField, Stepper as VanStepper, Switch as VanSwitch, Picker as VanPicker,
  Checkbox as VanCheckbox, Cell as VanCell, CellGroup as VanCellGroup, NoticeBar as VanNoticeBar,
  showSuccessToast, showToast, showConfirmDialog
} from 'vant';
import { commissionsApi, referralSettingsApi, servicesApi, staffApi, type ReferralSetting } from '@/api/modules';
import type { CommissionRule, ServiceItem, Staff } from '@/api/types';

const tabIndex = ref(0);
const rows = ref<CommissionRule[]>([]);
const services = ref<ServiceItem[]>([]);
const technicians = ref<Staff[]>([]);
const loading = ref(false);
const refreshing = ref(false);
const saving = ref(false);

const filterServiceId = ref<number | null>(null);
const filterTechnicianId = ref<number | null>(null);
const showSvcFilterPicker = ref(false);
const showTechFilterPicker = ref(false);
const svcFilterCols = computed(() => [{ text: '全部服务', value: 0 }, ...services.value.map((s) => ({ text: `${s.code} ${s.name}`, value: s.id }))]);
const techFilterCols = computed(() => [{ text: '全部技师', value: 0 }, ...technicians.value.map((t) => ({ text: `${t.employeeNo ?? ''} ${t.realName || t.username}`, value: t.id }))]);
function svcName(id?: number | null) { return id ? (services.value.find((s) => s.id === id)?.name ?? `#${id}`) : '全部服务'; }
function techName(id?: number | null) { const t = technicians.value.find((x) => x.id === id); return t ? (t.realName || t.username) : '全部技师'; }

function ruleLabel(t: string) { return ({ FixedAmount: '固定金额', Percentage: '百分比', Tiered: '阶梯', Timed: '按时计费' } as Record<string, string>)[t] ?? t; }
function fmtPart(row: CommissionRule, v: number) { return row.ruleType === 'Percentage' ? `${v}%` : `¥${v.toFixed(2)}`; }
function isDual(row: CommissionRule) { return row.ruleType === 'FixedAmount' || row.ruleType === 'Percentage'; }
function dualAmt(row: CommissionRule, kind: 'r' | 'd') { return (kind === 'r' ? row.rotationAmount : row.designationAmount) ?? row.amount; }
function fmtAmount(row: CommissionRule) {
  if (row.ruleType === 'Percentage') return `${row.amount}%`;
  if (row.ruleType === 'Timed') return `¥${row.amount}/时`;
  return `¥${row.amount.toFixed(2)}`;
}

interface TierRow { fromQty: number; amount: number }
function parseTiers(json?: string | null): TierRow[] {
  if (!json) return [{ fromQty: 0, amount: 0 }];
  try {
    const raw = JSON.parse(json);
    if (!Array.isArray(raw) || !raw.length) return [{ fromQty: 0, amount: 0 }];
    return raw.map((r: Record<string, unknown>) => ({ fromQty: Number(r.FromQty ?? r.fromQty ?? 0), amount: Number(r.Amount ?? r.amount ?? 0) }));
  } catch { return [{ fromQty: 0, amount: 0 }]; }
}
function serializeTiers(tiers: TierRow[]): string {
  return JSON.stringify([...tiers].filter((t) => Number.isFinite(t.fromQty)).sort((a, b) => a.fromQty - b.fromQty).map((t) => ({ FromQty: Math.round(t.fromQty), Amount: t.amount })));
}

async function reload() {
  loading.value = true;
  try { rows.value = await commissionsApi.list(filterServiceId.value ?? undefined, filterTechnicianId.value ?? undefined); }
  catch { /* */ } finally { loading.value = false; refreshing.value = false; }
}
async function loadOptions() {
  const [s, t] = await Promise.all([servicesApi.list(true), staffApi.list({ role: 'Technician', pageSize: 200 })]);
  services.value = s; technicians.value = t.items;
}
function onSvcFilter({ selectedValues }: { selectedValues: number[] }) { filterServiceId.value = selectedValues[0] || null; showSvcFilterPicker.value = false; reload(); }
function onTechFilter({ selectedValues }: { selectedValues: number[] }) { filterTechnicianId.value = selectedValues[0] || null; showTechFilterPicker.value = false; reload(); }

// 新建/编辑
const formOpen = ref(false);
const formMode = ref<'create' | 'edit'>('create');
const editingId = ref<number | null>(null);
const showFormSvcPicker = ref(false);
const showFormTechPicker = ref(false);
const form = reactive({
  serviceId: null as number | null, technicianId: null as number | null,
  ruleType: 'FixedAmount' as CommissionRule['ruleType'],
  amount: 0, tiers: [{ fromQty: 0, amount: 0 }] as TierRow[], priority: 0, isActive: true,
  rotationFixed: 0, designationFixed: 0, rotationPercent: 0, designationPercent: 0
});
const supportsDual = computed(() => form.ruleType === 'FixedAmount' || form.ruleType === 'Percentage');
const RULE_TYPES: { v: CommissionRule['ruleType']; label: string }[] = [
  { v: 'FixedAmount', label: '固定' }, { v: 'Percentage', label: '百分比' }, { v: 'Tiered', label: '阶梯' }, { v: 'Timed', label: '按时' }
];
const formSvcCols = computed(() => [{ text: '全部服务', value: 0 }, ...services.value.map((s) => ({ text: `${s.code} ${s.name}`, value: s.id }))]);
const formTechCols = computed(() => [{ text: '全部技师', value: 0 }, ...technicians.value.map((t) => ({ text: `${t.employeeNo ?? ''} ${t.realName || t.username}`, value: t.id }))]);

function openCreate() {
  formMode.value = 'create';
  editingId.value = null;
  Object.assign(form, { serviceId: null, technicianId: null, ruleType: 'FixedAmount', amount: 0, tiers: [{ fromQty: 0, amount: 0 }], priority: 0, isActive: true, rotationFixed: 0, designationFixed: 0, rotationPercent: 0, designationPercent: 0 });
  formOpen.value = true;
}
function openEdit(row: CommissionRule) {
  formMode.value = 'edit';
  editingId.value = row.id;
  const isFixed = row.ruleType === 'FixedAmount', isPct = row.ruleType === 'Percentage';
  Object.assign(form, {
    serviceId: row.serviceId ?? null, technicianId: row.technicianId ?? null, ruleType: row.ruleType,
    amount: row.amount, tiers: parseTiers(row.tieredRulesJson), priority: row.priority, isActive: row.isActive,
    rotationFixed: isFixed ? (row.rotationAmount ?? 0) : 0, designationFixed: isFixed ? (row.designationAmount ?? 0) : 0,
    rotationPercent: isPct ? (row.rotationAmount ?? 0) : 0, designationPercent: isPct ? (row.designationAmount ?? 0) : 0
  });
  formOpen.value = true;
}
function onFormSvc({ selectedValues }: { selectedValues: number[] }) { form.serviceId = selectedValues[0] || null; showFormSvcPicker.value = false; }
function onFormTech({ selectedValues }: { selectedValues: number[] }) { form.technicianId = selectedValues[0] || null; showFormTechPicker.value = false; }
function addTier() { const last = form.tiers[form.tiers.length - 1]; form.tiers.push({ fromQty: last ? last.fromQty + 10 : 0, amount: last?.amount ?? 0 }); }
function removeTier(i: number) { form.tiers.splice(i, 1); }

async function save() {
  const dual = supportsDual.value;
  const rotation = form.ruleType === 'FixedAmount' ? form.rotationFixed : form.rotationPercent;
  const designation = form.ruleType === 'FixedAmount' ? form.designationFixed : form.designationPercent;
  saving.value = true;
  try {
    const body = {
      serviceId: form.serviceId, technicianId: form.technicianId, ruleType: form.ruleType,
      amount: dual ? Math.min(rotation, designation) : form.amount,
      tieredRulesJson: form.ruleType === 'Tiered' ? serializeTiers(form.tiers) : null,
      priority: form.priority, isActive: form.isActive, assignmentSource: null,
      rotationAmount: dual ? rotation : null, designationAmount: dual ? designation : null
    };
    if (formMode.value === 'create') await commissionsApi.create(body);
    else if (editingId.value != null) { const { serviceId: _s, technicianId: _t, ...rest } = body; void _s; void _t; await commissionsApi.update(editingId.value, rest); }
    showSuccessToast('已保存');
    formOpen.value = false;
    reload();
  } catch { /* */ } finally { saving.value = false; }
}
async function toggleActive(row: CommissionRule) {
  try { await commissionsApi.bulkStatus([row.id], !row.isActive); showSuccessToast(row.isActive ? '已停用' : '已启用'); reload(); } catch { /* */ }
}
async function remove(row: CommissionRule) {
  if (row.isActive) { showToast('请先停用该规则再删除'); return; }
  try { await showConfirmDialog({ title: '删除规则', message: '确认删除该提成规则？' }); } catch { return; }
  try { await commissionsApi.remove(row.id); showSuccessToast('已删除'); reload(); } catch { /* */ }
}

// 批量设置
const bulkOpen = ref(false);
const bulkSaving = ref(false);
const bulk = reactive({
  serviceIds: [] as number[], technicianIds: [] as number[],
  ruleType: 'FixedAmount' as CommissionRule['ruleType'], amount: 0,
  rotationFixed: 0, designationFixed: 0, rotationPercent: 0, designationPercent: 0,
  tiers: [{ fromQty: 0, amount: 0 }] as TierRow[], priority: 0, overwriteExisting: false
});
const bulkDual = computed(() => bulk.ruleType === 'FixedAmount' || bulk.ruleType === 'Percentage');
const bulkPairs = computed(() => bulk.serviceIds.length * bulk.technicianIds.length);
function openBulk() {
  Object.assign(bulk, { serviceIds: [], technicianIds: [], ruleType: 'FixedAmount', amount: 0, rotationFixed: 0, designationFixed: 0, rotationPercent: 0, designationPercent: 0, tiers: [{ fromQty: 0, amount: 0 }], priority: 0, overwriteExisting: false });
  bulkOpen.value = true;
}
function toggleBulkId(arr: number[], id: number) { const i = arr.indexOf(id); i === -1 ? arr.push(id) : arr.splice(i, 1); }
async function submitBulk() {
  if (bulkPairs.value === 0) { showToast('请选择服务与技师'); return; }
  const dual = bulkDual.value;
  const rotation = bulk.ruleType === 'FixedAmount' ? bulk.rotationFixed : bulk.rotationPercent;
  const designation = bulk.ruleType === 'FixedAmount' ? bulk.designationFixed : bulk.designationPercent;
  bulkSaving.value = true;
  try {
    const res = await commissionsApi.bulk({
      serviceIds: bulk.serviceIds, technicianIds: bulk.technicianIds, ruleType: bulk.ruleType,
      amount: dual ? Math.min(rotation, designation) : bulk.amount,
      tieredRulesJson: bulk.ruleType === 'Tiered' ? serializeTiers(bulk.tiers) : null,
      priority: bulk.priority, isActive: true,
      rotationAmount: dual ? rotation : null, designationAmount: dual ? designation : null,
      overwriteExisting: bulk.overwriteExisting
    });
    showSuccessToast(`新增 ${res.created}，覆盖 ${res.updated}，跳过 ${res.skipped}`);
    bulkOpen.value = false;
    reload();
  } catch { /* */ } finally { bulkSaving.value = false; }
}

// 推荐规则
const referralSaving = ref(false);
const referral = reactive<ReferralSetting>({ customerReferralMode: 'None', customerRewardPercent: 0, customerFixedReward: 0, staffReferralMode: 'None', staffReferralFixedAmount: 0, staffReferralPercent: 0 });
async function loadReferral() { try { Object.assign(referral, await referralSettingsApi.get()); } catch { /* */ } }
async function saveReferral() {
  referralSaving.value = true;
  try { Object.assign(referral, await referralSettingsApi.update({ ...referral })); showSuccessToast('推荐规则已保存'); }
  catch { /* */ } finally { referralSaving.value = false; }
}
const custModes = [{ v: 'None', label: '关闭' }, { v: 'PercentPerRecharge', label: '充值返佣%' }, { v: 'FixedPerCard', label: '固定/张' }];
const staffModes = [{ v: 'None', label: '关闭' }, { v: 'FixedPerCard', label: '固定/张' }, { v: 'PercentOfOpenCard', label: '开卡实收%' }];

onMounted(async () => { await loadOptions(); reload(); loadReferral(); });
</script>

<template>
  <div class="qy-page commissions">
    <van-nav-bar title="提成规则" left-text="返回" left-arrow @click-left="$router.back()">
      <template #right><span v-if="tabIndex === 0" class="nav-add" @click="openCreate">新建</span></template>
    </van-nav-bar>

    <van-tabs v-model:active="tabIndex" sticky>
      <van-tab title="服务提成" />
      <van-tab title="推荐开卡提成" />
    </van-tabs>

    <!-- 服务提成 -->
    <template v-if="tabIndex === 0">
      <div class="filters">
        <span class="f" @click="showSvcFilterPicker = true">服务：{{ svcName(filterServiceId) }} ▾</span>
        <span class="f" @click="showTechFilterPicker = true">技师：{{ techName(filterTechnicianId) }} ▾</span>
        <van-button size="mini" type="warning" plain @click="openBulk">批量设置</van-button>
      </div>
      <van-pull-refresh v-model="refreshing" @refresh="reload">
        <van-empty v-if="!loading && rows.length === 0" description="暂无提成规则" />
        <div v-for="r in rows" :key="r.id" class="cr">
          <div class="cr-head">
            <span class="cr-type">{{ ruleLabel(r.ruleType) }}</span>
            <van-tag :type="r.isActive ? 'success' : 'danger'">{{ r.isActive ? '启用' : '停用' }}</van-tag>
          </div>
          <div class="cr-scope">{{ r.serviceName ?? '全部服务' }} · {{ r.technicianName ?? '全部技师' }} · 优先级 {{ r.priority }}</div>
          <div class="cr-val">
            <template v-if="isDual(r)">轮钟 {{ fmtPart(r, dualAmt(r, 'r')) }} · 点钟 {{ fmtPart(r, dualAmt(r, 'd')) }}</template>
            <template v-else>{{ fmtAmount(r) }}</template>
          </div>
          <div class="cr-actions">
            <van-button size="mini" @click="openEdit(r)">编辑</van-button>
            <van-button size="mini" :type="r.isActive ? 'warning' : 'success'" plain @click="toggleActive(r)">{{ r.isActive ? '停用' : '启用' }}</van-button>
            <van-button size="mini" type="danger" plain :disabled="r.isActive" @click="remove(r)">删除</van-button>
          </div>
        </div>
      </van-pull-refresh>
    </template>

    <!-- 推荐开卡提成 -->
    <template v-else>
      <van-cell-group inset title="顾客推荐（返到推荐顾客余额）">
        <van-field label="返佣方式">
          <template #input>
            <div class="seg wrap"><button v-for="m in custModes" :key="m.v" type="button" :class="{ on: referral.customerReferralMode === m.v }" @click="referral.customerReferralMode = m.v">{{ m.label }}</button></div>
          </template>
        </van-field>
        <van-field v-if="referral.customerReferralMode === 'PercentPerRecharge'" label="充值返佣%"><template #input><van-stepper v-model="referral.customerRewardPercent" :min="0" :max="100" :decimal-length="2" /></template></van-field>
        <van-field v-if="referral.customerReferralMode === 'FixedPerCard'" label="固定推荐费/张"><template #input><van-stepper v-model="referral.customerFixedReward" :min="0" :step="10" :decimal-length="2" /></template></van-field>
      </van-cell-group>
      <van-cell-group inset title="员工推荐（并入该员工当月工资）">
        <van-field label="提成模式">
          <template #input>
            <div class="seg wrap"><button v-for="m in staffModes" :key="m.v" type="button" :class="{ on: referral.staffReferralMode === m.v }" @click="referral.staffReferralMode = m.v">{{ m.label }}</button></div>
          </template>
        </van-field>
        <van-field v-if="referral.staffReferralMode === 'FixedPerCard'" label="固定提成/张"><template #input><van-stepper v-model="referral.staffReferralFixedAmount" :min="0" :step="10" :decimal-length="2" /></template></van-field>
        <van-field v-if="referral.staffReferralMode === 'PercentOfOpenCard'" label="开卡实收%"><template #input><van-stepper v-model="referral.staffReferralPercent" :min="0" :max="100" :decimal-length="2" /></template></van-field>
      </van-cell-group>
      <div class="submit"><van-button block type="primary" :loading="referralSaving" @click="saveReferral">保存推荐规则</van-button></div>
    </template>

    <!-- 过滤 picker -->
    <van-popup v-model:show="showSvcFilterPicker" position="bottom" round><van-picker :columns="svcFilterCols" @confirm="onSvcFilter" @cancel="showSvcFilterPicker = false" /></van-popup>
    <van-popup v-model:show="showTechFilterPicker" position="bottom" round><van-picker :columns="techFilterCols" @confirm="onTechFilter" @cancel="showTechFilterPicker = false" /></van-popup>

    <!-- 新建/编辑 -->
    <van-popup v-model:show="formOpen" position="bottom" round :style="{ maxHeight: '92%' }">
      <div class="sheet">
        <div class="sheet-title">{{ formMode === 'create' ? '新建提成规则' : '编辑提成规则' }}</div>
        <van-cell-group inset>
          <van-field label="适用服务" :model-value="svcName(form.serviceId)" readonly is-link @click="showFormSvcPicker = true" />
          <van-field label="适用技师" :model-value="techName(form.technicianId)" readonly is-link @click="showFormTechPicker = true" />
          <van-field label="规则类型">
            <template #input>
              <div class="seg wrap"><button v-for="rt in RULE_TYPES" :key="rt.v" type="button" :class="{ on: form.ruleType === rt.v }" @click="form.ruleType = rt.v">{{ rt.label }}</button></div>
            </template>
          </van-field>
          <template v-if="form.ruleType === 'FixedAmount'">
            <van-field label="轮钟金额"><template #input><van-stepper v-model="form.rotationFixed" :min="0" :step="5" :decimal-length="2" /></template></van-field>
            <van-field label="点钟金额"><template #input><van-stepper v-model="form.designationFixed" :min="0" :step="5" :decimal-length="2" /></template></van-field>
          </template>
          <template v-else-if="form.ruleType === 'Percentage'">
            <van-field label="轮钟比例%"><template #input><van-stepper v-model="form.rotationPercent" :min="0" :max="100" :decimal-length="2" /></template></van-field>
            <van-field label="点钟比例%"><template #input><van-stepper v-model="form.designationPercent" :min="0" :max="100" :decimal-length="2" /></template></van-field>
          </template>
          <van-field v-else :label="form.ruleType === 'Timed' ? '元/小时' : '默认数值'"><template #input><van-stepper v-model="form.amount" :min="0" :step="5" :decimal-length="2" /></template></van-field>
          <van-field label="优先级"><template #input><van-stepper v-model="form.priority" :min="0" :max="100" /></template></van-field>
          <van-field label="启用"><template #input><van-switch v-model="form.isActive" /></template></van-field>
        </van-cell-group>

        <div v-if="form.ruleType === 'Tiered'" class="tiers">
          <div class="t-h">阶梯档位（从第几单起 → 提成元）</div>
          <div v-for="(t, i) in form.tiers" :key="i" class="t-row">
            <van-stepper v-model="t.fromQty" :min="0" />
            <span class="t-arrow">→</span>
            <van-stepper v-model="t.amount" :min="0" :step="5" :decimal-length="2" />
            <van-button size="mini" type="danger" plain :disabled="form.tiers.length <= 1" @click="removeTier(i)">删</van-button>
          </div>
          <van-button size="small" plain @click="addTier">+ 添加档位</van-button>
        </div>

        <div class="sheet-actions"><van-button block type="primary" :loading="saving" @click="save">保存</van-button></div>
      </div>
    </van-popup>
    <van-popup v-model:show="showFormSvcPicker" position="bottom" round><van-picker :columns="formSvcCols" @confirm="onFormSvc" @cancel="showFormSvcPicker = false" /></van-popup>
    <van-popup v-model:show="showFormTechPicker" position="bottom" round><van-picker :columns="formTechCols" @confirm="onFormTech" @cancel="showFormTechPicker = false" /></van-popup>

    <!-- 批量设置 -->
    <van-popup v-model:show="bulkOpen" position="bottom" round :style="{ maxHeight: '94%' }">
      <div class="sheet">
        <div class="sheet-title">批量设置（{{ bulkPairs }} 个组合）</div>
        <div class="ms">
          <div class="ms-h">服务（{{ bulk.serviceIds.length }}）<span class="ms-all" @click="bulk.serviceIds = services.map(s => s.id)">全选</span><span class="ms-all" @click="bulk.serviceIds = []">清空</span></div>
          <div class="ms-list">
            <label v-for="s in services" :key="s.id" class="ms-item">
              <van-checkbox :model-value="bulk.serviceIds.includes(s.id)" @update:model-value="() => toggleBulkId(bulk.serviceIds, s.id)" />
              <span>{{ s.code }} {{ s.name }}</span>
            </label>
          </div>
          <div class="ms-h">技师（{{ bulk.technicianIds.length }}）<span class="ms-all" @click="bulk.technicianIds = technicians.map(t => t.id)">全选</span><span class="ms-all" @click="bulk.technicianIds = []">清空</span></div>
          <div class="ms-list">
            <label v-for="t in technicians" :key="t.id" class="ms-item">
              <van-checkbox :model-value="bulk.technicianIds.includes(t.id)" @update:model-value="() => toggleBulkId(bulk.technicianIds, t.id)" />
              <span>{{ t.employeeNo ?? '' }} {{ t.realName || t.username }}</span>
            </label>
          </div>
        </div>
        <van-cell-group inset>
          <van-field label="规则类型">
            <template #input>
              <div class="seg wrap"><button v-for="rt in RULE_TYPES" :key="rt.v" type="button" :class="{ on: bulk.ruleType === rt.v }" @click="bulk.ruleType = rt.v">{{ rt.label }}</button></div>
            </template>
          </van-field>
          <template v-if="bulk.ruleType === 'FixedAmount'">
            <van-field label="轮钟金额"><template #input><van-stepper v-model="bulk.rotationFixed" :min="0" :step="5" :decimal-length="2" /></template></van-field>
            <van-field label="点钟金额"><template #input><van-stepper v-model="bulk.designationFixed" :min="0" :step="5" :decimal-length="2" /></template></van-field>
          </template>
          <template v-else-if="bulk.ruleType === 'Percentage'">
            <van-field label="轮钟比例%"><template #input><van-stepper v-model="bulk.rotationPercent" :min="0" :max="100" :decimal-length="2" /></template></van-field>
            <van-field label="点钟比例%"><template #input><van-stepper v-model="bulk.designationPercent" :min="0" :max="100" :decimal-length="2" /></template></van-field>
          </template>
          <van-field v-else :label="bulk.ruleType === 'Timed' ? '元/小时' : '默认数值'"><template #input><van-stepper v-model="bulk.amount" :min="0" :step="5" :decimal-length="2" /></template></van-field>
          <van-field label="优先级"><template #input><van-stepper v-model="bulk.priority" :min="0" :max="100" /></template></van-field>
          <van-cell title="覆盖已有通用规则"><template #right-icon><van-switch v-model="bulk.overwriteExisting" /></template></van-cell>
        </van-cell-group>
        <div class="sheet-actions"><van-button block type="primary" :loading="bulkSaving" :disabled="bulkPairs === 0" @click="submitBulk">应用到 {{ bulkPairs }} 个组合</van-button></div>
      </div>
    </van-popup>
  </div>
</template>

<style scoped>
.nav-add { color: var(--qy-brand); font-size: 15px; }
.filters { display: flex; align-items: center; gap: 12px; padding: 10px 14px; flex-wrap: wrap; }
.filters .f { color: var(--qy-brand); font-size: 14px; }
.cr { background: #fff; margin: 8px 12px; padding: 14px; border-radius: 12px; }
.cr-head { display: flex; align-items: center; justify-content: space-between; }
.cr-type { font-size: 15px; font-weight: 600; }
.cr-scope { margin-top: 6px; color: #6b7280; font-size: 13px; }
.cr-val { margin-top: 4px; color: var(--qy-brand); font-weight: 600; font-size: 14px; }
.cr-actions { display: flex; gap: 8px; margin-top: 10px; }
.submit { padding: 16px; }
.sheet { padding: 16px 0 24px; }
.sheet-title { text-align: center; font-size: 17px; font-weight: 700; margin-bottom: 12px; }
.sheet-actions { padding: 16px 16px 0; }
.seg { display: flex; gap: 8px; width: 100%; }
.seg.wrap { flex-wrap: wrap; }
.seg button { flex: 1; min-width: 60px; border: 1px solid #d6dbe2; background: #fff; color: #4b5563; border-radius: 8px; padding: 6px 4px; font-size: 13px; }
.seg.wrap button { flex: 0 0 calc(25% - 6px); }
.seg button.on { background: var(--qy-brand); color: #fff; border-color: var(--qy-brand); }
.tiers { margin: 12px 16px; }
.t-h { font-weight: 600; margin-bottom: 8px; font-size: 14px; }
.t-row { display: flex; align-items: center; gap: 8px; margin-bottom: 8px; }
.t-arrow { color: #98a2b3; }
.ms { margin: 0 16px 12px; }
.ms-h { font-weight: 600; font-size: 14px; margin: 10px 0 6px; }
.ms-all { color: var(--qy-brand); font-size: 13px; font-weight: 400; margin-left: 10px; }
.ms-list { max-height: 140px; overflow-y: auto; border: 1px solid #eef1f4; border-radius: 8px; padding: 8px; }
.ms-item { display: flex; align-items: center; gap: 8px; padding: 5px 0; font-size: 13px; }
</style>
