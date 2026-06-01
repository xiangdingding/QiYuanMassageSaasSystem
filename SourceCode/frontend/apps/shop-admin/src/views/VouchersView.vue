<template>
  <div class="page">
    <el-card shadow="never">
      <div class="toolbar">
        <el-select v-model="kindFilter" placeholder="全部类型" clearable style="width:140px" @change="onFilterChange">
          <el-option label="店内券" value="StoreCoupon" />
          <el-option label="团购券" value="GroupBuy" />
        </el-select>
        <el-select v-model="statusFilter" placeholder="全部状态" clearable style="width:140px" @change="onFilterChange">
          <el-option label="生效中" value="Active" />
          <el-option label="已核销" value="Redeemed" />
          <el-option label="已过期" value="Expired" />
          <el-option label="已作废" value="Cancelled" />
        </el-select>
        <el-input v-model="keyword" placeholder="搜索券码/标题/平台" style="width:240px" clearable @keydown.enter="onFilterChange" @clear="onFilterChange" />
        <el-button type="primary" @click="onFilterChange">查询</el-button>
        <div class="spacer" />
        <el-button type="primary" :icon="Plus" @click="openNew">新建券</el-button>
        <el-button :icon="DocumentCopy" @click="openBatch">批量生成</el-button>
        <el-button
          :icon="Warning"
          :disabled="!hasCancellableSelection"
          :title="hasCancellableSelection ? '' : '请选择处于「生效中」的券'"
          @click="bulkCancel"
        >
          批量作废<span v-if="selected.length > 0"> ({{ selected.length }})</span>
        </el-button>
        <el-button
          :icon="Delete"
          type="danger"
          plain
          :disabled="!hasDeletableSelection"
          :title="hasDeletableSelection ? '' : '只有「已作废」的券才能删除'"
          @click="bulkDelete"
        >
          批量删除<span v-if="selected.length > 0"> ({{ selected.length }})</span>
        </el-button>
        <el-button :icon="Refresh" @click="reload">刷新</el-button>
      </div>

      <div class="table-wrap">
      <el-table
        :data="rows"
        v-loading="loading"
        stripe
        height="100%"
        @selection-change="(rows: VoucherDto[]) => (selected = rows)"
      >
        <el-table-column type="selection" width="44" />
        <el-table-column prop="kind" label="类型" width="80">
          <template #default="{ row }">{{ row.kind === 'GroupBuy' ? '团购券' : '店内券' }}</template>
        </el-table-column>
        <el-table-column prop="code" label="券码" width="170" show-overflow-tooltip />
        <el-table-column prop="title" label="标题" min-width="220" show-overflow-tooltip />
        <el-table-column prop="faceValue" label="面值" width="90" align="right">
          <template #default="{ row }">¥ {{ row.faceValue.toFixed(2) }}</template>
        </el-table-column>
        <el-table-column prop="minOrderAmount" label="最低额" width="100" align="right">
          <template #default="{ row }">¥ {{ row.minOrderAmount.toFixed(2) }}</template>
        </el-table-column>
        <el-table-column prop="discountPercent" label="折扣" width="80" align="right">
          <template #default="{ row }">{{ row.discountPercent ? (row.discountPercent * 10).toFixed(1) + '折' : '—' }}</template>
        </el-table-column>
        <el-table-column prop="platform" label="平台" width="100" show-overflow-tooltip>
          <template #default="{ row }">{{ row.platform || '—' }}</template>
        </el-table-column>
        <el-table-column prop="status" label="状态" width="90" align="center">
          <template #default="{ row }">
            <el-tag :type="statusTag(row.status)" size="small">{{ statusLabel(row.status) }}</el-tag>
          </template>
        </el-table-column>
        <el-table-column prop="expiresAt" label="到期" width="170">
          <template #default="{ row }">{{ formatExpiry(row.expiresAt) }}</template>
        </el-table-column>
        <el-table-column label="操作" width="80" align="center" fixed="right">
          <template #default="{ row }">
            <el-button size="small" type="danger" :disabled="row.status !== 'Active'" @click="cancel(row)">作废</el-button>
          </template>
        </el-table-column>
      </el-table>
      </div>

      <div class="pager">
        <el-pagination
          v-model:current-page="page"
          v-model:page-size="pageSize"
          :total="total"
          :page-sizes="[20, 50, 100, 200]"
          layout="total, sizes, prev, pager, next, jumper"
          background
          aria-label="优惠券列表分页"
          @current-change="reload"
          @size-change="onPageSizeChange"
        />
      </div>
    </el-card>

    <el-dialog v-model="batchOpen" title="批量生成优惠券" width="560px" :close-on-click-modal="false">
      <!-- 输入阶段：表单。生成成功后整体隐藏，弹窗高度直接收缩到结果区，避免外层滚动 -->
      <el-form v-if="batchCodes.length === 0" :model="batchForm" label-width="100px">
        <el-form-item label="类型" required>
          <el-radio-group v-model="batchForm.kind">
            <el-radio value="StoreCoupon">店内券</el-radio>
            <el-radio value="GroupBuy">团购券</el-radio>
          </el-radio-group>
        </el-form-item>
        <el-form-item label="数量" required>
          <el-input-number v-model="batchForm.count" :min="1" :max="500" :step="10" />
          <span style="margin-left:8px;color:#999">1-500 张</span>
        </el-form-item>
        <el-form-item label="标题" required><el-input v-model="batchForm.title" maxlength="200" placeholder="如：团购 100 元抵扣券" /></el-form-item>
        <el-form-item label="抵扣方式" required>
          <el-radio-group v-model="batchForm.discountMode">
            <el-radio value="face">满减面值</el-radio>
            <el-radio value="percent">折扣率</el-radio>
          </el-radio-group>
          <span style="margin-left:8px;color:#999">二选一</span>
        </el-form-item>
        <el-form-item v-if="batchForm.discountMode === 'face'" label="面值" required>
          <el-input-number v-model="batchForm.faceValue" :min="0.01" :precision="2" />
          <span style="margin-left:8px;color:#999">元，每张抵扣此金额</span>
        </el-form-item>
        <el-form-item v-else label="折扣率" required>
          <el-input-number v-model="batchForm.discountPercent" :min="0.01" :max="0.99" :step="0.05" :precision="2" />
          <span style="margin-left:8px;color:#999">0.9 表示 9 折</span>
        </el-form-item>
        <el-form-item label="最低订单额">
          <el-input-number v-model="batchForm.minOrderAmount" :min="0" :precision="2" />
          <span style="margin-left:8px;color:#999">元，订单需达到才能使用</span>
        </el-form-item>
        <el-form-item v-if="batchForm.kind === 'GroupBuy'" label="平台">
          <el-input v-model="batchForm.platform" maxlength="64" placeholder="Meituan / Dianping / Douyin" />
        </el-form-item>
        <el-form-item label="生效起"><el-date-picker v-model="batchForm.validFrom" type="datetime" /></el-form-item>
        <el-form-item label="到期"><el-date-picker v-model="batchForm.expiresAt" type="datetime" /></el-form-item>
        <el-form-item label="备注"><el-input v-model="batchForm.remark" type="textarea" :rows="2" /></el-form-item>
      </el-form>

      <!-- 结果阶段：紧凑摘要 + codes 列表。整体高度可控，弹窗不再撑出外层滚动条 -->
      <div v-else class="batch-result" aria-label="已生成的券码列表">
        <div class="batch-summary">
          已按规格生成 <strong>{{ batchCodes.length }}</strong> 张
          <el-tag size="small" :type="batchForm.kind === 'GroupBuy' ? 'warning' : 'success'">
            {{ batchForm.kind === 'GroupBuy' ? '团购券' : '店内券' }}
          </el-tag>
          <span class="muted">·</span>
          <span>{{ batchForm.discountMode === 'face'
            ? `满减 ¥${batchForm.faceValue.toFixed(2)}`
            : `${(batchForm.discountPercent * 10).toFixed(1)} 折` }}</span>
          <span v-if="batchForm.minOrderAmount > 0" class="muted">· 最低 ¥{{ batchForm.minOrderAmount.toFixed(2) }}</span>
        </div>
        <div class="batch-result-head">
          <span>下方为生成的券码，每行一个：</span>
          <el-button size="small" type="primary" :icon="DocumentCopy" @click="copyBatchCodes">复制全部</el-button>
        </div>
        <el-input
          :model-value="batchCodes.join('\n')"
          type="textarea"
          :autosize="{ minRows: 12, maxRows: 16 }"
          readonly
          resize="none"
          aria-label="生成的券码，每行一个"
        />
      </div>

      <template #footer>
        <el-button @click="batchOpen = false">关闭</el-button>
        <el-button v-if="batchCodes.length === 0" type="primary" :loading="batchSubmitting" @click="submitBatch">
          生成 {{ batchForm.count }} 张
        </el-button>
        <el-button v-else type="primary" @click="resetBatchForAnother">再生成一批</el-button>
      </template>
    </el-dialog>

    <el-dialog v-model="formOpen" title="新建券" width="480px">
      <el-form :model="form" label-width="100px">
        <el-form-item label="类型" required>
          <el-radio-group v-model="form.kind">
            <el-radio value="StoreCoupon">店内券</el-radio>
            <el-radio value="GroupBuy">团购券</el-radio>
          </el-radio-group>
        </el-form-item>
        <el-form-item label="券码" required>
          <el-input
            v-model="form.code"
            maxlength="64"
            placeholder="可手动录入团购券码，或点右侧生成店内码"
            style="width: calc(100% - 96px)"
          />
          <el-button
            style="margin-left: 8px"
            aria-label="按当前类型生成无歧义字符的券码"
            @click="form.code = generateCode(form.kind)"
          >生成</el-button>
        </el-form-item>
        <el-form-item label="标题" required><el-input v-model="form.title" maxlength="200" /></el-form-item>
        <el-form-item label="抵扣方式" required>
          <el-radio-group v-model="form.discountMode">
            <el-radio value="face">满减面值</el-radio>
            <el-radio value="percent">折扣率</el-radio>
          </el-radio-group>
          <span style="margin-left:8px;color:#999">二选一</span>
        </el-form-item>
        <el-form-item v-if="form.discountMode === 'face'" label="面值" required>
          <el-input-number v-model="form.faceValue" :min="0.01" :precision="2" />
          <span style="margin-left:8px;color:#999">元，结账时直接抵扣此金额</span>
        </el-form-item>
        <el-form-item v-else label="折扣率" required>
          <el-input-number v-model="form.discountPercent" :min="0.01" :max="0.99" :step="0.05" :precision="2" />
          <span style="margin-left:8px;color:#999">0.9 表示 9 折，需在 0-1 之间</span>
        </el-form-item>
        <el-form-item label="最低订单额"><el-input-number v-model="form.minOrderAmount" :min="0" :precision="2" /></el-form-item>
        <el-form-item label="平台" v-if="form.kind === 'GroupBuy'">
          <el-input v-model="form.platform" maxlength="64" placeholder="Meituan / Dianping" />
        </el-form-item>
        <el-form-item label="生效起"><el-date-picker v-model="form.validFrom" type="datetime" /></el-form-item>
        <el-form-item label="到期"><el-date-picker v-model="form.expiresAt" type="datetime" /></el-form-item>
        <el-form-item label="备注"><el-input v-model="form.remark" type="textarea" :rows="2" /></el-form-item>
      </el-form>
      <template #footer>
        <el-button @click="formOpen = false">取消</el-button>
        <el-button type="primary" :loading="saving" @click="save">保存</el-button>
      </template>
    </el-dialog>
  </div>
