<template>
  <div class="page">
    <el-card shadow="never">
      <div class="toolbar">
        <el-select v-model="query.status" placeholder="全部状态" clearable style="width: 140px">
          <el-option label="待结账" value="Pending" />
          <el-option label="已完成" value="Completed" />
          <el-option label="已取消" value="Cancelled" />
          <el-option label="已退款" value="Refunded" />
        </el-select>
        <el-date-picker
          v-model="dateRange"
          type="datetimerange"
          range-separator="至"
          start-placeholder="开始时间"
          end-placeholder="结束时间"
          format="YYYY-MM-DD HH:mm"
          value-format="YYYY-MM-DDTHH:mm:ss[Z]"
        />
        <el-button type="primary" @click="reload">查询</el-button>
        <el-button @click="resetQuery">重置</el-button>
      </div>

      <el-table :data="rows" v-loading="loading" stripe style="margin-top: 12px" @row-click="openDetail">
        <el-table-column prop="orderNo" label="订单号" min-width="180" />
        <el-table-column label="项数" width="60" prop="itemCount" />
        <el-table-column label="金额" width="100">
          <template #default="{ row }">¥{{ row.total.toFixed(2) }}</template>
        </el-table-column>
        <el-table-column label="实收" width="100">
          <template #default="{ row }">¥{{ row.paidAmount.toFixed(2) }}</template>
        </el-table-column>
        <el-table-column label="支付" width="100">
          <template #default="{ row }">{{ payLabel(row.payMethod) }}</template>
        </el-table-column>
        <el-table-column label="状态" width="100">
          <template #default="{ row }">
            <el-tag :type="statusType(row.status)">{{ statusLabel(row.status) }}</el-tag>
          </template>
        </el-table-column>
        <el-table-column label="会员卡号" min-width="160" prop="memberCardNo" :show-overflow-tooltip="true">
          <template #default="{ row }">
            <span class="nowrap-cell" :class="{ walkin: !isMemberPaid(row) }">
              {{ memberCardCell(row) }}
            </span>
          </template>
        </el-table-column>
        <el-table-column label="会员手机号" min-width="140" prop="memberPhone" :show-overflow-tooltip="true">
          <template #default="{ row }">
            <span class="nowrap-cell" :class="{ walkin: !isMemberPaid(row) }">
              {{ memberPhoneCell(row) }}
            </span>
          </template>
        </el-table-column>
        <el-table-column label="消费时间" min-width="160">
          <template #default="{ row }">{{ formatTime(row.completedAt ?? row.createdAt) }}</template>
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

    <el-drawer v-model="drawerOpen" title="订单详情" size="860px" class="order-detail-drawer">
      <div v-if="detail" class="detail-body">
        <el-descriptions :column="2" border size="large" class="detail-desc">
          <el-descriptions-item label="订单号" :span="2">{{ detail.orderNo }}</el-descriptions-item>
          <el-descriptions-item label="状态">
            <el-tag :type="statusType(detail.status)" size="large">{{ statusLabel(detail.status) }}</el-tag>
          </el-descriptions-item>
          <el-descriptions-item label="支付方式">{{ payLabel(detail.payMethod) }}</el-descriptions-item>
          <el-descriptions-item label="合计">
            ¥{{ headlineTotal(detail).toFixed(2) }}
            <span v-if="(detail.punchCardUsedCount ?? 0) > 0" class="muted" style="margin-left:6px">（含次卡面值）</span>
          </el-descriptions-item>
          <el-descriptions-item label="优惠">¥{{ detail.discountAmount.toFixed(2) }}</el-descriptions-item>
          <el-descriptions-item label="实收">¥{{ detail.paidAmount.toFixed(2) }}</el-descriptions-item>
          <el-descriptions-item label="收银员">{{ detail.cashierName ?? '—' }}</el-descriptions-item>
          <el-descriptions-item v-if="(detail.punchCardUsedCount ?? 0) > 0" label="消费次数">
            {{ detail.punchCardUsedCount }} 次（次卡核销）
          </el-descriptions-item>
          <template v-if="isMemberOrder(detail)">
            <el-descriptions-item label="会员卡号">{{ detail.memberCardNo ?? '—' }}</el-descriptions-item>
            <el-descriptions-item label="会员手机">{{ detail.memberPhone ?? '—' }}</el-descriptions-item>
            <el-descriptions-item label="会员姓名">{{ detail.memberName ?? '—' }}</el-descriptions-item>
            <el-descriptions-item label="卡类型">
              <el-tag
                v-if="detail.memberTypeKind"
                :type="detail.memberTypeKind === 'StoredValue' ? 'warning' : 'success'"
                size="large"
              >{{ detail.memberTypeName ?? memberTypeKindLabel(detail.memberTypeKind) }}</el-tag>
              <span v-else>{{ detail.memberTypeName ?? '普通' }}</span>
            </el-descriptions-item>
          </template>
          <el-descriptions-item v-else label="会员卡">{{ detail.memberCardNo ?? '—' }}</el-descriptions-item>
          <el-descriptions-item label="备注" :span="2">{{ detail.remark ?? '—' }}</el-descriptions-item>
        </el-descriptions>
        <el-divider class="detail-divider">项目明细</el-divider>
        <el-table :data="detail.items" class="detail-items" stripe>
          <el-table-column prop="serviceName" label="项目" min-width="180">
            <template #default="{ row }">
              <span class="item-name">{{ row.serviceName }}</span>
              <el-tag v-if="row.memberPackageId" size="small" type="success" style="margin-left:6px">次卡</el-tag>
            </template>
          </el-table-column>
          <el-table-column label="技师" width="150">
            <template #default="{ row }">
              <span>{{ row.technicianName }}</span>
              <el-tag v-if="row.transferredAt" size="small" type="warning" style="margin-left:4px">已转</el-tag>
            </template>
          </el-table-column>
          <el-table-column label="房间" width="90">
            <template #default="{ row }">{{ row.roomNo ?? '—' }}</template>
          </el-table-column>
          <el-table-column prop="quantity" label="次数" width="80" align="right">
            <template #default="{ row }">{{ row.quantity }} 次</template>
          </el-table-column>
          <el-table-column label="金额" width="120" align="right">
            <template #default="{ row }">¥{{ rowAmount(row).toFixed(2) }}</template>
          </el-table-column>
          <el-table-column label="提成" width="110" align="right">
            <template #default="{ row }">¥{{ row.commissionAmount.toFixed(2) }}</template>
          </el-table-column>
          <el-table-column
            v-if="detail.status === 'Pending' || detail.status === 'InProgress'"
            label="操作"
            width="100"
          >
            <template #default="{ row }">
              <el-button size="default" @click="openTransfer(row)">转钟</el-button>
            </template>
          </el-table-column>
        </el-table>

        <div class="actions">
          <el-button
            v-if="detail.status === 'Completed'"
            type="danger"
            size="large"
            @click="onRefund"
          >退款</el-button>
          <el-button
            v-if="detail.status === 'Pending'"
            size="large"
            @click="onCancel"
          >取消订单</el-button>
        </div>
      </div>
    </el-drawer>

    <el-dialog v-model="transferOpen" title="转钟（更换技师）" width="420px">
      <el-form label-width="90px">
        <el-form-item label="项目">{{ transferTarget?.serviceName }}</el-form-item>
        <el-form-item label="原技师">{{ transferTarget?.technicianName }}</el-form-item>
        <el-form-item label="新技师" required>
          <el-select v-model="transferTo" placeholder="选择新技师" filterable style="width: 100%">
            <el-option
              v-for="t in technicians"
              :key="t.id"
              :label="`${t.employeeNo ?? '-'} · ${t.realName ?? t.username}`"
              :value="t.id"
              :disabled="t.id === transferTarget?.technicianId"
            />
          </el-select>
        </el-form-item>
        <el-form-item label="原因">
          <el-input v-model="transferReason" maxlength="200" placeholder="如：客人要求换人 / 原技师有事" />
        </el-form-item>
      </el-form>
      <template #footer>
        <el-button @click="transferOpen = false">取消</el-button>
        <el-button type="primary" :loading="transferring" @click="doTransfer">确认转钟</el-button>
      </template>
    </el-dialog>
  </div>
