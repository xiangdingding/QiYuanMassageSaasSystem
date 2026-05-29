<template>
  <div class="page">
    <el-card shadow="never">
      <div class="toolbar" role="search">
        <el-radio-group v-model="filter.status" @change="reload">
          <el-radio-button value="Pending">待处理</el-radio-button>
          <el-radio-button value="Resolved">已处理</el-radio-button>
          <el-radio-button value="Cancelled">已取消</el-radio-button>
          <el-radio-button value="">全部</el-radio-button>
        </el-radio-group>
        <el-date-picker
          v-model="filter.range"
          type="daterange"
          value-format="YYYY-MM-DD"
          range-separator="至"
          start-placeholder="开始"
          end-placeholder="结束"
          style="width: 260px"
          @change="reload"
        />
        <div class="spacer" />
        <el-button type="primary" @click="openCreate">登记投诉</el-button>
        <el-button :icon="Refresh" @click="reload">刷新</el-button>
      </div>

      <el-table :data="rows" v-loading="loading" stripe style="margin-top:12px">
        <el-table-column prop="orderNo" label="订单号" width="160" />
        <el-table-column prop="serviceName" label="项目" width="160" />
        <el-table-column prop="originalTechnicianName" label="被投诉技师" width="120" />
        <el-table-column prop="memberName" label="会员" width="120" />
        <el-table-column label="标签 / 描述" min-width="240">
          <template #default="{ row }">
            <div v-if="row.tags">
              <el-tag v-for="t in (row.tags as string).split(',')" :key="t" size="small" type="warning" style="margin-right:4px">{{ t }}</el-tag>
            </div>
            <div v-if="row.comment" class="muted">{{ row.comment }}</div>
          </template>
        </el-table-column>
        <el-table-column label="状态" width="100">
          <template #default="{ row }">
            <el-tag :type="statusTag(row.status)">{{ statusLabel(row.status) }}</el-tag>
          </template>
        </el-table-column>
        <el-table-column label="处理结果" width="160">
          <template #default="{ row }">
            <span v-if="row.resolution">
              {{ resolutionLabel(row.resolution) }}
              <span v-if="row.reassignedToTechnicianName" class="muted">→ {{ row.reassignedToTechnicianName }}</span>
            </span>
            <span v-else class="muted">—</span>
          </template>
        </el-table-column>
        <el-table-column label="登记/处理" width="160">
          <template #default="{ row }">
            <div>{{ row.recordedByName || '—' }}</div>
            <div class="muted" v-if="row.resolvedByName">{{ row.resolvedByName }} · {{ formatTime(row.resolvedAt) }}</div>
          </template>
        </el-table-column>
        <el-table-column label="时间" width="160">
          <template #default="{ row }">{{ formatTime(row.createdAt) }}</template>
        </el-table-column>
        <el-table-column label="操作" width="200" fixed="right">
          <template #default="{ row }">
            <el-button v-if="row.status === 'Pending'" link type="primary" @click="openResolve(row)">处理</el-button>
            <el-button v-if="row.status === 'Pending'" link type="danger" @click="cancelOne(row)">取消</el-button>
          </template>
        </el-table-column>
      </el-table>

      <el-pagination
        style="margin-top: 12px; justify-content: flex-end; display: flex"
        :current-page="filter.page"
        :page-size="filter.pageSize"
        :total="total"
        :page-sizes="[20, 50]"
        layout="total, sizes, prev, pager, next, jumper"
        @current-change="(p: number) => { filter.page = p; reload(); }"
        @size-change="(s: number) => { filter.pageSize = s; filter.page = 1; reload(); }"
      />
    </el-card>

    <el-dialog v-model="createOpen" title="登记投诉" width="820px">
      <el-form :model="createForm" label-width="100px">
        <el-form-item label="不指定项目">
          <el-switch v-model="createForm.anonymous" />
          <span class="muted hint" style="margin-left: 8px">
            打开后无需选具体订单项，仅记录文字内容（处理时只能道歉/补偿或不予处理）
          </span>
        </el-form-item>
        <el-form-item label="被投诉技师" :required="createForm.anonymous ? false : true">
          <el-select v-model="createForm.technicianId" placeholder="选择被投诉的技师" filterable clearable
                     style="width: 260px" @change="onTechChanged">
            <el-option v-for="t in techList" :key="t.id"
                       :label="`${t.realName || t.username}（工号 ${t.employeeNo ?? '—'}）`" :value="t.id" />
          </el-select>
          <template v-if="!createForm.anonymous">
            <el-date-picker v-model="createForm.date" type="date" format="YYYY-MM-DD" value-format="YYYY-MM-DD"
                            :disabled-date="disableFuture" style="margin-left: 8px; width: 160px"
                            @change="lookupItems" />
            <el-button :loading="lookingUp" :disabled="!createForm.technicianId || !createForm.date"
                       style="margin-left: 8px" @click="lookupItems">查询订单</el-button>
          </template>
        </el-form-item>
        <el-form-item v-if="!createForm.anonymous" label="选择项目" required>
          <div v-if="!lookedUp" class="muted hint">请先选择技师与日期，再点"查询订单"。</div>
          <el-alert v-else-if="itemCandidates.length === 0" type="info" :closable="false" show-icon>
            该技师在该日没有已完成的服务项目。
          </el-alert>
          <el-table v-else :data="itemCandidates" v-loading="lookingUp" stripe size="small"
                    highlight-current-row max-height="260" @current-change="onPickItem">
            <el-table-column width="50">
              <template #default="{ row }">
                <el-radio v-model="createForm.orderItemId" :value="row.itemId" :disabled="row.hasPendingComplaint" />
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
            <el-table-column label="金额" width="90">
              <template #default="{ row }">¥{{ row.amount.toFixed(2) }}</template>
            </el-table-column>
            <el-table-column label="状态" width="90">
              <template #default="{ row }">
                <el-tag v-if="row.hasPendingComplaint" type="warning" size="small">已投诉</el-tag>
              </template>
            </el-table-column>
          </el-table>
        </el-form-item>
        <el-form-item label="标签">
          <el-input v-model="createForm.tags" placeholder="逗号分隔，如：态度差,力度不合适" />
        </el-form-item>
        <el-form-item label="描述">
          <el-input v-model="createForm.comment" type="textarea" :rows="3" placeholder="客户原话/补充" maxlength="500" show-word-limit />
        </el-form-item>
      </el-form>
      <template #footer>
        <el-button @click="createOpen = false">取消</el-button>
        <el-button type="primary" :loading="creating" :disabled="!canSubmitCreate" @click="submitCreate">登记投诉</el-button>
      </template>
    </el-dialog>

    <el-dialog v-model="resolveOpen" :title="`处理投诉 #${resolving?.id}`" width="480px">
      <div v-if="resolving" class="resolve-detail">
        <p v-if="resolving.orderItemId">
          <strong>{{ resolving.serviceName }}</strong> · 技师 {{ resolving.originalTechnicianName }}
        </p>
        <p v-else>
          <el-tag type="info" size="small" style="margin-right: 6px">匿名</el-tag>
          <span v-if="resolving.originalTechnicianName">被投诉技师：{{ resolving.originalTechnicianName }}</span>
          <span v-else class="muted">未指定订单项与技师</span>
        </p>
        <p v-if="resolving.tags" class="muted">{{ resolving.tags }}</p>
        <p v-if="resolving.comment" class="muted">{{ resolving.comment }}</p>
      </div>
      <el-form :model="resolveForm" label-width="100px">
        <el-form-item label="处理方式">
          <el-radio-group v-model="resolveForm.resolution">
            <el-radio v-if="resolving?.orderItemId" value="Reassigned">改派</el-radio>
            <el-radio v-if="resolving?.orderItemId" value="Refunded">退款</el-radio>
            <el-radio value="Apologized">道歉/补偿</el-radio>
            <el-radio value="NoAction">不予处理</el-radio>
          </el-radio-group>
          <div v-if="!resolving?.orderItemId" class="muted hint">
            该投诉未指定具体订单项，仅可选择 道歉/补偿 或 不予处理。
          </div>
        </el-form-item>
        <el-form-item v-if="resolveForm.resolution === 'Reassigned'" label="改派给">
          <el-select v-model="resolveForm.reassignedToTechnicianId" placeholder="请选择新技师" style="width:240px" filterable>
            <el-option
              v-for="s in techList"
              :key="s.id"
              :label="`${s.realName || s.username}（工号 ${s.employeeNo ?? '—'}）`"
              :value="s.id"
              :disabled="s.id === resolving?.originalTechnicianId"
            />
          </el-select>
        </el-form-item>
        <el-form-item label="备注">
          <el-input v-model="resolveForm.resolutionNote" type="textarea" :rows="2" maxlength="500" />
        </el-form-item>
      </el-form>
      <template #footer>
        <el-button @click="resolveOpen = false">取消</el-button>
        <el-button type="primary" :loading="saving" @click="submitResolve">确认处理</el-button>
      </template>
    </el-dialog>
  </div>