</template>

<script setup lang="ts">
import { computed, onMounted, reactive, ref } from 'vue';
import { ElMessage, ElMessageBox } from 'element-plus';
import { Delete, DocumentCopy, Plus, Refresh, Warning } from '@element-plus/icons-vue';
import { vouchersApi, type VoucherDto } from '@/api/modules';

const rows = ref<VoucherDto[]>([]);
const loading = ref(false);
const statusFilter = ref<string | undefined>(undefined);
const kindFilter = ref<string | undefined>(undefined);
const keyword = ref('');
const formOpen = ref(false);
const saving = ref(false);
const page = ref(1);
const pageSize = ref(20);
const total = ref(0);
const selected = ref<VoucherDto[]>([]);

/// 任一选中行处于 Active 时才允许批量作废
const hasCancellableSelection = computed(
  () => selected.value.some((r) => r.status === 'Active')
);
/// 任一选中行处于 Cancelled 时才允许批量删除
const hasDeletableSelection = computed(
  () => selected.value.some((r) => r.status === 'Cancelled')
);

const form = reactive({
  kind: 'StoreCoupon', code: '', title: '',
  discountMode: 'face' as 'face' | 'percent',
  faceValue: 0, minOrderAmount: 0, discountPercent: 0,
  validFrom: null as string | null, expiresAt: null as string | null,
  platform: '', remark: ''
});

