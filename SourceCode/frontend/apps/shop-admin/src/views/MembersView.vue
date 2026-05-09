<template>
  <div class="page">
    <el-card shadow="never">
      <div class="toolbar">
        <el-input
          v-model="query.keyword"
          placeholder="卡号 / 手机号 / 姓名"
          clearable
          style="width: 240px"
          @keyup.enter="reload"
        />
        <el-button type="primary" @click="reload">查询</el-button>
        <el-button @click="resetQuery">重置</el-button>
        <el-button type="success" @click="openCreate">开卡</el-button>
      </div>

      <el-table :data="rows" v-loading="loading" stripe style="margin-top: 12px">
        <el-table-column prop="cardNo" label="卡号" width="120" />
        <el-table-column prop="name" label="姓名" width="100" />
        <el-table-column prop="phone" label="手机号" width="130" />
        <el-table-column label="余额" width="120">
          <template #default="{ row }">
            <strong style="color: #d9534f">¥{{ row.balance.toFixed(2) }}</strong>
          </template>
        </el-table-column>
        <el-table-column label="累计充值" width="120">
          <template #default="{ row }">¥{{ row.totalRecharge.toFixed(2) }}</template>
        </el-table-column>
        <el-table-column label="累计消费" width="120">
          <template #default="{ row }">¥{{ row.totalConsumed.toFixed(2) }}</template>
        </el-table-column>
        <el-table-column label="折扣" width="80">
          <template #default="{ row }">
            <el-tag v-if="row.discount < 1" size="small" type="warning">{{ (row.discount * 10).toFixed(1) }}折</el-tag>
            <span v-else>—</span>
          </template>
        </el-table-column>
        <el-table-column label="操作" width="220" fixed="right">
          <template #default="{ row }">
            <el-button link type="primary" @click="openRecharge(row)">充值</el-button>
            <el-button link type="primary" @click="openHistory(row)">流水</el-button>
            <el-button link type="primary" @click="openEdit(row)">编辑</el-button>
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

    <el-dialog v-model="formOpen" :title="formMode === 'create' ? '开卡' : '编辑会员'" width="480px">
      <el-form :model="form" :rules="rules" ref="formRef" label-width="100px">
        <el-form-item label="卡号" prop="cardNo">
          <el-input v-model="form.cardNo" :disabled="formMode === 'edit'" />
        </el-form-item>
        <el-form-item label="手机号" prop="phone">
          <el-input v-model="form.phone" />
        </el-form-item>
        <el-form-item label="姓名">
          <el-input v-model="form.name" />
        </el-form-item>
        <el-form-item label="性别">
          <el-radio-group v-model="form.gender">
            <el-radio value="男">男</el-radio>
            <el-radio value="女">女</el-radio>
          </el-radio-group>
        </el-form-item>
        <el-form-item label="生日">
          <el-date-picker v-model="form.birthday" type="date" placeholder="可选" value-format="YYYY-MM-DD" />
        </el-form-item>
        <el-form-item label="折扣" prop="discount">
          <el-input-number v-model="form.discount" :min="0.1" :max="1" :step="0.05" :precision="2" />
          <span class="muted" style="margin-left: 8px">如 0.85 = 8.5 折</span>
        </el-form-item>
        <el-form-item v-if="formMode === 'create'" label="初始充值">
          <el-input-number v-model="form.initialBalance" :min="0" :precision="2" :step="100" />
        </el-form-item>
        <el-form-item label="备注">
          <el-input v-model="form.remark" type="textarea" :rows="2" />
        </el-form-item>
      </el-form>
      <template #footer>
        <el-button @click="formOpen = false">取消</el-button>
        <el-button type="primary" :loading="saving" @click="saveForm">保存</el-button>
      </template>
    </el-dialog>

    <el-dialog v-model="rechargeOpen" :title="`充值：${rechargeTarget?.cardNo}`" width="420px">
      <el-form :model="rcForm" label-width="100px">
        <el-form-item label="当前余额">¥ {{ rechargeTarget?.balance.toFixed(2) ?? '0.00' }}</el-form-item>
        <el-form-item label="充值金额">
          <el-input-number v-model="rcForm.amount" :min="0" :precision="2" :step="100" />
        </el-form-item>
        <el-form-item label="赠送金额">
          <el-input-number v-model="rcForm.bonusAmount" :min="0" :precision="2" :step="50" />
        </el-form-item>
        <el-form-item label="支付方式">
          <el-radio-group v-model="rcForm.payMethod">
            <el-radio-button value="Cash">现金</el-radio-button>
            <el-radio-button value="Wechat">微信</el-radio-button>
            <el-radio-button value="Alipay">支付宝</el-radio-button>
            <el-radio-button value="BankCard">银行卡</el-radio-button>
          </el-radio-group>
        </el-form-item>
        <el-form-item label="备注">
          <el-input v-model="rcForm.remark" type="textarea" :rows="2" />
        </el-form-item>
      </el-form>
      <template #footer>
        <el-button @click="rechargeOpen = false">取消</el-button>
        <el-button type="primary" :loading="saving" @click="doRecharge">确认充值</el-button>
      </template>
    </el-dialog>

    <el-drawer v-model="historyOpen" title="会员流水" size="520px">
      <el-tabs v-if="historyTarget" v-model="historyTab">
        <el-tab-pane label="充值记录" name="recharge">
          <el-table :data="rechargeList" size="small">
            <el-table-column prop="amount" label="金额" width="100">
              <template #default="{ row }">¥{{ row.amount.toFixed(2) }}</template>
            </el-table-column>
            <el-table-column prop="bonusAmount" label="赠送" width="80">
              <template #default="{ row }">¥{{ row.bonusAmount.toFixed(2) }}</template>
            </el-table-column>
            <el-table-column prop="balanceAfter" label="充后余额" width="100">
              <template #default="{ row }">¥{{ row.balanceAfter.toFixed(2) }}</template>
            </el-table-column>
            <el-table-column prop="payMethod" label="支付" width="80" />
            <el-table-column label="时间" min-width="140">
              <template #default="{ row }">{{ dayjs(row.createdAt).format('YYYY-MM-DD HH:mm') }}</template>
            </el-table-column>
          </el-table>
        </el-tab-pane>
        <el-tab-pane label="消费记录" name="consume">
          <el-table :data="orderList" size="small">
            <el-table-column prop="orderNo" label="订单" min-width="160" />
            <el-table-column prop="paidAmount" label="实收" width="100">
              <template #default="{ row }">¥{{ row.paidAmount.toFixed(2) }}</template>
            </el-table-column>
            <el-table-column prop="status" label="状态" width="80" />
            <el-table-column label="时间" min-width="140">
              <template #default="{ row }">{{ dayjs(row.createdAt).format('YYYY-MM-DD HH:mm') }}</template>
            </el-table-column>
          </el-table>
        </el-tab-pane>
      </el-tabs>
    </el-drawer>
  </div>
