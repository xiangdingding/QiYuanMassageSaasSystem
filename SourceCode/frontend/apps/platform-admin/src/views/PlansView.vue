<template>
  <div class="page">
    <el-card shadow="never">
      <div class="toolbar">
        <el-checkbox v-model="includeInactive" @change="reload">显示已停用</el-checkbox>
        <el-button type="primary" @click="openCreate">新建套餐</el-button>
      </div>
      <el-table :data="rows" v-loading="loading" stripe style="margin-top: 12px">
        <el-table-column prop="code" label="编码" width="120" />
        <el-table-column prop="name" label="名称" min-width="160" />
        <el-table-column prop="maxStores" label="门店上限" width="100" />
        <el-table-column prop="maxStaff" label="员工上限" width="100" />
        <el-table-column label="年费" width="120">
          <template #default="{ row }">¥ {{ row.annualPrice.toFixed(2) }}</template>
        </el-table-column>
        <el-table-column label="状态" width="100">
          <template #default="{ row }">
            <el-tag :type="row.isActive ? 'success' : 'info'">
              {{ row.isActive ? '启用' : '停用' }}
            </el-tag>
          </template>
        </el-table-column>
        <el-table-column label="操作" width="160">
          <template #default="{ row }">
            <el-button link type="primary" @click="openEdit(row)">编辑</el-button>
            <el-button
              link
              :type="row.isActive ? 'warning' : 'success'"
              @click="toggle(row)"
            >{{ row.isActive ? '停用' : '启用' }}</el-button>
          </template>
        </el-table-column>
      </el-table>
    </el-card>

    <el-dialog
      v-model="dialogOpen"
      :title="dialogMode === 'create' ? '新建套餐' : '编辑套餐'"
      width="480px"
    >
      <el-form :model="form" :rules="rules" ref="formRef" label-width="100px">
        <el-form-item label="编码" prop="code">
          <el-input v-model="form.code" :disabled="dialogMode === 'edit'" />
        </el-form-item>
        <el-form-item label="名称" prop="name">
          <el-input v-model="form.name" />
        </el-form-item>
        <el-form-item label="门店上限" prop="maxStores">
          <el-input-number v-model="form.maxStores" :min="1" />
        </el-form-item>
        <el-form-item label="员工上限" prop="maxStaff">
          <el-input-number v-model="form.maxStaff" :min="1" />
        </el-form-item>
        <el-form-item label="年费(元)" prop="annualPrice">
          <el-input-number v-model="form.annualPrice" :min="0" :precision="2" :step="100" />
        </el-form-item>
        <el-form-item label="启用">
          <el-switch v-model="form.isActive" />
        </el-form-item>
      </el-form>
      <template #footer>
        <el-button @click="dialogOpen = false">取消</el-button>
        <el-button type="primary" :loading="saving" @click="save">保存</el-button>
      </template>
    </el-dialog>
  </div>
</template>

<script setup lang="ts">
import { onMounted, reactive, ref } from 'vue';
import { ElMessage, type FormInstance, type FormRules } from 'element-plus';
import { plansApi } from '@/api/modules';
import type { Plan } from '@/api/types';

const rows = ref<Plan[]>([]);
const loading = ref(false);
const includeInactive = ref(false);

const dialogOpen = ref(false);
const dialogMode = ref<'create' | 'edit'>('create');
const editingId = ref<number | null>(null);
const saving = ref(false);
const formRef = ref<FormInstance>();

const form = reactive({
  code: '',
  name: '',
  maxStores: 1,
  maxStaff: 10,
  annualPrice: 0,
  featureJson: '',
  isActive: true
});

const rules: FormRules = {
  code: [{ required: true, message: '请输入编码', trigger: 'blur' }],
  name: [{ required: true, message: '请输入名称', trigger: 'blur' }]
};

async function reload() {
  loading.value = true;
  try {
    rows.value = await plansApi.list(includeInactive.value);
  } finally {
    loading.value = false;
  }
}

function openCreate() {
  dialogMode.value = 'create';
  editingId.value = null;
  Object.assign(form, { code: '', name: '', maxStores: 1, maxStaff: 10, annualPrice: 0, featureJson: '', isActive: true });
  dialogOpen.value = true;
}

function openEdit(row: Plan) {
  dialogMode.value = 'edit';
  editingId.value = row.id;
  Object.assign(form, {
    code: row.code,
    name: row.name,
    maxStores: row.maxStores,
    maxStaff: row.maxStaff,
    annualPrice: row.annualPrice,
    featureJson: row.featureJson ?? '',
    isActive: row.isActive
  });
  dialogOpen.value = true;
}

async function save() {
  if (!formRef.value) return;
  const ok = await formRef.value.validate().catch(() => false);
  if (!ok) return;
  saving.value = true;
  try {
    if (dialogMode.value === 'create') {
      await plansApi.create({ ...form, featureJson: form.featureJson || null });
    } else if (editingId.value != null) {
      const { code: _code, ...rest } = form;
      await plansApi.update(editingId.value, { ...rest, featureJson: form.featureJson || null });
    }
    ElMessage.success('已保存');
    dialogOpen.value = false;
    reload();
  } finally {
    saving.value = false;
  }
}

async function toggle(row: Plan) {
  const { code: _code, ...rest } = row;
  await plansApi.update(row.id, { ...rest, isActive: !row.isActive, featureJson: row.featureJson ?? null });
  ElMessage.success(row.isActive ? '已停用' : '已启用');
  reload();
}

onMounted(reload);
</script>

<style scoped>
.page { padding-bottom: 24px; }
.toolbar { display: flex; gap: 12px; align-items: center; }
</style>
