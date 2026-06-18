<script lang="ts">
export default { name: 'RoomsView' };
</script>

<script setup lang="ts">
import { computed, onMounted, reactive, ref } from 'vue';
import {
  NavBar as VanNavBar, PullRefresh as VanPullRefresh, Empty as VanEmpty, Tag as VanTag,
  Button as VanButton, Checkbox as VanCheckbox, Popup as VanPopup, Form as VanForm,
  Field as VanField, Stepper as VanStepper, Switch as VanSwitch, CellGroup as VanCellGroup,
  showSuccessToast, showToast, showConfirmDialog
} from 'vant';
import { roomsApi, timedRoomsApi, type TimedRoomSessionDto } from '@/api/modules';
import { useAppStore } from '@/stores/app';
import { useAuthStore } from '@/stores/auth';
import type { Room } from '@/api/types';

const appStore = useAppStore();
const auth = useAuthStore();

const rooms = ref<Room[]>([]);
const sessions = ref<TimedRoomSessionDto[]>([]);
const loading = ref(false);
const refreshing = ref(false);
const acting = ref(false);
const includeInactive = ref(false);

const formOpen = ref(false);
const formMode = ref<'create' | 'edit'>('create');
const editingId = ref<number | null>(null);
const saving = ref(false);
const form = reactive<{ roomNo: string; capacity: number; roomType: string; remark: string; isActive: boolean; isTimedRoom: boolean; hourlyRate: number }>({
  roomNo: '', capacity: 1, roomType: '', remark: '', isActive: true, isTimedRoom: false, hourlyRate: 0
});

const canManage = computed(() => auth.role === 'ShopOwner' || auth.role === 'StoreManager');

// 兼容历史数据：旧记录里的英文类型映射成中文展示
const ROOM_TYPE_LABELS: Record<string, string> = { standard: '标准间', vip: 'VIP', couple: '情侣间' };
function roomTypeLabel(t?: string | null): string {
  if (!t) return '';
  return ROOM_TYPE_LABELS[t.toLowerCase()] ?? t;
}

function fmt(n?: number | null): string {
  return (n ?? 0).toLocaleString('zh-CN', { minimumFractionDigits: 2, maximumFractionDigits: 2 });
}
// 进行中的计时 session（status === 'Open'），按房间索引
function openSession(roomId: number): TimedRoomSessionDto | undefined {
  return sessions.value.find((s) => s.roomId === roomId && s.status === 'Open');
}

async function load() {
  if (!appStore.activeStoreId) return;
  loading.value = true;
  try {
    const [rs, ss] = await Promise.all([
      roomsApi.list(appStore.activeStoreId, includeInactive.value),
      timedRoomsApi.sessions(appStore.activeStoreId).catch(() => [])
    ]);
    rooms.value = rs;
    sessions.value = ss;
  } catch {
    /* 拦截器已提示 */
  } finally {
    loading.value = false;
    refreshing.value = false;
  }
}

function openCreate() {
  formMode.value = 'create';
  editingId.value = null;
  Object.assign(form, { roomNo: '', capacity: 1, roomType: '', remark: '', isActive: true, isTimedRoom: false, hourlyRate: 0 });
  formOpen.value = true;
}
function openEdit(r: Room) {
  formMode.value = 'edit';
  editingId.value = r.id;
  Object.assign(form, {
    roomNo: r.roomNo, capacity: r.capacity,
    // 历史英文值在编辑时统一规整成中文，保存后自然清洗
    roomType: roomTypeLabel(r.roomType), remark: r.remark ?? '',
    isActive: r.isActive, isTimedRoom: r.isTimedRoom, hourlyRate: r.hourlyRate
  });
  formOpen.value = true;
}

async function save() {
  if (!form.roomNo.trim()) { showToast('房间号必填'); return; }
  if (form.isTimedRoom && form.hourlyRate <= 0) { showToast('计时房需设置大于 0 的小时单价'); return; }
  saving.value = true;
  try {
    const common = {
      roomNo: form.roomNo.trim(), capacity: form.capacity,
      roomType: form.roomType.trim() || null, remark: form.remark.trim() || null,
      isTimedRoom: form.isTimedRoom, hourlyRate: form.hourlyRate
    };
    if (formMode.value === 'create') {
      await roomsApi.create({ storeId: appStore.activeStoreId!, ...common });
    } else if (editingId.value != null) {
      await roomsApi.update(editingId.value, { ...common, isActive: form.isActive });
    }
    showSuccessToast('已保存');
    formOpen.value = false;
    await load();
  } catch { /* 拦截器已提示 */ } finally { saving.value = false; }
}

async function remove(r: Room) {
  try { await showConfirmDialog({ title: '删除房间', message: `确认删除房间 ${r.roomNo}？` }); }
  catch { return; }
  try { await roomsApi.remove(r.id); showSuccessToast('已删除'); await load(); } catch { /* */ }
}

async function cancelTiming(s: TimedRoomSessionDto) {
  try {
    await showConfirmDialog({ title: '取消计时', message: `取消 ${s.roomNo} 房的计时？该段不计费、不入账。` });
  } catch { return; }
  acting.value = true;
  try {
    await timedRoomsApi.cancel(s.id);
    showSuccessToast('已取消计时');
    await load();
  } catch { /* */ } finally { acting.value = false; }
}

