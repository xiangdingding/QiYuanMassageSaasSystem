<template>
  <div class="page">
    <el-card shadow="never">
      <div class="toolbar" role="search">
        <el-input
          v-model="query.keyword"
          placeholder="卡号 / 手机号 / 姓名"
          clearable
          style="width: 240px"
          aria-label="搜索会员，输入卡号、手机号或姓名后回车"
          @keyup.enter="reload"
        />
        <el-button type="primary" aria-label="执行会员搜索" @click="reload">查询</el-button>
        <el-button aria-label="重置搜索条件" @click="resetQuery">重置</el-button>
        <el-button type="success" aria-label="开新会员卡" @click="openCreate">开卡</el-button>
      </div>

      <div class="toolbar" style="margin-top:8px">
        <el-checkbox v-model="query.includeClosed" @change="reload">显示已关闭会员</el-checkbox>
      </div>

      <el-table :data="rows" v-loading="loading" stripe style="margin-top: 12px">
        <el-table-column prop="cardNo" label="卡号" width="120">
          <template #default="{ row }">
            <span :class="{ closed: !row.isActive }">{{ row.cardNo }}</span>
            <el-tag v-if="!row.isActive" size="small" type="info" style="margin-left:6px">已关闭</el-tag>
          </template>
        </el-table-column>
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
        <el-table-column label="引荐人" width="120">
          <template #default="{ row }">
            <span v-if="row.referredByMemberName">{{ row.referredByMemberName }}</span>
            <span v-else class="muted">—</span>
          </template>
        </el-table-column>
        <el-table-column label="操作" width="380" fixed="right">
          <template #default="{ row }">
            <el-button link type="primary" :disabled="!row.isActive" :aria-label="`给 ${row.name || row.cardNo} 充值`" @click="openRecharge(row)">充值</el-button>
            <el-button link type="primary" :aria-label="`查看 ${row.name || row.cardNo} 的流水`" @click="openHistory(row)">流水</el-button>
            <el-button link type="primary" :aria-label="`编辑 ${row.name || row.cardNo}`" @click="openEdit(row)">编辑</el-button>
            <el-button link type="warning" :disabled="!row.isActive || row.balance <= 0" :aria-label="`给 ${row.name || row.cardNo} 退卡`" @click="openRefund(row)">退卡</el-button>
            <el-button link type="warning" :disabled="!row.isActive || row.balance <= 0" :aria-label="`把 ${row.name || row.cardNo} 余额转赠`" @click="openTransfer(row)">转赠</el-button>
            <el-button link type="info" :aria-label="`查看 ${row.name || row.cardNo} 的引荐返佣`" @click="openReferrals(row)">引荐</el-button>
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
        <el-form-item label="引荐人">
          <el-input v-model="form.referrerKeyword" placeholder="卡号 / 手机号 搜索后选择" @keyup.enter="searchReferrer" />
          <el-button link size="small" @click="searchReferrer">查找</el-button>
          <el-radio-group v-if="referrerCandidates.length" v-model="form.referredByMemberId" style="margin-top:6px; display:flex; flex-direction:column; gap:6px">
            <el-radio v-for="c in referrerCandidates" :key="c.id" :value="c.id">
              {{ c.cardNo }}（{{ c.name || '未填' }} / {{ c.phone }}）
            </el-radio>
          </el-radio-group>
          <span v-if="form.referredByMemberId" class="muted">已选引荐人 ID = {{ form.referredByMemberId }} <el-button link type="danger" size="small" @click="form.referredByMemberId = null">清除</el-button></span>
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

    <el-drawer v-model="historyOpen" title="会员流水" size="640px">
      <el-tabs v-if="historyTarget" v-model="historyTab">
        <el-tab-pane label="资金流水" name="recharge">
          <el-table :data="rechargeList" size="small">
            <el-table-column label="类型" width="90">
              <template #default="{ row }">
                <el-tag size="small" :type="rechargeKindTag(row.kind)">{{ rechargeKindLabel(row.kind) }}</el-tag>
              </template>
            </el-table-column>
            <el-table-column prop="amount" label="金额" width="100">
              <template #default="{ row }">¥{{ row.amount.toFixed(2) }}</template>
            </el-table-column>
            <el-table-column prop="bonusAmount" label="赠送" width="80">
              <template #default="{ row }">¥{{ row.bonusAmount.toFixed(2) }}</template>
            </el-table-column>
            <el-table-column prop="balanceAfter" label="后余额" width="100">
              <template #default="{ row }">¥{{ row.balanceAfter.toFixed(2) }}</template>
            </el-table-column>
            <el-table-column prop="payMethod" label="支付" width="80" />
            <el-table-column label="对手会员" width="120">
              <template #default="{ row }">{{ row.counterpartyMemberName || '—' }}</template>
            </el-table-column>
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

    <el-dialog v-model="refundOpen" :title="`退卡：${refundTarget?.cardNo}`" width="420px">
      <el-form :model="rfForm" label-width="100px">
        <el-form-item label="当前余额">
          <strong style="color: #d9534f">¥ {{ refundTarget?.balance.toFixed(2) ?? '0.00' }}</strong>
        </el-form-item>
        <el-form-item label="退款金额" required>
          <el-input-number v-model="rfForm.refundAmount" :min="0.01" :max="refundTarget?.balance ?? 0" :precision="2" :step="50" />
          <el-button link size="small" style="margin-left:8px" @click="rfForm.refundAmount = refundTarget?.balance ?? 0">全退</el-button>
        </el-form-item>
        <el-form-item label="退款方式" required>
          <el-radio-group v-model="rfForm.refundMethod">
            <el-radio-button value="Cash">现金</el-radio-button>
            <el-radio-button value="Wechat">微信</el-radio-button>
            <el-radio-button value="Alipay">支付宝</el-radio-button>
            <el-radio-button value="BankCard">银行卡</el-radio-button>
          </el-radio-group>
        </el-form-item>
        <el-form-item label="原因">
          <el-input v-model="rfForm.reason" type="textarea" :rows="2" maxlength="200" />
        </el-form-item>
        <el-alert type="warning" :closable="false" title="退卡后会员卡将被关闭，不能再充值或消费。请确认与客户达成一致再操作。" />
      </el-form>
      <template #footer>
        <el-button @click="refundOpen = false">取消</el-button>
        <el-button type="warning" :loading="saving" @click="doRefund">确认退卡</el-button>
      </template>
    </el-dialog>

    <el-dialog v-model="transferOpen" :title="`转赠：${transferTarget?.cardNo}`" width="460px">
      <el-form :model="tfForm" label-width="100px">
        <el-form-item label="当前余额">
          <strong style="color: #d9534f">¥ {{ transferTarget?.balance.toFixed(2) ?? '0.00' }}</strong>
          <span class="muted" style="margin-left:8px">将一并转走</span>
        </el-form-item>
        <el-form-item label="转赠对象">
          <el-radio-group v-model="tfForm.mode">
            <el-radio-button value="existing">已有会员</el-radio-button>
            <el-radio-button value="new">新建会员</el-radio-button>
          </el-radio-group>
        </el-form-item>
        <template v-if="tfForm.mode === 'existing'">
          <el-form-item label="目标会员">
            <el-input v-model="tfForm.toQuery" placeholder="输入卡号 / 手机号搜索" @keyup.enter="searchTarget" />
            <el-button link size="small" @click="searchTarget">查找</el-button>
            <el-radio-group v-if="targetCandidates.length" v-model="tfForm.toMemberId" style="margin-top:8px; display:flex; flex-direction:column; gap:6px">
              <el-radio v-for="c in targetCandidates" :key="c.id" :value="c.id">
                {{ c.cardNo }}（{{ c.name || '未填' }} / {{ c.phone }}） 余额 ¥{{ c.balance.toFixed(2) }}
              </el-radio>
            </el-radio-group>
          </el-form-item>
        </template>
        <template v-else>
          <el-form-item label="新会员卡号" required>
            <el-input v-model="tfForm.newMemberCardNo" />
          </el-form-item>
          <el-form-item label="手机号" required>
            <el-input v-model="tfForm.newMemberPhone" />
          </el-form-item>
          <el-form-item label="姓名">
            <el-input v-model="tfForm.newMemberName" />
          </el-form-item>
        </template>
        <el-form-item label="原因">
          <el-input v-model="tfForm.reason" type="textarea" :rows="2" maxlength="200" placeholder="如：转赠给家人 / 卡号变更" />
        </el-form-item>
        <el-alert type="warning" :closable="false" title="转赠后原卡余额清零并关闭，目标卡余额累加（不计入对方累计充值，不会因此升级）。" />
      </el-form>
      <template #footer>
        <el-button @click="transferOpen = false">取消</el-button>
        <el-button type="warning" :loading="saving" @click="doTransfer">确认转赠</el-button>
      </template>
    </el-dialog>

    <el-dialog v-model="referralsOpen" :title="`${referralsData?.referrerName} 的引荐情况`" width="640px">
      <div v-if="referralsData">
        <div class="metric-row">
          <div>已引荐 <strong>{{ referralsData.referredCount }}</strong> 人</div>
          <div>累计返佣 <strong style="color:#d9534f">¥{{ referralsData.totalRewardEarned.toFixed(2) }}</strong></div>
        </div>
        <el-table :data="referralsData.referredMembers" size="small" stripe style="margin-top:12px">
          <el-table-column prop="cardNo" label="卡号" width="120" />
          <el-table-column prop="name" label="姓名" width="100" />
          <el-table-column prop="phone" label="手机号" width="140" />
          <el-table-column label="累计充值" width="120">
            <template #default="{ row }">¥{{ row.totalRecharge.toFixed(2) }}</template>
          </el-table-column>
          <el-table-column label="开卡时间" min-width="160">
            <template #default="{ row }">{{ dayjs(row.createdAt).format('YYYY-MM-DD HH:mm') }}</template>
          </el-table-column>
        </el-table>
        <p v-if="referralsData.referredCount === 0" class="muted" style="text-align:center; padding:30px 0">还没有引荐过会员</p>
      </div>
    </el-dialog>
  </div>
