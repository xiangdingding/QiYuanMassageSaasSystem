<script lang="ts">
export default { name: 'OrdersView' };
</script>

<script setup lang="ts">
import { computed, onMounted, ref } from 'vue';
import {
  NavBar as VanNavBar, Search as VanSearch, List as VanList, PullRefresh as VanPullRefresh,
  Empty as VanEmpty, Tag as VanTag, Tabs as VanTabs, Tab as VanTab,
  Popup as VanPopup, Cell as VanCell, CellGroup as VanCellGroup, Divider as VanDivider,
  Button as VanButton, Field as VanField, Picker as VanPicker,
  showSuccessToast, showConfirmDialog, showToast
} from 'vant';
import { ordersApi, ordersTransferApi, complaintsApi, staffApi } from '@/api/modules';
import { useAppStore } from '@/stores/app';
import { useAuthStore } from '@/stores/auth';
import { ORDER_STATUS_LABELS, PAY_METHOD_LABELS, type Order, type OrderItem, type OrderListItem, type Staff } from '@/api/types';

const appStore = useAppStore();
const auth = useAuthStore();

const keyword = ref('');
const status = ref('');
const tabIndex = ref(0);
const tabs: { label: string; value: string }[] = [
  { label: '全部', value: '' },
  { label: '待结账', value: 'Pending' },
  { label: '服务中', value: 'InProgress' },
  { label: '已完成', value: 'Completed' },
  { label: '已退款', value: 'Refunded' }
];

const list = ref<OrderListItem[]>([]);
const loading = ref(false);
const finished = ref(false);
const refreshing = ref(false);
const page = ref(1);
const pageSize = 20;

const detail = ref<Order | null>(null);
const showDetail = ref(false);
const detailLoading = ref(false);
const acting = ref(false);

const techList = ref<Staff[]>([]);

// 仅店主/店长可退款；收银员可取消待结账单。技师无 orders 访问。
const canRefund = auth.role === 'ShopOwner' || auth.role === 'StoreManager';

function fmt(n?: number | null): string {
  return (n ?? 0).toLocaleString('zh-CN', { minimumFractionDigits: 2, maximumFractionDigits: 2 });
}
function statusLabel(s: string) { return ORDER_STATUS_LABELS[s] ?? s; }
function payLabel(s: string) { return PAY_METHOD_LABELS[s] ?? s; }
function statusType(s: string) {
  switch (s) {
    case 'Completed': return 'success';
    case 'Pending': return 'warning';
    case 'InProgress': return 'primary';
    case 'Refunded': return 'danger';
    default: return 'default';
  }
}
// 单项是否可转钟（更换技师）：订单处于待结账/服务中
function canTransfer(o: Order) { return o.status === 'Pending' || o.status === 'InProgress'; }
// 单项是否可登记投诉：订单已完成
function canComplain(o: Order) { return o.status === 'Completed'; }

async function onLoad() {
  if (!appStore.activeStoreId) { finished.value = true; return; }
  loading.value = true;
  try {
    const res = await ordersApi.list({
      page: page.value,
      pageSize,
      storeId: appStore.activeStoreId,
      status: status.value || undefined,
      keyword: keyword.value.trim() || undefined
    });
    list.value.push(...res.items);
    page.value += 1;
    if (list.value.length >= res.total || res.items.length === 0) finished.value = true;
  } catch {
    finished.value = true;
  } finally {
    loading.value = false;
  }
}

function reset() { list.value = []; page.value = 1; finished.value = false; }
async function onRefresh() { reset(); await onLoad(); refreshing.value = false; }
function onSearch() { reset(); onLoad(); }
function onTab(idx: number) {
  status.value = tabs[idx].value;
  reset();
  onLoad();
}

async function openDetail(o: OrderListItem) {
  showDetail.value = true;
  detail.value = null;
  detailLoading.value = true;
  try {
    detail.value = await ordersApi.get(o.id);
  } catch {
    showDetail.value = false;
  } finally {
    detailLoading.value = false;
  }
}

async function onRefund() {
  if (!detail.value) return;
  try {
    await showConfirmDialog({
      title: '退款',
      message: `将退还订单 ${detail.value.orderNo} 实收 ¥${fmt(detail.value.paidAmount)}，确认？`
    });
  } catch { return; }
  acting.value = true;
  try {
    detail.value = await ordersApi.refund(detail.value.id);
    showSuccessToast('已退款');
    onSearch();
  } catch { /* 拦截器已提示 */ } finally { acting.value = false; }
}