/// 生成无歧义大写字母数字的券码：去掉 0/O/1/I/L 防止收银员口述出错。
/// 格式 "{SC|GB}-XXXX-XXXX"，便于读屏分段朗读，与 CS 端 VoucherFormWindow 的生成规则一致。
function generateCode(kind: string): string {
  const CHARS = 'ABCDEFGHJKMNPQRSTUVWXYZ23456789';
  const prefix = kind === 'GroupBuy' ? 'GB' : 'SC';
  let g1 = '', g2 = '';
  for (let i = 0; i < 4; i++) g1 += CHARS[Math.floor(Math.random() * CHARS.length)];
  for (let i = 0; i < 4; i++) g2 += CHARS[Math.floor(Math.random() * CHARS.length)];
  return `${prefix}-${g1}-${g2}`;
}

/// ISO 时间串统一截成 "YYYY-MM-DD HH:mm:ss"（北京时间）；空值返回"长期有效"。
function formatExpiry(iso: string | null): string {
  if (!iso) return '长期有效';
  // 后端返回的 ISO 串可能含 'T' 与时区，秒可有可无，统一显示到秒
  const m = /^(\d{4}-\d{2}-\d{2})[T ](\d{2}:\d{2}(?::\d{2})?)/.exec(iso);
  if (!m) return iso;
  const time = m[2].length === 5 ? `${m[2]}:00` : m[2];
  return `${m[1]} ${time}`;
}