</template>

<script setup lang="ts">
import { onMounted, reactive, ref } from 'vue';
import { ElMessage, ElMessageBox, type FormInstance, type FormRules } from 'element-plus';
import dayjs from 'dayjs';
import { membersApi, type ReferralSummaryDto } from '@/api/modules';
import { useAppStore } from '@/stores/app';
import type { Member } from '@/api/types';

const appStore = useAppStore();

const rows = ref<Member[]>([]);
const total = ref(0);
const loading = ref(false);
const saving = ref(false);

const query = reactive({ page: 1, pageSize: 20, keyword: '', includeClosed: false });

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
  remark: '',
  referrerKeyword: '',
  referredByMemberId: null as number | null
});
const referrerCandidates = ref<Member[]>([]);

async function searchReferrer() {
  if (!form.referrerKeyword.trim()) return;
  const r = await membersApi.list({
    keyword: form.referrerKeyword.trim(),
    page: 1, pageSize: 10,
    storeId: appStore.activeStoreId ?? undefined
  });
  referrerCandidates.value = r.items.filter((m) => m.isActive);
  if (!referrerCandidates.value.length) ElMessage.info('没有匹配的可用会员');
}
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
      storeId: appStore.activeStoreId ?? undefined,
      includeClosed: query.includeClosed
    });
    rows.value = data.items;
    total.value = data.total;
  } finally {
    loading.value = false;
  }
}

