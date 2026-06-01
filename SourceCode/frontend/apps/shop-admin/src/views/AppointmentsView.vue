<template>
  <div class="page">
    <el-card shadow="never">
      <div class="toolbar">
        <el-select v-model="query.status" placeholder="全部状态" clearable style="width: 140px">
          <el-option label="待确认" value="Pending" />
          <el-option label="已确认" value="Confirmed" />
          <el-option label="已到店" value="Arrived" />
          <el-option label="已完成" value="Completed" />
          <el-option label="已取消" value="Cancelled" />
          <el-option label="未到店" value="NoShow" />
        </el-select>
        <el-input
          v-model="query.keyword"
          placeholder="会员姓名 / 电话"
          clearable
          style="width: 280px"
          aria-label="按会员姓名或电话模糊查询，回车直接搜索"
          @keyup.enter="onSearch"
          @clear="onSearch"
        />
        <el-date-picker
          v-model="dateRange"
          type="daterange"
          range-separator="至"
          start-placeholder="开始日期"
          end-placeholder="结束日期"
          format="YYYY-MM-DD"
          value-format="YYYY-MM-DD"
        />
        <el-button type="primary" @click="onSearch">查询</el-button>
        <el-button @click="resetQuery">重置</el-button>
        <el-button
          type="success"
          :aria-label="'登记新的电话预约'"
          @click="openCreate"
        >登记电话预约</el-button>
      </div>

      <div class="table-wrap">
      <el-table :data="rows" v-loading="loading" stripe height="100%">
        <el-table-column label="到店时间" width="170">
          <template #default="{ row }">{{ dayjs(row.expectedArriveAt).format('YYYY-MM-DD HH:mm:ss') }}</template>
        </el-table-column>
        <el-table-column prop="customerName" label="姓名" width="120" />
        <el-table-column prop="customerPhone" label="电话" width="130" />
        <el-table-column label="人数" width="60" prop="partySize" />
        <el-table-column label="服务" min-width="120" prop="serviceName" />
        <el-table-column label="指定技师" width="140" prop="preferredTechnicianName" />
        <el-table-column label="状态" width="100">
          <template #default="{ row }">
            <el-tag :type="statusType(row.status)">{{ statusLabel(row.status) }}</el-tag>
          </template>
        </el-table-column>
        <el-table-column label="备注" min-width="160" prop="remark" show-overflow-tooltip />
        <el-table-column label="操作" width="220" fixed="right">
          <template #default="{ row }">
            <el-button v-if="row.status === 'Pending'" size="small" type="primary"
                       :aria-label="`确认 ${row.customerName} 的预约`" @click="confirm(row)">
              确认
            </el-button>
            <el-button
              v-if="row.status === 'Pending'"
              size="small"
              :aria-label="`修改 ${row.customerName} 的预约信息`"
              @click="openEdit(row)"
            >
              修改
            </el-button>
            <el-button v-if="row.status === 'Pending' || row.status === 'Confirmed'" size="small" type="success"
                       :aria-label="`标记 ${row.customerName} 已到店`" @click="arrive(row)">
              到店
            </el-button>
            <el-button
              v-if="row.status === 'Pending' || row.status === 'Confirmed'"
              size="small"
              type="danger"
              :aria-label="`取消 ${row.customerName} 的预约`"
              @click="cancel(row)"
            >
              取消
            </el-button>
            <el-button
              v-if="row.status === 'Cancelled'"
              size="small"
              type="primary"
              :aria-label="`基于 ${row.customerName} 的取消单再次预约`"
              @click="openRebook(row)"
            >
              再次预约
            </el-button>
          </template>
        </el-table-column>
      </el-table>
      </div>

      <div class="pager">
        <el-pagination
          v-model:current-page="query.page"
          v-model:page-size="query.pageSize"
          :total="total"
          layout="total, sizes, prev, pager, next, jumper"
          :page-sizes="[20, 50, 100]"
          @change="reload"
        />
      </div>
    </el-card>

    <el-dialog
      v-model="createOpen"
      :title="dialogTitle"
      width="560px"
      :aria-label="dialogTitle + '对话框'"
    >
      <el-form ref="createFormRef" :model="form" :rules="formRules" label-width="100px">
        <el-form-item label="客户姓名" prop="customerName">
          <el-input
            v-model="form.customerName"
            placeholder="客人称呼，如 张先生"
            maxlength="32"
            aria-label="客户姓名"
          />
        </el-form-item>
        <el-form-item label="客户电话" prop="customerPhone">
          <el-input
            v-model="form.customerPhone"
            placeholder="11 位手机号"
            maxlength="20"
            aria-label="客户电话"
          />
        </el-form-item>
        <el-form-item label="到店时间" prop="expectedArriveAt">
          <el-date-picker
            v-model="form.expectedArriveAt"
            type="datetime"
            placeholder="预约到店时间"
            format="YYYY-MM-DD HH:mm"
            value-format="YYYY-MM-DDTHH:mm:ss[Z]"
            style="width: 100%"
            aria-label="到店时间"
          />
        </el-form-item>
        <el-form-item label="人数" prop="partySize">
          <el-input-number
            v-model="form.partySize"
            :min="1"
            :max="20"
            controls-position="right"
            style="width: 160px"
            aria-label="到店人数"
          />
        </el-form-item>
        <el-form-item label="服务项目">
          <el-select
            v-model="form.serviceId"
            placeholder="可空，客人未指定时不选"
            clearable
            filterable
            style="width: 100%"
            aria-label="预约服务项目，可不指定"
          >
            <el-option
              v-for="s in services"
              :key="s.id"
              :label="`${s.name}（${s.durationMinutes} 分钟）`"
              :value="s.id"
            />
          </el-select>
        </el-form-item>
        <el-form-item label="指定技师">
          <el-select
            v-model="form.preferredTechnicianId"
            placeholder="可空，客人未指定时不选"
            clearable
            filterable
            style="width: 100%"
            aria-label="指定技师，可不指定"
          >
            <el-option
              v-for="t in technicians"
              :key="t.id"
              :label="`${t.employeeNo ?? '-'} · ${t.realName ?? t.username}`"
              :value="t.id"
            />
          </el-select>
        </el-form-item>
        <el-form-item label="备注">
          <el-input
            v-model="form.remark"
            type="textarea"
            :rows="2"
            maxlength="200"
            placeholder="客人特殊要求、过敏、提前到等"
            aria-label="预约备注"
          />
        </el-form-item>
      </el-form>
      <template #footer>
        <el-button :aria-label="'关闭对话框'" @click="createOpen = false">取消</el-button>
        <el-button
          type="primary"
          :loading="saving"
          :aria-label="editingId ? '保存预约修改' : '确认登记电话预约'"
          @click="submitCreate"
        >{{ editingId ? '保存修改' : '登记' }}</el-button>
      </template>
    </el-dialog>
  </div>