function statusLabel(s: string) {
  return ({ Active: '生效中', Redeemed: '已核销', Expired: '已过期', Cancelled: '已作废' } as Record<string, string>)[s] ?? s;
}
function statusTag(s: string): 'success' | 'info' | 'warning' | 'danger' {
  return s === 'Active' ? 'success' : s === 'Redeemed' ? 'info' : s === 'Expired' ? 'warning' : 'danger';
}

async function reload() {
  loading.value = true;
  try {
    const resp = await vouchersApi.list({
      status: statusFilter.value,
      kind: kindFilter.value,
      keyword: keyword.value || undefined,
      page: page.value,
      pageSize: pageSize.value
    });
    rows.value = resp.items;
    total.value = resp.total;
  } finally {
    loading.value = false;
  }
}

/// 筛选条件改变后回到第 1 页再加载，避免"切到第 5 页 → 改状态 → 仍在第 5 页但总数不够"的空表
function onFilterChange() {
  page.value = 1;
  reload();
}

function onPageSizeChange() {
  page.value = 1;
  reload();
}

function openNew() {
  Object.assign(form, {
    kind: 'StoreCoupon', code: '', title: '',
    discountMode: 'face', faceValue: 0, minOrderAmount: 0, discountPercent: 0,
    validFrom: null, expiresAt: null, platform: '', remark: ''
  });
  formOpen.value = true;
}