onMounted(async () => {
  if (!appStore.stores.length) await appStore.loadStores().catch(() => undefined);
  load();
});
</script>

<template>
  <div class="qy-page rooms">
    <van-nav-bar title="房间" left-text="返回" left-arrow @click-left="$router.back()">
      <template v-if="canManage" #right><span class="nav-add" @click="openCreate">新建</span></template>
    </van-nav-bar>

    <div v-if="canManage" class="bar">
      <van-checkbox v-model="includeInactive" shape="square" @change="load">显示已停用</van-checkbox>
    </div>

    <van-pull-refresh v-model="refreshing" @refresh="load">
      <van-empty v-if="!loading && rooms.length === 0" description="暂无房间" />

      <div v-for="r in rooms" :key="r.id" class="room-item" :class="{ timing: !!openSession(r.id) }">
        <div class="ri-left">
          <div class="ri-no">
            {{ r.roomNo }} <span class="ri-type" v-if="roomTypeLabel(r.roomType)">{{ roomTypeLabel(r.roomType) }}</span>
          </div>
          <div class="ri-tags">
            <van-tag v-if="!r.isActive" type="default">已停用</van-tag>
            <van-tag v-if="r.isTimedRoom" type="primary" plain>计时房 ¥{{ fmt(r.hourlyRate) }}/时</van-tag>
            <van-tag v-if="openSession(r.id)" type="warning">计时中</van-tag>
          </div>
          <div v-if="openSession(r.id)" class="ri-session">
            已计时 {{ openSession(r.id)!.elapsedMinutes }} 分钟 ·
            客 {{ openSession(r.id)!.customerName || openSession(r.id)!.memberName || '散客' }} ·
            预计 ¥{{ fmt(openSession(r.id)!.amount) }}
          </div>
        </div>
        <div class="ri-right">
          <span class="ri-cap">{{ r.capacity }} 人</span>
          <div class="ri-ops">
            <van-button
              v-if="canManage && openSession(r.id)"
              size="mini" type="warning" plain :loading="acting"
              @click="cancelTiming(openSession(r.id)!)"
            >取消计时</van-button>
            <template v-if="canManage">
              <van-button size="mini" @click="openEdit(r)">编辑</van-button>
              <van-button size="mini" type="danger" plain :disabled="!!openSession(r.id)" @click="remove(r)">删除</van-button>
            </template>
          </div>
        </div>
      </div>

      <p v-if="rooms.length" class="tip">
        计时房的开台与结算在收银台办理；此处可维护房间并查看进行中的计时。
      </p>
    </van-pull-refresh>

    <van-popup v-model:show="formOpen" position="bottom" round :style="{ maxHeight: '90%' }">
      <div class="sheet">
        <div class="sheet-title">{{ formMode === 'create' ? '新建房间' : '编辑房间' }}</div>
        <van-form @submit="save">
          <van-cell-group inset>
            <van-field v-model="form.roomNo" label="房间号" placeholder="如 01 / VIP-1" />
            <van-field label="容客(人)"><template #input><van-stepper v-model="form.capacity" :min="1" :max="20" /></template></van-field>
            <van-field v-model="form.roomType" label="类型" placeholder="选填，如 标准间 / VIP" />
            <van-field label="计时房"><template #input><van-switch v-model="form.isTimedRoom" /></template></van-field>
            <van-field v-if="form.isTimedRoom" label="小时单价"><template #input><van-stepper v-model="form.hourlyRate" :min="0" :step="10" :decimal-length="2" /></template></van-field>
            <van-field v-model="form.remark" label="备注" type="textarea" rows="2" autosize placeholder="选填" />
            <van-field v-if="formMode === 'edit'" label="启用"><template #input><van-switch v-model="form.isActive" /></template></van-field>
          </van-cell-group>
          <div class="sheet-actions">
            <van-button block type="primary" native-type="submit" :loading="saving">保存</van-button>
          </div>
        </van-form>
      </div>
    </van-popup>
  </div>
</template>

<style scoped>
.nav-add { color: var(--qy-brand); font-size: 15px; }
.bar { padding: 10px 14px; }
.room-item {
  display: flex; align-items: flex-start; justify-content: space-between;
  background: #fff; margin: 8px 12px; padding: 14px; border-radius: 12px;
}
.room-item.timing { box-shadow: 0 0 0 2px #f0a020 inset; }
.ri-no { font-size: 16px; font-weight: 600; }
.ri-type { color: #98a2b3; font-size: 13px; font-weight: 400; margin-left: 4px; }
.ri-tags { display: flex; flex-wrap: wrap; gap: 6px; margin-top: 8px; }
.ri-session { margin-top: 8px; color: #b06a00; font-size: 13px; }
.ri-right { display: flex; flex-direction: column; align-items: flex-end; gap: 8px; }
.ri-cap { color: #b0b8c4; font-size: 13px; }
.ri-ops { display: flex; flex-wrap: wrap; gap: 6px; justify-content: flex-end; }
.tip { color: #b0b8c4; font-size: 12px; text-align: center; padding: 14px 24px 20px; line-height: 1.6; }
.sheet { padding: 16px 0 24px; }
.sheet-title { text-align: center; font-size: 17px; font-weight: 700; margin-bottom: 12px; }
.sheet-actions { padding: 16px 16px 0; }
</style>