</template>

<script setup lang="ts">
import { computed, onMounted, reactive, ref, watch } from 'vue';
import { ElMessage, ElMessageBox, type FormInstance, type FormRules } from 'element-plus';
import dayjs from 'dayjs';
import { appointmentsApi, servicesApi, staffApi } from '@/api/modules';
import { useAppStore } from '@/stores/app';
import type { Appointment, AppointmentStatus, ServiceItem, Staff } from '@/api/types';

const appStore = useAppStore();

const query = reactive<{ page: number; pageSize: number; status: AppointmentStatus | ''; keyword: string; storeId?: number }>({
  page: 1, pageSize: 20, status: '', keyword: ''
});
const dateRange = ref<[string, string] | null>(null);
const rows = ref<Appointment[]>([]);
const total = ref(0);
const loading = ref(false);

const STATUS_LABELS: Record<AppointmentStatus, string> = {
  Pending: '待确认', Confirmed: '已确认', Arrived: '已到店',
  Completed: '已完成', Cancelled: '已取消', NoShow: '未到店'
};
const STATUS_TYPES: Record<AppointmentStatus, string> = {
  Pending: 'warning', Confirmed: 'primary', Arrived: 'success',
  Completed: '', Cancelled: 'danger', NoShow: 'info'
};
function statusLabel(s: AppointmentStatus) { return STATUS_LABELS[s] ?? s; }
function statusType(s: AppointmentStatus) { return STATUS_TYPES[s] ?? ''; }