</template>

<script setup lang="ts">
import { onMounted, reactive, ref } from 'vue';
import { ElMessage, ElMessageBox } from 'element-plus';
import dayjs from 'dayjs';
import { ordersApi, ordersTransferApi, staffApi } from '@/api/modules';
import { useAppStore } from '@/stores/app';
import type { Order, OrderItem, OrderListItem, Staff } from '@/api/types';

const appStore = useAppStore();

const rows = ref<OrderListItem[]>([]);
const total = ref(0);
const loading = ref(false);
const drawerOpen = ref(false);
const detail = ref<Order | null>(null);
const dateRange = ref<string[] | null>(null);

const technicians = ref<Staff[]>([]);
const transferOpen = ref(false);
const transferTarget = ref<OrderItem | null>(null);
const transferTo = ref<number | null>(null);
const transferReason = ref('');
const transferring = ref(false);

function openTransfer(row: OrderItem) {
  transferTarget.value = row;
  transferTo.value = null;
  transferReason.value = '';
  transferOpen.value = true;
}

async function doTransfer() {
  if (!detail.value || !transferTarget.value || !transferTo.value) {
    ElMessage.warning('请选择新技师');
    return;
  }
  transferring.value = true;
  try {
    detail.value = await ordersTransferApi.transfer(detail.value.id, transferTarget.value.id, {
      newTechnicianId: transferTo.value,
      reason: transferReason.value || null
    });
    ElMessage.success('转钟成功');
    transferOpen.value = false;
  } catch {
    /* http 已弹错 */
  } finally {
    transferring.value = false;
  }
}

