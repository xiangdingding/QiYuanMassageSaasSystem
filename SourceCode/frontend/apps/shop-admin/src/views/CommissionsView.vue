<template>
  <div class="page">
    <el-card shadow="never">
      <div class="toolbar">
        <el-select v-model="filterServiceId" placeholder="按服务过滤" clearable filterable style="width: 300px">
          <el-option v-for="s in services" :key="s.id" :label="`${s.code} ${s.name}`" :value="s.id" />
        </el-select>
        <el-select v-model="filterTechnicianId" placeholder="按技师过滤" clearable filterable style="width: 300px">
          <el-option v-for="t in technicians" :key="t.id" :label="`${t.employeeNo ?? ''} · ${t.realName ?? t.username}`" :value="t.id" />
        </el-select>
        <el-button type="primary" @click="reload">查询</el-button>
        <el-button type="success" @click="openCreate">新建规则</el-button>
        <el-button type="warning" @click="openBulk">批量设置</el-button>
      </div>

      <div class="table-wrap">
      <el-table :data="rows" v-loading="loading" stripe height="100%">
        <el-table-column label="规则类型" width="100">
          <template #default="{ row }">{{ ruleLabel(row.ruleType) }}</template>
        </el-table-column>
        <el-table-column label="适用服务" min-width="160">
          <template #default="{ row }">{{ row.serviceName ?? '全部服务' }}</template>
        </el-table-column>
        <el-table-column label="适用技师" min-width="160">
          <template #default="{ row }">{{ row.technicianName ?? '全部技师' }}</template>
        </el-table-column>
        <el-table-column label="数值" width="200">
          <template #default="{ row }">
            <template v-if="isDualRow(row)">
              <div>轮钟 {{ formatPart(row, pickDualAmount(row, 'rotation')) }}</div>
              <div>点钟 {{ formatPart(row, pickDualAmount(row, 'designation')) }}</div>
            </template>
            <div v-else>{{ formatAmount(row) }}</div>
          </template>
        </el-table-column>
        <el-table-column prop="priority" label="优先级" width="80" />
        <el-table-column label="状态" width="80">
          <template #default="{ row }">
            <el-tag :type="row.isActive ? 'success' : 'info'">{{ row.isActive ? '启用' : '停用' }}</el-tag>
          </template>
        </el-table-column>
        <el-table-column label="操作" width="160">
          <template #default="{ row }">
            <el-button link type="primary" @click="openEdit(row)">编辑</el-button>
            <el-button link type="danger" @click="onDelete(row)">删除</el-button>
          </template>
        </el-table-column>
      </el-table>
      </div>
    </el-card>

    <el-dialog v-model="formOpen" :title="formMode === 'create' ? '新建提成规则' : '编辑提成规则'" width="720px">
      <el-form :model="form" label-width="120px">
        <el-form-item label="适用服务">
          <el-select v-model="form.serviceId" placeholder="留空 = 全部服务" clearable filterable style="width: 100%">
            <el-option v-for="s in services" :key="s.id" :label="`${s.code} ${s.name}`" :value="s.id" />
          </el-select>
        </el-form-item>
        <el-form-item label="适用技师">
          <el-select v-model="form.technicianId" placeholder="留空 = 全部技师" clearable filterable style="width: 100%">
            <el-option v-for="t in technicians" :key="t.id" :label="`${t.employeeNo ?? ''} · ${t.realName ?? t.username}`" :value="t.id" />
          </el-select>
        </el-form-item>
        <el-form-item label="规则类型">
          <el-radio-group v-model="form.ruleType">
            <el-radio-button value="FixedAmount">固定金额</el-radio-button>
            <el-radio-button value="Percentage">百分比</el-radio-button>
            <el-radio-button value="Tiered">阶梯</el-radio-button>
            <el-radio-button value="Timed">按时计费</el-radio-button>
          </el-radio-group>
        </el-form-item>
        <el-form-item v-if="form.ruleType === 'FixedAmount'" label="按上钟方式">
          <div class="dual-amount">
            <div class="dual-row">
              <span class="dual-label">轮钟金额</span>
              <el-input-number v-model="form.rotationFixed" :min="0" :precision="2" :step="5" style="width: 200px" />
              <span class="muted">元</span>
            </div>
            <div class="dual-row">
              <span class="dual-label">点钟金额</span>
              <el-input-number v-model="form.designationFixed" :min="0" :precision="2" :step="5" style="width: 200px" />
              <span class="muted">元</span>
            </div>
            <div class="muted hint">轮钟项按轮钟金额计提，点钟项按点钟金额计提。</div>
          </div>
        </el-form-item>
        <el-form-item v-else-if="form.ruleType === 'Percentage'" label="按上钟方式">
          <div class="dual-amount">
            <div class="dual-row">
              <span class="dual-label">轮钟比例</span>
              <el-input-number v-model="form.rotationPercent" :min="0" :max="100" :precision="2" :step="1" style="width: 200px" />
              <span class="muted">%</span>
            </div>
            <div class="dual-row">
              <span class="dual-label">点钟比例</span>
              <el-input-number v-model="form.designationPercent" :min="0" :max="100" :precision="2" :step="1" style="width: 200px" />
              <span class="muted">%</span>
            </div>
            <div class="muted hint">按订单项金额的百分比计提。</div>
          </div>
        </el-form-item>
        <el-form-item v-else :label="amountLabel">
          <el-input-number v-model="form.amount" :min="0" :precision="2" :step="5" />
          <span class="muted" style="margin-left: 8px">{{ amountHint }}</span>
        </el-form-item>
        <el-form-item v-if="form.ruleType === 'Tiered'" label="阶梯配置">
          <div class="tier-editor">
            <table class="tier-table">
              <thead>
                <tr>
                  <th>从第几单起</th>
                  <th>提成金额（元）</th>
                  <th style="width: 60px"></th>
                </tr>
              </thead>
              <tbody>
                <tr v-for="(t, i) in form.tiers" :key="i">
                  <td>
                    <el-input-number v-model="t.fromQty" :min="0" :precision="0" :step="1" :controls="false" style="width: 140px" />
                  </td>
                  <td>
                    <el-input-number v-model="t.amount" :min="0" :precision="2" :step="5" :controls="false" style="width: 140px" />
                  </td>
                  <td>
                    <el-button link type="danger" :disabled="form.tiers.length <= 1" @click="removeTier(i)">删除</el-button>
                  </td>
                </tr>
              </tbody>
            </table>
            <el-button size="small" plain @click="addTier">+ 添加档位</el-button>
            <div class="muted hint">
              按"本月已完成单数"匹配最后一档。例如：第 1 档"0 单起 ¥20"、第 2 档"31 单起 ¥30"，
              代表 1~30 单每单提 ¥20，第 31 单起每单提 ¥30。
            </div>
          </div>
        </el-form-item>
        <el-form-item label="优先级">
          <el-input-number v-model="form.priority" :min="0" :max="100" />
          <span class="muted" style="margin-left: 8px">同维度下数值大的优先</span>
        </el-form-item>
        <el-form-item label="启用">
          <el-switch v-model="form.isActive" />
        </el-form-item>
      </el-form>
      <template #footer>
        <el-button @click="formOpen = false">取消</el-button>
        <el-button type="primary" :loading="saving" @click="save">保存</el-button>
      </template>
    </el-dialog>

    <el-dialog v-model="bulkOpen" title="批量设置提成规则" width="960px">
      <el-form :model="bulk" label-width="120px">
        <el-form-item label="服务项目">
          <div class="bulk-multi-row">
            <el-select v-model="bulk.serviceIds" multiple filterable collapse-tags collapse-tags-tooltip
                       placeholder="请选择一个或多个服务" style="flex:1; min-width: 600px">
              <el-option v-for="s in services" :key="s.id" :label="`${s.code} ${s.name}`" :value="s.id" />
            </el-select>
            <el-button size="small" @click="bulk.serviceIds = services.map(s => s.id)">全选</el-button>
            <el-button size="small" @click="bulk.serviceIds = []">清空</el-button>
          </div>
        </el-form-item>
        <el-form-item label="技师">
          <div class="bulk-multi-row">
            <el-select v-model="bulk.technicianIds" multiple filterable collapse-tags collapse-tags-tooltip
                       placeholder="请选择一个或多个技师" style="flex:1; min-width: 600px">
              <el-option v-for="t in technicians" :key="t.id"
                         :label="`${t.employeeNo ?? ''} · ${t.realName ?? t.username}`" :value="t.id" />
            </el-select>
            <el-button size="small" @click="bulk.technicianIds = technicians.map(t => t.id)">全选</el-button>
            <el-button size="small" @click="bulk.technicianIds = []">清空</el-button>
          </div>
        </el-form-item>
        <el-form-item label="规则类型">
          <el-radio-group v-model="bulk.ruleType">
            <el-radio-button value="FixedAmount">固定金额</el-radio-button>
            <el-radio-button value="Percentage">百分比</el-radio-button>
            <el-radio-button value="Tiered">阶梯</el-radio-button>
            <el-radio-button value="Timed">按时计费</el-radio-button>
          </el-radio-group>
        </el-form-item>
        <el-form-item v-if="bulk.ruleType === 'FixedAmount'" label="按上钟方式">
          <div class="dual-amount">
            <div class="dual-row">
              <span class="dual-label">轮钟金额</span>
              <el-input-number v-model="bulk.rotationFixed" :min="0" :precision="2" :step="5" style="width: 200px" />
              <span class="muted">元</span>
            </div>
            <div class="dual-row">
              <span class="dual-label">点钟金额</span>
              <el-input-number v-model="bulk.designationFixed" :min="0" :precision="2" :step="5" style="width: 200px" />
              <span class="muted">元</span>
            </div>
          </div>
        </el-form-item>
        <el-form-item v-else-if="bulk.ruleType === 'Percentage'" label="按上钟方式">
          <div class="dual-amount">
            <div class="dual-row">
              <span class="dual-label">轮钟比例</span>
              <el-input-number v-model="bulk.rotationPercent" :min="0" :max="100" :precision="2" :step="1" style="width: 200px" />
              <span class="muted">%</span>
            </div>
            <div class="dual-row">
              <span class="dual-label">点钟比例</span>
              <el-input-number v-model="bulk.designationPercent" :min="0" :max="100" :precision="2" :step="1" style="width: 200px" />
              <span class="muted">%</span>
            </div>
          </div>
        </el-form-item>
        <el-form-item v-else :label="bulkAmountLabel">
          <el-input-number v-model="bulk.amount" :min="0" :precision="2" :step="5" />
        </el-form-item>
        <el-form-item v-if="bulk.ruleType === 'Tiered'" label="阶梯档位">
          <div class="tier-editor">
            <table class="tier-table">
              <thead><tr><th>从第几单起</th><th>提成金额（元）</th><th style="width:60px"></th></tr></thead>
              <tbody>
                <tr v-for="(t, i) in bulk.tiers" :key="i">
                  <td><el-input-number v-model="t.fromQty" :min="0" :precision="0" :step="1" :controls="false" style="width:140px" /></td>
                  <td><el-input-number v-model="t.amount" :min="0" :precision="2" :step="5" :controls="false" style="width:140px" /></td>
                  <td><el-button link type="danger" :disabled="bulk.tiers.length <= 1" @click="bulk.tiers.splice(i,1)">删除</el-button></td>
                </tr>
              </tbody>
            </table>
            <el-button size="small" plain @click="addBulkTier">+ 添加档位</el-button>
          </div>
        </el-form-item>
        <el-form-item label="优先级">
          <el-input-number v-model="bulk.priority" :min="0" :max="100" />
        </el-form-item>
        <el-form-item label="覆盖已有">
          <el-switch v-model="bulk.overwriteExisting" active-text="覆盖" inactive-text="跳过" />
          <span class="muted" style="margin-left: 8px">
            {{ bulk.overwriteExisting ? '已有的"通用"规则（不含仅轮钟/仅点钟）会被更新' : '已存在的服务+技师组合将被跳过' }}
          </span>
        </el-form-item>
        <el-alert v-if="bulkPairCount > 0" type="info" :closable="false" show-icon>
          将处理 {{ bulkPairCount }} 个 (服务 × 技师) 组合
        </el-alert>
      </el-form>
      <template #footer>
        <el-button @click="bulkOpen = false">取消</el-button>
        <el-button type="primary" :loading="bulkSaving" :disabled="bulkPairCount === 0" @click="submitBulk">
          应用到 {{ bulkPairCount }} 个组合
        </el-button>
      </template>
    </el-dialog>
  </div>
