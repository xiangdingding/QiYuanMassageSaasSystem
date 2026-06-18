<script lang="ts">
export default { name: 'MembersView' };
</script>

<script setup lang="ts">
import { computed, onMounted, reactive, ref, watch } from 'vue';
import {
  NavBar as VanNavBar, Search as VanSearch, Checkbox as VanCheckbox, List as VanList,
  PullRefresh as VanPullRefresh, Empty as VanEmpty, Tag as VanTag, Button as VanButton,
  Popup as VanPopup, Form as VanForm, Field as VanField, Stepper as VanStepper, Picker as VanPicker,
  Tabs as VanTabs, Tab as VanTab, Cell as VanCell, CellGroup as VanCellGroup, NoticeBar as VanNoticeBar,
  Icon as VanIcon, showSuccessToast, showToast, showConfirmDialog
} from 'vant';
import {
  membersApi, memberTypesApi, servicesApi, staffApi, referralSettingsApi,
  type MemberType, type ReferralSummaryDto
} from '@/api/modules';
import { useAppStore } from '@/stores/app';
import { PAY_METHOD_LABELS, ORDER_STATUS_LABELS, type Member, type MemberPhoneGroup, type ServiceItem, type Staff } from '@/api/types';

const appStore = useAppStore();

const groups = ref<MemberPhoneGroup[]>([]);
const loading = ref(false);
const finished = ref(false);
const refreshing = ref(false);
const saving = ref(false);
const page = ref(1);
const pageSize = 20;
const keyword = ref('');
const includeClosed = ref(false);
const expanded = ref<Set<string>>(new Set());

function fmt(n?: number | null) { return (n ?? 0).toFixed(2); }
function isExpanded(p: string) { return expanded.value.has(p); }
function toggleExpand(p: string) {
  const n = new Set(expanded.value);
  n.has(p) ? n.delete(p) : n.add(p);
  expanded.value = n;
}
const PAY_LABEL: Record<string, string> = { ...PAY_METHOD_LABELS, Other: '其它' };
function payLabel(m: string) { return PAY_LABEL[m] ?? m; }
function orderStatusLabel(s: string) { return ORDER_STATUS_LABELS[s] ?? s; }
const RECHARGE_KIND: Record<string, string> = { Recharge: '充值', Refund: '退卡', TransferOut: '转出', TransferIn: '转入', ReferralBonus: '返佣' };
const payMethods = [
  { v: 'Wechat', label: '微信' }, { v: 'Alipay', label: '支付宝' },
  { v: 'BankCard', label: '银行卡' }, { v: 'Cash', label: '现金' }, { v: 'Other', label: '其它' }
];

async function onLoad() {
  loading.value = true;
  try {
    const data = await membersApi.grouped({
      page: page.value, pageSize, keyword: keyword.value.trim() || undefined,
      storeId: appStore.activeStoreId ?? undefined, includeClosed: includeClosed.value
    });
    groups.value.push(...data.items);
    page.value += 1;
    if (groups.value.length >= data.total || data.items.length === 0) finished.value = true;
  } catch { finished.value = true; } finally { loading.value = false; }
}
function reset() { groups.value = []; page.value = 1; finished.value = false; expanded.value = new Set(); }
async function onRefresh() { reset(); await onLoad(); refreshing.value = false; }
function onFilter() { reset(); onLoad(); }

// ---- 会员类型 / 服务（共享） ----
const memberTypes = ref<MemberType[]>([]);
const services = ref<ServiceItem[]>([]);
const activeTypes = computed(() => memberTypes.value.filter((t) => t.isActive));
async function ensureTypes() {
  if (memberTypes.value.length) return;
  const [ts, svc] = await Promise.all([
    memberTypesApi.list(false).catch(() => [] as MemberType[]),
    servicesApi.list(false).catch(() => [] as ServiceItem[])
  ]);
  memberTypes.value = ts; services.value = svc;
}
function boundUnitPrice(t: MemberType | null): number {
  if (!t || t.kind !== 'CountBased' || !t.serviceItemId) return 0;
  const s = services.value.find((x) => x.id === t.serviceItemId);
  if (!s) return 0;
  return s.memberPrice > 0 ? s.memberPrice : s.price;
}

