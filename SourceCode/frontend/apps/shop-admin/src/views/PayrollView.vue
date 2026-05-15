<template>
  <div class="page">
    <el-tabs v-model="tab">
      <el-tab-pane label="工资单" name="periods">
        <el-card shadow="never">
          <div class="toolbar">
            <span class="title">{{ activeStoreName }} 工资单</span>
            <el-date-picker
              v-model="yearFilter"
              type="year"
              format="YYYY"
              value-format="YYYY"
              placeholder="年份"
              style="width: 120px"
              @change="loadPeriods"
            />
            <div class="spacer" />
            <el-date-picker
              v-model="genMonth"
              type="month"
              format="YYYY-MM"
              value-format="YYYY-MM"
              placeholder="月份"
              style="width: 140px"
            />
            <el-button type="primary" :icon="Plus" :loading="generating" @click="generate">生成工资单</el-button>
            <el-button :icon="Refresh" @click="loadPeriods">刷新</el-button>
          </div>

          <el-table :data="periods" v-loading="loading" stripe style="margin-top:12px">
            <el-table-column label="月份" width="120">
              <template #default="{ row }">{{ row.year }}-{{ String(row.month).padStart(2, '0') }}</template>
            </el-table-column>
            <el-table-column prop="status" label="状态" width="100">
              <template #default="{ row }">
                <el-tag :type="statusTag(row.status)">{{ statusLabel(row.status) }}</el-tag>
              </template>
            </el-table-column>
            <el-table-column prop="itemCount" label="人数" width="80" />
            <el-table-column label="工资总额" width="140">
              <template #default="{ row }">¥{{ row.totalAmount.toFixed(2) }}</template>
            </el-table-column>
            <el-table-column prop="generatedAt" label="生成时间" width="180" />
            <el-table-column prop="operatorName" label="操作人" width="120" />
            <el-table-column prop="remark" label="备注" min-width="160" show-overflow-tooltip />
            <el-table-column label="操作" width="280" fixed="right">
              <template #default="{ row }">
                <el-button size="small" @click="openDetail(row)">查看</el-button>
                <el-button size="small" type="warning" :disabled="row.status !== 'Draft'" @click="lockPeriod(row)">封盘</el-button>
                <el-button size="small" type="success" :disabled="row.status !== 'Locked'" @click="markPaid(row)">已发放</el-button>
                <el-button size="small" type="danger" :disabled="row.status !== 'Draft'" @click="removeDraft(row)">删除</el-button>
              </template>
            </el-table-column>
          </el-table>
        </el-card>
      </el-tab-pane>

      <el-tab-pane label="薪资配置" name="profiles">
        <el-card shadow="never">
          <div class="toolbar">
            <span class="title">薪资配置（{{ activeStoreName }}）</span>
            <div class="spacer" />
            <el-button :icon="Refresh" @click="loadProfiles">刷新</el-button>
          </div>
          <el-table :data="profiles" v-loading="loading" stripe style="margin-top:12px">
            <el-table-column prop="userName" label="员工" width="140" />
            <el-table-column label="月底薪" width="120">
              <template #default="{ row }">¥{{ row.baseMonthly.toFixed(2) }}</template>
            </el-table-column>
            <el-table-column label="加班时薪" width="120">
              <template #default="{ row }">¥{{ row.overtimeHourRate.toFixed(2) }}</template>
            </el-table-column>
            <el-table-column label="满勤奖" width="140">
              <template #default="{ row }">¥{{ row.attendanceBonusAmount.toFixed(2) }} / {{ row.requiredAttendanceDays }} 天</template>
            </el-table-column>
            <el-table-column prop="remark" label="备注" min-width="160" />
            <el-table-column label="操作" width="120" fixed="right">
              <template #default="{ row }">
                <el-button size="small" @click="openProfile(row)">编辑</el-button>
              </template>
            </el-table-column>
          </el-table>
          <div v-if="profiles.length === 0" class="empty">还没有员工配置薪资。先在"员工管理"里建好员工，然后在这里编辑。</div>
        </el-card>
      </el-tab-pane>
    </el-tabs>

    <!-- 工资单详情对话框 -->
    <el-dialog v-model="detailOpen" :title="detailTitle" width="980px">
      <div v-if="detail" class="detail">
        <div class="metric-row">
          <div>状态：<el-tag :type="statusTag(detail.period.status)">{{ statusLabel(detail.period.status) }}</el-tag></div>
          <div>工资总额：<strong>¥{{ detail.period.totalAmount.toFixed(2) }}</strong></div>
          <div>人数：{{ detail.period.itemCount }}</div>
        </div>

        <el-table :data="detail.items" stripe size="small">
          <el-table-column prop="employeeNo" label="工号" width="70" />
          <el-table-column prop="userName" label="员工" width="100" />
          <el-table-column label="底薪" width="90">
            <template #default="{ row }">¥{{ row.baseSalary.toFixed(2) }}</template>
          </el-table-column>
          <el-table-column label="提成" width="100">
            <template #default="{ row }">¥{{ row.commissionTotal.toFixed(2) }}</template>
          </el-table-column>
          <el-table-column label="加班" width="120">
            <template #default="{ row }">
              {{ row.overtimeHours }}h / ¥{{ row.overtimeAmount.toFixed(2) }}
            </template>
          </el-table-column>
          <el-table-column label="满勤" width="90">
            <template #default="{ row }">¥{{ row.attendanceBonus.toFixed(2) }}</template>
          </el-table-column>
          <el-table-column label="调整" width="90">
            <template #default="{ row }">
              <span :class="{ neg: row.adjustmentTotal < 0 }">¥{{ row.adjustmentTotal.toFixed(2) }}</span>
            </template>
          </el-table-column>
          <el-table-column label="小费" width="80">
            <template #default="{ row }">¥{{ row.tipsTotal.toFixed(2) }}</template>
          </el-table-column>
          <el-table-column label="净额" width="120">
            <template #default="{ row }"><strong style="color:#d9534f">¥{{ row.netTotal.toFixed(2) }}</strong></template>
          </el-table-column>
          <el-table-column label="排班/请假" width="100">
            <template #default="{ row }">{{ row.scheduledDays }}/{{ row.leaveDays }}</template>
          </el-table-column>
          <el-table-column label="操作" width="160" fixed="right">
            <template #default="{ row }">
              <el-button size="small" :disabled="detail!.period.status !== 'Draft'" @click="openEditItem(row)">改</el-button>
              <el-button size="small" :disabled="detail!.period.status !== 'Draft'" @click="openAddAdj(row)">奖/扣</el-button>
            </template>
          </el-table-column>
        </el-table>

        <div v-if="expandedItem" class="adj-section">
          <h4>{{ expandedItem.userName }} 的奖扣明细</h4>
          <el-table :data="expandedItem.adjustments" size="small" stripe>
            <el-table-column prop="kind" label="类型" width="80">
              <template #default="{ row }">{{ row.kind === 'Bonus' ? '奖金' : '扣款' }}</template>
            </el-table-column>
            <el-table-column label="金额" width="100">
              <template #default="{ row }">
                <span :class="{ neg: row.kind === 'Deduction' }">
                  {{ row.kind === 'Bonus' ? '+' : '-' }}¥{{ row.amount.toFixed(2) }}
                </span>
              </template>
            </el-table-column>
            <el-table-column prop="reason" label="原因" min-width="160" />
            <el-table-column prop="operatorName" label="操作人" width="100" />
            <el-table-column prop="createdAt" label="时间" width="160" />
            <el-table-column label="操作" width="80" fixed="right">
              <template #default="{ row }">
                <el-button size="small" type="danger" :disabled="detail!.period.status !== 'Draft'" @click="removeAdj(expandedItem!.id, row.id)">删</el-button>
              </template>
            </el-table-column>
          </el-table>
        </div>
      </div>
    </el-dialog>

    <!-- 工资单条目编辑 -->
    <el-dialog v-model="editItemOpen" title="编辑工资项" width="420px">
      <el-form v-if="editingItem" :model="editForm" label-width="120px">
        <el-form-item label="员工">{{ editingItem.userName }}</el-form-item>
        <el-form-item label="加班小时数">
          <el-input-number v-model="editForm.overtimeHours" :min="0" :precision="2" />
        </el-form-item>
        <el-form-item label="满勤奖（覆盖）">
          <el-input-number v-model="editForm.attendanceBonusOverride" :min="-1" :precision="2" />
          <span class="hint">填 -1 沿用配置自动计算</span>
        </el-form-item>
        <el-form-item label="备注">
          <el-input v-model="editForm.remark" type="textarea" :rows="2" maxlength="500" />
        </el-form-item>
      </el-form>
      <template #footer>
        <el-button @click="editItemOpen = false">取消</el-button>
        <el-button type="primary" :loading="saving" @click="saveItem">保存</el-button>
      </template>
    </el-dialog>

    <!-- 奖扣录入 -->
    <el-dialog v-model="addAdjOpen" title="新增奖金/扣款" width="420px">
      <el-form v-if="adjTarget" :model="adjForm" label-width="100px">
        <el-form-item label="员工">{{ adjTarget.userName }}</el-form-item>
        <el-form-item label="类型" required>
          <el-radio-group v-model="adjForm.kind">
            <el-radio value="Bonus">奖金（加项）</el-radio>
            <el-radio value="Deduction">扣款（减项）</el-radio>
          </el-radio-group>
        </el-form-item>
        <el-form-item label="金额" required>
          <el-input-number v-model="adjForm.amount" :min="0.01" :precision="2" />
        </el-form-item>
        <el-form-item label="原因" required>
          <el-input v-model="adjForm.reason" maxlength="200" placeholder="如：迟到 3 次扣款 / 推荐新员工奖金" />
        </el-form-item>
      </el-form>
      <template #footer>
        <el-button @click="addAdjOpen = false">取消</el-button>
        <el-button type="primary" :loading="saving" @click="saveAdj">保存</el-button>
      </template>
    </el-dialog>

    <!-- 薪资配置编辑 -->
    <el-dialog v-model="profileOpen" title="薪资配置" width="460px">
      <el-form v-if="editingProfile" :model="profileForm" label-width="110px">
        <el-form-item label="员工">{{ editingProfile.userName }}</el-form-item>
        <el-form-item label="月底薪">
          <el-input-number v-model="profileForm.baseMonthly" :min="0" :precision="2" />
        </el-form-item>
        <el-form-item label="加班时薪">
          <el-input-number v-model="profileForm.overtimeHourRate" :min="0" :precision="2" />
        </el-form-item>
        <el-form-item label="满勤奖额度">
          <el-input-number v-model="profileForm.attendanceBonusAmount" :min="0" :precision="2" />
        </el-form-item>
        <el-form-item label="满勤所需天数">
          <el-input-number v-model="profileForm.requiredAttendanceDays" :min="0" />
          <span class="hint">0 = 不发满勤</span>
        </el-form-item>
        <el-form-item label="备注">
          <el-input v-model="profileForm.remark" type="textarea" :rows="2" maxlength="500" />
        </el-form-item>
      </el-form>
      <template #footer>
        <el-button @click="profileOpen = false">取消</el-button>
        <el-button type="primary" :loading="saving" @click="saveProfile">保存</el-button>
      </template>
    </el-dialog>
  </div>