</template>

<script setup lang="ts">
import { computed, onMounted, reactive, ref } from 'vue';
import { ElMessage, ElMessageBox } from 'element-plus';
import { commissionsApi, servicesApi, staffApi } from '@/api/modules';
import type { CommissionRule, ServiceItem, Staff } from '@/api/types';

const rows = ref<CommissionRule[]>([]);
const services = ref<ServiceItem[]>([]);
const technicians = ref<Staff[]>([]);
const loading = ref(false);
const saving = ref(false);

const filterServiceId = ref<number | null>(null);
const filterTechnicianId = ref<number | null>(null);

const formOpen = ref(false);
const formMode = ref<'create' | 'edit'>('create');
const editingId = ref<number | null>(null);
interface TierRow { fromQty: number; amount: number }

const form = reactive({
  serviceId: null as number | null,
  technicianId: null as number | null,
  ruleType: 'FixedAmount' as 'FixedAmount' | 'Percentage' | 'Tiered' | 'Timed',
  amount: 0,
  tiers: [] as TierRow[],
  priority: 0,
  isActive: true,
  assignmentSource: null as 'Rotation' | 'Designation' | null,
  // 双金额按规则类型独立缓存，切换 ruleType 不会串值
  rotationFixed: null as number | null,
  designationFixed: null as number | null,
  rotationPercent: null as number | null,
  designationPercent: null as number | null
});

