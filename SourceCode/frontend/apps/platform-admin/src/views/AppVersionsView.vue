<template>
  <div class="page">
    <el-card shadow="never">
      <div class="toolbar">
        <el-radio-group v-model="platformFilter" @change="reload">
          <el-radio-button label="">全部平台</el-radio-button>
          <el-radio-button label="Cs">CS 桌面端</el-radio-button>
          <el-radio-button label="Android">安卓移动端</el-radio-button>
        </el-radio-group>
        <el-button type="primary" @click="openCreate">发布新版本</el-button>
      </div>

      <el-alert
        type="info"
        :closable="false"
        show-icon
        title="客户端启动时自动检测：取对应平台「启用中、版本号最大」的一条为最新版。设置了「最低支持版本」且用户低于它时为强制更新。"
        style="margin-top: 12px"
      />

      <el-table :data="rows" v-loading="loading" stripe style="margin-top: 12px">
        <el-table-column label="平台" width="120">
          <template #default="{ row }">
            <el-tag :type="row.platform === 'Cs' ? 'primary' : 'success'">
              {{ row.platform === 'Cs' ? 'CS 桌面端' : '安卓移动端' }}
            </el-tag>
          </template>
        </el-table-column>
        <el-table-column prop="version" label="版本号" width="110" />
        <el-table-column label="最低支持版本" width="130">
          <template #default="{ row }">
            <span v-if="row.minSupportedVersion">
              {{ row.minSupportedVersion }}
              <el-tag type="danger" size="small" effect="plain">强制</el-tag>
            </span>
            <span v-else class="muted">—</span>
          </template>
        </el-table-column>
        <el-table-column label="下载地址" min-width="220">
          <template #default="{ row }">
            <el-link type="primary" :href="row.downloadUrl" target="_blank">{{ row.downloadUrl }}</el-link>
          </template>
        </el-table-column>
        <el-table-column label="大小" width="90">
          <template #default="{ row }">{{ row.fileSizeBytes ? fmtSize(row.fileSizeBytes) : '—' }}</template>
        </el-table-column>
        <el-table-column label="状态" width="90">
          <template #default="{ row }">
            <el-tag :type="row.isActive ? 'success' : 'info'">{{ row.isActive ? '启用' : '停用' }}</el-tag>
          </template>
        </el-table-column>
        <el-table-column prop="publishedByName" label="发布人" width="100">
          <template #default="{ row }">{{ row.publishedByName || '—' }}</template>
        </el-table-column>
        <el-table-column label="发布时间" width="160">
          <template #default="{ row }">{{ fmtTime(row.createdAt) }}</template>
        </el-table-column>
        <el-table-column label="操作" width="200" fixed="right">
          <template #default="{ row }">
            <el-button link type="primary" @click="openEdit(row)">编辑</el-button>
            <el-button link :type="row.isActive ? 'warning' : 'success'" @click="toggle(row)">
              {{ row.isActive ? '停用' : '启用' }}
            </el-button>
            <el-button link type="danger" @click="remove(row)">删除</el-button>
          </template>
        </el-table-column>
      </el-table>
    </el-card>

    <el-dialog v-model="dialogOpen" :title="dialogMode === 'create' ? '发布新版本' : '编辑版本'" width="560px">
      <el-form :model="form" :rules="rules" ref="formRef" label-width="120px">
        <el-form-item label="平台" prop="platform">
          <el-radio-group v-model="form.platform" :disabled="dialogMode === 'edit'">
            <el-radio-button label="Cs">CS 桌面端</el-radio-button>
            <el-radio-button label="Android">安卓移动端</el-radio-button>
          </el-radio-group>
        </el-form-item>
        <el-form-item label="版本号" prop="version">
          <el-input v-model="form.version" placeholder="如 1.2.0" />
        </el-form-item>
        <el-form-item label="最低支持版本">
          <el-input v-model="form.minSupportedVersion" placeholder="留空=不强制；低于此版本则强制更新" />
        </el-form-item>
        <el-form-item label="下载地址" prop="downloadUrl">
          <el-input
            v-model="form.downloadUrl"
            :placeholder="form.platform === 'Cs' ? 'setup.exe 完整下载链接' : 'apk 完整下载链接'"
          />
        </el-form-item>
        <el-form-item label="更新日志">
          <el-input v-model="form.changelog" type="textarea" :rows="4" placeholder="本次更新内容，向用户展示" />
        </el-form-item>
        <el-form-item label="安装包大小">
          <el-input-number v-model="form.fileSizeBytes" :min="0" :step="1048576" controls-position="right" />
          <span class="muted" style="margin-left: 8px">字节（选填，用于下载进度）</span>
        </el-form-item>
        <el-form-item label="SHA256">
          <el-input v-model="form.sha256" placeholder="选填，下载后完整性校验" />
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
import { ElMessage, ElMessageBox, type FormInstance, type FormRules } from 'element-plus';
import { appVersionsApi } from '@/api/modules';
import type { AppVersion, AppPlatform } from '@/api/types';