</template>

<script setup lang="ts">
import { computed, onMounted, reactive, ref, watch } from 'vue';
import dayjs from 'dayjs';
import { ElMessage, ElMessageBox } from 'element-plus';
import { Plus, Refresh } from '@element-plus/icons-vue';
import {
  payrollApi, staffApi,
  type PayrollPeriodDto, type PayrollPeriodDetailDto, type PayrollItemDto,
  type SalaryProfileDto
} from '@/api/modules';
import { useAppStore } from '@/stores/app';
import type { Staff } from '@/api/types';

const appStore = useAppStore();
const tab = ref<'periods' | 'profiles'>('periods');

const periods = ref<PayrollPeriodDto[]>([]);
const profiles = ref<SalaryProfileDto[]>([]);
const loading = ref(false);
const saving = ref(false);
const generating = ref(false);

const yearFilter = ref<string | null>(dayjs().format('YYYY'));
const genMonth = ref(dayjs().format('YYYY-MM'));

const detailOpen = ref(false);
const detail = ref<PayrollPeriodDetailDto | null>(null);
const expandedItem = ref<PayrollItemDto | null>(null);

const editItemOpen = ref(false);
const editingItem = ref<PayrollItemDto | null>(null);
const editForm = reactive({ overtimeHours: 0, attendanceBonusOverride: -1, remark: '' });

