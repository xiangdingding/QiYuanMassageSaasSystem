<template>
  <div class="page">
    <el-card shadow="never">
      <div class="toolbar">
        <el-input
          v-model="query.keyword"
          placeholder="账号 / 姓名 / 手机号"
          clearable
          style="width: 220px"
          @keyup.enter="reload"
        />
        <el-select v-model="query.role" placeholder="全部角色" clearable style="width: 140px">
          <el-option label="店主" value="ShopOwner" />
          <el-option label="店长" value="StoreManager" />
          <el-option label="收银员" value="Cashier" />
          <el-option label="技师" value="Technician" />
        </el-select>
        <el-select v-model="query.storeId" placeholder="全部门店" clearable style="width: 160px">
          <el-option v-for="s in appStore.stores" :key="s.id" :label="s.name" :value="s.id" />
        </el-select>
        <el-button type="primary" @click="reload">查询</el-button>
        <el-button @click="resetQuery">重置</el-button>
        <el-button type="success" @click="openCreate">添加员工</el-button>
      </div>

      <div class="table-wrap">
      <el-table :data="rows" v-loading="loading" stripe height="100%">
        <el-table-column prop="employeeNo" label="工号" width="80" />
        <el-table-column prop="username" label="账号" min-width="140" />
        <el-table-column prop="realName" label="姓名" min-width="110" />
        <el-table-column prop="phone" label="手机号" min-width="140" />
        <el-table-column label="角色" width="100">
          <template #default="{ row }">{{ roleLabel(row.role) }}</template>
        </el-table-column>
        <el-table-column label="所属门店" min-width="160">
          <template #default="{ row }">{{ storeName(row.storeId) }}</template>
        </el-table-column>
        <el-table-column label="盲人" width="80">
          <template #default="{ row }">
            <el-tag v-if="row.isBlind" size="small">盲人</el-tag>
            <span v-else>—</span>
          </template>
        </el-table-column>
        <el-table-column label="状态" width="80">
          <template #default="{ row }">
            <el-tag :type="row.isActive ? 'success' : 'info'">{{ row.isActive ? '在职' : '停用' }}</el-tag>
          </template>
        </el-table-column>
        <el-table-column label="操作" :width="$actCol(300)" fixed="right">
          <template #default="{ row }">
            <el-button link type="primary" @click="openEdit(row)">编辑</el-button>
            <el-button link type="warning" @click="openResetPwd(row)">重置密码</el-button>
            <el-button link type="primary" @click="openTransfer(row)">跨店调动</el-button>
          </template>
        </el-table-column>
      </el-table>
      </div>

      <el-pagination
        class="pager"
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

    <el-dialog v-model="formOpen" :title="formMode === 'create' ? '添加员工' : '编辑员工'" width="820px" top="6vh">
      <el-form :model="form" :rules="rules" ref="formRef" label-width="100px">
        <el-row :gutter="16">
          <el-col :span="12">
            <el-form-item label="工号">
              <el-input-number v-model="form.employeeNo" :min="0" :precision="0" style="width:100%" />
            </el-form-item>
          </el-col>
          <el-col :span="12">
            <el-form-item label="手机号" prop="phone">
              <el-input v-model="form.phone" placeholder="11 位手机号，用作登录账号" />
            </el-form-item>
          </el-col>

          <el-col v-if="formMode === 'create'" :span="12">
            <el-form-item label="初始密码" prop="password">
              <el-input v-model="form.password" type="password" show-password placeholder="至少 6 位" />
            </el-form-item>
          </el-col>
          <el-col :span="12">
            <el-form-item label="姓名" prop="realName">
              <el-input v-model="form.realName" />
            </el-form-item>
          </el-col>

          <el-col :span="12">
            <el-form-item label="身份证号" prop="idCardNo" required>
              <el-input v-model="form.idCardNo" placeholder="18 位身份证号" maxlength="18" @input="onIdCardChange" />
            </el-form-item>
          </el-col>
          <el-col :span="12">
            <el-form-item label="出生日期">
              <el-date-picker v-model="form.birthDate" type="date" value-format="YYYY-MM-DD" placeholder="可由身份证号自动带出" style="width:100%" />
            </el-form-item>
          </el-col>

          <el-col :span="12">
            <el-form-item label="紧急联系人">
              <el-input v-model="form.emergencyContactName" placeholder="姓名" />
            </el-form-item>
          </el-col>
          <el-col :span="12">
            <el-form-item label="联系人电话">
              <el-input v-model="form.emergencyContactPhone" placeholder="紧急联系电话" />
            </el-form-item>
          </el-col>

          <el-col :span="12">
            <el-form-item label="入职日期">
              <el-date-picker v-model="form.hireDate" type="date" value-format="YYYY-MM-DD" style="width:100%" />
            </el-form-item>
          </el-col>
          <el-col :span="12">
            <el-form-item label="离职日期">
              <el-date-picker v-model="form.terminationDate" type="date" value-format="YYYY-MM-DD" placeholder="在职留空" style="width:100%" />
            </el-form-item>
          </el-col>

          <el-col :span="12">
            <el-form-item label="角色" prop="role">
              <el-select v-model="form.role" style="width:100%">
                <el-option label="店主" value="ShopOwner" />
                <el-option label="店长" value="StoreManager" />
                <el-option label="收银员" value="Cashier" />
                <el-option label="技师" value="Technician" />
              </el-select>
            </el-form-item>
          </el-col>
          <el-col :span="12">
            <el-form-item label="所属门店" prop="storeId">
              <el-select v-model="form.storeId" style="width:100%">
                <el-option v-for="s in appStore.stores" :key="s.id" :label="s.name" :value="s.id" />
              </el-select>
            </el-form-item>
          </el-col>

          <el-col :span="12">
            <el-form-item label="盲人技师">
              <el-switch v-model="form.isBlind" />
            </el-form-item>
          </el-col>
          <el-col :span="12">
            <el-form-item label="状态">
              <el-switch v-model="form.isActive" active-text="在职" inactive-text="停用" />
            </el-form-item>
          </el-col>

          <el-col :span="24">
            <el-form-item label="专长">
              <div class="spec-tags" role="group" aria-label="选择专长（可多选）">
                <el-check-tag
                  v-for="opt in SPECIALTY_OPTIONS"
                  :key="opt"
                  :checked="selectedSpecialties.includes(opt)"
                  class="spec-tag"
                  :aria-label="`专长 ${opt}${selectedSpecialties.includes(opt) ? '，已选中' : ''}`"
                  @change="toggleSpecialty(opt)"
                >
                  {{ opt }}
                </el-check-tag>
              </div>
              <span class="muted" style="margin-left:4px">可多选，便于排班和派单</span>
            </el-form-item>
          </el-col>
        </el-row>
      </el-form>
      <template #footer>
        <el-button @click="formOpen = false">取消</el-button>
        <el-button type="primary" :loading="saving" @click="save">保存</el-button>
      </template>
    </el-dialog>

    <el-dialog v-model="pwdOpen" :title="`重置密码：${pwdTarget?.username}`" width="380px">
      <el-form>
        <el-form-item label="新密码">
          <el-input v-model="newPassword" type="password" show-password placeholder="至少 6 位" />
        </el-form-item>
      </el-form>
      <template #footer>
        <el-button @click="pwdOpen = false">取消</el-button>
        <el-button type="primary" :loading="saving" @click="doResetPwd">确认</el-button>
      </template>
    </el-dialog>

    <el-dialog v-model="transferOpen" :title="`跨店调动：${transferTarget?.realName || transferTarget?.username}`" width="520px">
      <el-form :model="tfForm" label-width="110px">
        <el-form-item label="当前门店">{{ storeName(transferTarget?.storeId) }}</el-form-item>
        <el-form-item label="调入门店" required>
          <el-select v-model="tfForm.toStoreId" style="width: 100%">
            <el-option
              v-for="s in appStore.stores"
              :key="s.id"
              :label="s.name"
              :value="s.id"
              :disabled="s.id === transferTarget?.storeId"
            />
          </el-select>
        </el-form-item>
        <el-form-item label="调动类型" required>
          <el-radio-group v-model="tfForm.kind">
            <el-radio value="Permanent">永久调动</el-radio>
            <el-radio value="Temporary">临时借调</el-radio>
          </el-radio-group>
        </el-form-item>
        <el-form-item v-if="tfForm.kind === 'Temporary'" label="预计归还" required>
          <el-date-picker v-model="tfForm.expectedReturnAt" type="date" value-format="YYYY-MM-DD" placeholder="选择日期" />
        </el-form-item>
        <el-form-item label="原因">
          <el-input v-model="tfForm.reason" type="textarea" :rows="2" maxlength="500" />
        </el-form-item>
        <el-alert
          type="warning"
          :closable="false"
          title="调动后该员工的叫号队列会迁到新店并置为下班，需在新店重新上钟。"
        />
      </el-form>

      <div v-if="transferHistory.length" class="history">
        <h4>调动历史</h4>
        <el-table :data="transferHistory" size="small">
          <el-table-column label="方向" min-width="160">
            <template #default="{ row }">{{ row.fromStoreName }} → {{ row.toStoreName }}</template>
          </el-table-column>
          <el-table-column label="类型" width="90">
            <template #default="{ row }">{{ row.kind === 'Permanent' ? '永久' : '临时' }}</template>
          </el-table-column>
          <el-table-column label="状态" width="90">
            <template #default="{ row }">
              <el-tag size="small" :type="row.status === 'InEffect' ? 'success' : 'info'">
                {{ transferStatusLabel(row.status) }}
              </el-tag>
            </template>
          </el-table-column>
          <el-table-column label="操作" :width="$actCol(90)" fixed="right">
            <template #default="{ row }">
              <el-button
                v-if="row.kind === 'Temporary' && row.status === 'InEffect'"
                link type="primary" size="small"
                @click="returnTransfer(row)">
                归还
              </el-button>
            </template>
          </el-table-column>
        </el-table>
      </div>

      <template #footer>
        <el-button @click="transferOpen = false">取消</el-button>
        <el-button type="primary" :loading="saving" @click="doTransfer">确认调动</el-button>
      </template>
    </el-dialog>
  </div>