</template>

<script setup lang="ts">
import { onMounted, reactive, ref } from 'vue';
import { ElMessage, type FormInstance, type FormRules } from 'element-plus';
import dayjs from 'dayjs';
import { membersApi } from '@/api/modules';
import { useAppStore } from '@/stores/app';
import type { Member } from '@/api/types';

const appStore = useAppStore();

const rows = ref<Member[]>([]);
const total = ref(0);
const loading = ref(false);
const saving = ref(false);

const query = reactive({ page: 1, pageSize: 20, keyword: '' });

const formOpen = ref(false);
const formMode = ref<'create' | 'edit'>('create');
const editingId = ref<number | null>(null);
const formRef = ref<FormInstance>();
const form = reactive({
  cardNo: '',
  phone: '',
  name: '',
  gender: '',
  birthday: '',
  discount: 1,
  initialBalance: 0,
  remark: ''
});
const rules: FormRules = {
  cardNo: [{ required: true, message: '请输入卡号', trigger: 'blur' }],
  phone: [{ required: true, message: '请输入手机号', trigger: 'blur' }],
  discount: [{ required: true, message: '请输入折扣', trigger: 'blur' }]
};

const rechargeOpen = ref(false);
const rechargeTarget = ref<Member | null>(null);
const rcForm = reactive({ amount: 100, bonusAmount: 0, payMethod: 'Cash', remark: '' });

