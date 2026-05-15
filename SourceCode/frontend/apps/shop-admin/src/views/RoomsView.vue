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
            <span v-else>{{ row.roomType || '—' }}</span>
          </template>
        </el-table-column>
        <el-table-column prop="capacity" label="容量" width="70" />
        <el-table-column label="计时单价" width="100">
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
        <el-table-column label="操作" width="260" fixed="right">
          <template #default="{ row }">
            <template v-if="row.isTimedRoom && row.isActive">
              <el-button v-if="!timedOpen(row.id)" size="small" type="success" @click="openStart(row)">开始计时</el-button>
              <el-button v-else size="small" type="warning" @click="openStop(row)">结束计时</el-button>
            </template>
            <el-button v-if="canManage" size="small" :aria-label="`编辑 ${row.roomNo} 号房`" @click="openEdit(row)">编辑</el-button>
            <el-button v-if="canManage" size="small" type="danger" :disabled="row.isOccupied || !!timedOpen(row.id)"
                       @click="remove(row)">删除</el-button>
          </template>
        </el-table-column>
      </el-table>
    </el-card>

    <el-card v-if="timedSessions.length" shadow="never" style="margin-top:12px">
      <template #header><span>今日计时房记录</span></template>
      <el-table :data="timedSessions" size="small" stripe>
        <el-table-column prop="roomNo" label="房间" width="90" />
        <el-table-column label="客户" width="120">
          <template #default="{ row }">{{ row.customerName || row.memberName || '散客' }}</template>
        </el-table-column>
        <el-table-column label="时长" width="100">
          <template #default="{ row }">{{ row.billedMinutes || row.elapsedMinutes }} 分钟</template>
        </el-table-column>
        <el-table-column label="金额" width="100">
          <template #default="{ row }">¥{{ row.amount.toFixed(2) }}</template>
        </el-table-column>
        <el-table-column label="支付" width="90">
          <template #default="{ row }">{{ payLabel(row.payMethod) }}</template>
        </el-table-column>
        <el-table-column label="状态" width="100">
          <template #default="{ row }">
            <el-tag size="small" :type="row.status === 'Open' ? 'warning' : row.status === 'Settled' ? 'success' : 'info'">
              {{ timedStatusLabel(row.status) }}
            </el-tag>
          </template>
        </el-table-column>
        <el-table-column label="操作" width="90" fixed="right">
          <template #default="{ row }">
            <el-button v-if="row.status === 'Open'" link type="danger" size="small" @click="cancelSession(row)">作废</el-button>
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

    <el-dialog v-model="startOpen" :title="`开始计时：${startTarget?.roomNo} 号房`" width="400px">
      <el-form label-width="90px">
        <el-form-item label="单价">¥{{ startTarget?.hourlyRate.toFixed(2) }} / 小时</el-form-item>
        <el-form-item label="客户姓名">
          <el-input v-model="startForm.customerName" placeholder="散客可填，会员另选" maxlength="64" />
        </el-form-item>
        <el-form-item label="备注">
          <el-input v-model="startForm.remark" maxlength="200" />
        </el-form-item>
      </el-form>
      <template #footer>
        <el-button @click="startOpen = false">取消</el-button>
        <el-button type="primary" :loading="saving" @click="doStart">开始计时</el-button>
      </template>
    </el-dialog>

    <el-dialog v-model="stopOpen" :title="`结束计时：${stopTarget?.roomNo} 号房`" width="400px">
      <el-form label-width="90px">
        <el-form-item label="已计时">{{ stopSession?.elapsedMinutes }} 分钟</el-form-item>
        <el-form-item label="预估金额">
          ¥{{ estimatedAmount.toFixed(2) }}
          <span class="hint">按结算时实际时长为准</span>
        </el-form-item>
        <el-form-item label="支付方式" required>
          <el-radio-group v-model="stopPayMethod">
            <el-radio-button value="Cash">现金</el-radio-button>
            <el-radio-button value="Wechat">微信</el-radio-button>
            <el-radio-button value="Alipay">支付宝</el-radio-button>
            <el-radio-button value="MemberCard">会员卡</el-radio-button>
            <el-radio-button value="BankCard">银行卡</el-radio-button>
          </el-radio-group>
        </el-form-item>
      </el-form>
      <template #footer>
        <el-button @click="stopOpen = false">取消</el-button>
        <el-button type="primary" :loading="saving" @click="doStop">结算</el-button>
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

const timedSessions = ref<TimedRoomSessionDto[]>([]);
function timedOpen(roomId: number): TimedRoomSessionDto | undefined {
  return timedSessions.value.find((s) => s.roomId === roomId && s.status === 'Open');
}
function timedStatusLabel(s: string) {
  return ({ Open: '计时中', Settled: '已结算', Cancelled: '已作废' } as Record<string, string>)[s] ?? s;
}
function payLabel(p: string) {
  return ({ Cash: '现金', Wechat: '微信', Alipay: '支付宝', MemberCard: '会员卡', BankCard: '银行卡', Unpaid: '未付' } as Record<string, string>)[p] ?? p;
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

function openNew() {
  Object.assign(form, { id: null, roomNo: '', capacity: 1, roomType: null, remark: null, isActive: true, isTimedRoom: false, hourlyRate: 0 });
  formOpen.value = true;
}

function openEdit(row: Room) {
  Object.assign(form, {
    id: row.id, roomNo: row.roomNo, capacity: row.capacity,
    roomType: row.roomType ?? null, remark: row.remark ?? null, isActive: row.isActive,
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

// ---- 计时房操作 ----
const startOpen = ref(false);
const startTarget = ref<Room | null>(null);
const startForm = reactive({ customerName: '', remark: '' });

function openStart(row: Room) {
  startTarget.value = row;
  startForm.customerName = '';
  startForm.remark = '';
  startOpen.value = true;
}

async function doStart() {
  if (!startTarget.value) return;
  saving.value = true;
  try {
    await timedRoomsApi.start(startTarget.value.id, {
      customerName: startForm.customerName.trim() || null,
      remark: startForm.remark.trim() || null
    });
    startOpen.value = false;
    ElMessage.success('已开始计时');
    await reload();
  } finally {
    saving.value = false;
  }
}

const stopOpen = ref(false);
const stopTarget = ref<Room | null>(null);
const stopSession = ref<TimedRoomSessionDto | null>(null);
const stopPayMethod = ref('Cash');
const estimatedAmount = computed(() => {
  if (!stopSession.value) return 0;
  return (stopSession.value.elapsedMinutes / 60) * stopSession.value.hourlyRateSnapshot;
});

function openStop(row: Room) {
  const s = timedOpen(row.id);
  if (!s) return;
  stopTarget.value = row;
  stopSession.value = s;
  stopPayMethod.value = 'Cash';
  stopOpen.value = true;
}

async function doStop() {
  if (!stopSession.value) return;
  saving.value = true;
  try {
    const settled = await timedRoomsApi.stop(stopSession.value.id, stopPayMethod.value);
    stopOpen.value = false;
    ElMessage.success(`已结算 ¥${settled.amount.toFixed(2)}`);
    await reload();
  } finally {
    saving.value = false;
  }
}

async function cancelSession(row: TimedRoomSessionDto) {
  await ElMessageBox.confirm(`确认作废 ${row.roomNo} 号房的计时记录？`, '提示', { type: 'warning' }).catch(() => null);
  await timedRoomsApi.cancel(row.id);
  ElMessage.success('已作废');
  await reload();
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
</style>