</template>

<script setup lang="ts">
import { computed, onMounted, reactive, ref } from 'vue';
import { ElMessage, ElMessageBox, type FormInstance, type FormRules } from 'element-plus';
import { staffApi, type StaffTransferDto } from '@/api/modules';
import { useAppStore } from '@/stores/app';
import type { Staff } from '@/api/types';

const appStore = useAppStore();

const rows = ref<Staff[]>([]);
const total = ref(0);
const loading = ref(false);
const saving = ref(false);

const query = reactive({ page: 1, pageSize: 20, keyword: '', role: '', storeId: null as number | null });

const formOpen = ref(false);
const formMode = ref<'create' | 'edit'>('create');
const editingId = ref<number | null>(null);
const formRef = ref<FormInstance>();
const form = reactive({
  password: '',
  realName: '',
  phone: '',
  employeeNo: null as number | null,
  role: 'Technician' as 'ShopOwner' | 'StoreManager' | 'Cashier' | 'Technician',
  storeId: null as number | null,
  isBlind: false,
  isActive: true,
  idCardNo: '',
  birthDate: '' as string | null,
  emergencyContactName: '',
  emergencyContactPhone: '',
  hireDate: '' as string | null,
  terminationDate: '' as string | null,
  specialties: ''
});

/// 可选专长清单。后续若需要按租户自定义，可改成接口拉取。
const SPECIALTY_OPTIONS = [
  '肩颈', '足疗', '头疗', '推拿', '按摩', '艾灸', '拔罐', '刮痧',
  '中医理疗', '泰式按摩', '精油 SPA', '产后修复', '小儿推拿', '盲人按摩'
];