const supportsDualAmount = computed(() => form.ruleType === 'FixedAmount' || form.ruleType === 'Percentage');

function parseTiers(json: string | null | undefined): TierRow[] {
  if (!json) return [{ fromQty: 0, amount: 0 }];
  try {
    const raw = JSON.parse(json);
    if (!Array.isArray(raw) || raw.length === 0) return [{ fromQty: 0, amount: 0 }];
    return raw.map((r: any) => ({
      fromQty: Number(r.FromQty ?? r.fromQty ?? 0),
      amount: Number(r.Amount ?? r.amount ?? 0)
    }));
  } catch {
    return [{ fromQty: 0, amount: 0 }];
  }
}

function serializeTiers(tiers: TierRow[]): string {
  const sorted = [...tiers]
    .filter(t => Number.isFinite(t.fromQty) && Number.isFinite(t.amount))
    .sort((a, b) => a.fromQty - b.fromQty)
    .map(t => ({ FromQty: Math.round(t.fromQty), Amount: t.amount }));
  return JSON.stringify(sorted);
}

function addTier() {
  const last = form.tiers[form.tiers.length - 1];
  form.tiers.push({ fromQty: last ? last.fromQty + 10 : 0, amount: last?.amount ?? 0 });
}

function removeTier(i: number) {
  form.tiers.splice(i, 1);
}

