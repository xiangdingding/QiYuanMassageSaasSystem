<template>
  <div class="page">
    <el-card shadow="never">
      <div class="toolbar">
        <el-select v-model="filterServiceId" placeholder="按服务过滤" clearable filterable style="width: 200px">
          <el-option v-for="s in services" :key="s.id" :label="`${s.code} ${s.name}`" :value="s.id" />
        </el-select>
        <el-select v-model="filterTechnicianId" placeholder="按技师过滤" clearable filterable style="width: 200px">
          <el-option v-for="t in technicians" :key="t.id" :label="`${t.employeeNo ?? ''} · ${t.realName ?? t.username}`" :value="t.id" />
        </el-select>
        <el-button type="primary" @click="reload">查询</el-button>
        <el-button type="success" @click="openCreate">新建规则</el-button>
      </div>

      <el-table :data="rows" v-loading="loading" stripe style="margin-top: 12px">
        <el-table-column label="规则类型" width="100">
          <template #default="{ row }">{{ ruleLabel(row.ruleType) }}</template>
        </el-table-column>
        <el-table-column label="适用服务" min-width="160">
          <template #default="{ row }">{{ row.serviceName ?? '全部服务' }}</template>
        </el-table-column>
        <el-table-column label="适用技师" min-width="160">
          <template #default="{ row }">{{ row.technicianName ?? '全部技师' }}</template>
        </el-table-column>
        <el-table-column label="数值" width="100">
          <template #default="{ row }">{{ formatAmount(row) }}</template>
        </el-table-column>
        <el-table-column label="适用来源" width="100">
          <template #default="{ row }">
            <el-tag
              v-if="row.assignmentSource === 'Rotation'"
              type="info" size="small"
            >仅轮钟</el-tag>
            <el-tag
              v-else-if="row.assignmentSource === 'Designation'"
              type="warning" size="small"
            >仅点钟</el-tag>
            <span v-else class="muted">不限</span>
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
    </el-card>

    <el-dialog v-model="formOpen" :title="formMode === 'create' ? '新建提成规则' : '编辑提成规则'" width="500px">
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
        <el-form-item label="适用来源">
          <el-radio-group v-model="form.assignmentSource" aria-label="该规则适用的上钟方式">
            <el-radio-button :value="null">不限</el-radio-button>
            <el-radio-button value="Rotation">仅轮钟</el-radio-button>
            <el-radio-button value="Designation">仅点钟</el-radio-button>
          </el-radio-group>
          <div class="muted" style="margin-top:4px">
            为同一对"服务+技师"分别配两条规则（一条仅轮钟，一条仅点钟），就能让两种上钟方式走不同提成。
          </div>
        </el-form-item>
        <el-form-item label="规则类型">
          <el-radio-group v-model="form.ruleType">
            <el-radio-button value="FixedAmount">固定金额</el-radio-button>
            <el-radio-button value="Percentage">百分比</el-radio-button>
            <el-radio-button value="Tiered">阶梯</el-radio-button>
            <el-radio-button value="Timed">按时计费</el-radio-button>
          </el-radio-group>
        </el-form-item>
        <el-form-item :label="amountLabel">
          <el-input-number v-model="form.amount" :min="0" :precision="2" :step="form.ruleType === 'Percentage' ? 1 : 5" />
          <span class="muted" style="margin-left: 8px">{{ amountHint }}</span>
        </el-form-item>
        <el-form-item v-if="form.ruleType === 'Tiered'" label="阶梯配置">
          <el-input v-model="form.tieredRulesJson" type="textarea" :rows="4" placeholder='[{"FromQty":0,"Amount":20},{"FromQty":31,"Amount":30}]' />
          <div class="muted" style="margin-top: 4px">JSON 数组，按"本月已完成单数"匹配最后一档</div>
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
const form = reactive({
  serviceId: null as number | null,
  technicianId: null as number | null,
  ruleType: 'FixedAmount' as 'FixedAmount' | 'Percentage' | 'Tiered' | 'Timed',
  amount: 0,
  tieredRulesJson: '',
  priority: 0,
  isActive: true,
  assignmentSource: null as 'Rotation' | 'Designation' | null
});

function ruleLabel(t: string) {
  return ({ FixedAmount: '固定金额', Percentage: '百分比', Tiered: '阶梯', Timed: '按时计费' } as Record<string, string>)[t] ?? t;
}

function formatAmount(row: CommissionRule) {
  if (row.ruleType === 'Percentage') return `${row.amount}%`;
  if (row.ruleType === 'Timed') return `¥${row.amount}/小时`;
  return `¥${row.amount.toFixed(2)}`;
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
    amount: 0, tieredRulesJson: '', priority: 0, isActive: true,
    assignmentSource: null
  });
  formOpen.value = true;
}

function openEdit(row: CommissionRule) {
  formMode.value = 'edit';
  editingId.value = row.id;
  Object.assign(form, {
    serviceId: row.serviceId,
    technicianId: row.technicianId,
    ruleType: row.ruleType,
    amount: row.amount,
    tieredRulesJson: row.tieredRulesJson ?? '',
    priority: row.priority,
    isActive: row.isActive,
    assignmentSource: row.assignmentSource ?? null
  });
  formOpen.value = true;
}

async function save() {
  saving.value = true;
  try {
    const body = {
      serviceId: form.serviceId,
      technicianId: form.technicianId,
      ruleType: form.ruleType,
      amount: form.amount,
      tieredRulesJson: form.tieredRulesJson || null,
      priority: form.priority,
      isActive: form.isActive,
      assignmentSource: form.assignmentSource
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
</style>