const selectedSpecialties = computed(() =>
  form.specialties ? form.specialties.split(',').map((s) => s.trim()).filter(Boolean) : []
);

function toggleSpecialty(opt: string) {
  const set = new Set(selectedSpecialties.value);
  if (set.has(opt)) set.delete(opt); else set.add(opt);
  form.specialties = Array.from(set).join(',');
}
const rules: FormRules = {
  phone: [
    { required: true, message: '请输入手机号', trigger: 'blur' },
    { pattern: /^\d{11}$/, message: '请输入 11 位手机号', trigger: 'blur' }
  ],
  password: [{ required: true, message: '请输入密码', trigger: 'blur' }, { min: 6, message: '至少 6 位', trigger: 'blur' }],
  realName: [{ required: true, message: '请输入姓名', trigger: 'blur' }],
  idCardNo: [
    { required: true, message: '请输入身份证号', trigger: 'blur' },
    {
      validator: (_r, val: string, cb) => {
        if (!val) return cb();
        if (!/^\d{17}[\dXx]$/.test(val)) return cb(new Error('身份证号格式不正确（18 位）'));
        cb();
      },
      trigger: 'blur'
    }
  ],
  role: [{ required: true, message: '请选择角色', trigger: 'change' }],
  storeId: [{ required: true, message: '请选择门店', trigger: 'change' }]
};

