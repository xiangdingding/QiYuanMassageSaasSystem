<template>
  <div class="page">
    <el-card shadow="never">
      <div class="toolbar">
        <span class="title">优惠券 / 团购券</span>
        <el-select v-model="statusFilter" placeholder="全部状态" clearable style="width:140px" @change="reload">
          <el-option label="生效中" value="Active" />
          <el-option label="已核销" value="Redeemed" />
          <el-option label="已过期" value="Expired" />
          <el-option label="已作废" value="Cancelled" />
        </el-select>
        <el-input v-model="keyword" placeholder="搜索券码/标题" style="width:220px" clearable @keydown.enter="reload" />
        <div class="spacer" />
        <el-button type="primary" :icon="Plus" @click="openNew">新建券</el-button>
        <el-button :icon="Refresh" @click="reload">刷新</el-button>
      </div>

      <el-table :data="rows" v-loading="loading" stripe style="margin-top:12px">
        <el-table-column prop="kind" label="类型" width="100">
          <template #default="{ row }">{{ row.kind === 'GroupBuy' ? '团购券' : '店内券' }}</template>
        </el-table-column>
        <el-table-column prop="code" label="券码" width="160" />
        <el-table-column prop="title" label="标题" min-width="160" />
        <el-table-column prop="faceValue" label="面值" width="100" />
        <el-table-column prop="minOrderAmount" label="最低额" width="100" />
        <el-table-column prop="discountPercent" label="折扣" width="100">
          <template #default="{ row }">{{ row.discountPercent ? (row.discountPercent * 10).toFixed(1) + '折' : '—' }}</template>
        </el-table-column>
        <el-table-column prop="platform" label="平台" width="100" />
        <el-table-column prop="status" label="状态" width="100">
          <template #default="{ row }">
            <el-tag :type="statusTag(row.status)">{{ statusLabel(row.status) }}</el-tag>
          </template>
        </el-table-column>
        <el-table-column prop="expiresAt" label="到期" width="160" />
        <el-table-column label="操作" width="120" fixed="right">
          <template #default="{ row }">
            <el-button size="small" type="danger" :disabled="row.status !== 'Active'" @click="cancel(row)">作废</el-button>
          </template>
        </el-table-column>
      </el-table>
    </el-card>

    <el-dialog v-model="formOpen" title="新建券" width="480px">
      <el-form :model="form" label-width="100px">
        <el-form-item label="类型" required>
          <el-radio-group v-model="form.kind">
            <el-radio value="StoreCoupon">店内券</el-radio>
            <el-radio value="GroupBuy">团购券</el-radio>
          </el-radio-group>
        </el-form-item>
        <el-form-item label="券码" required><el-input v-model="form.code" maxlength="64" /></el-form-item>
        <el-form-item label="标题" required><el-input v-model="form.title" maxlength="200" /></el-form-item>
        <el-form-item label="面值"><el-input-number v-model="form.faceValue" :min="0" :precision="2" /></el-form-item>
        <el-form-item label="最低订单额"><el-input-number v-model="form.minOrderAmount" :min="0" :precision="2" /></el-form-item>
        <el-form-item label="折扣百分比">
          <el-input-number v-model="form.discountPercent" :min="0" :max="1" :step="0.05" :precision="2" />
          <span style="margin-left:8px;color:#999">0.9 表示 9 折</span>
        </el-form-item>
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
import { onMounted, reactive, ref } from 'vue';
import { ElMessage, ElMessageBox } from 'element-plus';
import { Plus, Refresh } from '@element-plus/icons-vue';
import { vouchersApi, type VoucherDto } from '@/api/modules';

const rows = ref<VoucherDto[]>([]);
const loading = ref(false);
const statusFilter = ref<string | undefined>(undefined);
const keyword = ref('');
const formOpen = ref(false);
const saving = ref(false);

const form = reactive({
  kind: 'StoreCoupon', code: '', title: '',
  faceValue: 0, minOrderAmount: 0, discountPercent: 0,
  validFrom: null as string | null, expiresAt: null as string | null,
  platform: '', remark: ''
});

function statusLabel(s: string) {
  return ({ Active: '生效中', Redeemed: '已核销', Expired: '已过期', Cancelled: '已作废' } as Record<string, string>)[s] ?? s;
}
function statusTag(s: string): 'success' | 'info' | 'warning' | 'danger' {
  return s === 'Active' ? 'success' : s === 'Redeemed' ? 'info' : s === 'Expired' ? 'warning' : 'danger';
}

async function reload() {
  loading.value = true;
  try {
    rows.value = await vouchersApi.list({ status: statusFilter.value, keyword: keyword.value || undefined });
  } finally {
    loading.value = false;
  }
}

function openNew() {
  Object.assign(form, {
    kind: 'StoreCoupon', code: '', title: '',
    faceValue: 0, minOrderAmount: 0, discountPercent: 0,
    validFrom: null, expiresAt: null, platform: '', remark: ''
  });
  formOpen.value = true;
}

async function save() {
  if (!form.code.trim() || !form.title.trim()) { ElMessage.warning('券码与标题必填'); return; }
  saving.value = true;
  try {
    await vouchersApi.create({
      kind: form.kind, code: form.code.trim(), title: form.title.trim(),
      faceValue: form.faceValue, minOrderAmount: form.minOrderAmount,
      discountPercent: form.discountPercent || null,
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

async function cancel(row: VoucherDto) {
  await ElMessageBox.confirm(`确认作废券 ${row.code}？`, '提示', { type: 'warning' }).catch(() => null);
  await vouchersApi.cancel(row.id);
  ElMessage.success('已作废');
  await reload();
}

onMounted(reload);
</script>

<style scoped>
.page { padding-bottom: 24px; }
.toolbar { display: flex; gap: 12px; align-items: center; }
.toolbar .title { font-weight: 600; font-size: 16px; }
.spacer { flex: 1; }
</style>