async function save() {
  if (!form.code.trim() || !form.title.trim()) { ElMessage.warning('券码与标题必填'); return; }
  // Radio 模式互斥：另一字段强制清零，后端 ValidateDiscount 才会放行
  const useFace = form.discountMode === 'face';
  if (useFace && form.faceValue <= 0) { ElMessage.warning('请填面值'); return; }
  if (!useFace && (form.discountPercent <= 0 || form.discountPercent >= 1)) {
    ElMessage.warning('折扣率需在 0-1 之间'); return;
  }
  saving.value = true;
  try {
    await vouchersApi.create({
      kind: form.kind, code: form.code.trim(), title: form.title.trim(),
      faceValue: useFace ? form.faceValue : 0,
      minOrderAmount: form.minOrderAmount,
      discountPercent: useFace ? null : form.discountPercent,
      validFrom: form.validFrom, expiresAt: form.expiresAt,
      platform: form.platform || null, remark: form.remark || null
    });
    formOpen.value = false;
    ElMessage.success('已创建');
    await reload();
  } finally {
    saving.value = false;
  }
}

const batchOpen = ref(false);
const batchSubmitting = ref(false);
const batchCodes = ref<string[]>([]);
const batchForm = reactive({
  kind: 'StoreCoupon', count: 100, title: '',
  discountMode: 'face' as 'face' | 'percent',
  faceValue: 100, minOrderAmount: 100, discountPercent: 0.9,
  validFrom: null as string | null, expiresAt: null as string | null,
  platform: '', remark: ''
});

function openBatch() {
  Object.assign(batchForm, {
    kind: 'StoreCoupon', count: 100, title: '',
    discountMode: 'face', faceValue: 100, minOrderAmount: 100, discountPercent: 0.9,
    validFrom: null, expiresAt: null, platform: '', remark: ''
  });
  batchCodes.value = [];
  batchOpen.value = true;
}

function resetBatchForAnother() {
  // 保留上次的字段配置（数量、面值等），只清空已生成的码，方便快速再生成同规格一批
  batchCodes.value = [];
}

async function submitBatch() {
  if (!batchForm.title.trim()) { ElMessage.warning('标题必填'); return; }
  const useFace = batchForm.discountMode === 'face';
  if (useFace && batchForm.faceValue <= 0) { ElMessage.warning('请填面值'); return; }
  if (!useFace && (batchForm.discountPercent <= 0 || batchForm.discountPercent >= 1)) {
    ElMessage.warning('折扣率需在 0-1 之间'); return;
  }
  batchSubmitting.value = true;
  try {
    const resp = await vouchersApi.batch({
      kind: batchForm.kind, count: batchForm.count, title: batchForm.title.trim(),
      faceValue: useFace ? batchForm.faceValue : 0,
      minOrderAmount: batchForm.minOrderAmount,
      discountPercent: useFace ? null : batchForm.discountPercent,
      validFrom: batchForm.validFrom, expiresAt: batchForm.expiresAt,
      platform: batchForm.platform || null, remark: batchForm.remark || null
    });
    batchCodes.value = resp.codes;
    ElMessage.success(`已生成 ${resp.created} 张券`);
    await reload();
  } finally {
    batchSubmitting.value = false;
  }
}

async function copyBatchCodes() {
  try {
    await navigator.clipboard.writeText(batchCodes.value.join('\n'));
    ElMessage.success(`已复制 ${batchCodes.value.length} 张券码`);
  } catch {
    ElMessage.warning('剪贴板权限被拒，请手动选中复制');
  }
}