function ruleLabel(t: string) {
  return ({ FixedAmount: '固定金额', Percentage: '百分比', Tiered: '阶梯', Timed: '按时计费' } as Record<string, string>)[t] ?? t;
}

function formatAmount(row: CommissionRule) {
  if (row.ruleType === 'Percentage') return `${row.amount}%`;
  if (row.ruleType === 'Timed') return `¥${row.amount}/小时`;
  return `¥${row.amount.toFixed(2)}`;
}

function formatPart(row: CommissionRule, v: number) {
  return row.ruleType === 'Percentage' ? `${v}%` : `¥${v.toFixed(2)}`;
}

function hasDualAmount(row: CommissionRule) {
  return (row.ruleType === 'FixedAmount' || row.ruleType === 'Percentage')
    && (row.rotationAmount != null || row.designationAmount != null);
}

/** 列表"数值"展示策略：FixedAmount/Percentage 全部按双金额渲染；
 * 旧规则仅设了 amount 没设双金额时，把 amount 同时作为轮钟/点钟金额展示。 */
function isDualRow(row: CommissionRule) {
  return row.ruleType === 'FixedAmount' || row.ruleType === 'Percentage';
}

function pickDualAmount(row: CommissionRule, kind: 'rotation' | 'designation'): number {
  if (kind === 'rotation') return row.rotationAmount ?? row.amount;
  return row.designationAmount ?? row.amount;
}