</template>

<script setup lang="ts">
import { computed, onMounted, reactive, ref, watch } from 'vue';
import { ElMessage, ElMessageBox } from 'element-plus';
import { Refresh } from '@element-plus/icons-vue';
import dayjs from 'dayjs';
import { complaintsApi, ordersApi, staffApi, type ComplaintDto, type TechnicianServedItemDto } from '@/api/modules';
import { useAppStore } from '@/stores/app';
import type { Staff } from '@/api/types';

const appStore = useAppStore();
const rows = ref<ComplaintDto[]>([]);
const total = ref(0);
const loading = ref(false);
const saving = ref(false);
const creating = ref(false);

const filter = reactive<{ status: string; range: string[] | null; page: number; pageSize: number }>(
  { status: 'Pending', range: null, page: 1, pageSize: 20 }
);

const createOpen = ref(false);
const createForm = reactive({
  anonymous: false,
  technicianId: null as number | null,
  date: dayjs().format('YYYY-MM-DD'),
  orderItemId: 0,
  tags: '',
  comment: ''
});
const itemCandidates = ref<TechnicianServedItemDto[]>([]);
const lookingUp = ref(false);
const lookedUp = ref(false);

const canSubmitCreate = computed(() => {
  if (createForm.anonymous) return true;
  return createForm.orderItemId > 0;
});