// ---- 开卡 / 编辑 ----
const formOpen = ref(false);
const formMode = ref<'create' | 'edit'>('create');
const editingId = ref<number | null>(null);
const phoneLocked = ref(false);
const showTypePicker = ref(false);
const staffOptions = ref<Staff[]>([]);
const showStaffPicker = ref(false);
const referrerKeyword = ref('');
const referrerResults = ref<MemberPhoneGroup[]>([]);
const form = reactive({
  cardNo: '', phone: '', name: '', gender: '', birthday: '',
  discount: 1, initialBalance: 0, count: 0, payMethod: 'Wechat', remark: '',
  memberTypeId: null as number | null,
  referredByStaffId: null as number | null,
  referredByMemberId: null as number | null, referrerLabel: ''
});
const selectedType = computed(() => memberTypes.value.find((t) => t.id === form.memberTypeId) ?? null);
const countFace = computed(() => Math.round(form.count * boundUnitPrice(selectedType.value) * 100) / 100);
const countCharge = computed(() => Math.round(countFace.value * form.discount * 100) / 100);
const storedCharge = computed(() => Math.round(form.initialBalance * form.discount * 100) / 100);
const typeColumns = () => activeTypes.value.map((t) => ({ text: `${t.name}（${t.kind === 'StoredValue' ? '充值卡' : '计次卡'}）`, value: t.id }));
const staffColumns = () => [{ text: '不指定', value: 0 }, ...staffOptions.value.map((s) => ({ text: `${s.employeeNo ?? ''} · ${s.realName || s.username}`, value: s.id }))];
function typeName() { return selectedType.value ? selectedType.value.name : '选择会员类型'; }
function staffName() {
  const s = staffOptions.value.find((x) => x.id === form.referredByStaffId);
  return s ? (s.realName || s.username) : '不指定';
}

async function ensureReferralCtx() {
  try {
    const r = await staffApi.list({ pageSize: 200, storeId: appStore.activeStoreId ?? undefined });
    staffOptions.value = r.items.filter((s) => s.isActive);
  } catch { /* 非致命 */ }
}

watch(() => form.phone, (np) => {
  if (formMode.value !== 'create') return;
  if (np.length < 11) { form.cardNo = np; return; }
  const g = groups.value.find((x) => x.phone === np);
  form.cardNo = g ? np + String(g.cardCount + 1).padStart(2, '0') : np;
});

async function openCreate(phone?: string) {
  formMode.value = 'create';
  editingId.value = null;
  phoneLocked.value = false;
  Object.assign(form, {
    cardNo: '', phone: '', name: '', gender: '', birthday: '',
    discount: 1, initialBalance: 0, count: 0, payMethod: 'Wechat', remark: '',
    memberTypeId: null, referredByStaffId: null, referredByMemberId: null, referrerLabel: ''
  });
  referrerKeyword.value = ''; referrerResults.value = [];
  formOpen.value = true;
  await Promise.all([ensureTypes(), ensureReferralCtx()]);
  if (phone) {
    const g = groups.value.find((x) => x.phone === phone);
    form.phone = phone;
    if (g?.primaryName) form.name = g.primaryName;
    phoneLocked.value = true;
  }
}
async function openEdit(c: Member) {
  formMode.value = 'edit';
  editingId.value = c.id;
  Object.assign(form, {
    cardNo: c.cardNo, phone: c.phone, name: c.name ?? '', gender: c.gender ?? '', birthday: c.birthday ?? '',
    discount: c.discount, initialBalance: 0, count: 0, payMethod: 'Wechat', remark: c.remark ?? '',
    memberTypeId: null,
    referredByStaffId: c.referredByStaffId ?? null,
    referredByMemberId: c.referredByMemberId ?? null,
    referrerLabel: c.referredByMemberName ?? ''
  });
  referrerKeyword.value = ''; referrerResults.value = [];
  formOpen.value = true;
  await ensureReferralCtx();
}
function onTypePicked({ selectedValues }: { selectedValues: number[] }) {
  form.memberTypeId = selectedValues[0] ?? null;
  showTypePicker.value = false;
  const t = selectedType.value;
  if (!t) return;
  form.discount = t.discount;
  if (t.kind === 'StoredValue') { form.initialBalance = t.minRechargeAmount ?? 0; form.count = 0; }
  else { form.count = t.minPurchaseCount ?? 1; form.initialBalance = 0; }
}
function onStaffPicked({ selectedValues }: { selectedValues: number[] }) {
  form.referredByStaffId = selectedValues[0] || null;
  showStaffPicker.value = false;
}
async function searchReferrer() {
  const k = referrerKeyword.value.trim();
  if (!k) { referrerResults.value = []; return; }
  try {
    const r = await membersApi.grouped({ keyword: k, pageSize: 10, includeClosed: false, storeId: appStore.activeStoreId ?? undefined });
    referrerResults.value = r.items;
  } catch { referrerResults.value = []; }
}
function pickReferrer(g: MemberPhoneGroup) {
  const card = g.cards.find((c) => c.isActive) ?? g.cards[0];
  form.referredByMemberId = card?.id ?? null;
  form.referrerLabel = `${g.phone} · ${g.primaryName ?? '未填'}`;
  referrerResults.value = [];
  referrerKeyword.value = '';
}
function clearReferrer() { form.referredByMemberId = null; form.referrerLabel = ''; }

