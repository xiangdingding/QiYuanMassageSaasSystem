<template>
  <div class="page">
    <el-card shadow="never">
      <div class="toolbar">
        <span class="title">服务评价</span>
        <el-input-number v-model="ratingFilter" :min="1" :max="5" controls-position="right" placeholder="星级" style="width:120px" />
        <el-date-picker v-model="dateRange" type="daterange" range-separator="-" start-placeholder="开始" end-placeholder="结束" />
        <div class="spacer" />
        <el-button type="primary" @click="openCreate">代客录入</el-button>
        <el-button :icon="Refresh" @click="reload">刷新</el-button>
      </div>

      <el-tabs v-model="tab" class="page-tabs" @tab-change="onTabChange">
        <el-tab-pane label="全部评价" name="list">
          <div class="table-wrap">
          <el-table :data="rows" v-loading="loading" stripe height="100%">
            <el-table-column prop="technicianName" label="技师" width="120" />
            <el-table-column prop="memberName" label="顾客" width="120" />
            <el-table-column prop="rating" label="评分" width="100">
              <template #default="{ row }">
                <span :aria-label="`${row.rating} 星`">{{ '★'.repeat(row.rating) }}</span>
              </template>
            </el-table-column>
            <el-table-column prop="tags" label="标签" width="200" />
            <el-table-column prop="comment" label="评论" min-width="240" show-overflow-tooltip />
            <el-table-column prop="createdAt" label="时间" width="180" />
          </el-table>
          </div>
        </el-tab-pane>
        <el-tab-pane label="技师汇总" name="summary">
          <div class="table-wrap">
          <el-table :data="summary" v-loading="loading" stripe height="100%">
            <el-table-column prop="technicianName" label="技师" width="160" />
            <el-table-column prop="reviewCount" label="评价数" width="120" />
            <el-table-column prop="averageRating" label="平均分" width="120" />
          </el-table>
          </div>
        </el-tab-pane>
      </el-tabs>
    </el-card>

    <el-dialog v-model="createOpen" title="代客录入评价" width="820px">
      <el-form :model="createForm" label-width="100px">
        <el-form-item label="技师" required>
          <el-select v-model="createForm.technicianId" placeholder="选择服务技师" filterable clearable
                     style="width: 260px" @change="onTechChanged">
            <el-option v-for="t in techList" :key="t.id"
                       :label="`${t.realName || t.username}（工号 ${t.employeeNo ?? '—'}）`" :value="t.id" />
          </el-select>
          <el-date-picker v-model="createForm.date" type="date" format="YYYY-MM-DD" value-format="YYYY-MM-DD"
                          :disabled-date="disableFuture" style="margin-left: 8px; width: 160px"
                          @change="lookupItems" />
          <el-button :loading="lookingUp" :disabled="!createForm.technicianId || !createForm.date"
                     style="margin-left: 8px" @click="lookupItems">查询订单</el-button>
        </el-form-item>
        <el-form-item label="选择项目" required>
          <div v-if="!lookedUp" class="muted hint">请先选择技师与日期，再点"查询订单"。</div>
          <el-alert v-else-if="itemCandidates.length === 0" type="info" :closable="false" show-icon>
            该技师在该日没有已完成的服务项目。
          </el-alert>
          <el-table v-else :data="itemCandidates" v-loading="lookingUp" stripe size="small"
                    highlight-current-row max-height="260" @current-change="onPickItem">
            <el-table-column width="50">
              <template #default="{ row }">
                <el-radio v-model="createForm.orderItemId" :value="row.itemId" :disabled="row.hasReview" />
              </template>
            </el-table-column>
            <el-table-column prop="orderNo" label="订单号" width="160" />
            <el-table-column prop="serviceName" label="服务" min-width="120" />
            <el-table-column label="完成时间" width="140">
              <template #default="{ row }">{{ formatTime(row.completedAt) }}</template>
            </el-table-column>
            <el-table-column label="会员" min-width="100">
              <template #default="{ row }">
                <span v-if="row.memberName">{{ row.memberName }}</span>
                <span v-else-if="row.memberCardNo">{{ row.memberCardNo }}</span>
                <span v-else class="muted">散客</span>
              </template>
            </el-table-column>
            <el-table-column label="状态" width="90">
              <template #default="{ row }">
                <el-tag v-if="row.hasReview" type="success" size="small">已评价</el-tag>
              </template>
            </el-table-column>
          </el-table>
        </el-form-item>
        <el-form-item label="评分" required>
          <el-rate v-model="createForm.rating" :max="5" aria-label="评分，一到五星" />
        </el-form-item>
        <el-form-item label="标签">
          <el-input v-model="createForm.tags" placeholder="逗号分隔，如：手法专业,态度好" />
        </el-form-item>
        <el-form-item label="评论">
          <el-input v-model="createForm.comment" type="textarea" :rows="3" placeholder="客户原话/补充" maxlength="500" show-word-limit />
        </el-form-item>
      </el-form>
      <template #footer>
        <el-button @click="createOpen = false">取消</el-button>
        <el-button type="primary" :loading="creating" :disabled="!canSubmitCreate" @click="submitCreate">提交评价</el-button>
      </template>
    </el-dialog>
  </div>
