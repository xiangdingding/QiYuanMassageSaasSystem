<template>
  <div class="page">
    <el-card shadow="never">
      <div class="toolbar">
        <span class="title">房间/床位 — {{ activeStoreName }}</span>
        <el-checkbox v-model="includeInactive" @change="reload">含已停用</el-checkbox>
        <div class="spacer" />
        <el-button v-if="canManage" type="primary" :icon="Plus" @click="openNew">新建房间</el-button>
        <el-button :icon="Refresh" @click="reload">刷新</el-button>
      </div>

      <el-table :data="rows" v-loading="loading" stripe style="margin-top: 12px">
        <el-table-column prop="roomNo" label="房间号" width="100" />
        <el-table-column prop="roomType" label="类型" width="120">
          <template #default="{ row }">{{ row.roomType || '—' }}</template>
        </el-table-column>
        <el-table-column prop="capacity" label="容量" width="80" />
        <el-table-column label="状态" width="100">
          <template #default="{ row }">
            <el-tag v-if="!row.isActive" type="info">已停用</el-tag>
            <el-tag v-else-if="row.isOccupied" type="warning">占用中</el-tag>
            <el-tag v-else type="success">可用</el-tag>
          </template>
        </el-table-column>
        <el-table-column label="占用订单" min-width="180">
          <template #default="{ row }">{{ row.occupiedByOrderNo || '—' }}</template>
        </el-table-column>
        <el-table-column prop="remark" label="备注" min-width="160" show-overflow-tooltip />
        <el-table-column v-if="canManage" label="操作" width="180" fixed="right">
          <template #default="{ row }">
            <el-button size="small" :aria-label="`编辑 ${row.roomNo} 号房`" @click="openEdit(row)">编辑</el-button>
            <el-button size="small" type="danger" :disabled="row.isOccupied"
                       :aria-label="`删除 ${row.roomNo} 号房${row.isOccupied ? '（占用中无法删除）' : ''}`"
                       @click="remove(row)">删除</el-button>
          </template>
        </el-table-column>
      </el-table>
    </el-card>

    <el-dialog v-model="formOpen" :title="form.id ? '编辑房间' : '新建房间'" width="420px">
      <el-form :model="form" label-width="80px">
        <el-form-item label="房间号" required>
          <el-input v-model="form.roomNo" maxlength="32" placeholder="如 01 / VIP-1" />
        </el-form-item>
        <el-form-item label="容量" required>
          <el-input-number v-model="form.capacity" :min="1" :max="20" />
        </el-form-item>
        <el-form-item label="类型">
          <el-select v-model="form.roomType" placeholder="可选" clearable filterable allow-create style="width: 100%">
            <el-option label="标准间" value="standard" />
            <el-option label="VIP" value="vip" />
            <el-option label="情侣间" value="couple" />
          </el-select>
        </el-form-item>
        <el-form-item label="备注">
          <el-input v-model="form.remark" type="textarea" :rows="2" maxlength="500" />
        </el-form-item>
        <el-form-item v-if="form.id" label="启用">
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
import { computed, onMounted, reactive, ref, watch } from 'vue';
import { ElMessage, ElMessageBox } from 'element-plus';
import { Plus, Refresh } from '@element-plus/icons-vue';
import { roomsApi } from '@/api/modules';
import { useAppStore } from '@/stores/app';
import { useAuthStore } from '@/stores/auth';
import type { Room } from '@/api/types';

const appStore = useAppStore();
const auth = useAuthStore();

const rows = ref<Room[]>([]);
const loading = ref(false);
const includeInactive = ref(false);
const formOpen = ref(false);
const saving = ref(false);
const form = reactive<{ id: number | null; roomNo: string; capacity: number; roomType: string | null; remark: string | null; isActive: boolean }>({
  id: null, roomNo: '', capacity: 1, roomType: null, remark: null, isActive: true
});

const canManage = computed(() => auth.role === 'ShopOwner' || auth.role === 'StoreManager');
const activeStoreName = computed(
  () => appStore.stores.find((s) => s.id === appStore.activeStoreId)?.name ?? ''
);

async function reload() {
  if (!appStore.activeStoreId) return;
  loading.value = true;
  try {
    rows.value = await roomsApi.list(appStore.activeStoreId, includeInactive.value);
  } finally {
    loading.value = false;
  }
}

function openNew() {
  Object.assign(form, { id: null, roomNo: '', capacity: 1, roomType: null, remark: null, isActive: true });
  formOpen.value = true;
}

function openEdit(row: Room) {
  Object.assign(form, {
    id: row.id, roomNo: row.roomNo, capacity: row.capacity,
    roomType: row.roomType ?? null, remark: row.remark ?? null, isActive: row.isActive
  });
  formOpen.value = true;
}

async function save() {
  if (!form.roomNo.trim()) { ElMessage.warning('房间号必填'); return; }
  saving.value = true;
  try {
    if (form.id) {
      await roomsApi.update(form.id, {
        roomNo: form.roomNo.trim(), capacity: form.capacity,
        roomType: form.roomType, remark: form.remark, isActive: form.isActive
      });
    } else {
      await roomsApi.create({
        storeId: appStore.activeStoreId!,
        roomNo: form.roomNo.trim(), capacity: form.capacity,
        roomType: form.roomType, remark: form.remark
      });
    }
    formOpen.value = false;
    ElMessage.success('已保存');
    await reload();
  } catch {
    /* http 已弹错 */
  } finally {
    saving.value = false;
  }
}

async function remove(row: Room) {
  await ElMessageBox.confirm(`确认删除房间 ${row.roomNo}？`, '提示', { type: 'warning' }).catch(() => null);
  await roomsApi.remove(row.id);
  ElMessage.success('已删除');
  await reload();
}

watch(() => appStore.activeStoreId, () => reload());
onMounted(async () => {
  await appStore.loadStores();
  await reload();
});
</script>

<style scoped>
.page { padding-bottom: 24px; }
.toolbar { display: flex; gap: 12px; align-items: center; }
.toolbar .title { font-weight: 600; font-size: 16px; }
.spacer { flex: 1; }
</style>