async function saveForm() {
  if (!appStore.activeStoreId) { showToast('未选择门店'); return; }
  if (!form.phone.trim() || !form.cardNo.trim()) { showToast('手机号与卡号必填'); return; }
  if (formMode.value === 'create') {
    const t = selectedType.value;
    if (!t) { showToast('请选择会员类型'); return; }
    if (t.kind === 'StoredValue' && form.initialBalance < (t.minRechargeAmount ?? 0)) { showToast(`充值不能低于 ¥${(t.minRechargeAmount ?? 0).toFixed(2)}`); return; }
    if (t.kind === 'CountBased' && form.count < (t.minPurchaseCount ?? 1)) { showToast(`次数不能低于 ${t.minPurchaseCount ?? 1}`); return; }
  }
  saving.value = true;
  try {
    if (formMode.value === 'create') {
      const t = selectedType.value!;
      const paid = t.kind === 'CountBased' ? countCharge.value : form.initialBalance;
      await membersApi.create({
        storeId: appStore.activeStoreId, cardNo: form.cardNo, phone: form.phone,
        name: form.name || null, gender: form.gender || null, birthday: form.birthday || null,
        discount: form.discount, initialBalance: paid, remark: form.remark || null,
        referredByMemberId: form.referredByMemberId, referredByStaffId: form.referredByStaffId,
        memberTypeId: form.memberTypeId, count: form.count, payMethod: form.payMethod
      });
    } else if (editingId.value != null) {
      await membersApi.update(editingId.value, {
        phone: form.phone, name: form.name || null, gender: form.gender || null,
        birthday: form.birthday || null, discount: form.discount, remark: form.remark || null,
        referredByMemberId: form.referredByMemberId, updateReferredByMember: true,
        referredByStaffId: form.referredByStaffId, updateStaffReferral: true
      });
    }
    showSuccessToast('已保存');
    formOpen.value = false;
    onFilter();
  } catch { /* */ } finally { saving.value = false; }
}

// ---- 充值 ----
const rechargeOpen = ref(false);
const rechargeTarget = ref<Member | null>(null);
const rc = reactive({ amount: 100, bonusAmount: 0, count: 1, payMethod: 'Wechat', remark: '' });
const rechargeType = computed(() => {
  const t = rechargeTarget.value;
  if (!t?.memberTypeId) return null;
  return memberTypes.value.find((x) => x.id === t.memberTypeId) ?? null;
});
const rcCountFace = computed(() => Math.round(rc.count * boundUnitPrice(rechargeType.value) * 100) / 100);
const rcCountCharge = computed(() => Math.round(rcCountFace.value * (rechargeType.value?.discount ?? 1) * 100) / 100);
const rcStoredCharge = computed(() => Math.round(rc.amount * (rechargeType.value?.discount ?? 1) * 100) / 100);
async function openRecharge(c: Member) {
  if (!c.isActive) { showToast('该卡已关闭，不能充值'); return; }
  rechargeTarget.value = c;
  Object.assign(rc, { amount: 100, bonusAmount: 0, count: 1, payMethod: 'Wechat', remark: '' });
  rechargeOpen.value = true;
  await ensureTypes();
  const t = rechargeType.value;
  if (t?.kind === 'StoredValue') { rc.amount = t.minRechargeAmount ?? 100; rc.bonusAmount = t.bonusAmount ?? 0; }
  else if (t?.kind === 'CountBased') { rc.count = t.minPurchaseCount ?? 1; rc.amount = 0; }
}
async function doRecharge() {
  if (!rechargeTarget.value) return;
  const t = rechargeType.value;
  saving.value = true;
  try {
    if (t) {
      if (t.kind === 'StoredValue' && rc.amount < (t.minRechargeAmount ?? 0)) { showToast(`不能低于 ¥${(t.minRechargeAmount ?? 0).toFixed(2)}`); return; }
      if (t.kind === 'CountBased' && rc.count < (t.minPurchaseCount ?? 1)) { showToast(`不能低于 ${t.minPurchaseCount ?? 1} 次`); return; }
      const cash = t.kind === 'CountBased' ? rcCountCharge.value : rc.amount;
      await membersApi.issueCard(rechargeTarget.value.id, {
        memberTypeId: t.id, amount: cash, count: t.kind === 'CountBased' ? rc.count : 0,
        payMethod: rc.payMethod, remark: rc.remark || null
      });
    } else {
      if (rc.amount <= 0) { showToast('充值金额必须 > 0'); return; }
      await membersApi.recharge({ memberId: rechargeTarget.value.id, amount: rc.amount, bonusAmount: rc.bonusAmount, payMethod: rc.payMethod, remark: rc.remark || null });
    }
    showSuccessToast('充值成功');
    rechargeOpen.value = false;
    onFilter();
  } catch { /* */ } finally { saving.value = false; }
}

// ---- 流水 ----
const historyOpen = ref(false);
const historyTab = ref(0);
const rechargeList = ref<Record<string, any>[]>([]);
const orderList = ref<Record<string, any>[]>([]);
async function openHistory(c: Member) {
  historyOpen.value = true;
  historyTab.value = 0;
  rechargeList.value = []; orderList.value = [];
  const [rs, os] = await Promise.all([
    membersApi.rechargeHistory(c.id).catch(() => []),
    membersApi.consumptionHistory(c.id).catch(() => [])
  ]);
  rechargeList.value = rs as Record<string, any>[];
  orderList.value = os as Record<string, any>[];
}

