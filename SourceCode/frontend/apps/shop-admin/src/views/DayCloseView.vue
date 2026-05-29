<template>
  <div class="page">
    <el-card shadow="never">
      <div class="toolbar">
        <span class="title">日结 / 交接班</span>
        <el-date-picker
          v-model="businessDate"
          type="date"
          format="YYYY-MM-DD"
          value-format="YYYY-MM-DD"
          :disabled-date="disableFuture"
          @change="(v: string) => loadPreviewForDate(v)"
        />
        <el-button :icon="Refresh" @click="loadPreview">刷新</el-button>
        <span v-if="preview && preview.dayCloseCutoffMinutes > 0" class="cutoff-hint">
          营业日切日 {{ formatCutoff(preview.dayCloseCutoffMinutes) }}
        </span>
      </div>

      <div v-if="preview" class="preview">
        <el-alert v-if="preview.alreadyClosed" type="warning" :closable="false" show-icon>
          该日已日结，仅供查看。
        </el-alert>

        <el-row :gutter="16" style="margin-top: 12px">
          <el-col :span="6">
            <el-statistic title="完成订单数" :value="preview.orderCount" />
          </el-col>
          <el-col :span="6">
            <el-statistic title="营业总额" :value="preview.revenueTotal" :precision="2" prefix="¥" />
          </el-col>
          <el-col :span="6">
            <el-statistic title="预期现金" :value="preview.expectedCash" :precision="2" prefix="¥" />
          </el-col>
          <el-col :span="6">
            <el-statistic title="充值入账" :value="preview.rechargeAmount" :precision="2" prefix="¥" />
          </el-col>
        </el-row>

        <el-divider>支付方式分布</el-divider>
        <el-row :gutter="16">
          <el-col :span="4"><el-statistic title="现金" :value="preview.cashAmount" :precision="2" prefix="¥" /></el-col>
          <el-col :span="4"><el-statistic title="会员卡" :value="preview.memberCardAmount" :precision="2" prefix="¥" /></el-col>
          <el-col :span="4"><el-statistic title="微信" :value="preview.wechatAmount" :precision="2" prefix="¥" /></el-col>
          <el-col :span="4"><el-statistic title="支付宝" :value="preview.alipayAmount" :precision="2" prefix="¥" /></el-col>
          <el-col :span="4"><el-statistic title="银行卡" :value="preview.bankCardAmount" :precision="2" prefix="¥" /></el-col>
        </el-row>

        <el-divider>实收清点</el-divider>
        <el-form label-width="120px" style="max-width: 480px">
          <el-form-item label="实际现金">
            <el-input-number
              v-model="actualCash"
              :precision="2"
              :step="10"
              :min="0"
              :disabled="preview.alreadyClosed"
              style="width: 200px"
            />
            <span v-if="!preview.alreadyClosed" class="variance" :class="varianceClass">
              差额：¥{{ variance.toFixed(2) }}
            </span>
          </el-form-item>
          <el-form-item label="备注">
            <el-input
              v-model="remark"
              type="textarea"
              :rows="2"
              maxlength="500"
              :disabled="preview.alreadyClosed"
            />
          </el-form-item>
          <el-form-item v-if="!preview.alreadyClosed">
            <el-button type="primary" :loading="submitting" @click="submit">
              提交日结
            </el-button>
          </el-form-item>
        </el-form>
      </div>
    </el-card>

    <el-card shadow="never" style="margin-top: 16px">
      <template #header><span>历史日结</span></template>
      <el-table :data="history" stripe>
        <el-table-column label="日期" width="120">
          <template #default="{ row }">{{ dayjs(row.businessDate).format('YYYY-MM-DD') }}</template>
        </el-table-column>
        <el-table-column label="订单数" width="80" prop="orderCount" />
        <el-table-column label="营业额" width="120">
          <template #default="{ row }">¥{{ row.revenueTotal.toFixed(2) }}</template>
        </el-table-column>
        <el-table-column label="预期现金" width="120">
          <template #default="{ row }">¥{{ row.expectedCash.toFixed(2) }}</template>
        </el-table-column>
        <el-table-column label="实收现金" width="120">
          <template #default="{ row }">¥{{ row.actualCash.toFixed(2) }}</template>
        </el-table-column>
        <el-table-column label="差额" width="120">
          <template #default="{ row }">
            <el-tag :type="row.variance === 0 ? 'success' : (row.variance < 0 ? 'danger' : 'warning')">
              ¥{{ row.variance.toFixed(2) }}
            </el-tag>
          </template>
        </el-table-column>
        <el-table-column label="操作员" width="120" prop="operatorName" />
        <el-table-column label="备注" min-width="160" prop="remark" show-overflow-tooltip />
        <el-table-column v-if="canRevoke" label="操作" width="100" fixed="right">
          <template #default="{ row }">
            <el-button link type="danger" @click="revoke(row)">撤销</el-button>
          </template>
        </el-table-column>
      </el-table>
    </el-card>
  </div>
</template>