/// 身份证 6..13 位 = YYYYMMDD → 自动写入出生日期
function onIdCardChange(v: string) {
  if (!v || v.length < 14) return;
  if (!/^\d{17}[\dXx]$|^\d{14,}/.test(v)) return;
  const y = v.slice(6, 10);
  const m = v.slice(10, 12);
  const d = v.slice(12, 14);
  if (/^\d{4}$/.test(y) && /^\d{2}$/.test(m) && /^\d{2}$/.test(d)) {
    const mm = parseInt(m, 10);
    const dd = parseInt(d, 10);
    if (mm >= 1 && mm <= 12 && dd >= 1 && dd <= 31) {
      form.birthDate = `${y}-${m}-${d}`;
    }
  }
}

const pwdOpen = ref(false);
const pwdTarget = ref<Staff | null>(null);
const newPassword = ref('');

function roleLabel(r: string) {
  return ({ ShopOwner: '店主', StoreManager: '店长', Cashier: '收银员', Technician: '技师', PlatformAdmin: '平台' } as Record<string, string>)[r] ?? r;
}
const storeMap = computed(() => Object.fromEntries(appStore.stores.map((s) => [s.id, s.name])));
function storeName(id: number | null | undefined) {
  if (!id) return '—';
  return storeMap.value[id] ?? `#${id}`;
}

async function reload() {
  loading.value = true;
  try {
    const data = await staffApi.list({
      page: query.page,
      pageSize: query.pageSize,
      keyword: query.keyword || undefined,
      role: query.role || undefined,
      storeId: query.storeId ?? undefined
    });
    rows.value = data.items;
    total.value = data.total;
  } finally {
    loading.value = false;
  }
}

function resetQuery() {
  query.keyword = '';
  query.role = '';
  query.storeId = null;
  query.page = 1;
  reload();
}

function openCreate() {
  formMode.value = 'create';
  editingId.value = null;
  Object.assign(form, {
    password: '', realName: '', phone: '',
    employeeNo: null, role: 'Technician',
    storeId: appStore.activeStoreId,
    isBlind: false, isActive: true,
    idCardNo: '', birthDate: '',
    emergencyContactName: '', emergencyContactPhone: '',
    hireDate: '', terminationDate: '',
    specialties: ''
  });
  formOpen.value = true;
}

function openEdit(row: Staff) {
  formMode.value = 'edit';
  editingId.value = row.id;
  Object.assign(form, {
    password: '',
    realName: row.realName ?? '',
    phone: row.phone ?? '',
    employeeNo: row.employeeNo ?? null,
    role: row.role as any,
    storeId: row.storeId ?? null,
    isBlind: row.isBlind,
    isActive: row.isActive,
    idCardNo: row.idCardNo ?? '',
    birthDate: row.birthDate ?? '',
    emergencyContactName: row.emergencyContactName ?? '',
    emergencyContactPhone: row.emergencyContactPhone ?? '',
    hireDate: row.hireDate ?? '',
    terminationDate: row.terminationDate ?? '',
    specialties: row.specialties ?? ''
  });
  formOpen.value = true;
}