const RECHARGE_KIND_LABEL: Record<string, string> = {
  Recharge: '充值', Refund: '退卡', TransferOut: '转出', TransferIn: '转入', ReferralBonus: '返佣'
};
const RECHARGE_KIND_TAG: Record<string, 'info' | 'warning' | 'success' | 'danger'> = {
  Recharge: 'success', Refund: 'danger', TransferOut: 'warning', TransferIn: 'info', ReferralBonus: 'success'
};
function rechargeKindLabel(k: string) { return RECHARGE_KIND_LABEL[k] ?? k; }
function rechargeKindTag(k: string) { return RECHARGE_KIND_TAG[k] ?? 'info'; }

function resetQuery() {
  query.keyword = '';
  query.page = 1;
  reload();
}

function openCreate() {
  formMode.value = 'create';
  editingId.value = null;
  Object.assign(form, {
    cardNo: '', phone: '', name: '', gender: '', birthday: '',
    discount: 1, initialBalance: 0, remark: '',
    referrerKeyword: '', referredByMemberId: null
  });
  referrerCandidates.value = [];
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
    remark: row.remark ?? '',
    referrerKeyword: '',
    referredByMemberId: row.referredByMemberId ?? null
  });
  referrerCandidates.value = [];
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
        remark: form.remark || null,
        referredByMemberId: form.referredByMemberId
      });
    } else if (editingId.value != null) {
      await membersApi.update(editingId.value, {
        phone: form.phone,
        name: form.name || null,
        gender: form.gender || null,
        birthday: form.birthday || null,
        discount: form.discount,
        remark: form.remark || null,
        referredByMemberId: form.referredByMemberId
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

// ---- 退卡 ----
const refundOpen = ref(false);
const refundTarget = ref<Member | null>(null);
const rfForm = reactive({ refundAmount: 0, refundMethod: 'Cash', reason: '' });

function openRefund(row: Member) {
  refundTarget.value = row;
  rfForm.refundAmount = row.balance;
  rfForm.refundMethod = 'Cash';
  rfForm.reason = '';
  refundOpen.value = true;
}

async function doRefund() {
  if (!refundTarget.value) return;
  if (rfForm.refundAmount <= 0) { ElMessage.warning('退款金额必须 > 0'); return; }
  try {
    await ElMessageBox.confirm(
      `确认从「${refundTarget.value.name || refundTarget.value.cardNo}」退还 ¥${rfForm.refundAmount.toFixed(2)}，并关闭该卡？`,
      '退卡确认',
      { type: 'warning' }
    );
  } catch { return; }
  saving.value = true;
  try {
    await membersApi.refund(refundTarget.value.id, {
      refundAmount: rfForm.refundAmount,
      refundMethod: rfForm.refundMethod,
      reason: rfForm.reason || null
    });
    ElMessage.success('已退卡');
    refundOpen.value = false;
    reload();
  } finally {
    saving.value = false;
  }
}

// ---- 转赠 ----
const transferOpen = ref(false);
const transferTarget = ref<Member | null>(null);
const tfForm = reactive({
  mode: 'existing' as 'existing' | 'new',
  toQuery: '',
  toMemberId: null as number | null,
  newMemberCardNo: '',
  newMemberPhone: '',
  newMemberName: '',
  reason: ''
});
const targetCandidates = ref<Member[]>([]);

function openTransfer(row: Member) {
  transferTarget.value = row;
  tfForm.mode = 'existing';
  tfForm.toQuery = '';
  tfForm.toMemberId = null;
  tfForm.newMemberCardNo = '';
  tfForm.newMemberPhone = '';
  tfForm.newMemberName = '';
  tfForm.reason = '';
  targetCandidates.value = [];
  transferOpen.value = true;
}

async function searchTarget() {
  if (!tfForm.toQuery.trim()) return;
  const r = await membersApi.list({
    keyword: tfForm.toQuery.trim(),
    page: 1, pageSize: 10,
    storeId: appStore.activeStoreId ?? undefined
  });
  targetCandidates.value = r.items.filter((m) => m.id !== transferTarget.value?.id && m.isActive);
  if (!targetCandidates.value.length) ElMessage.info('没有匹配的可用会员');
}

async function doTransfer() {
  if (!transferTarget.value) return;
  if (tfForm.mode === 'existing' && !tfForm.toMemberId) {
    ElMessage.warning('请选择目标会员');
    return;
  }
  if (tfForm.mode === 'new' && (!tfForm.newMemberCardNo || !tfForm.newMemberPhone)) {
    ElMessage.warning('新会员卡号和手机号必填');
    return;
  }
  saving.value = true;
  try {
    await membersApi.transfer(transferTarget.value.id, {
      toMemberId: tfForm.mode === 'existing' ? tfForm.toMemberId : null,
      newMemberCardNo: tfForm.mode === 'new' ? tfForm.newMemberCardNo : null,
      newMemberPhone: tfForm.mode === 'new' ? tfForm.newMemberPhone : null,
      newMemberName: tfForm.mode === 'new' ? tfForm.newMemberName || null : null,
      reason: tfForm.reason || null
    });
    ElMessage.success('已转赠');
    transferOpen.value = false;
    reload();
  } finally {
    saving.value = false;
  }
}

// ---- 引荐 ----
const referralsOpen = ref(false);
const referralsData = ref<ReferralSummaryDto | null>(null);

async function openReferrals(row: Member) {
  referralsData.value = await membersApi.referrals(row.id);
  referralsOpen.value = true;
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
.closed { color: #999; text-decoration: line-through; }
.metric-row { display: flex; gap: 32px; align-items: center; font-size: 14px; }
</style>
