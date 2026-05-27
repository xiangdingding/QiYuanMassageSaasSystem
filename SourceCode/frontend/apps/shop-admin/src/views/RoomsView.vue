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
          <template #default="{ row }">
            <el-tag v-if="row.isTimedRoom" type="primary" size="small">计时房</el-tag>
            <span v-else>{{ roomTypeLabel(row.roomType) }}</span>
          </template>
        </el-table-column>
        <el-table-column prop="capacity" label="容客（人）" width="100" />
        <el-table-column label="计时单价" width="110">
          <template #default="{ row }">
            <span v-if="row.isTimedRoom">¥{{ row.hourlyRate.toFixed(2) }}/时</span>
            <span v-else>—</span>
          </template>
        </el-table-column>
        <el-table-column label="状态" width="120">
          <template #default="{ row }">
            <el-tag v-if="!row.isActive" type="info">已停用</el-tag>
            <el-tag v-else-if="row.isTimedRoom && timedOpen(row.id)" type="warning">计时中</el-tag>
            <el-tag v-else-if="row.isOccupied" type="warning">占用中</el-tag>
            <el-tag v-else type="success">可用</el-tag>
          </template>
        </el-table-column>
        <el-table-column label="占用 / 计时" min-width="200">
          <template #default="{ row }">
            <span v-if="row.isTimedRoom && timedOpen(row.id)">
              已计时 {{ timedOpen(row.id)!.elapsedMinutes }} 分钟 · 客 {{ timedOpen(row.id)!.customerName || timedOpen(row.id)!.memberName || '散客' }}
            </span>
            <span v-else>{{ row.occupiedByOrderNo || '—' }}</span>
          </template>
        </el-table-column>
        <el-table-column label="操作" width="240" fixed="right">
          <template #default="{ row }">
            <el-button v-if="canManage" size="small" :aria-label="`编辑 ${row.roomNo} 号房`" @click="openEdit(row)">编辑</el-button>
            <el-button
              v-if="row.isTimedRoom && timedOpen(row.id)"
              size="small"
              type="danger"
              :aria-label="`取消 ${row.roomNo} 号房当前计时（不计费）`"
              @click="cancelTiming(row, timedOpen(row.id)!)"
            >取消计时</el-button>
            <el-button v-if="canManage" size="small" type="danger" :disabled="row.isOccupied || !!timedOpen(row.id)"
                       @click="remove(row)">删除</el-button>
          </template>
        </el-table-column>
      </el-table>
    </el-card>

    <el-dialog v-model="formOpen" :title="form.id ? '编辑房间' : '新建房间'" width="460px">
      <el-form :model="form" label-width="110px" class="room-form">
        <el-form-item label="房间号" required>
          <el-input v-model="form.roomNo" maxlength="32" placeholder="如 01 / VIP-1" />
        </el-form-item>
        <el-form-item label="容客（人）" required>
          <el-input-number v-model="form.capacity" :min="1" :max="20" />
        </el-form-item>
        <el-form-item label="类型">
          <el-select v-model="form.roomType" placeholder="可选" clearable filterable allow-create style="width: 100%">
            <el-option label="标准间" value="标准间" />
            <el-option label="VIP" value="VIP" />
            <el-option label="情侣间" value="情侣间" />
          </el-select>
        </el-form-item>
        <el-form-item label="计时房">
          <el-switch v-model="form.isTimedRoom" />
          <span class="hint">按停留时长计费，而非按服务项目</span>
        </el-form-item>
        <el-form-item v-if="form.isTimedRoom" label="小时单价" required>
          <el-input-number v-model="form.hourlyRate" :min="0" :precision="2" :step="10" />
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
import { roomsApi, timedRoomsApi, type TimedRoomSessionDto } from '@/api/modules';
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
const form = reactive<{ id: number | null; roomNo: string; capacity: number; roomType: string | null; remark: string | null; isActive: boolean; isTimedRoom: boolean; hourlyRate: number }>({
  id: null, roomNo: '', capacity: 1, roomType: null, remark: null, isActive: true, isTimedRoom: false, hourlyRate: 0
});

