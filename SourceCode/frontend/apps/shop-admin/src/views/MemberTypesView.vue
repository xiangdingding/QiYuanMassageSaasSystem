<template>
  <div class="page">
    <el-card shadow="never">
      <template #header>
        <div class="header-row">
          <div>
            <h2 class="card-title">会员类型</h2>
            <p class="muted">定义可售卡种：充值卡（充钱进余额）/ 计次卡（绑定服务、按次扣减）</p>
          </div>
          <el-button type="primary" @click="openCreate">新增会员类型</el-button>
        </div>
      </template>

      <el-tabs v-model="filterKind" @tab-change="reload">
        <el-tab-pane label="全部" name="" />
        <el-tab-pane label="充值卡" name="StoredValue" />
        <el-tab-pane label="计次卡" name="CountBased" />
      </el-tabs>

      <el-table :data="rows" v-loading="loading" stripe>
        <el-table-column label="排序" width="70" prop="sort" />
        <el-table-column label="编码" width="120">
          <template #default="{ row }">
            <code class="code-tag">{{ row.code }}</code>
          </template>
        </el-table-column>
        <el-table-column label="名称" min-width="120" prop="name" />
        <el-table-column label="类型" width="100">
          <template #default="{ row }">
            <el-tag :type="row.kind === 'StoredValue' ? 'warning' : 'success'" size="small">
              {{ row.kind === 'StoredValue' ? '充值卡' : '计次卡' }}
            </el-tag>
          </template>
        </el-table-column>
        <el-table-column label="绑定服务 / 最低门槛" min-width="180">
          <template #default="{ row }">
            <template v-if="row.kind === 'StoredValue'">
              <span>最低 ¥{{ row.minRechargeAmount?.toFixed(2) ?? '—' }}</span>
            </template>
            <template v-else>
              <span>{{ row.serviceItemName ?? '—' }}</span>
              <span class="muted" style="margin-left:6px">≥ {{ row.minPurchaseCount ?? '—' }} 次</span>
            </template>
          </template>
        </el-table-column>
        <el-table-column label="折扣" width="80">
          <template #default="{ row }">
            <span v-if="row.discount < 1">{{ (row.discount * 10).toFixed(1) }} 折</span>
            <span v-else>原价</span>
          </template>
        </el-table-column>
        <el-table-column label="赠送" width="100">
          <template #default="{ row }">
            <span v-if="row.kind === 'StoredValue' && (row.bonusAmount ?? 0) > 0">
              送 ¥{{ row.bonusAmount?.toFixed(2) }}
            </span>
            <span v-else-if="row.kind === 'CountBased' && (row.bonusCount ?? 0) > 0">
              送 {{ row.bonusCount }} 次
            </span>
            <span v-else class="muted">—</span>
          </template>
        </el-table-column>
        <el-table-column label="有效期" width="100">
          <template #default="{ row }">
            <span v-if="row.validDays">{{ row.validDays }} 天</span>
            <span v-else class="muted">永久</span>
          </template>
        </el-table-column>
        <el-table-column label="状态" width="80">
          <template #default="{ row }">
            <el-tag :type="row.isActive ? 'success' : 'info'" size="small" effect="plain">
              {{ row.isActive ? '启用' : '停用' }}
            </el-tag>
          </template>
        </el-table-column>
        <el-table-column label="操作" width="140" fixed="right">
          <template #default="{ row }">
            <el-button link type="primary" @click="openEdit(row)">编辑</el-button>
            <el-button link type="danger" @click="removeRow(row)">删除</el-button>
          </template>
        </el-table-column>
      </el-table>
    </el-card>

    <el-dialog v-model="formOpen" :title="formMode === 'create' ? '新增会员类型' : '编辑会员类型'" width="560px">
      <el-form :model="form" :rules="rules" ref="formRef" label-width="120px">
        <el-form-item label="类型" prop="kind">
          <el-radio-group v-model="form.kind" :disabled="formMode === 'edit'">
            <el-radio-button value="StoredValue">充值卡</el-radio-button>
            <el-radio-button value="CountBased">计次卡</el-radio-button>
          </el-radio-group>
          <span class="muted">类型一经创建不可修改</span>
        </el-form-item>
        <el-form-item label="编码" prop="code">
          <el-input v-model="form.code" :disabled="formMode === 'edit'" placeholder="如 GOLD / FOOT100（大写+下划线）" />
        </el-form-item>
        <el-form-item label="名称" prop="name">
          <el-input v-model="form.name" placeholder="如 黄金卡 / 100 次足疗卡" maxlength="64" />
        </el-form-item>

        <template v-if="form.kind === 'CountBased'">
          <el-form-item label="绑定服务" prop="serviceItemId">
            <el-select v-model="form.serviceItemId" placeholder="选择计次卡对应的服务项目" style="width:100%">
              <el-option
                v-for="s in services"
                :key="s.id"
                :value="s.id"
                :label="`${s.name}（${s.durationMinutes}分钟·¥${s.price.toFixed(0)}）`"
              />
            </el-select>
          </el-form-item>
          <el-form-item label="最低购买次数" prop="minPurchaseCount">
            <el-input-number v-model="form.minPurchaseCount" :min="1" :max="9999" />
          </el-form-item>
          <el-form-item label="赠送次数">
            <el-input-number v-model="form.bonusCount" :min="0" :max="9999" />
            <span class="muted" style="margin-left:8px">开卡 / 续费时一次性赠送</span>
          </el-form-item>
        </template>

        <template v-else>
          <el-form-item label="最低充值金额" prop="minRechargeAmount">
            <el-input-number v-model="form.minRechargeAmount" :min="0.01" :precision="2" :step="100" />
          </el-form-item>
          <el-form-item label="赠送金额">
            <el-input-number v-model="form.bonusAmount" :min="0" :precision="2" :step="50" :value-on-clear="null" placeholder="无赠送可留空" />
            <span class="muted" style="margin-left:8px">开卡 / 续费时一次性赠送</span>
          </el-form-item>
        </template>

        <el-form-item label="折扣" prop="discount">
          <el-input-number v-model="form.discount" :min="0.1" :max="1" :step="0.05" :precision="2" />
          <span class="muted" style="margin-left:8px">0.85 = 8.5 折</span>
        </el-form-item>
        <el-form-item label="有效天数">
          <el-input-number v-model="form.validDays" :min="0" :max="3650" />
          <span class="muted" style="margin-left:8px">0 / 留空 = 永不过期</span>
        </el-form-item>
        <el-form-item label="启用">
          <el-switch v-model="form.isActive" />
        </el-form-item>
        <el-form-item label="备注">
          <el-input v-model="form.remark" type="textarea" :rows="2" maxlength="200" />
        </el-form-item>
        <el-form-item label="排序" prop="sort">
          <el-input-number v-model="form.sort" :min="0" :max="999" />
          <span class="muted" style="margin-left:8px">数字越小越靠前</span>
        </el-form-item>
      </el-form>
      <template #footer>
        <el-button @click="formOpen = false">取消</el-button>
        <el-button type="primary" :loading="saving" @click="saveForm">保存</el-button>
      </template>
    </el-dialog>
  </div>
