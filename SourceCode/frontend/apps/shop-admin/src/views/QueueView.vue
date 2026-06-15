<template>
  <div class="page">
    <el-card shadow="never">
      <div class="toolbar">
        <span class="title">技师排队看板</span>
        <el-tag size="large">在岗 {{ counts.OnDuty }}</el-tag>
        <el-tag size="large" type="warning">休息 {{ counts.Resting }}</el-tag>
        <el-tag size="large" type="info">下班 {{ counts.OffDuty }}</el-tag>
        <div class="spacer" />
        <el-button v-if="canManage" type="primary" :icon="Bell" aria-label="叫下一个排队技师" @click="callNext">叫号</el-button>
        <el-button v-if="canManage" :icon="Refresh" aria-label="重置今日轮次计数" @click="resetDay">重置今日轮次</el-button>
      </div>

      <div v-if="lastCalled" class="called">
        刚叫到：<strong>{{ lastCalled.employeeNo ?? '-' }} 号 · {{ lastCalled.technicianName }}</strong>
      </div>

      <!-- 技师自助：我的班次（无前台管理权限的技师专用） -->
      <div v-if="!canManage && my" class="my-shift" role="group" aria-label="我的班次">
        <div class="my-head">
          <span>我的状态：</span>
          <el-tag size="large" :type="stateType(my.state)">{{ stateLabel(my.state) }}</el-tag>
          <span class="my-meta">今日 {{ my.todayRoundCount }} 钟 · 排队号 {{ my.queuePosition || '—' }}</span>
        </div>
        <div class="my-actions">
          <el-button
            type="primary"
            :disabled="my.state === 'OnDuty'"
            aria-label="我要上钟"
            @click="setMyState('OnDuty')"
          >上钟</el-button>
          <el-button
            type="warning"
            :disabled="my.state === 'Resting' || my.state === 'OffDuty'"
            aria-label="我要休息"
            @click="setMyState('Resting')"
          >休息</el-button>
          <el-button
            class="off-btn"
            :disabled="my.state === 'OffDuty'"
            aria-label="我要下班"
            @click="setMyState('OffDuty')"
          >下班</el-button>
        </div>
        <div v-if="my.currentRoomNo || my.currentServiceName" class="my-current">
          当前上钟：<strong>{{ my.currentRoomNo ? my.currentRoomNo + ' 房 ' : '' }}{{ my.currentServiceName }}</strong>
          <span v-if="my.currentCustomerName"> · 客户 {{ my.currentCustomerName }}</span>
          <div v-if="my.currentCustomerHasNotes" class="my-notes" role="alert">
            上钟前必读 —
            <span v-if="my.currentCustomerPreferences">偏好：{{ my.currentCustomerPreferences }}；</span>
            <span v-if="my.currentCustomerHealth">健康：{{ my.currentCustomerHealth }}</span>
          </div>
        </div>
      </div>

      <div class="table-wrap">
      <el-table :data="rows" v-loading="loading" stripe height="100%">
        <el-table-column prop="employeeNo" label="工号" width="80" />
        <el-table-column prop="technicianName" label="姓名" min-width="120" />
        <el-table-column label="状态" width="120">
          <template #default="{ row }">
            <el-tag :type="stateType(row.state)">{{ stateLabel(row.state) }}</el-tag>
          </template>
        </el-table-column>
        <el-table-column prop="queuePosition" label="排队号" width="80" />
        <el-table-column prop="todayRoundCount" label="今日轮次" width="100" />
        <el-table-column label="进入时间" width="160">
          <template #default="{ row }">{{ row.enteredAt ? dayjs(row.enteredAt).format('HH:mm') : '—' }}</template>
        </el-table-column>
        <el-table-column label="最近叫号" width="160">
          <template #default="{ row }">{{ row.lastCalledAt ? dayjs(row.lastCalledAt).format('HH:mm') : '—' }}</template>
        </el-table-column>
        <el-table-column v-if="canManage" label="操作" :width="$actCol(320)" fixed="right">
          <template #default="{ row }">
            <el-button
              size="small"
              :type="row.state === 'OnDuty' ? '' : 'primary'"
              :disabled="row.state === 'OnDuty'"
              :aria-label="`将技师 ${row.technicianName} 设为上钟`"
              @click="setState(row.technicianId, 'OnDuty')"
            >上钟</el-button>
            <el-button
              size="small"
              :type="row.state === 'Resting' ? '' : 'warning'"
              :disabled="row.state === 'Resting' || row.state === 'OffDuty'"
              :aria-label="`将技师 ${row.technicianName} 设为休息`"
              @click="setState(row.technicianId, 'Resting')"
            >休息</el-button>
            <el-button
              size="small"
              :type="row.state === 'OffDuty' ? '' : 'info'"
              :disabled="row.state === 'OffDuty'"
              :aria-label="`将技师 ${row.technicianName} 设为下班`"
              @click="setState(row.technicianId, 'OffDuty')"
            >下班</el-button>
          </template>
        </el-table-column>
      </el-table>
      </div>
    </el-card>
  </div>
</template>

<script setup lang="ts">
import { computed, onMounted, onUnmounted, ref } from 'vue';
import { ElMessage, ElMessageBox } from 'element-plus';
import { Bell, Refresh } from '@element-plus/icons-vue';
import dayjs from 'dayjs';
import { queueApi, staffApi } from '@/api/modules';
import { useAppStore } from '@/stores/app';
import { useAuthStore } from '@/stores/auth';
import { useAnnouncer } from '@/composables/useAnnouncer';
import type { MyQueue, QueueRow, Staff } from '@/api/types';