async function reload() {
  if (!appStore.activeStoreId) return;
  loading.value = true;
  try {
    const r = await appointmentsApi.list({
      storeId: appStore.activeStoreId,
      status: query.status || undefined,
      from: dateRange.value?.[0],
      to: dateRange.value?.[1] ? dayjs(dateRange.value[1]).add(1, 'day').format('YYYY-MM-DD') : undefined,
      keyword: query.keyword.trim() || undefined,
      page: query.page,
      pageSize: query.pageSize
    });
    rows.value = r.items;
    total.value = r.total;
  } finally {
    loading.value = false;
  }
}

/// 任何筛选条件改变后回到第 1 页再查
function onSearch() {
  query.page = 1;
  reload();
}

function resetQuery() {
  query.status = '';
  query.keyword = '';
  dateRange.value = null;
  query.page = 1;
  reload();
}

async function confirm(row: Appointment) {
  await appointmentsApi.confirm(row.id);
  ElMessage.success('已确认');
  await reload();
}

async function arrive(row: Appointment) {
  await appointmentsApi.arrive(row.id);
  ElMessage.success('已到店');
  await reload();
}

async function cancel(row: Appointment) {
  const { value } = await ElMessageBox.prompt('请输入取消原因（可选）', '取消预约', {
    inputPlaceholder: '比如：客人临时改期'
  }).catch(() => ({ value: null as string | null }));
  if (value === null) return;
  await appointmentsApi.cancel(row.id, value || null);
  ElMessage.success('已取消');
  await reload();
}

// ---- 登记电话预约 / 再次预约 / 修改预约 ----
const createOpen = ref(false);
const saving = ref(false);
const services = ref<ServiceItem[]>([]);
const technicians = ref<Staff[]>([]);
const createFormRef = ref<FormInstance | null>(null);
/// 非空 = 当前是"再次预约"模式（基于某条已取消单），仅用于切换弹窗标题做提示
const rebookFromId = ref<number | null>(null);
/// 非空 = 当前是"修改"模式，提交时走 update 而不是 create
const editingId = ref<number | null>(null);

const dialogTitle = computed(() => {
  if (editingId.value) return '修改预约信息';
  if (rebookFromId.value) return '再次预约（基于已取消单）';
  return '登记电话预约';
});
const form = reactive<{
  customerName: string;
  customerPhone: string;
  expectedArriveAt: string;
  partySize: number;
  serviceId: number | null;
  preferredTechnicianId: number | null;
  remark: string;
}>({
  customerName: '',
  customerPhone: '',
  expectedArriveAt: '',
  partySize: 1,
  serviceId: null,
  preferredTechnicianId: null,
  remark: ''
});
const formRules: FormRules = {
  customerName: [{ required: true, message: '请填写客户姓名', trigger: 'blur' }],
  customerPhone: [
    { required: true, message: '请填写客户电话', trigger: 'blur' },
    { pattern: /^\d{6,20}$/, message: '电话号码只允许数字', trigger: 'blur' }
  ],
  expectedArriveAt: [{ required: true, message: '请选择到店时间', trigger: 'change' }],
  partySize: [{ required: true, type: 'number', min: 1, max: 20, message: '人数 1-20', trigger: 'change' }]
};

