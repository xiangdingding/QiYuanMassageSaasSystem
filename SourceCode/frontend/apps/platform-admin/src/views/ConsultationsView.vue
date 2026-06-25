<template>
  <div class="page">
    <el-card shadow="never">
      <div class="toolbar">
        <el-input
          v-model="query.keyword"
          placeholder="搜索电话 / 称呼 / 内容"
          clearable
          style="width: 240px"
          @keyup.enter="reload"
        />
        <el-select v-model="query.status" placeholder="全部状态" clearable style="width: 140px">
          <el-option label="待处理" value="Pending" />
          <el-option label="跟进中" value="Processing" />
          <el-option label="已完成" value="Done" />
          <el-option label="无效" value="Invalid" />
        </el-select>
        <el-button type="primary" @click="reload">查询</el-button>
        <el-button @click="resetQuery">重置</el-button>
      </div>

      <el-table :data="rows" v-loading="loading" stripe style="margin-top: 12px">
        <el-table-column label="状态" width="100">
          <template #default="{ row }">
            <el-tag :type="statusType(row.status)" effect="dark">{{ statusLabel(row.status) }}</el-tag>
          </template>
        </el-table-column>
        <el-table-column prop="phone" label="联系电话" min-width="130" />
        <el-table-column prop="contactName" label="称呼" min-width="100">
          <template #default="{ row }">{{ row.contactName || '—' }}</template>
        </el-table-column>
        <el-table-column prop="content" label="咨询内容" min-width="280" show-overflow-tooltip />
        <el-table-column label="来源" width="120">
          <template #default="{ row }">{{ sourceLabel(row.source) }}</template>
        </el-table-column>
        <el-table-column label="提交时间" min-width="160">
          <template #default="{ row }">{{ formatTime(row.createdAt) }}</template>
        </el-table-column>
        <el-table-column label="处理" min-width="160">
          <template #default="{ row }">
            <span v-if="!row.processedAt" class="muted">—</span>
            <span v-else>
              {{ row.processedByName || '平台' }} · {{ formatTime(row.processedAt) }}
            </span>
          </template>
        </el-table-column>
        <el-table-column label="操作" width="160" fixed="right">
          <template #default="{ row }">
            <el-button link type="primary" @click="openProcess(row)">查看 / 处理</el-button>
          </template>
        </el-table-column>
      </el-table>

      <el-pagination
        style="margin-top: 12px; justify-content: flex-end; display: flex"
        :current-page="query.page"
        :page-size="query.pageSize"
        :total="total"
        :page-sizes="[10, 20, 50]"
        layout="total, sizes, prev, pager, next, jumper"
        @current-change="(p: number) => { query.page = p; reload(); }"
        @size-change="(s: number) => { query.pageSize = s; query.page = 1; reload(); }"
      />
    </el-card>

    <!-- 处理弹窗 -->
    <el-dialog v-model="dialogOpen" title="业务咨询处理" width="560px">
      <template v-if="current">
        <el-descriptions :column="1" border size="small" class="detail">
          <el-descriptions-item label="联系电话">
            <b>{{ current.phone }}</b>
          </el-descriptions-item>
          <el-descriptions-item label="称呼">{{ current.contactName || '—' }}</el-descriptions-item>
          <el-descriptions-item label="咨询内容">
            <div class="content-box">{{ current.content }}</div>
          </el-descriptions-item>
          <el-descriptions-item label="来源">{{ sourceLabel(current.source) }}</el-descriptions-item>
          <el-descriptions-item label="提交时间">{{ formatTime(current.createdAt) }}</el-descriptions-item>
        </el-descriptions>

        <el-form label-width="80px" style="margin-top: 16px">
          <el-form-item label="处理状态">
            <el-radio-group v-model="processForm.status">
              <el-radio-button value="Pending">待处理</el-radio-button>
              <el-radio-button value="Processing">跟进中</el-radio-button>
              <el-radio-button value="Done">已完成</el-radio-button>
              <el-radio-button value="Invalid">无效</el-radio-button>
            </el-radio-group>
          </el-form-item>
          <el-form-item label="处理备注">
            <el-input
              v-model="processForm.processNote"
              type="textarea"
              :rows="3"
              maxlength="1000"
              show-word-limit
              placeholder="跟进记录 / 结论，如：已电话联系，约定周三上门演示"
            />
          </el-form-item>
        </el-form>
      </template>
      <template #footer>
        <el-button @click="dialogOpen = false">取消</el-button>
        <el-button type="primary" :loading="saving" @click="saveProcess">保存</el-button>
      </template>
    </el-dialog>
  </div>
</template>

<script setup lang="ts">
import { onMounted, reactive, ref } from 'vue';
import { ElMessage } from 'element-plus';
import { consultationsApi } from '@/api/modules';
import type { Consultation, ConsultationStatus } from '@/api/types';

const rows = ref<Consultation[]>([]);
const total = ref(0);
const loading = ref(false);

const query = reactive<{ page: number; pageSize: number; keyword: string; status: string }>({
  page: 1,
  pageSize: 20,
  keyword: '',
  status: ''
});

const dialogOpen = ref(false);
const saving = ref(false);
const current = ref<Consultation | null>(null);
const processForm = reactive<{ status: ConsultationStatus; processNote: string }>({
  status: 'Processing',
  processNote: ''
});

function statusType(s: ConsultationStatus) {
  return { Pending: 'danger', Processing: 'warning', Done: 'success', Invalid: 'info' }[s] ?? 'info';
}
function statusLabel(s: ConsultationStatus) {
  return { Pending: '待处理', Processing: '跟进中', Done: '已完成', Invalid: '无效' }[s] ?? s;
}
function sourceLabel(src?: string | null) {
  if (!src) return '官网';
  if (src.startsWith('website')) return '官网';
  return src;
}
function formatTime(v: string) {
  return new Date(v).toLocaleString('zh-CN', { hour12: false });
}

async function reload() {
  loading.value = true;
  try {
    const data = await consultationsApi.list({
      page: query.page,
      pageSize: query.pageSize,
      keyword: query.keyword || undefined,
      status: query.status || undefined
    });
    rows.value = data.items;
    total.value = data.total;
  } finally {
    loading.value = false;
  }
}

function resetQuery() {
  query.keyword = '';
  query.status = '';
  query.page = 1;
  reload();
}

function openProcess(row: Consultation) {
  current.value = row;
  // 默认把"待处理"推进到"跟进中"，其余保持原状态，减少点击
  processForm.status = row.status === 'Pending' ? 'Processing' : row.status;
  processForm.processNote = row.processNote ?? '';
  dialogOpen.value = true;
}

async function saveProcess() {
  if (!current.value) return;
  saving.value = true;
  try {
    await consultationsApi.process(current.value.id, {
      status: processForm.status,
      processNote: processForm.processNote || null
    });
    ElMessage.success('已保存');
    dialogOpen.value = false;
    reload();
  } catch {
    /* http interceptor surfaces error */
  } finally {
    saving.value = false;
  }
}

onMounted(reload);
</script>

<style scoped>
.page { padding-bottom: 24px; }
.toolbar { display: flex; gap: 8px; align-items: center; flex-wrap: wrap; }
.muted { color: var(--el-text-color-secondary); }
.detail :deep(.el-descriptions__label) { width: 88px; }
.content-box { white-space: pre-wrap; word-break: break-word; line-height: 1.7; }
</style>
