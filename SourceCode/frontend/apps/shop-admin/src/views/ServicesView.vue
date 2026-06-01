<template>
  <div class="page">
    <el-card shadow="never">
      <div class="toolbar">
        <el-checkbox v-model="includeInactive" @change="reload">显示已停用</el-checkbox>
        <el-button type="primary" @click="openCreate">新建项目</el-button>
      </div>
      <div class="table-wrap">
      <el-table :data="rows" v-loading="loading" stripe height="100%">
        <el-table-column prop="sort" label="排序" width="80" sortable />
        <el-table-column prop="code" label="编码" width="100" sortable />
        <el-table-column prop="name" label="名称" min-width="180" sortable />
        <el-table-column prop="durationMinutes" label="时长(分)" width="90" />
        <el-table-column label="原价" width="100">
          <template #default="{ row }">¥{{ row.price.toFixed(2) }}</template>
        </el-table-column>
        <el-table-column label="会员价" width="100">
          <template #default="{ row }">¥{{ row.memberPrice.toFixed(2) }}</template>
        </el-table-column>
        <el-table-column label="状态" width="80">
          <template #default="{ row }">
            <el-tag :type="row.isActive ? 'success' : 'info'">{{ row.isActive ? '启用' : '停用' }}</el-tag>
          </template>
        </el-table-column>
        <el-table-column label="操作" width="180">
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
      </div>
    </el-card>

    <el-dialog v-model="formOpen" :title="formMode === 'create' ? '新建服务项目' : '编辑服务项目'" width="480px">
      <el-form :model="form" :rules="rules" ref="formRef" label-width="100px">
        <el-form-item label="编码" prop="code">
          <el-input v-model="form.code" />
        </el-form-item>
        <el-form-item label="名称" prop="name">
          <el-input v-model="form.name" />
        </el-form-item>
        <el-form-item label="时长(分钟)" prop="durationMinutes">
          <el-input-number v-model="form.durationMinutes" :min="1" :max="600" />
        </el-form-item>
        <el-form-item label="原价" prop="price">
          <el-input-number v-model="form.price" :min="0" :precision="2" :step="10" :value-on-clear="null" placeholder="请输入" />
        </el-form-item>
        <el-form-item label="会员价" prop="memberPrice">
          <el-input-number v-model="form.memberPrice" :min="0" :precision="2" :step="10" :value-on-clear="null" placeholder="请输入" />
        </el-form-item>
        <el-form-item label="说明">
          <el-input v-model="form.description" type="textarea" :rows="2" />
        </el-form-item>
        <el-form-item label="启用">
          <el-switch v-model="form.isActive" />
        </el-form-item>
        <el-form-item label="排序" prop="sort">
          <el-input-number v-model="form.sort" :min="0" :precision="0" :step="1" />
          <span class="muted" style="margin-left:8px">值越小越靠前</span>
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
import { onMounted, reactive, ref } from 'vue';
import { ElMessage, type FormInstance, type FormRules } from 'element-plus';
import { servicesApi } from '@/api/modules';
import type { ServiceItem } from '@/api/types';

const rows = ref<ServiceItem[]>([]);
const loading = ref(false);
const includeInactive = ref(false);

const formOpen = ref(false);
const formMode = ref<'create' | 'edit'>('create');
const editingId = ref<number | null>(null);
const formRef = ref<FormInstance>();
const saving = ref(false);

const form = reactive({
  code: '',
  name: '',
  durationMinutes: 60,
  price: null as number | null,
  memberPrice: null as number | null,
  description: '',
  isActive: true,
  sort: 0
});
const rules: FormRules = {
  code: [{ required: true, message: '请输入编码', trigger: 'blur' }],
  name: [{ required: true, message: '请输入名称', trigger: 'blur' }],
  durationMinutes: [{ required: true, message: '请输入时长', trigger: 'blur' }],
  price: [{ required: true, type: 'number', message: '请输入原价', trigger: 'blur' }],
  memberPrice: [{ required: true, type: 'number', message: '请输入会员价', trigger: 'blur' }]
};

async function reload() {
  loading.value = true;
  try {
    rows.value = await servicesApi.list(includeInactive.value);
  } finally {
    loading.value = false;
  }
}

function openCreate() {
  formMode.value = 'create';
  editingId.value = null;
  // 默认排序号 = 当前列表中最大排序号 + 1（含已停用项），保证新建项排到最后
  const maxSort = rows.value.reduce((m, r) => Math.max(m, r.sort ?? 0), 0);
  Object.assign(form, { code: '', name: '', durationMinutes: 60, price: null, memberPrice: null, description: '', isActive: true, sort: maxSort + 1 });
  formOpen.value = true;
}

function openEdit(row: ServiceItem) {
  formMode.value = 'edit';
  editingId.value = row.id;
  Object.assign(form, {
    code: row.code,
    name: row.name,
    durationMinutes: row.durationMinutes,
    price: row.price,
    memberPrice: row.memberPrice,
    description: row.description ?? '',
    isActive: row.isActive,
    sort: row.sort ?? 0
  });
  formOpen.value = true;
}

async function save() {
  if (!formRef.value) return;
  const ok = await formRef.value.validate().catch(() => false);
  if (!ok) return;
  // 校验规则已保证原价/会员价必填为数字，这里收窄到非空 number 满足接口类型
  const { price, memberPrice } = form;
  if (price == null || memberPrice == null) return;
  const payload = { ...form, price, memberPrice };
  saving.value = true;
  try {
    if (formMode.value === 'create') {
      await servicesApi.create(payload);
    } else if (editingId.value != null) {
      await servicesApi.update(editingId.value, payload);
    }
    ElMessage.success('已保存');
    formOpen.value = false;
    reload();
  } finally {
    saving.value = false;
  }
}

async function toggle(row: ServiceItem) {
  const { code: _code, ...rest } = row;
  await servicesApi.update(row.id, { ...rest, isActive: !row.isActive });
  ElMessage.success(row.isActive ? '已停用' : '已启用');
  reload();
}

onMounted(reload);
</script>

<style scoped>
.page { padding-bottom: 24px; }
.toolbar { display: flex; gap: 12px; align-items: center; }
.muted { color: var(--el-text-color-secondary); font-size: 12px; }
</style>