// ---- 退卡 ----
const refundOpen = ref(false);
const refundTarget = ref<Member | null>(null);
const rf = reactive({ refundAmount: 0, refundMethod: 'Wechat', reason: '' });
function openRefund(c: Member) {
  refundTarget.value = c;
  rf.refundAmount = c.balance; rf.refundMethod = 'Wechat'; rf.reason = '';
  refundOpen.value = true;
}
async function doRefund() {
  if (!refundTarget.value) return;
  if (rf.refundAmount <= 0) { showToast('退款金额必须 > 0'); return; }
  try { await showConfirmDialog({ title: '退卡确认', message: `从「${refundTarget.value.name || refundTarget.value.cardNo}」退还 ¥${rf.refundAmount.toFixed(2)} 并关闭该卡？` }); }
  catch { return; }
  saving.value = true;
  try {
    await membersApi.refund(refundTarget.value.id, { refundAmount: rf.refundAmount, refundMethod: rf.refundMethod, reason: rf.reason || null });
    showSuccessToast('已退卡');
    refundOpen.value = false;
    onFilter();
  } catch { /* */ } finally { saving.value = false; }
}

// ---- 转赠 ----
const transferOpen = ref(false);
const transferTarget = ref<Member | null>(null);
const tf = reactive({ mode: 'existing' as 'existing' | 'new', toQuery: '', toMemberId: null as number | null, newMemberCardNo: '', newMemberPhone: '', newMemberName: '', reason: '' });
const targetCandidates = ref<Member[]>([]);
function openTransfer(c: Member) {
  transferTarget.value = c;
  Object.assign(tf, { mode: 'existing', toQuery: '', toMemberId: null, newMemberCardNo: '', newMemberPhone: '', newMemberName: '', reason: '' });
  targetCandidates.value = [];
  transferOpen.value = true;
}
async function searchTarget() {
  if (!tf.toQuery.trim()) return;
  const r = await membersApi.list({ keyword: tf.toQuery.trim(), page: 1, pageSize: 10, storeId: appStore.activeStoreId ?? undefined });
  targetCandidates.value = r.items.filter((m) => m.id !== transferTarget.value?.id && m.isActive);
  if (!targetCandidates.value.length) showToast('没有匹配的可用会员');
}
async function doTransfer() {
  if (!transferTarget.value) return;
  if (tf.mode === 'existing' && !tf.toMemberId) { showToast('请选择目标会员'); return; }
  if (tf.mode === 'new' && (!tf.newMemberCardNo || !tf.newMemberPhone)) { showToast('新会员卡号和手机号必填'); return; }
  saving.value = true;
  try {
    await membersApi.transfer(transferTarget.value.id, {
      toMemberId: tf.mode === 'existing' ? tf.toMemberId : null,
      newMemberCardNo: tf.mode === 'new' ? tf.newMemberCardNo : null,
      newMemberPhone: tf.mode === 'new' ? tf.newMemberPhone : null,
      newMemberName: tf.mode === 'new' ? (tf.newMemberName || null) : null,
      reason: tf.reason || null
    });
    showSuccessToast('已转赠');
    transferOpen.value = false;
    onFilter();
  } catch { /* */ } finally { saving.value = false; }
}

// ---- 引荐 ----
const referralsOpen = ref(false);
const referralsData = ref<ReferralSummaryDto | null>(null);
async function openReferrals(c: Member) {
  try { referralsData.value = await membersApi.referrals(c.id); referralsOpen.value = true; } catch { /* */ }
}

onMounted(async () => {
  if (!appStore.stores.length) await appStore.loadStores().catch(() => undefined);
  onLoad();
});
</script>