const amountLabel = computed(() => {
  switch (form.ruleType) {
    case 'Percentage': return '百分比';
    case 'Timed': return '元/小时';
    case 'Tiered': return '默认数值';
    default: return '提成金额';
  }
});
const amountHint = computed(() => {
  switch (form.ruleType) {
    case 'Percentage': return '0~100，按订单项金额比例';
    case 'Timed': return '按服务时长比例计算';
    case 'Tiered': return '阶梯未匹配时使用';
    default: return '元';
  }
});

async function reload() {
  loading.value = true;
  try {
    rows.value = await commissionsApi.list(filterServiceId.value ?? undefined, filterTechnicianId.value ?? undefined);
  } finally {
    loading.value = false;
  }
}

async function loadOptions() {
  const [s, t] = await Promise.all([
    servicesApi.list(true),
    staffApi.list({ role: 'Technician', pageSize: 200 })
  ]);
  services.value = s;
  technicians.value = t.items;
}

function openCreate() {
  formMode.value = 'create';
  editingId.value = null;
  Object.assign(form, {
    serviceId: null, technicianId: null, ruleType: 'FixedAmount',
    amount: 0, tiers: parseTiers(null), priority: 0, isActive: true,
    assignmentSource: null,
    rotationFixed: null, designationFixed: null,
    rotationPercent: null, designationPercent: null
  });
  formOpen.value = true;
}

function openEdit(row: CommissionRule) {
  formMode.value = 'edit';
  editingId.value = row.id;
  // 编辑时按规则类型把已存的双金额恢复到对应那一组，另一组保留 null
  const isFixed = row.ruleType === 'FixedAmount';
  const isPercent = row.ruleType === 'Percentage';
  Object.assign(form, {
    serviceId: row.serviceId,
    technicianId: row.technicianId,
    ruleType: row.ruleType,
    amount: row.amount,
    tiers: parseTiers(row.tieredRulesJson),
    priority: row.priority,
    isActive: row.isActive,
    assignmentSource: row.assignmentSource ?? null,
    rotationFixed: isFixed ? (row.rotationAmount ?? null) : null,
    designationFixed: isFixed ? (row.designationAmount ?? null) : null,
    rotationPercent: isPercent ? (row.rotationAmount ?? null) : null,
    designationPercent: isPercent ? (row.designationAmount ?? null) : null
  });
  formOpen.value = true;
}

async function save() {
  const dual = supportsDualAmount.value;
  // 按当前规则类型取对应那一组双金额
  const rotation = form.ruleType === 'FixedAmount' ? form.rotationFixed
    : form.ruleType === 'Percentage' ? form.rotationPercent : null;
  const designation = form.ruleType === 'FixedAmount' ? form.designationFixed
    : form.ruleType === 'Percentage' ? form.designationPercent : null;
  if (dual) {
    if (rotation == null || designation == null) {
      ElMessage.error('请同时填写轮钟与点钟金额');
      return;
    }
  }
  saving.value = true;
  try {
    const body = {
      serviceId: form.serviceId,
      technicianId: form.technicianId,
      ruleType: form.ruleType,
      // FixedAmount/Percentage 下 amount 不再单独展示，用双金额的较低值兜底（计算器在双金额齐全时不会读 amount）
      amount: dual ? Math.min(rotation ?? 0, designation ?? 0) : form.amount,
      tieredRulesJson: form.ruleType === 'Tiered' ? serializeTiers(form.tiers) : null,
      priority: form.priority,
      isActive: form.isActive,
      assignmentSource: null,
      rotationAmount: dual ? rotation : null,
      designationAmount: dual ? designation : null
    };
    if (formMode.value === 'create') {
      await commissionsApi.create(body);
    } else if (editingId.value != null) {
      const { serviceId: _s, technicianId: _t, ...rest } = body;
      await commissionsApi.update(editingId.value, rest);
    }
    ElMessage.success('已保存');
    formOpen.value = false;
    reload();
  } finally {
    saving.value = false;
  }
}