async function onCancel() {
  if (!detail.value) return;
  try {
    await showConfirmDialog({ title: '取消订单', message: `取消未结账订单 ${detail.value.orderNo}？` });
  } catch { return; }
  acting.value = true;
  try {
    await ordersApi.cancel(detail.value.id);
    showToast('已取消');
    showDetail.value = false;
    onSearch();
  } catch { /* 拦截器已提示 */ } finally { acting.value = false; }
}

// ---- 转钟（更换技师）----
const showTransfer = ref(false);
const showTechPicker = ref(false);
const transferTarget = ref<OrderItem | null>(null);
const transferTechId = ref<number | null>(null);
const transferReason = ref('');
const transferring = ref(false);
const techColumns = computed(() =>
  techList.value
    .filter((t) => t.id !== transferTarget.value?.technicianId)
    .map((t) => ({ text: `${t.realName || t.username}（${t.employeeNo ?? '—'}）`, value: t.id }))
);
function transferTechName() {
  const t = techList.value.find((x) => x.id === transferTechId.value);
  return t ? (t.realName || t.username) : '选择新技师';
}
function openTransfer(item: OrderItem) {
  transferTarget.value = item;
  transferTechId.value = null;
  transferReason.value = '';
  showTransfer.value = true;
}
function onTransferTechPicked({ selectedValues }: { selectedValues: number[] }) {
  transferTechId.value = selectedValues[0] ?? null;
  showTechPicker.value = false;
}
async function doTransfer() {
  if (!detail.value || !transferTarget.value || !transferTechId.value) { showToast('请选择新技师'); return; }
  transferring.value = true;
  try {
    detail.value = await ordersTransferApi.transfer(detail.value.id, transferTarget.value.id, {
      newTechnicianId: transferTechId.value,
      reason: transferReason.value.trim() || null
    });
    showSuccessToast('转钟成功');
    showTransfer.value = false;
  } catch { /* 拦截器已提示 */ } finally { transferring.value = false; }
}

// ---- 登记投诉 ----
const showComplaint = ref(false);
const complaintTarget = ref<OrderItem | null>(null);
const complaintTags = ref<string[]>([]);
const complaintComment = ref('');
const complainting = ref(false);
const tagOptions = ['态度差', '力度不合适', '技术生疏', '迟到/超时', '卫生不佳', '环境嘈杂', '乱收费', '中途离岗'];
function openComplaint(item: OrderItem) {
  complaintTarget.value = item;
  complaintTags.value = [];
  complaintComment.value = '';
  showComplaint.value = true;
}
function toggleTag(t: string) {
  const i = complaintTags.value.indexOf(t);
  if (i === -1) complaintTags.value.push(t); else complaintTags.value.splice(i, 1);
}
async function doComplaint() {
  if (!complaintTarget.value) return;
  complainting.value = true;
  try {
    await complaintsApi.create({
      orderItemId: complaintTarget.value.id,
      tags: complaintTags.value.length ? complaintTags.value.join(',') : null,
      comment: complaintComment.value.trim() || null
    });
    showSuccessToast('已登记投诉');
    showComplaint.value = false;
  } catch { /* 拦截器已提示 */ } finally { complainting.value = false; }
}

async function loadTechs() {
  if (!appStore.activeStoreId) return;
  const r = await staffApi.list({ role: 'Technician', storeId: appStore.activeStoreId, page: 1, pageSize: 200 }).catch(() => null);
  if (r) techList.value = r.items;
}

onMounted(async () => {
  if (!appStore.stores.length) await appStore.loadStores().catch(() => undefined);
  onLoad();
  loadTechs();
});
</script>