async function cancel(row: VoucherDto) {
  await ElMessageBox.confirm(`确认作废券 ${row.code}？`, '提示', { type: 'warning' }).catch(() => null);
  await vouchersApi.cancel(row.id);
  ElMessage.success('已作废');
  await reload();
}

/// 把后端 skipped 列表汇总成一句可朗读的中文摘要
function summarizeSkipped(skipped: { id: number; code: string | null; reason: string }[]): string {
  if (skipped.length === 0) return '';
  const sample = skipped.slice(0, 3).map((s) => `${s.code ?? `id=${s.id}`}（${s.reason}）`).join('、');
  return skipped.length > 3 ? `${sample} 等 ${skipped.length} 条` : sample;
}

async function bulkCancel() {
  const ids = selected.value.filter((r) => r.status === 'Active').map((r) => r.id);
  if (ids.length === 0) { ElMessage.warning('没有可作废的券（只有「生效中」状态才能作废）'); return; }
  const proceed = await ElMessageBox.confirm(
    `确认批量作废 ${ids.length} 张券？已核销/已作废/已过期的将跳过。`,
    '批量作废', { type: 'warning' }
  ).then(() => true).catch(() => false);
  if (!proceed) return;
  try {
    const resp = await vouchersApi.bulkCancel(ids);
    const tail = resp.skipped.length > 0 ? `；跳过 ${resp.skipped.length} 张：${summarizeSkipped(resp.skipped)}` : '';
    ElMessage.success(`已作废 ${resp.affected} 张${tail}`);
    selected.value = [];
    await reload();
  } catch { /* http 拦截器已弹 */ }
}

async function bulkDelete() {
  const ids = selected.value.filter((r) => r.status === 'Cancelled').map((r) => r.id);
  if (ids.length === 0) { ElMessage.warning('没有可删除的券（只有「已作废」状态才能删除）'); return; }
  const proceed = await ElMessageBox.confirm(
    `确认永久删除 ${ids.length} 张已作废的券？删除后无法恢复。`,
    '批量删除', { type: 'error', confirmButtonText: '删除', confirmButtonClass: 'el-button--danger' }
  ).then(() => true).catch(() => false);
  if (!proceed) return;
  try {
    const resp = await vouchersApi.bulkDelete(ids);
    const tail = resp.skipped.length > 0 ? `；跳过 ${resp.skipped.length} 张：${summarizeSkipped(resp.skipped)}` : '';
    ElMessage.success(`已删除 ${resp.affected} 张${tail}`);
    selected.value = [];
    await reload();
  } catch { /* http 拦截器已弹 */ }
}

onMounted(reload);
</script>

<style scoped>
/* 视口锁定：工具栏 / 分页固定，仅 .table-wrap 内的表格滚动 */
.page { height: 100%; display: flex; flex-direction: column; min-height: 0; }
.page > :deep(.el-card) { flex: 1 1 auto; display: flex; flex-direction: column; min-height: 0; }
.page > :deep(.el-card) > :deep(.el-card__body) {
  flex: 1 1 auto;
  display: flex;
  flex-direction: column;
  min-height: 0;
  overflow: hidden;
}
.toolbar { display: flex; gap: 12px; align-items: center; flex: 0 0 auto; }
.table-wrap { flex: 1 1 auto; min-height: 0; margin-top: 12px; }
.spacer { flex: 1; }
.batch-result {
  padding: 12px;
  background: #f6fbf7;
  border: 1px solid #d4e9d8;
  border-radius: 4px;
}
.batch-summary {
  display: flex;
  align-items: center;
  gap: 6px;
  font-size: 13px;
  color: #1f2937;
  margin-bottom: 10px;
  padding-bottom: 8px;
  border-bottom: 1px dashed #d4e9d8;
}
.batch-summary .muted { color: #999; }
.batch-result-head {
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-bottom: 8px;
  font-size: 13px;
  color: #2D6A4F;
}
.pager { display: flex; justify-content: flex-end; padding: 12px 0 0; flex: 0 0 auto; }
</style>