const query = reactive<{ page: number; pageSize: number; status: string }>({
  page: 1,
  pageSize: 20,
  status: ''
});

function payLabel(m: string) {
  return ({
    Cash: '现金', MemberCard: '会员卡', Wechat: '微信', Alipay: '支付宝', BankCard: '银行卡', Unpaid: '未支付'
  } as Record<string, string>)[m] ?? m;
}
function statusLabel(s: string) {
  return ({
    Pending: '待结账', InProgress: '服务中', Completed: '已完成', Cancelled: '已取消', Refunded: '已退款'
  } as Record<string, string>)[s] ?? s;
}
function statusType(s: string) {
  return ({
    Pending: 'warning', InProgress: 'info', Completed: 'success', Cancelled: 'info', Refunded: 'danger'
  } as Record<string, any>)[s] ?? '';
}
function formatTime(v: string) {
  return dayjs(v).format('YYYY-MM-DD HH:mm');
}

/// 是否会员卡支付：用支付方式判断，非会员卡支付的订单一律按散客展示
function isMemberPaid(row: OrderListItem) {
  return row.payMethod === 'MemberCard';
}
function memberCardCell(row: OrderListItem) {
  if (!isMemberPaid(row)) return '散客';
  return row.memberCardNo ?? '散客';
}
function memberPhoneCell(row: OrderListItem) {
  if (!isMemberPaid(row)) return '散客';
  return row.memberPhone ?? '散客';
}

function memberTypeKindLabel(k: string | null | undefined) {
  return k === 'StoredValue' ? '充值卡' : k === 'CountBased' ? '次卡' : '普通';
}

/// 详情卡片是否按"会员消费"展示：会员卡支付，或订单挂着会员
function isMemberOrder(o: Order) {
  return o.payMethod === 'MemberCard' || o.memberId != null || !!o.memberCardNo;
}