<script setup lang="ts">
import { computed, onMounted, onUnmounted, ref, watch } from 'vue';
import { ElMessage, ElMessageBox } from 'element-plus';
import { Refresh } from '@element-plus/icons-vue';
import dayjs from 'dayjs';
import { dayClosesApi } from '@/api/modules';
import { useAppStore } from '@/stores/app';
import { useAuthStore } from '@/stores/auth';
import type { DayClose, DayClosePreview } from '@/api/types';

const appStore = useAppStore();
const authStore = useAuthStore();
const canRevoke = computed(() => authStore.isOwner || authStore.isManager);
const businessDate = ref(dayjs().format('YYYY-MM-DD'));
const preview = ref<DayClosePreview | null>(null);
const actualCash = ref(0);
const remark = ref('');
const submitting = ref(false);
const history = ref<DayClose[]>([]);

const variance = computed(() => actualCash.value - (preview.value?.expectedCash ?? 0));
const varianceClass = computed(() => {
  if (variance.value === 0) return 'ok';
  return variance.value < 0 ? 'short' : 'over';
});

function disableFuture(d: Date): boolean {
  return d.getTime() > Date.now();
}

async function loadPreview() {
  if (!appStore.activeStoreId) return;
  // 不显式传 date：让后端按门店切日时间返回"当前业务日"；前端再把它显示出来，
  // 避免页面跨午夜停留导致 businessDate 仍指向昨天。
  preview.value = await dayClosesApi.preview(appStore.activeStoreId);
  businessDate.value = dayjs(preview.value.businessDate).format('YYYY-MM-DD');
  actualCash.value = preview.value.expectedCash;
  remark.value = '';
}

async function loadPreviewForDate(date: string) {
  if (!appStore.activeStoreId) return;
  preview.value = await dayClosesApi.preview(appStore.activeStoreId, date);
  actualCash.value = preview.value.expectedCash;
  remark.value = '';
}

async function loadHistory() {
  if (!appStore.activeStoreId) return;
  history.value = await dayClosesApi.history(appStore.activeStoreId);
}

async function submit() {
  if (!appStore.activeStoreId || !preview.value) return;
  if (Math.abs(variance.value) > 0.005) {
    await ElMessageBox.confirm(
      `差额 ¥${variance.value.toFixed(2)}，确定提交？`,
      '差额确认',
      { type: 'warning' }
    ).catch(() => null);
  }
  submitting.value = true;
  try {
    await dayClosesApi.submit({
      storeId: appStore.activeStoreId,
      businessDate: businessDate.value,
      actualCash: actualCash.value,
      remark: remark.value || null
    });
    ElMessage.success('日结已提交');
    await Promise.all([loadPreview(), loadHistory()]);
  } catch {
    /* http 已弹错 */
  } finally {
    submitting.value = false;
  }
}

async function revoke(row: DayClose) {
  const dateLabel = dayjs(row.businessDate).format('YYYY-MM-DD');
  const confirmed = await ElMessageBox.prompt(
    `将撤销 ${dateLabel} 的日结记录（营业额 ¥${row.revenueTotal.toFixed(2)}）。撤销后可重新提交。请填写撤销原因：`,
    '撤销日结',
    {
      confirmButtonText: '确认撤销',
      cancelButtonText: '取消',
      type: 'warning',
      inputPattern: /.+/,
      inputErrorMessage: '请填写撤销原因'
    }
  ).catch(() => null);
  if (!confirmed) return;
  try {
    await dayClosesApi.revoke(row.id, confirmed.value);
    ElMessage.success('已撤销');
    await Promise.all([loadPreview(), loadHistory()]);
  } catch {
    /* http 已弹错 */
  }
}

function formatCutoff(m: number): string {
  const h = Math.floor(m / 60).toString().padStart(2, '0');
  const mm = (m % 60).toString().padStart(2, '0');
  return `${h}:${mm}`;
}

let refreshTimer: ReturnType<typeof setInterval> | null = null;
function onVisibility() {
  if (document.visibilityState === 'visible') loadPreview();
}

watch(() => appStore.activeStoreId, () => {
  loadPreview();
  loadHistory();
});

onMounted(async () => {
  await appStore.loadStores();
  await Promise.all([loadPreview(), loadHistory()]);
  document.addEventListener('visibilitychange', onVisibility);
  // 每 5 分钟自动跟当前业务日同步一次，避免页面停留跨切日点
  refreshTimer = setInterval(() => { loadPreview(); }, 5 * 60 * 1000);
});

onUnmounted(() => {
  document.removeEventListener('visibilitychange', onVisibility);
  if (refreshTimer) clearInterval(refreshTimer);
});
</script>

<style scoped>
.page { padding-bottom: 24px; }
.toolbar { display: flex; gap: 12px; align-items: center; }
.toolbar .title { font-weight: 600; font-size: 16px; }
.preview { margin-top: 16px; }
.variance { margin-left: 12px; font-weight: 600; }
.variance.ok { color: #67c23a; }
.variance.short { color: #f56c6c; }
.variance.over { color: #e6a23c; }
.cutoff-hint { color: #909399; font-size: 12px; margin-left: 8px; }
</style>