<template>
  <div class="qy-page members">
    <van-nav-bar title="会员管理" left-text="返回" left-arrow @click-left="$router.back()">
      <template #right><span class="nav-add" @click="openCreate()">开卡</span></template>
    </van-nav-bar>

    <van-search v-model="keyword" placeholder="卡号 / 手机号 / 姓名" @search="onFilter" @clear="onFilter" />
    <div class="bar"><van-checkbox v-model="includeClosed" shape="square" @change="onFilter">显示已关闭会员</van-checkbox></div>

    <van-pull-refresh v-model="refreshing" @refresh="onRefresh">
      <van-empty v-if="finished && groups.length === 0" description="暂无会员，点右上角开卡" />
      <van-list v-else v-model:loading="loading" :finished="finished" finished-text="没有更多了" @load="onLoad">
        <div v-for="g in groups" :key="g.phone" class="grp">
          <div class="grp-head" @click="toggleExpand(g.phone)">
            <div class="gh-main">
              <div class="gh-l1">
                <span class="gh-name">{{ g.primaryName || '未填姓名' }}</span>
                <span class="gh-phone">{{ g.phone }}</span>
                <van-tag v-if="g.hasInactive" type="default">含关闭卡</van-tag>
              </div>
              <div class="gh-l2">{{ g.cardCount }}张 · 余额 <b class="qy-money">¥{{ fmt(g.totalBalance) }}</b> · 充 ¥{{ fmt(g.totalRecharge) }} · 消 ¥{{ fmt(g.totalConsumed) }}</div>
            </div>
            <van-icon :name="isExpanded(g.phone) ? 'arrow-up' : 'arrow-down'" />
          </div>

          <div v-if="isExpanded(g.phone)" class="cards">
            <div v-for="c in g.cards" :key="c.id" class="cardrow" :class="{ closed: !c.isActive }">
              <div class="cr-l1">
                <span class="cr-no">{{ c.cardNo }}</span>
                <van-tag v-if="c.memberTypeName" plain :type="c.memberTypeKind === 'CountBased' ? 'success' : 'warning'">{{ c.memberTypeName }}</van-tag>
                <van-tag v-if="c.discount < 1" type="warning">{{ (c.discount * 10).toFixed(1) }}折</van-tag>
                <van-tag v-if="!c.isActive" type="default">已关闭</van-tag>
              </div>
              <div class="cr-l2">
                余额 ¥{{ fmt(c.balance) }} · 充 ¥{{ fmt(c.totalRecharge) }} · 消 ¥{{ fmt(c.totalConsumed) }}
                <span v-if="c.memberTypeKind === 'CountBased'"> · 剩 {{ c.remainCount ?? 0 }} 次</span>
              </div>
              <div class="cr-actions">
                <van-button size="mini" type="primary" :disabled="!c.isActive" @click="openRecharge(c)">充值</van-button>
                <van-button size="mini" @click="openHistory(c)">流水</van-button>
                <van-button size="mini" @click="openEdit(c)">编辑</van-button>
                <van-button size="mini" type="warning" plain :disabled="!c.isActive || c.balance <= 0" @click="openRefund(c)">退卡</van-button>
                <van-button size="mini" type="warning" plain :disabled="!c.isActive || c.balance <= 0" @click="openTransfer(c)">转赠</van-button>
                <van-button size="mini" @click="openReferrals(c)">引荐</van-button>
              </div>
            </div>
            <div class="grp-foot"><van-button size="small" type="success" plain @click="openCreate(g.phone)">加办一张卡</van-button></div>
          </div>
        </div>
      </van-list>
    </van-pull-refresh>

    <!-- 开卡 / 编辑 -->
    <van-popup v-model:show="formOpen" position="bottom" round :style="{ maxHeight: '94%' }">
      <div class="sheet">
        <div class="sheet-title">{{ formMode === 'create' ? '开卡' : '编辑会员' }}</div>
        <van-form @submit="saveForm">
          <van-cell-group inset>
            <van-field v-model="form.phone" label="手机号" :readonly="phoneLocked && formMode === 'create'" placeholder="必填" />
            <van-field v-model="form.cardNo" label="卡号" :readonly="formMode === 'edit'" placeholder="必填" />
            <van-field v-model="form.name" label="姓名" placeholder="选填" />
            <van-field label="性别">
              <template #input>
                <div class="seg sm">
                  <button type="button" :class="{ on: form.gender === '男' }" @click="form.gender = '男'">男</button>
                  <button type="button" :class="{ on: form.gender === '女' }" @click="form.gender = '女'">女</button>
                </div>
              </template>
            </van-field>
            <van-field label="生日"><template #input><input v-model="form.birthday" type="date" class="dt" /></template></van-field>

            <template v-if="formMode === 'create'">
              <van-field label="会员类型" :model-value="typeName()" readonly is-link required @click="showTypePicker = true" />
              <template v-if="selectedType?.kind === 'StoredValue'">
                <van-field label="充值金额"><template #input><van-stepper v-model="form.initialBalance" :min="selectedType.minRechargeAmount ?? 0" :step="100" :decimal-length="2" /></template></van-field>
                <van-cell title="实收金额" :value="`¥ ${fmt(storedCharge)}`" :label="`× ${(form.discount * 10).toFixed(1)} 折`" />
                <van-cell title="到账余额" :value="`¥ ${fmt(form.initialBalance + (selectedType.bonusAmount ?? 0))}`" :label="`含赠送 ¥${fmt(selectedType.bonusAmount)}`" />
              </template>
              <template v-else-if="selectedType?.kind === 'CountBased'">
                <van-field label="购买次数"><template #input><van-stepper v-model="form.count" :min="selectedType.minPurchaseCount ?? 1" /></template></van-field>
                <van-cell title="实充次数" :value="`${form.count + (selectedType.bonusCount ?? 0)} 次`" :label="`含赠送 ${selectedType.bonusCount ?? 0} 次`" />
                <van-cell title="充值金额" :value="`¥ ${fmt(countFace)}`" />
                <van-cell title="实收金额" :value="`¥ ${fmt(countCharge)}`" :label="`× ${(form.discount * 10).toFixed(1)} 折`" />
              </template>
              <van-field label="支付来源">
                <template #input>
                  <div class="seg wrap">
                    <button v-for="pm in payMethods" :key="pm.v" type="button" :class="{ on: form.payMethod === pm.v }" @click="form.payMethod = pm.v">{{ pm.label }}</button>
                  </div>
                </template>
              </van-field>
            </template>

            <van-field label="员工推荐人" :model-value="staffName()" readonly is-link @click="showStaffPicker = true" />
            <van-field label="顾客引荐人">
              <template #input>
                <div class="ref-wrap">
                  <div v-if="form.referrerLabel" class="ref-cur">{{ form.referrerLabel }} <span class="ref-clear" @click="clearReferrer">清除</span></div>
                  <div class="ref-search">
                    <input v-model="referrerKeyword" class="ref-input" placeholder="卡号/手机号搜索" @keyup.enter="searchReferrer" />
                    <van-button size="mini" @click="searchReferrer">查找</van-button>
                  </div>
                  <div v-for="r in referrerResults" :key="r.phone" class="ref-item" @click="pickReferrer(r)">
                    {{ r.phone }} · {{ r.primaryName ?? '未填' }} · {{ r.cardCount }}张
                  </div>
                </div>
              </template>
            </van-field>

            <van-field v-model="form.remark" label="备注" type="textarea" rows="1" autosize placeholder="选填" />
          </van-cell-group>
          <div class="sheet-actions"><van-button block type="primary" native-type="submit" :loading="saving">保存</van-button></div>
        </van-form>
      </div>
    </van-popup>
    <van-popup v-model:show="showTypePicker" position="bottom" round>
      <van-picker :columns="typeColumns()" @confirm="onTypePicked" @cancel="showTypePicker = false" />
    </van-popup>
    <van-popup v-model:show="showStaffPicker" position="bottom" round>
      <van-picker :columns="staffColumns()" @confirm="onStaffPicked" @cancel="showStaffPicker = false" />
    </van-popup>

    <!-- 充值 -->
    <van-popup v-model:show="rechargeOpen" position="bottom" round :style="{ maxHeight: '90%' }">
      <div class="sheet">
        <div class="sheet-title">充值：{{ rechargeTarget?.cardNo }}</div>
        <van-cell-group inset>
          <van-cell title="当前余额" :value="`¥ ${fmt(rechargeTarget?.balance)}`" />
          <van-notice-bar v-if="rechargeType" wrapable :scrollable="false"
            :text="`${rechargeType.name}（${rechargeType.kind === 'StoredValue' ? '充值卡' : '计次卡'}）${rechargeType.discount < 1 ? ' · ' + (rechargeType.discount * 10).toFixed(1) + '折' : ''}`" />
          <template v-if="rechargeType?.kind === 'CountBased'">
            <van-field label="充值次数"><template #input><van-stepper v-model="rc.count" :min="rechargeType.minPurchaseCount ?? 1" /></template></van-field>
            <van-cell title="实充次数" :value="`${rc.count + (rechargeType.bonusCount ?? 0)} 次`" />
            <van-cell title="充值金额" :value="`¥ ${fmt(rcCountFace)}`" />
            <van-cell title="实收金额" :value="`¥ ${fmt(rcCountCharge)}`" />
          </template>
          <template v-else>
            <van-field label="充值金额"><template #input><van-stepper v-model="rc.amount" :min="rechargeType?.minRechargeAmount ?? 0" :step="100" :decimal-length="2" /></template></van-field>
            <van-field v-if="!rechargeType" label="赠送金额"><template #input><van-stepper v-model="rc.bonusAmount" :min="0" :step="50" :decimal-length="2" /></template></van-field>
            <van-cell v-if="rechargeType" title="实收金额" :value="`¥ ${fmt(rcStoredCharge)}`" />
            <van-cell v-if="rechargeType" title="到账余额" :value="`¥ ${fmt(rc.amount + (rechargeType.bonusAmount ?? 0))}`" />
          </template>
          <van-field label="支付方式">
            <template #input>
              <div class="seg wrap">
                <button v-for="pm in payMethods" :key="pm.v" type="button" :class="{ on: rc.payMethod === pm.v }" @click="rc.payMethod = pm.v">{{ pm.label }}</button>
              </div>
            </template>
          </van-field>
          <van-field v-model="rc.remark" label="备注" type="textarea" rows="1" autosize placeholder="选填" />
        </van-cell-group>
        <div class="sheet-actions"><van-button block type="primary" :loading="saving" @click="doRecharge">确认充值</van-button></div>
      </div>
    </van-popup>

    <!-- 流水 -->
    <van-popup v-model:show="historyOpen" position="bottom" round :style="{ maxHeight: '85%' }">
      <div class="sheet">
        <div class="sheet-title">会员流水</div>
        <van-tabs v-model:active="historyTab">
          <van-tab title="资金流水">
            <van-empty v-if="!rechargeList.length" description="暂无记录" />
            <div v-for="(r, i) in rechargeList" :key="i" class="flow">
              <div class="fl-l">
                <van-tag :type="r.kind === 'Refund' || r.kind === 'TransferOut' ? 'danger' : 'success'">{{ RECHARGE_KIND[r.kind] ?? r.kind }}</van-tag>
                <span class="fl-amt qy-money">¥{{ fmt(r.amount) }}</span>
                <span v-if="r.bonusAmount" class="fl-bonus">送¥{{ fmt(r.bonusAmount) }}</span>
              </div>
              <div class="fl-r">
                <div>后余额 ¥{{ fmt(r.balanceAfter) }} · {{ payLabel(r.payMethod) }}</div>
                <div class="fl-time">{{ String(r.createdAt).slice(0, 19).replace('T', ' ') }}</div>
              </div>
            </div>
          </van-tab>
          <van-tab title="消费记录">
            <van-empty v-if="!orderList.length" description="暂无记录" />
            <div v-for="(o, i) in orderList" :key="i" class="flow">
              <div class="fl-l"><span class="fl-no">{{ o.orderNo }}</span></div>
              <div class="fl-r">
                <div>实收 ¥{{ fmt(o.paidAmount) }} · {{ orderStatusLabel(o.status) }}</div>
                <div class="fl-time">{{ String(o.createdAt).slice(0, 19).replace('T', ' ') }}</div>
              </div>
            </div>
          </van-tab>
        </van-tabs>
      </div>
    </van-popup>

    <!-- 退卡 -->
    <van-popup v-model:show="refundOpen" position="bottom" round>
      <div class="sheet">
        <div class="sheet-title">退卡：{{ refundTarget?.cardNo }}</div>
        <van-cell-group inset>
          <van-cell title="当前余额" :value="`¥ ${fmt(refundTarget?.balance)}`" />
          <van-field label="退款金额"><template #input><van-stepper v-model="rf.refundAmount" :min="0" :max="refundTarget?.balance ?? 0" :step="50" :decimal-length="2" /></template></van-field>
          <van-field label="退款方式">
            <template #input>
              <div class="seg wrap">
                <button v-for="pm in payMethods" :key="pm.v" type="button" :class="{ on: rf.refundMethod === pm.v }" @click="rf.refundMethod = pm.v">{{ pm.label }}</button>
              </div>
            </template>
          </van-field>
          <van-field v-model="rf.reason" label="原因" type="textarea" rows="1" autosize placeholder="选填" />
        </van-cell-group>
        <van-notice-bar wrapable :scrollable="false" text="退卡后会员卡将被关闭，不能再充值或消费。" />
        <div class="sheet-actions"><van-button block type="warning" :loading="saving" @click="doRefund">确认退卡</van-button></div>
      </div>
    </van-popup>

    <!-- 转赠 -->
    <van-popup v-model:show="transferOpen" position="bottom" round :style="{ maxHeight: '88%' }">
      <div class="sheet">
        <div class="sheet-title">转赠：{{ transferTarget?.cardNo }}</div>
        <van-cell-group inset>
          <van-cell title="当前余额" :value="`¥ ${fmt(transferTarget?.balance)}`" label="将一并转走" />
          <van-field label="转赠对象">
            <template #input>
              <div class="seg sm">
                <button type="button" :class="{ on: tf.mode === 'existing' }" @click="tf.mode = 'existing'">已有会员</button>
                <button type="button" :class="{ on: tf.mode === 'new' }" @click="tf.mode = 'new'">新建会员</button>
              </div>
            </template>
          </van-field>
          <template v-if="tf.mode === 'existing'">
            <van-field label="目标会员">
              <template #input>
                <div class="ref-wrap">
                  <div class="ref-search">
                    <input v-model="tf.toQuery" class="ref-input" placeholder="卡号/手机号" @keyup.enter="searchTarget" />
                    <van-button size="mini" @click="searchTarget">查找</van-button>
                  </div>
                  <label v-for="c in targetCandidates" :key="c.id" class="cand">
                    <input type="radio" :value="c.id" v-model="tf.toMemberId" />
                    {{ c.cardNo }}（{{ c.name || '未填' }}/{{ c.phone }}）余额 ¥{{ fmt(c.balance) }}
                  </label>
                </div>
              </template>
            </van-field>
          </template>
          <template v-else>
            <van-field v-model="tf.newMemberCardNo" label="新卡号" placeholder="必填" />
            <van-field v-model="tf.newMemberPhone" label="手机号" placeholder="必填" />
            <van-field v-model="tf.newMemberName" label="姓名" placeholder="选填" />
          </template>
          <van-field v-model="tf.reason" label="原因" type="textarea" rows="1" autosize placeholder="如转赠给家人" />
        </van-cell-group>
        <van-notice-bar wrapable :scrollable="false" text="转赠后原卡余额清零并关闭，目标卡余额累加（不计入对方累计充值）。" />
        <div class="sheet-actions"><van-button block type="warning" :loading="saving" @click="doTransfer">确认转赠</van-button></div>
      </div>
    </van-popup>

    <!-- 引荐 -->
    <van-popup v-model:show="referralsOpen" position="bottom" round :style="{ maxHeight: '80%' }">
      <div class="sheet" v-if="referralsData">
        <div class="sheet-title">{{ referralsData.referrerName }} 的引荐</div>
        <div class="ref-metric">已引荐 <b>{{ referralsData.referredCount }}</b> 人 · 累计返佣 <b class="qy-money">¥{{ fmt(referralsData.totalRewardEarned) }}</b></div>
        <van-empty v-if="referralsData.referredCount === 0" description="还没有引荐过会员" />
        <van-cell-group v-else inset>
          <van-cell v-for="m in referralsData.referredMembers" :key="m.memberId"
            :title="`${m.name || '未填'} · ${m.phone}`" :label="`卡号 ${m.cardNo} · ${String(m.createdAt).slice(0,10)}`"
            :value="`充 ¥${fmt(m.totalRecharge)}`" />
        </van-cell-group>
      </div>
    </van-popup>
  </div>