const addAdjOpen = ref(false);
const adjTarget = ref<PayrollItemDto | null>(null);
const adjForm = reactive({ kind: 'Bonus', amount: 0, reason: '' });

const profileOpen = ref(false);
const editingProfile = ref<SalaryProfileDto | null>(null);
const profileForm = reactive({
  baseMonthly: 0, overtimeHourRate: 0,
  attendanceBonusAmount: 0, requiredAttendanceDays: 0, remark: ''
});

const activeStoreName = computed(
  () => appStore.stores.find((s) => s.id === appStore.activeStoreId)?.name ?? ''
);
const detailTitle = computed(() =>
  detail.value ? `${detail.value.period.year}-${String(detail.value.period.month).padStart(2, '0')} 工资单` : ''
);

function statusLabel(s: string) {
  return ({ Draft: '草稿', Locked: '已封盘', Paid: '已发放' } as Record<string, string>)[s] ?? s;
}
function statusTag(s: string): 'info' | 'warning' | 'success' {
  return s === 'Draft' ? 'info' : s === 'Locked' ? 'warning' : 'success';
}

async function loadPeriods() {
  if (!appStore.activeStoreId) return;
  loading.value = true;
  try {
    periods.value = await payrollApi.periods(
      appStore.activeStoreId,
      yearFilter.value ? Number(yearFilter.value) : undefined
    );
  } finally {
    loading.value = false;
  }
}

