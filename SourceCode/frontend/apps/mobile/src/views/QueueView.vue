<script lang="ts">
export default { name: 'QueueView' };
</script>

<script setup lang="ts">
import { computed, onActivated, onMounted, ref } from 'vue';
import {
  PullRefresh as VanPullRefresh, Empty as VanEmpty, Tag as VanTag, Button as VanButton,
  ActionSheet as VanActionSheet, showSuccessToast, showConfirmDialog
} from 'vant';
import { queueApi } from '@/api/modules';
import { useAppStore } from '@/stores/app';
import { useAuthStore } from '@/stores/auth';
import { QUEUE_STATE_LABELS, type QueueRow } from '@/api/types';

const appStore = useAppStore();
const auth = useAuthStore();

const rows = ref<QueueRow[]>([]);
const refreshing = ref(false);
const loading = ref(false);
const calling = ref(false);

const canManage = computed(() => auth.role !== 'Technician'); // 店主/店长/收银员可管理排队
const myTechId = computed(() => auth.user?.id ?? null);

const sheetShow = ref(false);
const sheetTarget = ref<QueueRow | null>(null);
const sheetActions = [
  { name: '上钟', state: 'OnDuty' },
  { name: '休息', state: 'Resting' },
  { name: '下班', state: 'OffDuty' },
  { name: '置空闲', state: 'Idle' }
];

function stateTag(s: QueueRow['state']) {
  switch (s) {
    case 'OnDuty': return 'success';
    case 'Resting': return 'warning';
    case 'OffDuty': return 'default';
    default: return 'primary';
  }
}

async function load() {
  if (!appStore.activeStoreId) return;
  loading.value = true;
  try {
    rows.value = await queueApi.list(appStore.activeStoreId);
  } catch {
    /* ignore */
  } finally {
    loading.value = false;
    refreshing.value = false;
  }
}

async function callNext() {
  if (!appStore.activeStoreId) return;
  calling.value = true;
  try {
    const r = await queueApi.callNext(appStore.activeStoreId);
    if (r.technicianId) showSuccessToast(`叫到：${r.technicianName}（第 ${r.position} 位）`);
    else showSuccessToast('当前无可叫技师');
    await load();
  } catch {
    /* ignore */
  } finally {
    calling.value = false;
  }
}

function onRowClick(row: QueueRow) {
  if (canManage.value) {
    sheetTarget.value = row;
    sheetShow.value = true;
  } else if (row.technicianId === myTechId.value) {
    // 技师点自己这行：自助改状态
    sheetTarget.value = row;
    sheetShow.value = true;
  }
}

async function onSelectState(action: { state: string }) {
  const row = sheetTarget.value;
  sheetShow.value = false;
  if (!row) return;
  try {
    if (canManage.value) {
      await queueApi.setState(row.technicianId, action.state);
    } else {
      await queueApi.setMyState(action.state);
    }
    showSuccessToast('已更新');
    await load();
  } catch {
    /* ignore */
  }
}

async function resetDay() {
  if (!appStore.activeStoreId) return;
  try {
    await showConfirmDialog({ title: '重置当日排队', message: '将清空今日接钟计数与排位，确认？' });
  } catch {
    return; // 取消
  }
  try {
    await queueApi.resetDay(appStore.activeStoreId);
    showSuccessToast('已重置');
    await load();
  } catch {
    /* ignore */
  }
}

onMounted(async () => {
  if (!appStore.stores.length) await appStore.loadStores().catch(() => undefined);
  load();
});
onActivated(load);
</script>

<template>
  <div class="qy-page queue">
    <div class="queue-bar">
      <div class="qb-store">{{ appStore.activeStore?.name || '门店' }} · 技师排队</div>
      <div v-if="canManage" class="qb-actions">
        <van-button size="small" plain @click="resetDay">重置</van-button>
        <van-button size="small" type="primary" :loading="calling" @click="callNext">叫下一钟</van-button>
      </div>
    </div>

    <van-pull-refresh v-model="refreshing" @refresh="load">
      <van-empty v-if="!loading && rows.length === 0" description="暂无排队技师" />
      <div
        v-for="(r, i) in rows"
        :key="r.id"
        class="q-item"
        :class="{ me: r.technicianId === myTechId }"
        @click="onRowClick(r)"
      >
        <div class="q-pos">{{ r.queuePosition || (i + 1) }}</div>
        <div class="q-mid">
          <div class="q-name">
            {{ r.technicianName }}
            <span v-if="r.employeeNo" class="q-no">#{{ r.employeeNo }}</span>
            <van-tag v-if="r.technicianId === myTechId" type="primary">我</van-tag>
          </div>
          <div class="q-meta">今日 {{ r.todayRoundCount }} 钟</div>
        </div>
        <van-tag :type="stateTag(r.state)" size="large">{{ QUEUE_STATE_LABELS[r.state] }}</van-tag>
      </div>
    </van-pull-refresh>

    <van-action-sheet
      v-model:show="sheetShow"
      :actions="sheetActions"
      :title="sheetTarget ? `设置「${sheetTarget.technicianName}」状态` : '设置状态'"
      cancel-text="取消"
      close-on-click-action
      @select="onSelectState"
    />
  </div>
</template>

<style scoped>
.queue-bar {
  position: sticky; top: 0; z-index: 2; background: #fff;
  display: flex; align-items: center; justify-content: space-between;
  padding: 12px 14px; border-bottom: 1px solid #eef1f4;
}
.qb-store { font-size: 15px; font-weight: 600; }
.qb-actions { display: flex; gap: 8px; }
.q-item {
  display: flex; align-items: center; gap: 12px;
  background: #fff; margin: 8px 12px; padding: 14px; border-radius: 12px;
}
.q-item.me { box-shadow: 0 0 0 2px var(--qy-brand) inset; }
.q-pos {
  width: 30px; height: 30px; border-radius: 50%; background: #eef6f4; color: var(--qy-brand);
  display: flex; align-items: center; justify-content: center; font-weight: 700; font-size: 14px;
}
.q-mid { flex: 1; }
.q-name { font-size: 16px; font-weight: 600; display: flex; align-items: center; gap: 6px; }
.q-no { color: #b0b8c4; font-size: 13px; font-weight: 400; }
.q-meta { margin-top: 4px; color: #98a2b3; font-size: 13px; }
</style>