</template>

<script setup lang="ts">
import { onMounted, reactive, ref } from 'vue';
import { ElMessage, ElMessageBox, type FormInstance, type FormRules } from 'element-plus';
import { memberTypesApi, servicesApi, type MemberType, type MemberTypeKind } from '@/api/modules';
import type { ServiceItem } from '@/api/types';

const rows = ref<MemberType[]>([]);
const services = ref<ServiceItem[]>([]);
const loading = ref(false);
const saving = ref(false);
const filterKind = ref<'' | MemberTypeKind>('');

const formOpen = ref(false);
const formMode = ref<'create' | 'edit'>('create');
const editingId = ref<number | null>(null);
const formRef = ref<FormInstance>();

const form = reactive({
  code: '',
  name: '',
  sort: 0,
  kind: 'StoredValue' as MemberTypeKind,
  serviceItemId: null as number | null,
  minRechargeAmount: 1000 as number | null,
  minPurchaseCount: 10 as number | null,
  discount: 1,
  bonusAmount: 0 as number | null,
  bonusCount: 0 as number | null,
  validDays: 365 as number | null,
  isActive: true,
  remark: ''
});

const rules: FormRules = {
  code: [
    { required: true, message: '请输入编码', trigger: 'blur' },
    { pattern: /^[A-Z][A-Z0-9_]*$/, message: '必须以大写字母开头，仅大写字母 / 数字 / 下划线', trigger: 'blur' }
  ],
  name: [{ required: true, message: '请输入名称', trigger: 'blur' }],
  kind: [{ required: true, message: '请选择类型', trigger: 'change' }],
  discount: [{ required: true, message: '请输入折扣', trigger: 'blur' }]
};

