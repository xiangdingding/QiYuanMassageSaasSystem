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

      <el-table :data="rows" v-loading="loading" stripe style="margin-top: 12px">
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
        <el-table-column v-if="canManage" label="操作" width="320" fixed="right">
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
import type { QueueRow, Staff } from '@/api/types';

const appStore = useAppStore();
const auth = useAuthStore();
const announcer = useAnnouncer();

const rows = ref<QueueRow[]>([]);
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
    // 若后端返回的人少于已知技师人数，前端补一行 OffDuty 占位
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
  } finally {
    loading.value = false;
  }
}

async function setState(technicianId: number, state: string) {
  await queueApi.setState(technicianId, state);
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
  await ElMessageBox.confirm('确认重置今日所有技师轮次？', '提示', { type: 'warning' }).catch(() => null);
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
</style>