const rows = ref<AppVersion[]>([]);
const loading = ref(false);
const platformFilter = ref('');

const dialogOpen = ref(false);
const dialogMode = ref<'create' | 'edit'>('create');
const editingId = ref<number | null>(null);
const saving = ref(false);
const formRef = ref<FormInstance>();

const emptyForm = () => ({
  platform: 'Cs' as AppPlatform,
  version: '',
  minSupportedVersion: '',
  downloadUrl: '',
  changelog: '',
  fileSizeBytes: undefined as number | undefined,
  sha256: '',
  isActive: true
});
const form = reactive(emptyForm());

const rules: FormRules = {
  platform: [{ required: true, message: '请选择平台', trigger: 'change' }],
  version: [{ required: true, message: '请输入版本号', trigger: 'blur' }],
  downloadUrl: [{ required: true, message: '请输入下载地址', trigger: 'blur' }]
};

function fmtSize(bytes: number) {
  if (bytes >= 1024 * 1024) return (bytes / 1024 / 1024).toFixed(1) + ' MB';
  if (bytes >= 1024) return (bytes / 1024).toFixed(0) + ' KB';
  return bytes + ' B';
}
function fmtTime(s: string) {
  return s ? new Date(s).toLocaleString('zh-CN', { hour12: false }) : '';
}

async function reload() {
  loading.value = true;
  try {
    rows.value = await appVersionsApi.list(platformFilter.value || undefined);
  } finally {
    loading.value = false;
  }
}

function openCreate() {
  dialogMode.value = 'create';
  editingId.value = null;
  Object.assign(form, emptyForm());
  dialogOpen.value = true;
}

function openEdit(row: AppVersion) {
  dialogMode.value = 'edit';
  editingId.value = row.id;
  Object.assign(form, {
    platform: row.platform,
    version: row.version,
    minSupportedVersion: row.minSupportedVersion ?? '',
    downloadUrl: row.downloadUrl,
    changelog: row.changelog ?? '',
    fileSizeBytes: row.fileSizeBytes ?? undefined,
    sha256: row.sha256 ?? '',
    isActive: row.isActive
  });
  dialogOpen.value = true;
}

function payload() {
  return {
    version: form.version.trim(),
    minSupportedVersion: form.minSupportedVersion.trim() || null,
    downloadUrl: form.downloadUrl.trim(),
    changelog: form.changelog.trim() || null,
    fileSizeBytes: form.fileSizeBytes ?? null,
    sha256: form.sha256.trim() || null,
    isActive: form.isActive
  };
}

async function save() {
  if (!formRef.value) return;
  const ok = await formRef.value.validate().catch(() => false);
  if (!ok) return;
  saving.value = true;
  try {
    if (dialogMode.value === 'create') {
      await appVersionsApi.create({ platform: form.platform, ...payload() });
    } else if (editingId.value != null) {
      await appVersionsApi.update(editingId.value, payload());
    }
    ElMessage.success('已保存');
    dialogOpen.value = false;
    reload();
  } finally {
    saving.value = false;
  }
}

async function toggle(row: AppVersion) {
  await appVersionsApi.update(row.id, {
    version: row.version,
    minSupportedVersion: row.minSupportedVersion ?? null,
    downloadUrl: row.downloadUrl,
    changelog: row.changelog ?? null,
    fileSizeBytes: row.fileSizeBytes ?? null,
    sha256: row.sha256 ?? null,
    isActive: !row.isActive
  });
  ElMessage.success(row.isActive ? '已停用' : '已启用');
  reload();
}

async function remove(row: AppVersion) {
  // 用户取消时 confirm 会 reject，直接 return 不删除
  const confirmed = await ElMessageBox.confirm(
    `确定删除 ${row.platform} ${row.version} 这条版本记录吗？`,
    '删除确认',
    { type: 'warning' }
  ).then(() => true).catch(() => false);
  if (!confirmed) return;

  await appVersionsApi.remove(row.id);
  ElMessage.success('已删除');
  reload();
}

onMounted(reload);
</script>

<style scoped>
.page { padding-bottom: 24px; }
.toolbar { display: flex; gap: 12px; align-items: center; justify-content: space-between; }
.muted { color: var(--el-text-color-secondary); }
</style>