async function loadProfiles() {
  if (!appStore.activeStoreId) return;
  loading.value = true;
  try {
    // 拿员工再合并 profile（员工有可能没设过）
    const staff = (await staffApi.list({ storeId: appStore.activeStoreId, page: 1, pageSize: 200 })).items;
    const profileMap = new Map<number, SalaryProfileDto>(
      (await payrollApi.profiles(appStore.activeStoreId)).map((p) => [p.userId, p])
    );
    profiles.value = staff.map((u: Staff) => profileMap.get(u.id) ?? ({
      userId: u.id, userName: u.realName ?? u.username,
      baseMonthly: 0, overtimeHourRate: 0,
      attendanceBonusAmount: 0, requiredAttendanceDays: 0, remark: null
    }));
  } finally {
    loading.value = false;
  }
}

async function generate() {
  if (!appStore.activeStoreId) return;
  const [y, m] = genMonth.value.split('-').map(Number);
  generating.value = true;
  try {
    await payrollApi.generate(appStore.activeStoreId, y, m, null);
    ElMessage.success('已生成草稿，请进入查看明细');
    await loadPeriods();
  } finally {
    generating.value = false;
  }
}

async function openDetail(row: PayrollPeriodDto) {
  detail.value = await payrollApi.period(row.id);
  expandedItem.value = null;
  detailOpen.value = true;
}

async function lockPeriod(row: PayrollPeriodDto) {
  await ElMessageBox.confirm(
    `封盘后该月工资单不可再修改。确认封盘 ${row.year}-${row.month}？`,
    '提示', { type: 'warning' }
  ).catch(() => null);
  await payrollApi.lock(row.id);
  ElMessage.success('已封盘');
  await loadPeriods();
}

async function markPaid(row: PayrollPeriodDto) {
  await ElMessageBox.confirm(`确认 ${row.year}-${row.month} 已发放工资？`, '提示').catch(() => null);
  await payrollApi.markPaid(row.id);
  ElMessage.success('已标记发放');
  await loadPeriods();
}

async function removeDraft(row: PayrollPeriodDto) {
  await ElMessageBox.confirm(`确认删除 ${row.year}-${row.month} 工资单草稿？`, '提示', { type: 'warning' }).catch(() => null);
  await payrollApi.removeDraft(row.id);
  ElMessage.success('已删除');
  await loadPeriods();
}

function openEditItem(item: PayrollItemDto) {
  editingItem.value = item;
  editForm.overtimeHours = item.overtimeHours;
  editForm.attendanceBonusOverride = -1;
  editForm.remark = item.remark ?? '';
  editItemOpen.value = true;
}