/// 只读：本店当前进行中的计时 session，仅用于"计时中"标签和占用提示，
/// 实际开台/结束/收费已搬到 PosView 与技师小程序
const timedSessions = ref<TimedRoomSessionDto[]>([]);
function timedOpen(roomId: number): TimedRoomSessionDto | undefined {
  return timedSessions.value.find((s) => s.roomId === roomId && s.status === 'Open');
}

/// 兼容历史数据：旧记录里的英文类型映射成中文展示
const ROOM_TYPE_LABELS: Record<string, string> = {
  standard: '标准间',
  vip: 'VIP',
  couple: '情侣间'
};
function roomTypeLabel(t: string | null | undefined) {
  if (!t) return '—';
  return ROOM_TYPE_LABELS[t.toLowerCase()] ?? t;
}
function normalizeRoomType(t: string | null | undefined): string | null {
  if (!t) return null;
  return ROOM_TYPE_LABELS[t.toLowerCase()] ?? t;
}

const canManage = computed(() => auth.role === 'ShopOwner' || auth.role === 'StoreManager');
const activeStoreName = computed(
  () => appStore.stores.find((s) => s.id === appStore.activeStoreId)?.name ?? ''
);

async function reload() {
  if (!appStore.activeStoreId) return;
  loading.value = true;
  try {
    const [roomList, sessions] = await Promise.all([
      roomsApi.list(appStore.activeStoreId, includeInactive.value),
      timedRoomsApi.sessions(appStore.activeStoreId)
    ]);
    rows.value = roomList;
    timedSessions.value = sessions;
  } finally {
    loading.value = false;
  }
}

/// 取消一段进行中的计时（误开台 / 客人临时不消费）：标为 Cancelled，不计费、不入账。
async function cancelTiming(room: Room, session: TimedRoomSessionDto) {
  const ok = await ElMessageBox.confirm(
    `确认取消 ${room.roomNo} 号房当前计时？已计 ${session.elapsedMinutes} 分钟将被作废、不计费。`,
    '取消计时',
    { type: 'warning', confirmButtonText: '确认取消', cancelButtonText: '不取消' }
  ).then(() => true).catch(() => false);
  if (!ok) return;
  try {
    await timedRoomsApi.cancel(session.id);
    await reload();
    ElMessage.success(`${room.roomNo} 号房计时已取消`);
  } catch { /* http 已弹错 */ }
}

function openNew() {
  Object.assign(form, { id: null, roomNo: '', capacity: 1, roomType: null, remark: null, isActive: true, isTimedRoom: false, hourlyRate: 0 });
  formOpen.value = true;
}

function openEdit(row: Room) {
  Object.assign(form, {
    id: row.id, roomNo: row.roomNo, capacity: row.capacity,
    // 历史英文值在编辑时统一规整成中文，保存后自然清洗
    roomType: normalizeRoomType(row.roomType),
    remark: row.remark ?? null, isActive: row.isActive,
    isTimedRoom: row.isTimedRoom, hourlyRate: row.hourlyRate
  });
  formOpen.value = true;
}

async function save() {
  if (!form.roomNo.trim()) { ElMessage.warning('房间号必填'); return; }
  if (form.isTimedRoom && form.hourlyRate <= 0) { ElMessage.warning('计时房需设置大于 0 的小时单价'); return; }
  saving.value = true;
  try {
    if (form.id) {
      await roomsApi.update(form.id, {
        roomNo: form.roomNo.trim(), capacity: form.capacity,
        roomType: form.roomType, remark: form.remark, isActive: form.isActive,
        isTimedRoom: form.isTimedRoom, hourlyRate: form.hourlyRate
      });
    } else {
      await roomsApi.create({
        storeId: appStore.activeStoreId!,
        roomNo: form.roomNo.trim(), capacity: form.capacity,
        roomType: form.roomType, remark: form.remark,
        isTimedRoom: form.isTimedRoom, hourlyRate: form.hourlyRate
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
.hint { color: #999; margin-left: 8px; font-size: 12px; }
/* 表单标签不换行：容客（人）等较长中文标签在窄对话框里也保持单行 */
.room-form :deep(.el-form-item__label) { white-space: nowrap; }
</style>