</template>

<script setup lang="ts">
import { computed, onMounted, reactive, ref, watch } from 'vue';
import { ElMessage } from 'element-plus';
import { Refresh } from '@element-plus/icons-vue';
import dayjs from 'dayjs';
import { ordersApi, reviewsApi, staffApi, type ServiceReviewDto, type TechnicianServedItemDto } from '@/api/modules';
import { useAppStore } from '@/stores/app';
import type { Staff } from '@/api/types';

const appStore = useAppStore();
const tab = ref<'list' | 'summary'>('list');
const rows = ref<ServiceReviewDto[]>([]);
const summary = ref<any[]>([]);
const loading = ref(false);
const ratingFilter = ref<number | undefined>(undefined);
const dateRange = ref<[Date, Date] | null>(null);

// —— 代客录入评价 ——
const createOpen = ref(false);
const creating = ref(false);
const techList = ref<Staff[]>([]);
const itemCandidates = ref<TechnicianServedItemDto[]>([]);
const lookingUp = ref(false);
const lookedUp = ref(false);
const createForm = reactive({
  technicianId: null as number | null,
  date: dayjs().format('YYYY-MM-DD'),
  orderItemId: 0,
  orderId: 0,
  rating: 5,
  tags: '',
  comment: ''
});

const canSubmitCreate = computed(() => createForm.orderItemId > 0 && createForm.rating >= 1);

function disableFuture(d: Date) { return d.getTime() > Date.now(); }
function formatTime(t: string | null) { return t ? dayjs(t).format('YYYY-MM-DD HH:mm') : ''; }

function openCreate() {
  createForm.technicianId = null;
  createForm.date = dayjs().format('YYYY-MM-DD');
  createForm.orderItemId = 0;
  createForm.orderId = 0;
  createForm.rating = 5;
  createForm.tags = '';
  createForm.comment = '';
  itemCandidates.value = [];
  lookedUp.value = false;
  createOpen.value = true;
}

function onTechChanged() { lookupItems(); }

async function lookupItems() {
  if (!appStore.activeStoreId || !createForm.technicianId || !createForm.date) return;
  lookingUp.value = true;
  createForm.orderItemId = 0;
  createForm.orderId = 0;
  try {
    itemCandidates.value = await ordersApi.itemsByTechnician(
      appStore.activeStoreId, createForm.technicianId, createForm.date);
    lookedUp.value = true;
  } finally {
    lookingUp.value = false;
  }
}

function onPickItem(row: TechnicianServedItemDto | null) {
  if (row && !row.hasReview) {
    createForm.orderItemId = row.itemId;
    createForm.orderId = row.orderId;
  }
}

async function loadTechList() {
  if (!appStore.activeStoreId) return;
  const r = await staffApi.list({ role: 'Technician', storeId: appStore.activeStoreId, page: 1, pageSize: 200 });
  techList.value = r.items;
}

async function submitCreate() {
  if (createForm.orderItemId <= 0) { ElMessage.warning('请先选择被评价的服务项'); return; }
  if (createForm.rating < 1) { ElMessage.warning('请先打分'); return; }
  // orderItemId 唯一确定订单项，orderId 由所选行带出
  const picked = itemCandidates.value.find((i) => i.itemId === createForm.orderItemId);
  const orderId = picked?.orderId ?? createForm.orderId;
  creating.value = true;
  try {
    await reviewsApi.submit({
      orderId,
      orderItemId: createForm.orderItemId,
      rating: createForm.rating,
      tags: createForm.tags.trim() || null,
      comment: createForm.comment.trim() || null
    });
    ElMessage.success('已录入评价');
    createOpen.value = false;
    reload();
  } finally {
    creating.value = false;
  }
}

async function reload() {
  loading.value = true;
  try {
    const from = dateRange.value?.[0]?.toISOString();
    const to = dateRange.value?.[1]?.toISOString();
    if (tab.value === 'list') {
      rows.value = await reviewsApi.list({
        rating: ratingFilter.value,
        from, to
      });
    } else {
      const data = await reviewsApi.technicianSummary({ from, to }) as any[];
      summary.value = data;
    }
  } finally {
    loading.value = false;
  }
}

function onTabChange() { reload(); }
watch([ratingFilter, dateRange], () => reload());
watch(() => appStore.activeStoreId, () => loadTechList());

onMounted(async () => {
  await appStore.loadStores();
  await Promise.all([reload(), loadTechList()]);
});
</script>

<style scoped>
.toolbar { display: flex; gap: 12px; align-items: center; }
.toolbar .title { font-weight: 600; font-size: 16px; }
/* tab 容器视口锁定，table-wrap 在每个 tab-pane 内自滚 */
.page-tabs { flex: 1 1 auto; display: flex; flex-direction: column; min-height: 0; margin-top: 8px; }
.page-tabs :deep(.el-tabs__content) { flex: 1 1 auto; min-height: 0; overflow: hidden; }
.page-tabs :deep(.el-tab-pane) { height: 100%; display: flex; flex-direction: column; }
.spacer { flex: 1; }
.muted { color: var(--el-text-color-secondary); font-size: 12px; }
.hint { margin-top: 4px; line-height: 1.4; }
</style>
