<template>
  <div class="page">
    <el-card shadow="never">
      <div class="toolbar">
        <span class="title">会员套餐（计次/期限卡）</span>
        <el-select v-model="statusFilter" placeholder="全部状态" clearable style="width:140px" @change="reload">
          <el-option label="生效中" value="Active" />
          <el-option label="已用完" value="Used" />
          <el-option label="已过期" value="Expired" />
          <el-option label="已作废" value="Cancelled" />
        </el-select>
        <div class="spacer" />
        <el-button type="primary" :icon="Plus" @click="openNew">售卡</el-button>
        <el-button :icon="Refresh" @click="reload">刷新</el-button>
      </div>

      <el-table :data="rows" v-loading="loading" stripe style="margin-top:12px">
        <el-table-column prop="memberName" label="会员" width="120" />
        <el-table-column prop="title" label="套餐" min-width="160" />
        <el-table-column prop="kind" label="类型" width="100">
          <template #default="{ row }">{{ row.kind === 'Counter' ? '计次卡' : '期限卡' }}</template>
        </el-table-column>
        <el-table-column prop="serviceName" label="服务" width="140">
          <template #default="{ row }">{{ row.serviceName || '不限' }}</template>
        </el-table-column>
        <el-table-column label="次数" width="100">
          <template #default="{ row }">{{ row.remainCount }} / {{ row.totalCount }}</template>
        </el-table-column>
        <el-table-column prop="paidAmount" label="售价" width="100" />
        <el-table-column prop="expiresAt" label="到期" width="160">
          <template #default="{ row }">{{ row.expiresAt || '永不' }}</template>
        </el-table-column>
        <el-table-column prop="status" label="状态" width="100" />
        <el-table-column label="操作" width="120" fixed="right">
          <template #default="{ row }">
            <el-button size="small" type="danger" :disabled="row.status !== 'Active'" @click="cancel(row)">作废</el-button>
          </template>
        </el-table-column>
      </el-table>
    </el-card>

    <el-dialog v-model="formOpen" title="售卖会员套餐" width="480px">
      <el-form :model="form" label-width="100px">
        <el-form-item label="会员" required>
          <el-select v-model="form.memberId" filterable remote :remote-method="searchMembers" :loading="memberSearching" placeholder="按卡号/手机号搜索" style="width:100%">
            <el-option v-for="m in memberOptions" :key="m.id" :label="`${m.cardNo}（${m.name || m.phone}）`" :value="m.id" />
          </el-select>
        </el-form-item>
        <el-form-item label="类型" required>
          <el-radio-group v-model="form.kind">
            <el-radio value="Counter">计次卡</el-radio>
            <el-radio value="Period">期限卡</el-radio>
          </el-radio-group>
        </el-form-item>
        <el-form-item label="服务" :required="form.kind === 'Counter'">
          <el-select v-model="form.serviceId" filterable clearable placeholder="选择服务" style="width:100%">
            <el-option v-for="s in serviceList" :key="s.id" :label="s.name" :value="s.id" />
          </el-select>
        </el-form-item>
        <el-form-item label="套餐名称" required><el-input v-model="form.title" maxlength="100" /></el-form-item>
        <el-form-item label="售价"><el-input-number v-model="form.paidAmount" :min="0" :precision="2" /></el-form-item>
        <el-form-item label="总次数"><el-input-number v-model="form.totalCount" :min="0" /></el-form-item>
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
import {
  memberPackagesApi, membersApi, servicesApi,
  type MemberPackageDto
} from '@/api/modules';
import { useAppStore } from '@/stores/app';
import type { Member, ServiceItem } from '@/api/types';

const appStore = useAppStore();
const rows = ref<MemberPackageDto[]>([]);
const loading = ref(false);
const statusFilter = ref<string | undefined>(undefined);
const formOpen = ref(false);
const saving = ref(false);
const memberSearching = ref(false);
const memberOptions = ref<Member[]>([]);
const serviceList = ref<ServiceItem[]>([]);

const form = reactive({
  memberId: 0 as number, kind: 'Counter',
  serviceId: null as number | null,
  title: '', paidAmount: 0, totalCount: 10,
  validFrom: null as string | null, expiresAt: null as string | null,
  remark: ''
});

async function reload() {
  loading.value = true;
  try {
    rows.value = await memberPackagesApi.list({
      storeId: appStore.activeStoreId ?? undefined,
      status: statusFilter.value
    });
  } finally {
    loading.value = false;
  }
}

async function searchMembers(q: string) {
  if (!q) return;
  memberSearching.value = true;
  try {
    const data = await membersApi.list({ keyword: q, page: 1, pageSize: 20 });
    memberOptions.value = data.items;
  } finally {
    memberSearching.value = false;
  }
}

function openNew() {
  Object.assign(form, {
    memberId: 0, kind: 'Counter', serviceId: null,
    title: '', paidAmount: 0, totalCount: 10,
    validFrom: null, expiresAt: null, remark: ''
  });
  formOpen.value = true;
}

async function save() {
  if (!form.memberId) { ElMessage.warning('请选择会员'); return; }
  if (form.kind === 'Counter' && (!form.serviceId || form.totalCount < 1)) {
    ElMessage.warning('计次卡需选择服务且总次数 >= 1'); return;
  }
  saving.value = true;
  try {
    await memberPackagesApi.create({
      memberId: form.memberId, storeId: appStore.activeStoreId,
      kind: form.kind, serviceId: form.serviceId,
      title: form.title.trim(), paidAmount: form.paidAmount,
      totalCount: form.totalCount,
      validFrom: form.validFrom, expiresAt: form.expiresAt,
      remark: form.remark || null
    });
    formOpen.value = false;
    ElMessage.success('已售卡');
    await reload();
  } finally {
    saving.value = false;
  }
}

async function cancel(row: MemberPackageDto) {
  await ElMessageBox.confirm(`确认作废套餐"${row.title}"？`, '提示', { type: 'warning' }).catch(() => null);
  await memberPackagesApi.cancel(row.id);
  ElMessage.success('已作废');
  await reload();
}

onMounted(async () => {
  serviceList.value = await servicesApi.list();
  await reload();
});
</script>

<style scoped>
.page { padding-bottom: 24px; }
.toolbar { display: flex; gap: 12px; align-items: center; }
.toolbar .title { font-weight: 600; font-size: 16px; }
.spacer { flex: 1; }
</style>