function disableFuture(d: Date) { return d.getTime() > Date.now(); }

function openCreate() {
  createForm.anonymous = false;
  createForm.technicianId = null;
  createForm.date = dayjs().format('YYYY-MM-DD');
  createForm.orderItemId = 0;
  createForm.tags = '';
  createForm.comment = '';
  itemCandidates.value = [];
  lookedUp.value = false;
  createOpen.value = true;
}

function onTechChanged() {
  if (!createForm.anonymous) lookupItems();
}

async function lookupItems() {
  if (!appStore.activeStoreId || !createForm.technicianId || !createForm.date) return;
  lookingUp.value = true;
  createForm.orderItemId = 0;
  try {
    itemCandidates.value = await ordersApi.itemsByTechnician(
      appStore.activeStoreId, createForm.technicianId, createForm.date);
    lookedUp.value = true;
  } finally {
    lookingUp.value = false;
  }
}

function onPickItem(row: TechnicianServedItemDto | null) {
  if (row && !row.hasPendingComplaint) createForm.orderItemId = row.itemId;
}

const resolveOpen = ref(false);
const resolving = ref<ComplaintDto | null>(null);
const resolveForm = reactive<{ resolution: string; reassignedToTechnicianId: number | null; resolutionNote: string }>(
  { resolution: 'Reassigned', reassignedToTechnicianId: null, resolutionNote: '' }
);
const techList = ref<Staff[]>([]);