// ============ 批量设置 ============
const bulkOpen = ref(false);
const bulkSaving = ref(false);
const bulk = reactive({
  serviceIds: [] as number[],
  technicianIds: [] as number[],
  ruleType: 'FixedAmount' as 'FixedAmount' | 'Percentage' | 'Tiered' | 'Timed',
  amount: 0,
  rotationFixed: null as number | null,
  designationFixed: null as number | null,
  rotationPercent: null as number | null,
  designationPercent: null as number | null,
  tiers: [{ fromQty: 0, amount: 0 }] as TierRow[],
  priority: 0,
  overwriteExisting: false
});
const bulkSupportsDual = computed(() => bulk.ruleType === 'FixedAmount' || bulk.ruleType === 'Percentage');
const bulkPairCount = computed(() => bulk.serviceIds.length * bulk.technicianIds.length);
const bulkAmountLabel = computed(() => bulk.ruleType === 'Timed' ? '元/小时' : '默认数值');

function openBulk() {
  Object.assign(bulk, {
    serviceIds: [],
    technicianIds: [],
    ruleType: 'FixedAmount',
    amount: 0,
    rotationFixed: null, designationFixed: null,
    rotationPercent: null, designationPercent: null,
    tiers: [{ fromQty: 0, amount: 0 }],
    priority: 0,
    overwriteExisting: false
  });
  bulkOpen.value = true;
}

function addBulkTier() {
  const last = bulk.tiers[bulk.tiers.length - 1];
  bulk.tiers.push({ fromQty: last ? last.fromQty + 10 : 0, amount: last?.amount ?? 0 });
}

async function submitBulk() {
  if (bulkPairCount.value === 0) return;
  const dual = bulkSupportsDual.value;
  const rotation = bulk.ruleType === 'FixedAmount' ? bulk.rotationFixed
    : bulk.ruleType === 'Percentage' ? bulk.rotationPercent : null;
  const designation = bulk.ruleType === 'FixedAmount' ? bulk.designationFixed
    : bulk.ruleType === 'Percentage' ? bulk.designationPercent : null;
  if (dual) {
    if (rotation == null || designation == null) {
      ElMessage.error('请同时填写轮钟与点钟金额');
      return;
    }
  }
  bulkSaving.value = true;
  try {
    const body = {
      serviceIds: bulk.serviceIds,
      technicianIds: bulk.technicianIds,
      ruleType: bulk.ruleType,
      amount: dual ? Math.min(rotation ?? 0, designation ?? 0) : bulk.amount,
      tieredRulesJson: bulk.ruleType === 'Tiered' ? serializeTiers(bulk.tiers) : null,
      priority: bulk.priority,
      isActive: true,
      rotationAmount: dual ? rotation : null,
      designationAmount: dual ? designation : null,
      overwriteExisting: bulk.overwriteExisting
    };
    const result = await commissionsApi.bulk(body);
    ElMessage.success(`已应用：新增 ${result.created}，覆盖 ${result.updated}，跳过 ${result.skipped}`);
    bulkOpen.value = false;
    reload();
  } finally {
    bulkSaving.value = false;
  }
}

async function onDelete(row: CommissionRule) {
  await ElMessageBox.confirm('确认删除该规则？', '提示', { type: 'warning' }).catch(() => null);
  await commissionsApi.remove(row.id);
  ElMessage.success('已删除');
  reload();
}

onMounted(async () => {
  await loadOptions();
  reload();
});
</script>

<style scoped>
.page { padding-bottom: 24px; }
.toolbar { display: flex; gap: 8px; align-items: center; flex-wrap: wrap; }
.muted { color: var(--el-text-color-secondary); font-size: 12px; }
.dual-amount { display: flex; flex-direction: column; gap: 8px; }
.dual-row { display: flex; align-items: center; gap: 8px; }
.dual-label { width: 64px; color: var(--el-text-color-regular); font-size: 13px; }
.hint { line-height: 1.5; }
.tier-editor { display: flex; flex-direction: column; gap: 8px; }
.tier-table { border-collapse: collapse; width: 100%; max-width: 400px; }
.tier-table th { font-weight: 500; color: var(--el-text-color-regular); font-size: 13px; padding: 4px 8px; text-align: left; }
.tier-table td { padding: 4px 8px; }
.bulk-multi-row { display: flex; gap: 8px; align-items: center; }
</style>