/// 合计金额取面值优先（含次卡），缺失时退回 total
function headlineTotal(o: Order) {
  return o.listTotal && o.listTotal > 0 ? o.listTotal : o.total;
}

/// 明细金额：面值优先，否则按 listUnitPrice × quantity，最后回退到 itemTotal
function rowAmount(row: OrderItem) {
  if (row.listAmount != null && row.listAmount > 0) return row.listAmount;
  if (row.listUnitPrice != null && row.listUnitPrice > 0)
    return Math.round(row.listUnitPrice * row.quantity * 100) / 100;
  return row.itemTotal;
}

async function reload() {
  if (!appStore.activeStoreId) return;
  loading.value = true;
  try {
    const data = await ordersApi.list({
      page: query.page,
      pageSize: query.pageSize,
      storeId: appStore.activeStoreId,
      status: query.status || undefined,
      from: dateRange.value?.[0],
      to: dateRange.value?.[1]
    });
    rows.value = data.items;
    total.value = data.total;
  } finally {
    loading.value = false;
  }
}

function resetQuery() {
  query.status = '';
  dateRange.value = null;
  query.page = 1;
  reload();
}

async function openDetail(row: OrderListItem) {
  drawerOpen.value = true;
  detail.value = await ordersApi.get(row.id);
}

async function onRefund() {
  if (!detail.value) return;
  const reason = await ElMessageBox.prompt('请输入退款原因', '确认退款', {
    inputPlaceholder: '可选',
    confirmButtonText: '确认退款',
    cancelButtonText: '取消',
    type: 'warning'
  }).catch(() => null);
  if (!reason) return;
  await ordersApi.refund(detail.value.id, reason.value || null);
  ElMessage.success('已退款');
  drawerOpen.value = false;
  reload();
}

async function onCancel() {
  if (!detail.value) return;
  await ElMessageBox.confirm('确认取消该订单？', '提示', { type: 'warning' }).catch(() => null);
  await ordersApi.cancel(detail.value.id);
  ElMessage.success('已取消');
  drawerOpen.value = false;
  reload();
}

onMounted(async () => {
  await appStore.loadStores();
  await reload();
  staffApi.list({ role: 'Technician', pageSize: 200, storeId: appStore.activeStoreId ?? undefined })
    .then((r) => { technicians.value = r.items; })
    .catch(() => null);
});
</script>

<style scoped>
.page { padding-bottom: 24px; }
.toolbar { display: flex; gap: 8px; align-items: center; flex-wrap: wrap; }
.actions { margin-top: 20px; display: flex; gap: 12px; justify-content: flex-end; }
:deep(.el-table .el-table__row) { cursor: pointer; }

/* 订单详情抽屉：整体放大，留更多呼吸空间 */
.detail-body { padding: 4px 4px 24px; }
.order-detail-drawer :deep(.el-drawer__header) { font-size: 18px; font-weight: 600; margin-bottom: 12px; }
.detail-desc :deep(.el-descriptions__label) { font-size: 15px; padding: 12px 12px; min-width: 96px; }
.detail-desc :deep(.el-descriptions__content) { font-size: 15px; padding: 12px 12px; }
.detail-divider { margin: 24px 0 12px; }
.detail-divider :deep(.el-divider__text) { font-size: 16px; font-weight: 600; }
.detail-items :deep(.el-table__cell) { padding: 12px 8px; font-size: 15px; }
.detail-items :deep(.el-table__header th) { font-size: 14px; font-weight: 600; }
.detail-items .item-name { font-weight: 500; }
.muted { color: var(--el-text-color-secondary); font-size: 13px; }
/* 会员卡号 / 手机号单行显示，超出由 el-table 的 tooltip 接管 */
.nowrap-cell { white-space: nowrap; display: inline-block; max-width: 100%; }
/* 散客标记：灰色弱化，与真实会员信息区分 */
.nowrap-cell.walkin { color: var(--el-text-color-secondary); }
</style>