</template>

<style scoped>
.nav-add { color: var(--qy-brand); font-size: 15px; }
.bar { padding: 8px 14px; }
.grp { background: #fff; margin: 8px 12px; border-radius: 12px; overflow: hidden; }
.grp-head { display: flex; align-items: center; justify-content: space-between; padding: 14px; }
.gh-l1 { display: flex; align-items: center; gap: 8px; }
.gh-name { font-size: 16px; font-weight: 600; }
.gh-phone { color: #98a2b3; font-size: 13px; }
.gh-l2 { margin-top: 6px; color: #6b7280; font-size: 13px; }
.gh-l2 b { color: var(--qy-brand); }
.cards { border-top: 1px solid #f1f3f5; padding: 4px 14px 12px; }
.cardrow { padding: 12px 0; border-bottom: 1px solid #f5f6f8; }
.cardrow.closed { opacity: .6; }
.cr-l1 { display: flex; align-items: center; gap: 6px; flex-wrap: wrap; }
.cr-no { font-weight: 600; font-size: 15px; }
.cr-l2 { margin-top: 6px; color: #6b7280; font-size: 13px; }
.cr-actions { display: flex; flex-wrap: wrap; gap: 6px; margin-top: 10px; }
.grp-foot { padding-top: 10px; display: flex; justify-content: flex-end; }
.sheet { padding: 16px 0 24px; }
.sheet-title { text-align: center; font-size: 17px; font-weight: 700; margin-bottom: 12px; }
.sheet-actions { padding: 16px 16px 0; }
.seg { display: flex; gap: 8px; width: 100%; }
.seg.sm { max-width: 200px; }
.seg.wrap { flex-wrap: wrap; }
.seg button { flex: 1; border: 1px solid #d6dbe2; background: #fff; color: #4b5563; border-radius: 8px; padding: 6px 4px; font-size: 13px; }
.seg.wrap button { flex: 0 0 calc(33% - 6px); }
.seg button.on { background: var(--qy-brand); color: #fff; border-color: var(--qy-brand); }
.dt { border: none; outline: none; font-size: 14px; background: transparent; font-family: inherit; color: #1f2733; width: 100%; }
.ref-wrap { width: 100%; }
.ref-cur { font-size: 13px; color: #16a34a; margin-bottom: 6px; }
.ref-clear { color: #ee4d4d; margin-left: 6px; }
.ref-search { display: flex; gap: 8px; align-items: center; }
.ref-input { flex: 1; border: 1px solid #e3e7ec; border-radius: 8px; padding: 6px 10px; font-size: 14px; outline: none; }
.ref-item { padding: 8px 0; border-bottom: 1px solid #f1f3f5; font-size: 13px; color: #4b5563; }
.cand { display: block; padding: 8px 0; font-size: 13px; color: #4b5563; }
.flow { display: flex; justify-content: space-between; padding: 12px 16px; border-bottom: 1px solid #f5f6f8; }
.fl-l { display: flex; align-items: center; gap: 8px; }
.fl-amt { font-weight: 700; color: var(--qy-brand); }
.fl-bonus { color: #16a34a; font-size: 12px; }
.fl-no { font-weight: 600; font-size: 14px; }
.fl-r { text-align: right; color: #6b7280; font-size: 13px; }
.fl-time { color: #b0b8c4; font-size: 12px; margin-top: 2px; }
.ref-metric { text-align: center; margin-bottom: 12px; color: #4b5563; }
.ref-metric b { color: var(--qy-brand); }
</style>