const appStore = useAppStore();
const auth = useAuthStore();
const announcer = useAnnouncer();

const rows = ref<QueueRow[]>([]);
const my = ref<MyQueue | null>(null);
const loading = ref(false);
const lastCalled = ref<{ technicianName: string; employeeNo: number | null } | null>(null);

const canManage = computed(() => auth.role !== 'Technician');

const counts = computed(() => {
  const c = { OnDuty: 0, Resting: 0, OffDuty: 0, Idle: 0 };
  rows.value.forEach((r) => { c[r.state] = (c[r.state] || 0) + 1; });
  return c;
});

function stateLabel(s: string) {
  return ({ OnDuty: '在岗', Resting: '休息', OffDuty: '下班', Idle: '空闲' } as Record<string, string>)[s] ?? s;
}
function stateType(s: string) {
  return ({ OnDuty: 'success', Resting: 'warning', OffDuty: 'info', Idle: '' } as Record<string, any>)[s] ?? '';
}

async function reload() {
  if (!appStore.activeStoreId) return;
  loading.value = true;
  try {
    const data = await queueApi.list(appStore.activeStoreId);
    if (canManage.value) {
      // 仅前台需要补全未上钟技师的占位行（便于把他们设上钟）；
      // 查花名册要 ShopStaff 权限，技师没有，技师只看只读队列即可。
      const staff = await staffApi.list({ role: 'Technician', storeId: appStore.activeStoreId, pageSize: 200 });
      const placeholders: QueueRow[] = staff.items
        .filter((t: Staff) => !data.find((q) => q.technicianId === t.id))
        .map((t: Staff) => ({
          id: 0,
          technicianId: t.id,
          technicianName: t.realName ?? t.username,
          employeeNo: t.employeeNo ?? null,
          state: 'OffDuty',
          queuePosition: 0,
          todayRoundCount: 0,
          enteredAt: null,
          lastCalledAt: null
        }));
      rows.value = [...data, ...placeholders];
    } else {
      rows.value = data;
      my.value = await queueApi.me();
    }
  } finally {
    loading.value = false;
  }
}

async function setState(technicianId: number, state: string) {
  await queueApi.setState(technicianId, state);
  await reload();
}

// 技师自助上钟/休息/下班
async function setMyState(state: string) {
  await queueApi.setMyState(state);
  announcer.speak(state === 'OnDuty' ? '已上钟' : state === 'Resting' ? '已休息' : '已下班');
  await reload();
}

async function callNext() {
  if (!appStore.activeStoreId) return;
  const result = await queueApi.callNext(appStore.activeStoreId);
  if (!result.technicianId) {
    ElMessage.warning('没有在岗的技师');
    announcer.speak('目前无在岗技师');
    lastCalled.value = null;
  } else {
    lastCalled.value = {
      technicianName: result.technicianName ?? '',
      employeeNo: result.employeeNo ?? null
    };
    ElMessage.success(`已叫 ${result.employeeNo ?? ''} 号 · ${result.technicianName ?? ''}`);
    // 叫号广播：连读两遍，让休息区盲人技师都能听清
    const noTxt = result.employeeNo != null ? `${result.employeeNo} 号` : '';
    announcer.speak(`请 ${noTxt}，${result.technicianName ?? ''} 技师上钟`, 2);
    await reload();
  }
}

async function resetDay() {
  if (!appStore.activeStoreId) return;
  const ok = await ElMessageBox.confirm('确认重置今日所有技师轮次？', '提示', { type: 'warning' }).then(() => true).catch(() => false);
  if (!ok) return;
  await queueApi.resetDay(appStore.activeStoreId);
  ElMessage.success('已重置');
  reload();
}

let timer: number | null = null;
onMounted(async () => {
  await appStore.loadStores();
  await reload();
  // 浏览器 voices 异步加载，先预热避免第一次叫号无声
  announcer.prewarm();
  timer = window.setInterval(reload, 15000);
});
onUnmounted(() => {
  if (timer != null) window.clearInterval(timer);
});
</script>

<style scoped>
.page { padding-bottom: 24px; }
.toolbar { display: flex; gap: 12px; align-items: center; flex-wrap: wrap; }
.toolbar .title { font-weight: 600; font-size: 16px; }
.spacer { flex: 1; }
.called {
  background: #ecf5ff;
  padding: 12px;
  border-radius: 4px;
  margin-top: 12px;
  font-size: 16px;
}
.my-shift {
  margin-top: 12px;
  padding: 16px;
  background: #f8fafc;
  border: 1px solid #e2e8f0;
  border-radius: 8px;
}
.my-head { display: flex; align-items: center; gap: 10px; font-size: 15px; }
.my-head .my-meta { color: #6b7280; font-size: 13px; }
.my-actions { display: flex; gap: 12px; margin-top: 12px; }
/* 下班按钮：白底黑字（与 CS 端白色按钮一致） */
.my-actions .off-btn {
  background: #fff;
  color: #1a1a1a;
  border: 1px solid #dcdfe6;
}
.my-actions .off-btn:hover:not(:disabled) {
  background: #f5f7fa;
  color: #1a1a1a;
  border-color: #c0c4cc;
}
.my-current { margin-top: 12px; font-size: 14px; }
.my-notes {
  margin-top: 8px;
  padding: 8px 10px;
  background: #fff7ed;
  border: 1px solid #fed7aa;
  border-radius: 4px;
  color: #9a3412;
  font-size: 13px;
}
</style>