async function save() {
  if (!formRef.value) return;
  const ok = await formRef.value.validate().catch(() => false);
  if (!ok) return;
  saving.value = true;
  try {
    if (formMode.value === 'create') {
      await staffApi.create({
        username: form.phone, // 用手机号当登录账号
        password: form.password,
        realName: form.realName || null,
        phone: form.phone || null,
        employeeNo: form.employeeNo,
        role: form.role,
        storeId: form.storeId,
        isBlind: form.isBlind,
        idCardNo: form.idCardNo || null,
        birthDate: form.birthDate || null,
        emergencyContactName: form.emergencyContactName || null,
        emergencyContactPhone: form.emergencyContactPhone || null,
        hireDate: form.hireDate || null,
        terminationDate: form.terminationDate || null,
        specialties: form.specialties || null
      });
    } else if (editingId.value != null) {
      await staffApi.update(editingId.value, {
        realName: form.realName || null,
        phone: form.phone || null,
        employeeNo: form.employeeNo,
        role: form.role,
        storeId: form.storeId,
        isBlind: form.isBlind,
        isActive: form.isActive,
        idCardNo: form.idCardNo || null,
        birthDate: form.birthDate || null,
        emergencyContactName: form.emergencyContactName || null,
        emergencyContactPhone: form.emergencyContactPhone || null,
        hireDate: form.hireDate || null,
        terminationDate: form.terminationDate || null,
        specialties: form.specialties || null
      });
    }
    ElMessage.success('已保存');
    formOpen.value = false;
    reload();
  } finally {
    saving.value = false;
  }
}

function openResetPwd(row: Staff) {
  pwdTarget.value = row;
  newPassword.value = '';
  pwdOpen.value = true;
}
async function doResetPwd() {
  if (!pwdTarget.value) return;
  if (newPassword.value.length < 6) {
    ElMessage.warning('密码至少 6 位');
    return;
  }
  saving.value = true;
  try {
    await staffApi.resetPassword(pwdTarget.value.id, newPassword.value);
    ElMessage.success('密码已重置');
    pwdOpen.value = false;
  } finally {
    saving.value = false;
  }
}

// ---- 跨店调动 ----
const transferOpen = ref(false);
const transferTarget = ref<Staff | null>(null);
const transferHistory = ref<StaffTransferDto[]>([]);
const tfForm = reactive<{ toStoreId: number | null; kind: string; expectedReturnAt: string | null; reason: string }>(
  { toStoreId: null, kind: 'Permanent', expectedReturnAt: null, reason: '' }
);

function transferStatusLabel(s: string) {
  return ({ InEffect: '生效中', Returned: '已归还', Cancelled: '已撤销' } as Record<string, string>)[s] ?? s;
}

async function openTransfer(row: Staff) {
  transferTarget.value = row;
  tfForm.toStoreId = null;
  tfForm.kind = 'Permanent';
  tfForm.expectedReturnAt = null;
  tfForm.reason = '';
  transferOpen.value = true;
  transferHistory.value = await staffApi.transfers({ userId: row.id });
}

async function doTransfer() {
  if (!transferTarget.value) return;
  if (!tfForm.toStoreId) {
    ElMessage.warning('请选择调入门店');
    return;
  }
  if (tfForm.kind === 'Temporary' && !tfForm.expectedReturnAt) {
    ElMessage.warning('临时借调需填预计归还日期');
    return;
  }
  saving.value = true;
  try {
    await staffApi.transfer(transferTarget.value.id, {
      toStoreId: tfForm.toStoreId,
      kind: tfForm.kind,
      expectedReturnAt: tfForm.kind === 'Temporary' ? tfForm.expectedReturnAt : null,
      reason: tfForm.reason || null
    });
    ElMessage.success('调动完成');
    transferOpen.value = false;
    reload();
  } finally {
    saving.value = false;
  }
}

async function returnTransfer(row: StaffTransferDto) {
  await ElMessageBox.confirm(
    `确认归还借调？该员工将调回 ${row.fromStoreName}。`, '提示', { type: 'warning' }
  ).catch(() => null);
  await staffApi.returnTransfer(row.id);
  ElMessage.success('已归还');
  transferOpen.value = false;
  reload();
}

onMounted(async () => {
  await appStore.loadStores();
  reload();
});
</script>

<style scoped>
.page { padding-bottom: 24px; }
.toolbar { display: flex; gap: 8px; align-items: center; flex-wrap: wrap; }
.history { margin-top: 16px; }
.history h4 { margin: 8px 0; }
.muted { color: var(--el-text-color-secondary); font-size: 12px; }
.spec-tags { display: flex; flex-wrap: wrap; gap: 8px; width: 100%; }
.spec-tag {
  font-size: 14px;
  padding: 6px 14px;
  cursor: pointer;
  user-select: none;
}
</style>
