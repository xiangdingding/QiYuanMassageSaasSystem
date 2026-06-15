<template>
  <div class="page">
    <el-card shadow="never">
      <div class="toolbar">
        <span class="title">总店与分店管理</span>
        <div class="spacer" />
        <el-button type="primary" @click="openCreate">新建分店</el-button>
        <el-button :icon="Refresh" @click="reload">刷新</el-button>
      </div>
      <div class="table-wrap">
      <el-table :data="rows" v-loading="loading" stripe height="100%">
        <el-table-column prop="name" label="名称" min-width="180">
          <template #default="{ row }">
            {{ row.name }}
            <el-tag v-if="row.isHeadquarters" size="small" type="warning">总店</el-tag>
          </template>
        </el-table-column>
        <el-table-column prop="address" label="地址" min-width="200" />
        <el-table-column prop="phone" label="电话" width="140" />
        <el-table-column label="营业日切日" width="120">
          <template #default="{ row }">{{ formatCutoff(row.dayCloseCutoffMinutes) }}</template>
        </el-table-column>
        <el-table-column label="状态" width="100">
          <template #default="{ row }">
            <el-tag :type="row.isActive ? 'success' : 'info'">{{ row.isActive ? '营业中' : '已停' }}</el-tag>
          </template>
        </el-table-column>
        <el-table-column label="操作" :width="$actCol(120)">
          <template #default="{ row }">
            <el-button link type="primary" @click="openEdit(row)">编辑</el-button>
          </template>
        </el-table-column>
      </el-table>
      </div>
    </el-card>

    <el-dialog v-model="formOpen" :title="formMode === 'create' ? '新建分店' : '编辑门店'" width="480px">
      <el-form :model="form" :rules="rules" ref="formRef" label-width="100px">
        <el-form-item label="名称" prop="name">
          <el-input v-model="form.name" />
        </el-form-item>
        <el-form-item label="地址">
          <el-input v-model="form.address" />
        </el-form-item>
        <el-form-item label="电话">
          <el-input v-model="form.phone" />
        </el-form-item>
        <el-form-item v-if="formMode === 'create'" label="所属总店">
          <el-select v-model="form.parentStoreId" placeholder="选择上级总店" style="width: 100%">
            <el-option v-for="s in headquarters" :key="s.id" :label="s.name" :value="s.id" />
          </el-select>
        </el-form-item>
        <el-form-item v-if="formMode === 'edit'" label="状态">
          <el-switch v-model="form.isActive" active-text="营业中" inactive-text="停业" />
        </el-form-item>
        <el-form-item label="营业日切日">
          <el-time-picker
            v-model="cutoffPickerValue"
            format="HH:mm"
            value-format="HH:mm"
            placeholder="00:00 = 自然日切日"
            style="width: 100%"
            :clearable="false"
          />
          <div class="hint">
            北京时间。营业到凌晨的门店请把切日点设在停业后（如 06:00），
            该时刻之前完成的订单仍归到前一个营业日。
          </div>
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
import { ElMessage, type FormInstance, type FormRules } from 'element-plus';
import { Refresh } from '@element-plus/icons-vue';
import { storesApi } from '@/api/modules';
import { useAppStore } from '@/stores/app';
import type { Store } from '@/api/types';

const appStore = useAppStore();
const rows = ref<Store[]>([]);
const loading = ref(false);
const saving = ref(false);

const formOpen = ref(false);
const formMode = ref<'create' | 'edit'>('create');
const editingId = ref<number | null>(null);
const formRef = ref<FormInstance>();
const form = reactive({
  name: '',
  address: '',
  phone: '',
  parentStoreId: null as number | null,
  isActive: true,
  dayCloseCutoffMinutes: 0
});

const cutoffPickerValue = computed<string>({
  get: () => minutesToHHmm(form.dayCloseCutoffMinutes),
  set: (v: string) => { form.dayCloseCutoffMinutes = hhmmToMinutes(v); }
});

function minutesToHHmm(m: number): string {
  const safe = Math.max(0, Math.min(1439, m || 0));
  const h = Math.floor(safe / 60).toString().padStart(2, '0');
  const mm = (safe % 60).toString().padStart(2, '0');
  return `${h}:${mm}`;
}
function hhmmToMinutes(s: string): number {
  if (!s) return 0;
  const [h, m] = s.split(':').map(Number);
  return (h || 0) * 60 + (m || 0);
}
function formatCutoff(m: number): string {
  return m > 0 ? `${minutesToHHmm(m)} 切日` : '自然日';
}
const rules: FormRules = {
  name: [{ required: true, message: '请输入名称', trigger: 'blur' }]
};

const headquarters = computed(() => rows.value.filter((s) => s.isHeadquarters));

async function reload() {
  loading.value = true;
  try {
    rows.value = await storesApi.list();
  } finally {
    loading.value = false;
  }
}

function openCreate() {
  formMode.value = 'create';
  editingId.value = null;
  Object.assign(form, {
    name: '', address: '', phone: '',
    parentStoreId: headquarters.value[0]?.id ?? null,
    isActive: true,
    dayCloseCutoffMinutes: 0
  });
  formOpen.value = true;
}

function openEdit(row: Store) {
  formMode.value = 'edit';
  editingId.value = row.id;
  Object.assign(form, {
    name: row.name,
    address: row.address ?? '',
    phone: row.phone ?? '',
    parentStoreId: row.parentStoreId ?? null,
    isActive: row.isActive,
    dayCloseCutoffMinutes: row.dayCloseCutoffMinutes ?? 0
  });
  formOpen.value = true;
}

async function save() {
  if (!formRef.value) return;
  const ok = await formRef.value.validate().catch(() => false);
  if (!ok) return;
  saving.value = true;
  try {
    if (formMode.value === 'create') {
      await storesApi.create({
        name: form.name,
        address: form.address || null,
        phone: form.phone || null,
        parentStoreId: form.parentStoreId,
        dayCloseCutoffMinutes: form.dayCloseCutoffMinutes
      });
    } else if (editingId.value != null) {
      await storesApi.update(editingId.value, {
        name: form.name,
        address: form.address || null,
        phone: form.phone || null,
        isActive: form.isActive,
        dayCloseCutoffMinutes: form.dayCloseCutoffMinutes
      });
    }
    ElMessage.success('已保存');
    formOpen.value = false;
    await appStore.loadStores(true);
    reload();
  } finally {
    saving.value = false;
  }
}

onMounted(async () => {
  await appStore.loadStores();
  reload();
});
</script>

<style scoped>
.page { padding-bottom: 24px; }
.toolbar { display: flex; gap: 12px; align-items: center; }
.title { font-weight: 600; font-size: 16px; }
.spacer { flex: 1; }
.hint { color: #909399; font-size: 12px; margin-top: 4px; line-height: 1.5; }
</style>