async function reload() {
  loading.value = true;
  try {
    const kind = filterKind.value || undefined;
    rows.value = await memberTypesApi.list(true, kind as MemberTypeKind | undefined);
  } finally {
    loading.value = false;
  }
}

function openCreate() {
  formMode.value = 'create';
  editingId.value = null;
  // 默认排序号 = 当前列表中最大排序号 + 1（与服务项目逻辑一致），新建项默认排到最后
  const maxSort = rows.value.reduce((m, r) => Math.max(m, r.sort ?? 0), 0);
  Object.assign(form, {
    code: '',
    name: '',
    sort: maxSort + 1,
    kind: 'StoredValue' as MemberTypeKind,
    serviceItemId: null,
    minRechargeAmount: 1000,
    minPurchaseCount: 10,
    discount: 1,
    bonusAmount: null,
    bonusCount: 0,
    validDays: 365,
    isActive: true,
    remark: ''
  });
  formOpen.value = true;
}

function openEdit(row: MemberType) {
  formMode.value = 'edit';
  editingId.value = row.id;
  Object.assign(form, {
    code: row.code,
    name: row.name,
    sort: row.sort,
    kind: row.kind,
    serviceItemId: row.serviceItemId ?? null,
    minRechargeAmount: row.minRechargeAmount ?? null,
    minPurchaseCount: row.minPurchaseCount ?? null,
    discount: row.discount,
    bonusAmount: row.bonusAmount ?? null,
    bonusCount: row.bonusCount ?? 0,
    validDays: row.validDays ?? null,
    isActive: row.isActive,
    remark: row.remark ?? ''
  });
  formOpen.value = true;
}

async function saveForm() {
  if (!formRef.value) return;
  const ok = await formRef.value.validate().catch(() => false);
  if (!ok) return;

  // 按 Kind 校验关键字段
  if (form.kind === 'CountBased') {
    if (!form.serviceItemId) { ElMessage.warning('计次卡必须选择服务项目'); return; }
    if (!form.minPurchaseCount || form.minPurchaseCount <= 0) {
      ElMessage.warning('计次卡必须设置最低购买次数'); return;
    }
  } else {
    if (!form.minRechargeAmount || form.minRechargeAmount <= 0) {
      ElMessage.warning('充值卡必须设置最低充值金额'); return;
    }
  }

  saving.value = true;
  try {
    const isCountBased = form.kind === 'CountBased';
    const payload = {
      code: form.code.trim().toUpperCase(),
      name: form.name.trim(),
      sort: form.sort,
      kind: form.kind,
      serviceItemId: isCountBased ? form.serviceItemId : null,
      minRechargeAmount: isCountBased ? null : form.minRechargeAmount,
      minPurchaseCount: isCountBased ? form.minPurchaseCount : null,
      discount: form.discount,
      bonusAmount: isCountBased ? null : (form.bonusAmount || 0),
      bonusCount: isCountBased ? (form.bonusCount || 0) : null,
      validDays: form.validDays && form.validDays > 0 ? form.validDays : null,
      isActive: form.isActive,
      remark: form.remark || null
    };
    if (formMode.value === 'create') {
      await memberTypesApi.create(payload as any);
    } else if (editingId.value != null) {
      await memberTypesApi.update(editingId.value, payload as any);
    }
    ElMessage.success('已保存');
    formOpen.value = false;
    reload();
  } finally {
    saving.value = false;
  }
}

async function removeRow(row: MemberType) {
  try {
    await ElMessageBox.confirm(
      `确认删除会员类型「${row.name}」？已开出的会员卡不会受影响，但此后不能再用该类型开卡。`,
      '删除会员类型',
      { type: 'warning' }
    );
  } catch { return; }
  await memberTypesApi.remove(row.id);
  ElMessage.success('已删除');
  reload();
}

onMounted(async () => {
  // 服务项目用于计次卡绑定下拉
  services.value = await servicesApi.list(false).catch(() => []);
  reload();
});
</script>

<style scoped>
.page { padding-bottom: 24px; }
.header-row { display: flex; align-items: flex-start; justify-content: space-between; gap: 16px; }
.card-title { margin: 0; font-size: 16px; }
.muted { color: var(--el-text-color-secondary); font-size: 12px; margin: 2px 0 0; }
.code-tag {
  font-family: ui-monospace, SFMono-Regular, Menlo, Consolas, monospace;
  font-size: 12px;
  background: var(--el-fill-color-light);
  padding: 1px 6px;
  border-radius: 4px;
}
</style>