const historyOpen = ref(false);
const historyTarget = ref<Member | null>(null);
const historyTab = ref<'recharge' | 'consume'>('recharge');
const rechargeList = ref<any[]>([]);
const orderList = ref<any[]>([]);

async function reload() {
  loading.value = true;
  try {
    const data = await membersApi.list({
      page: query.page,
      pageSize: query.pageSize,
      keyword: query.keyword || undefined,
      storeId: appStore.activeStoreId ?? undefined
    });
    rows.value = data.items;
    total.value = data.total;
  } finally {
    loading.value = false;
  }
}

function resetQuery() {
  query.keyword = '';
  query.page = 1;
  reload();
}

function openCreate() {
  formMode.value = 'create';
  editingId.value = null;
  Object.assign(form, { cardNo: '', phone: '', name: '', gender: '', birthday: '', discount: 1, initialBalance: 0, remark: '' });
  formOpen.value = true;
}

function openEdit(row: Member) {
  formMode.value = 'edit';
  editingId.value = row.id;
  Object.assign(form, {
    cardNo: row.cardNo,
    phone: row.phone,
    name: row.name ?? '',
    gender: row.gender ?? '',
    birthday: row.birthday ?? '',
    discount: row.discount,
    initialBalance: 0,
    remark: row.remark ?? ''
  });
  formOpen.value = true;
}

async function saveForm() {
  if (!formRef.value) return;
  const ok = await formRef.value.validate().catch(() => false);
  if (!ok) return;
  if (!appStore.activeStoreId) {
    ElMessage.error('未选择门店');
    return;
  }
  saving.value = true;
  try {
    if (formMode.value === 'create') {
      await membersApi.create({
        storeId: appStore.activeStoreId,
        cardNo: form.cardNo,
        phone: form.phone,
        name: form.name || null,
        gender: form.gender || null,
        birthday: form.birthday || null,
        discount: form.discount,
        initialBalance: form.initialBalance,
        remark: form.remark || null
      });
    } else if (editingId.value != null) {
      await membersApi.update(editingId.value, {
        phone: form.phone,
        name: form.name || null,
        gender: form.gender || null,
        birthday: form.birthday || null,
        discount: form.discount,
        remark: form.remark || null
      });
    }
    ElMessage.success('已保存');
    formOpen.value = false;
    reload();
  } finally {
    saving.value = false;
  }
}

function openRecharge(row: Member) {
  rechargeTarget.value = row;
  Object.assign(rcForm, { amount: 100, bonusAmount: 0, payMethod: 'Cash', remark: '' });
  rechargeOpen.value = true;
}

async function doRecharge() {
  if (!rechargeTarget.value) return;
  if (rcForm.amount <= 0) {
    ElMessage.warning('充值金额必须 > 0');
    return;
  }
  saving.value = true;
  try {
    await membersApi.recharge({
      memberId: rechargeTarget.value.id,
      amount: rcForm.amount,
      bonusAmount: rcForm.bonusAmount,
      payMethod: rcForm.payMethod,
      remark: rcForm.remark || null
    });
    ElMessage.success(`充值成功，到账 ¥${(rcForm.amount + rcForm.bonusAmount).toFixed(2)}`);
    rechargeOpen.value = false;
    reload();
  } finally {
    saving.value = false;
  }
}

async function openHistory(row: Member) {
  historyTarget.value = row;
  historyOpen.value = true;
  const [rs, os] = await Promise.all([
    membersApi.rechargeHistory(row.id),
    membersApi.consumptionHistory(row.id)
  ]);
  rechargeList.value = rs as any[];
  orderList.value = os as any[];
}

onMounted(async () => {
  await appStore.loadStores();
  reload();
});
</script>

<style scoped>
.page { padding-bottom: 24px; }
.toolbar { display: flex; gap: 8px; align-items: center; flex-wrap: wrap; }
.muted { color: var(--el-text-color-secondary); font-size: 12px; }
</style>