<template>
  <div class="qy-page orders">
    <van-nav-bar title="订单流水" left-text="返回" left-arrow @click-left="$router.back()" />
    <van-search
      v-model="keyword"
      placeholder="订单号 / 会员卡号 / 手机号"
      @search="onSearch"
      @clear="onSearch"
    />
    <van-tabs v-model:active="tabIndex" @change="onTab" sticky>
      <van-tab v-for="t in tabs" :key="t.value" :title="t.label" />
    </van-tabs>

    <van-pull-refresh v-model="refreshing" @refresh="onRefresh">
      <van-empty v-if="finished && list.length === 0" description="暂无订单" />
      <van-list
        v-else
        v-model:loading="loading"
        :finished="finished"
        finished-text="没有更多了"
        @load="onLoad"
      >
        <div v-for="o in list" :key="o.id" class="order-item" @click="openDetail(o)">
          <div class="oi-top">
            <span class="oi-no">{{ o.orderNo }}</span>
            <van-tag :type="statusType(o.status)">{{ statusLabel(o.status) }}</van-tag>
          </div>
          <div class="oi-mid">
            <span>{{ o.memberCardNo || o.memberPhone || '散客' }}</span>
            <span class="oi-money qy-money">¥{{ fmt(o.paidAmount) }}</span>
          </div>
          <div class="oi-bottom">
            <span>{{ o.itemCount }} 个项目 · {{ payLabel(o.payMethod) }}</span>
            <span>{{ o.createdAt.slice(0, 16).replace('T', ' ') }}</span>
          </div>
        </div>
      </van-list>
    </van-pull-refresh>

    <van-popup v-model:show="showDetail" position="bottom" round :style="{ maxHeight: '85%' }">
      <div class="detail">
        <div v-if="detailLoading" class="dt-loading">加载中…</div>
        <template v-else-if="detail">
          <div class="dt-head">
            <div class="dh-no">{{ detail.orderNo }}</div>
            <van-tag :type="statusType(detail.status)" size="large">{{ statusLabel(detail.status) }}</van-tag>
          </div>

          <van-cell-group inset>
            <van-cell title="实收" :value="`¥ ${fmt(detail.paidAmount)}`" />
            <van-cell title="应收" :value="`¥ ${fmt(detail.total)}`" />
            <van-cell v-if="detail.discountAmount" title="优惠" :value="`¥ ${fmt(detail.discountAmount)}`" />
            <van-cell title="支付方式" :value="payLabel(detail.payMethod)" />
            <van-cell title="会员" :value="detail.memberName || detail.memberCardNo || '散客'" />
            <van-cell v-if="detail.cashierName" title="收银员" :value="detail.cashierName" />
            <van-cell title="下单时间" :value="detail.createdAt.slice(0, 19).replace('T', ' ')" />
          </van-cell-group>

          <van-divider>服务项目</van-divider>
          <div v-for="it in detail.items" :key="it.id" class="dt-line">
            <div class="dl-l">
              <div class="dl-name">
                {{ it.serviceName }}<span v-if="it.quantity > 1"> ×{{ it.quantity }}</span>
              </div>
              <div class="dl-sub">
                {{ it.technicianName || '—' }}
                <van-tag v-if="it.assignmentSource === 'Rotation'" type="default" plain>轮钟</van-tag>
                <van-tag v-else-if="it.assignmentSource === 'Designation'" type="warning" plain>点钟</van-tag>
                <van-tag v-if="it.transferredAt" type="warning">已转</van-tag>
                <span v-if="it.roomNo"> · {{ it.roomNo }}房</span>
                <span v-if="it.memberPackageId" class="punch">次卡核销</span>
              </div>
              <div class="dl-ops">
                <van-button v-if="canTransfer(detail)" size="mini" plain @click="openTransfer(it)">转钟</van-button>
                <van-button v-if="canComplain(detail)" size="mini" type="warning" plain @click="openComplaint(it)">投诉</van-button>
              </div>
            </div>
            <div class="dl-amt qy-money">¥{{ fmt(it.itemTotal) }}</div>
          </div>

          <template v-if="detail.roomCharges && detail.roomCharges.length">
            <van-divider>计时房费</van-divider>
            <div v-for="rc in detail.roomCharges" :key="rc.sessionId" class="dt-line">
              <div class="dl-l">
                <div class="dl-name">{{ rc.roomNo }} 房</div>
                <div class="dl-sub">{{ rc.minutes }} 分钟 · ¥{{ fmt(rc.hourlyRate) }}/时</div>
              </div>
              <div class="dl-amt qy-money">¥{{ fmt(rc.amount) }}</div>
            </div>
          </template>

          <div class="dt-actions">
            <van-button
              v-if="canRefund && detail.status === 'Completed'"
              type="danger" block :loading="acting" @click="onRefund"
            >退款</van-button>
            <van-button
              v-if="detail.status === 'Pending'"
              block :loading="acting" @click="onCancel"
            >取消订单</van-button>
          </div>
        </template>
      </div>
    </van-popup>

    <!-- 转钟 -->
    <van-popup v-model:show="showTransfer" position="bottom" round :style="{ maxHeight: '85%' }">
      <div class="sheet">
        <div class="sheet-title">转钟（更换技师）</div>
        <van-cell-group inset>
          <van-cell title="项目" :value="transferTarget?.serviceName || ''" />
          <van-cell title="原技师" :value="transferTarget?.technicianName || '未指派'" />
          <van-cell title="新技师" :value="transferTechName()" is-link @click="showTechPicker = true" />
          <van-field v-model="transferReason" label="原因" placeholder="选填，如 客人要求换人" maxlength="200" />
        </van-cell-group>
        <div class="sheet-actions">
          <van-button block type="primary" :loading="transferring" @click="doTransfer">确认转钟</van-button>
        </div>
      </div>
    </van-popup>

    <van-popup v-model:show="showTechPicker" position="bottom" round>
      <van-picker :columns="techColumns" @confirm="onTransferTechPicked" @cancel="showTechPicker = false" />
    </van-popup>

    <!-- 登记投诉 -->
    <van-popup v-model:show="showComplaint" position="bottom" round :style="{ maxHeight: '85%' }">
      <div class="sheet">
        <div class="sheet-title">登记投诉</div>
        <van-cell-group inset>
          <van-cell title="项目" :value="complaintTarget?.serviceName || ''" />
          <van-cell title="技师" :value="complaintTarget?.technicianName || '—'" />
        </van-cell-group>
        <div class="tag-pick">
          <span
            v-for="t in tagOptions"
            :key="t"
            class="tag-chip"
            :class="{ on: complaintTags.includes(t) }"
            @click="toggleTag(t)"
          >{{ t }}</span>
        </div>
        <van-cell-group inset>
          <van-field
            v-model="complaintComment"
            label="描述"
            type="textarea"
            rows="2"
            autosize
            maxlength="500"
            placeholder="客户原话 / 补充，选填"
          />
        </van-cell-group>
        <div class="sheet-actions">
          <van-button block type="warning" :loading="complainting" @click="doComplaint">登记投诉</van-button>
        </div>
      </div>
    </van-popup>
  </div>