/// 服务 / 技师列表懒加载：弹窗首开时拉一次，复用即可
async function ensureLookupsLoaded() {
  if (services.value.length > 0 && technicians.value.length > 0) return;
  try {
    const [s, t] = await Promise.all([
      servicesApi.list(false),
      staffApi.list({ role: 'Technician', pageSize: 200, storeId: appStore.activeStoreId ?? undefined })
    ]);
    services.value = s;
    technicians.value = t.items;
  } catch { /* http 已弹错 */ }
}

async function openCreate() {
  if (!appStore.activeStoreId) {
    ElMessage.warning('请先选择门店');
    return;
  }
  rebookFromId.value = null;
  editingId.value = null;
  Object.assign(form, {
    customerName: '',
    customerPhone: '',
    // 默认 30 分钟后，避免客户立刻到店时还要手动改。
    // 沿用 el-date-picker 的 value-format（本地时间字符串拼 Z），与查询区一致
    expectedArriveAt: dayjs().add(30, 'minute').format('YYYY-MM-DDTHH:mm:ss[Z]'),
    partySize: 1,
    serviceId: null,
    preferredTechnicianId: null,
    remark: ''
  });
  createOpen.value = true;
  await ensureLookupsLoaded();
}

/// 基于一条已取消单再次预约：客人信息照搬，时间清空让前台和客人重定
async function openRebook(row: Appointment) {
  if (!appStore.activeStoreId) {
    ElMessage.warning('请先选择门店');
    return;
  }
  rebookFromId.value = row.id;
  editingId.value = null;
  Object.assign(form, {
    customerName: row.customerName,
    customerPhone: row.customerPhone,
    // 旧时间已过去，给个 30 分钟后的默认，前台再据电话沟通调整
    expectedArriveAt: dayjs().add(30, 'minute').format('YYYY-MM-DDTHH:mm:ss[Z]'),
    partySize: row.partySize || 1,
    serviceId: row.serviceId ?? null,
    preferredTechnicianId: row.preferredTechnicianId ?? null,
    remark: row.remark ?? ''
  });
  createOpen.value = true;
  await ensureLookupsLoaded();
}

/// 修改未确认（Pending）的预约：信息全数预填，到店时间保留原值
async function openEdit(row: Appointment) {
  if (row.status !== 'Pending') {
    ElMessage.warning('仅未确认的预约可修改');
    return;
  }
  editingId.value = row.id;
  rebookFromId.value = null;
  Object.assign(form, {
    customerName: row.customerName,
    customerPhone: row.customerPhone,
    expectedArriveAt: row.expectedArriveAt,
    partySize: row.partySize || 1,
    serviceId: row.serviceId ?? null,
    preferredTechnicianId: row.preferredTechnicianId ?? null,
    remark: row.remark ?? ''
  });
  createOpen.value = true;
  await ensureLookupsLoaded();
}

async function submitCreate() {
  if (!createFormRef.value || !appStore.activeStoreId) return;
  const ok = await createFormRef.value.validate().catch(() => false);
  if (!ok) return;
  saving.value = true;
  try {
    const body = {
      serviceId: form.serviceId,
      preferredTechnicianId: form.preferredTechnicianId,
      customerName: form.customerName.trim(),
      customerPhone: form.customerPhone.trim(),
      expectedArriveAt: form.expectedArriveAt,
      partySize: form.partySize,
      remark: form.remark.trim() || null
    };
    if (editingId.value) {
      await appointmentsApi.update(editingId.value, body);
      ElMessage.success('已保存修改');
    } else {
      await appointmentsApi.create({ storeId: appStore.activeStoreId, ...body });
      ElMessage.success('已登记，状态待确认');
    }
    createOpen.value = false;
    await reload();
  } catch {
    /* http 已弹错 */
  } finally {
    saving.value = false;
  }
}

watch(() => appStore.activeStoreId, () => reload());
onMounted(async () => {
  await appStore.loadStores();
  await reload();
});
</script>

<style scoped>
.page { padding-bottom: 24px; }
.toolbar { display: flex; gap: 12px; align-items: center; flex-wrap: wrap; }
.pager { margin-top: 12px; display: flex; justify-content: flex-end; }
</style>