async function saveItem() {
  if (!editingItem.value || !detail.value) return;
  saving.value = true;
  try {
    const updated = await payrollApi.updateItem(
      editingItem.value.id,
      editForm.overtimeHours,
      editForm.attendanceBonusOverride,
      editForm.remark || null
    );
    mergeItem(updated);
    editItemOpen.value = false;
    ElMessage.success('已保存');
    await refreshTotals();
  } finally {
    saving.value = false;
  }
}

function openAddAdj(item: PayrollItemDto) {
  adjTarget.value = item;
  expandedItem.value = item;
  adjForm.kind = 'Bonus';
  adjForm.amount = 0;
  adjForm.reason = '';
  addAdjOpen.value = true;
}

async function saveAdj() {
  if (!adjTarget.value) return;
  if (adjForm.amount <= 0 || !adjForm.reason.trim()) {
    ElMessage.warning('金额与原因必填');
    return;
  }
  saving.value = true;
  try {
    const updated = await payrollApi.addAdjustment(
      adjTarget.value.id, adjForm.kind, adjForm.amount, adjForm.reason.trim()
    );
    mergeItem(updated);
    expandedItem.value = updated;
    addAdjOpen.value = false;
    ElMessage.success('已新增');
    await refreshTotals();
  } finally {
    saving.value = false;
  }
}

async function removeAdj(itemId: number, adjId: number) {
  const updated = await payrollApi.removeAdjustment(itemId, adjId);
  mergeItem(updated);
  if (expandedItem.value?.id === itemId) expandedItem.value = updated;
  ElMessage.success('已删除');
  await refreshTotals();
}

function mergeItem(updated: PayrollItemDto) {
  if (!detail.value) return;
  const idx = detail.value.items.findIndex((i) => i.id === updated.id);
  if (idx >= 0) detail.value.items.splice(idx, 1, updated);
}

async function refreshTotals() {
  if (!detail.value) return;
  detail.value.period.totalAmount = detail.value.items.reduce((a, b) => a + b.netTotal, 0);
}

function openProfile(p: SalaryProfileDto) {
  editingProfile.value = p;
  profileForm.baseMonthly = p.baseMonthly;
  profileForm.overtimeHourRate = p.overtimeHourRate;
  profileForm.attendanceBonusAmount = p.attendanceBonusAmount;
  profileForm.requiredAttendanceDays = p.requiredAttendanceDays;
  profileForm.remark = p.remark ?? '';
  profileOpen.value = true;
}

async function saveProfile() {
  if (!editingProfile.value) return;
  saving.value = true;
  try {
    await payrollApi.upsertProfile(editingProfile.value.userId, {
      baseMonthly: profileForm.baseMonthly,
      overtimeHourRate: profileForm.overtimeHourRate,
      attendanceBonusAmount: profileForm.attendanceBonusAmount,
      requiredAttendanceDays: profileForm.requiredAttendanceDays,
      remark: profileForm.remark || null
    });
    profileOpen.value = false;
    ElMessage.success('已保存');
    await loadProfiles();
  } finally {
    saving.value = false;
  }
}

watch(() => appStore.activeStoreId, () => {
  if (tab.value === 'periods') loadPeriods();
  else loadProfiles();
});
watch(tab, (t) => {
  if (t === 'periods') loadPeriods();
  else loadProfiles();
});

onMounted(async () => {
  await appStore.loadStores();
  await loadPeriods();
});
</script>

<style scoped>
.page { padding-bottom: 24px; }
.toolbar { display: flex; gap: 12px; align-items: center; flex-wrap: wrap; }
.toolbar .title { font-weight: 600; font-size: 16px; }
.spacer { flex: 1; }
.empty { color: #999; padding: 40px 0; text-align: center; }
.detail { display: flex; flex-direction: column; gap: 12px; }
.metric-row { display: flex; gap: 32px; align-items: center; font-size: 14px; }
.adj-section { padding-top: 8px; border-top: 1px dashed var(--el-border-color); }
.adj-section h4 { margin: 8px 0; }
.hint { color: #999; margin-left: 8px; font-size: 12px; }
.neg { color: #c45656; }
</style>