</template>

<style scoped>
.order-item { background: #fff; margin: 8px 12px; padding: 14px; border-radius: 12px; }
.oi-top { display: flex; align-items: center; justify-content: space-between; }
.oi-no { font-weight: 600; font-size: 15px; }
.oi-mid { display: flex; align-items: center; justify-content: space-between; margin-top: 8px; }
.oi-money { font-size: 17px; font-weight: 700; color: var(--qy-brand); }
.oi-bottom { display: flex; justify-content: space-between; margin-top: 8px; color: #98a2b3; font-size: 12px; }
.detail { padding: 18px 0 28px; }
.dt-loading { text-align: center; padding: 40px 0; color: #98a2b3; }
.dt-head { display: flex; align-items: center; justify-content: space-between; padding: 0 18px 14px; }
.dh-no { font-size: 18px; font-weight: 700; }
.dt-line { display: flex; justify-content: space-between; align-items: flex-start; padding: 10px 18px; }
.dl-l { min-width: 0; flex: 1; }
.dl-name { font-size: 15px; font-weight: 500; }
.dl-sub { margin-top: 4px; color: #98a2b3; font-size: 13px; display: flex; align-items: center; flex-wrap: wrap; gap: 4px; }
.dl-ops { display: flex; gap: 8px; margin-top: 8px; }
.punch { color: var(--qy-brand); margin-left: 2px; }
.dl-amt { font-weight: 600; padding-left: 10px; }
.dt-actions { padding: 14px 18px 0; display: flex; flex-direction: column; gap: 10px; }
.sheet { padding: 16px 0 24px; }
.sheet-title { text-align: center; font-size: 17px; font-weight: 700; margin-bottom: 12px; }
.sheet-actions { padding: 16px 16px 0; }
.tag-pick { display: flex; flex-wrap: wrap; gap: 8px; padding: 12px 16px; }
.tag-chip {
  padding: 6px 12px; border-radius: 16px; background: #f2f4f7; color: #475467;
  font-size: 13px; border: 1px solid transparent;
}
.tag-chip.on { background: #fff4e6; color: #b06a00; border-color: #f0a020; }
</style>