function statusLabel(s: string) {
  return ({ Pending: '待处理', Resolved: '已处理', Cancelled: '已取消' } as Record<string, string>)[s] ?? s;
}
function statusTag(s: string): 'warning' | 'success' | 'info' {
  return s === 'Pending' ? 'warning' : s === 'Resolved' ? 'success' : 'info';
}
function resolutionLabel(r: string) {
  return ({ Reassigned: '改派', Refunded: '退款', Apologized: '道歉/补偿', NoAction: '不予处理' } as Record<string, string>)[r] ?? r;
}
function formatTime(t: string | null) {
  return t ? dayjs(t).format('YYYY-MM-DD HH:mm') : '';
}

async function reload() {
  loading.value = true;
  try {
    const res = await complaintsApi.list({
      storeId: appStore.activeStoreId ?? undefined,
      status: filter.status || undefined,
      from: filter.range?.[0],
      to: filter.range?.[1],
      page: filter.page,
      pageSize: filter.pageSize
    });
    rows.value = res.items;
    total.value = res.total;
  } finally {
    loading.value = false;
  }
}

async function loadTechList() {
  if (!appStore.activeStoreId) return;
  const r = await staffApi.list({ role: 'Technician', storeId: appStore.activeStoreId, page: 1, pageSize: 200 });
  techList.value = r.items;
}

async function submitCreate() {
  if (!createForm.anonymous && createForm.orderItemId <= 0) {
    ElMessage.warning('请先选择被投诉的服务项');
    return;
  }
  if (createForm.anonymous && !appStore.activeStoreId) {
    ElMessage.warning('未选择门店');
    return;
  }
  creating.value = true;
  try {
    await complaintsApi.create(createForm.anonymous
      ? {
          orderItemId: null,
          storeId: appStore.activeStoreId,
          technicianId: createForm.technicianId,
          tags: createForm.tags.trim() || null,
          comment: createForm.comment.trim() || null
        }
      : {
          orderItemId: createForm.orderItemId,
          tags: createForm.tags.trim() || null,
          comment: createForm.comment.trim() || null
        });
    ElMessage.success('已登记投诉');
    createOpen.value = false;
    reload();
  } finally {
    creating.value = false;
  }
}

function openResolve(row: ComplaintDto) {
  resolving.value = row;
  // 没挂订单项的投诉不能改派/退款，默认选道歉/补偿
  resolveForm.resolution = row.orderItemId ? 'Reassigned' : 'Apologized';
  resolveForm.reassignedToTechnicianId = null;
  resolveForm.resolutionNote = '';
  resolveOpen.value = true;
}

async function submitResolve() {
  if (!resolving.value) return;
  if (resolveForm.resolution === 'Reassigned' && !resolveForm.reassignedToTechnicianId) {
    ElMessage.warning('请选择改派目标');
    return;
  }
  saving.value = true;
  try {
    await complaintsApi.resolve(resolving.value.id, {
      resolution: resolveForm.resolution,
      reassignedToTechnicianId: resolveForm.resolution === 'Reassigned' ? resolveForm.reassignedToTechnicianId : null,
      resolutionNote: resolveForm.resolutionNote.trim() || null
    });
    ElMessage.success('已处理');
    resolveOpen.value = false;
    reload();
  } finally {
    saving.value = false;
  }
}

async function cancelOne(row: ComplaintDto) {
  await ElMessageBox.confirm(`确认取消订单 ${row.orderNo} 上的这条投诉记录？`, '提示', { type: 'warning' }).catch(() => null);
  await complaintsApi.cancel(row.id);
  ElMessage.success('已取消');
  reload();
}

watch(() => appStore.activeStoreId, () => {
  reload();
  loadTechList();
});

onMounted(async () => {
  await appStore.loadStores();
  await Promise.all([reload(), loadTechList()]);
});
</script>

<style scoped>
.page { padding-bottom: 24px; }
.toolbar { display: flex; gap: 12px; align-items: center; flex-wrap: wrap; }
.spacer { flex: 1; }
.muted { color: var(--el-text-color-secondary); font-size: 12px; }
.ck-header { display: flex; justify-content: space-between; align-items: baseline; }
.resolve-detail { background: #f5f7fa; padding: 12px; border-radius: 6px; margin-bottom: 12px; }
.resolve-detail p { margin: 4px 0; }
.hint { margin-top: 4px; line-height: 1.4; }
</style>
