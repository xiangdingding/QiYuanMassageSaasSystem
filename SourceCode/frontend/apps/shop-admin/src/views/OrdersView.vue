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
        <el-input
          v-model="query.keyword"
          placeholder="订单号 / 会员卡号 / 姓名 / 手机号"
          clearable
          style="width: 320px"
          aria-label="按订单号、会员卡号、姓名或手机号模糊查询，回车直接搜索"
          @keyup.enter="onSearch"
          @clear="onSearch"
        />
        <el-date-picker
          v-model="dateRange"
          type="datetimerange"
          range-separator="至"
          start-placeholder="开始时间"
          end-placeholder="结束时间"
          format="YYYY-MM-DD HH:mm"
          value-format="YYYY-MM-DDTHH:mm:ss[Z]"
        />
        <el-button type="primary" @click="onSearch">查询</el-button>
        <el-button @click="resetQuery">重置</el-button>
      </div>

      <div class="table-wrap">
        <el-table :data="rows" v-loading="loading" stripe height="100%" @row-click="openDetail">
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

    <el-drawer
      v-model="drawerOpen"
      :title="detail ? `订单详情 ${detail.orderNo}` : '订单详情'"
      :aria-label="detail ? drawerAriaLabel(detail) : '订单详情'"
      size="860px"
      class="order-detail-drawer"
    >
      <div v-if="detail" class="detail-body">
        <el-descriptions
          :column="2"
          border
          size="large"
          class="detail-desc"
          :aria-label="`订单 ${detail.orderNo} 概要`"
        >
          <el-descriptions-item label="订单号" :span="2">{{ detail.orderNo }}</el-descriptions-item>
          <el-descriptions-item label="状态">
            <el-tag
              :type="statusType(detail.status)"
              size="large"
              :aria-label="`订单状态 ${statusLabel(detail.status)}`"
            >{{ statusLabel(detail.status) }}</el-tag>
          </el-descriptions-item>
          <el-descriptions-item label="支付方式">
            <span :aria-label="`支付方式 ${payLabel(detail.payMethod)}`">{{ payLabel(detail.payMethod) }}</span>
          </el-descriptions-item>
          <el-descriptions-item label="合计">
            <span :aria-label="`合计 ${yuanReadable(headlineTotal(detail))}${(detail.punchCardUsedCount ?? 0) > 0 ? '，含次卡面值' : ''}`">
              ¥{{ headlineTotal(detail).toFixed(2) }}
            </span>
            <span v-if="(detail.punchCardUsedCount ?? 0) > 0" class="muted" style="margin-left:6px" aria-hidden="true">（含次卡面值）</span>
          </el-descriptions-item>
          <el-descriptions-item label="优惠">
            <span :aria-label="`优惠 ${yuanReadable(detail.discountAmount)}`">¥{{ detail.discountAmount.toFixed(2) }}</span>
          </el-descriptions-item>
          <el-descriptions-item label="实收">
            <span :aria-label="`实收 ${yuanReadable(detail.paidAmount)}`">¥{{ detail.paidAmount.toFixed(2) }}</span>
          </el-descriptions-item>
          <el-descriptions-item label="收银员">{{ detail.cashierName ?? '—' }}</el-descriptions-item>
          <el-descriptions-item v-if="(detail.punchCardUsedCount ?? 0) > 0" label="消费次数">
            <span :aria-label="`次卡核销消费 ${detail.punchCardUsedCount} 次`">
              {{ detail.punchCardUsedCount }} 次（次卡核销）
            </span>
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
                :aria-label="`卡类型 ${detail.memberTypeName ?? memberTypeKindLabel(detail.memberTypeKind)}`"
              >{{ detail.memberTypeName ?? memberTypeKindLabel(detail.memberTypeKind) }}</el-tag>
              <span v-else>{{ detail.memberTypeName ?? '普通' }}</span>
            </el-descriptions-item>
          </template>
          <el-descriptions-item v-else label="会员卡">{{ detail.memberCardNo ?? '—' }}</el-descriptions-item>
          <el-descriptions-item label="备注" :span="2">{{ detail.remark ?? '—' }}</el-descriptions-item>
        </el-descriptions>
        <el-divider v-if="detail.items.length > 0" class="detail-divider">项目明细</el-divider>
        <el-table
          v-if="detail.items.length > 0"
          :data="detail.items"
          class="detail-items"
          stripe
          :aria-label="`订单 ${detail.orderNo} 的服务项目明细，共 ${detail.items.length} 项`"
          :row-class-name="itemRowAriaClass"
        >
          <el-table-column prop="serviceName" label="项目" min-width="180">
            <template #default="{ row }">
              <span class="item-name" :aria-label="itemRowAriaLabel(row)">{{ row.serviceName }}</span>
              <el-tag
                v-if="row.memberPackageId"
                size="small"
                type="success"
                style="margin-left:6px"
                aria-label="本项次卡核销"
              >次卡</el-tag>
            </template>
          </el-table-column>
          <el-table-column label="技师" width="220">
            <template #default="{ row }">
              <span>{{ row.technicianName }}</span>
              <el-tag
                v-if="row.assignmentSource === 'Rotation'"
                size="small" type="info" style="margin-left:4px"
                aria-label="上钟方式 轮钟"
              >轮钟</el-tag>
              <el-tag
                v-else-if="row.assignmentSource === 'Designation'"
                size="small" type="warning" style="margin-left:4px"
                aria-label="上钟方式 点钟"
              >点钟</el-tag>
              <el-tag
                v-if="row.transferredAt"
                size="small" type="warning" style="margin-left:4px"
                aria-label="已转钟"
              >已转</el-tag>
            </template>
          </el-table-column>
          <el-table-column label="房间" width="90">
            <template #default="{ row }">
              <span :aria-label="row.roomNo ? `房间 ${row.roomNo}` : '未指定房间'">{{ row.roomNo ?? '—' }}</span>
            </template>
          </el-table-column>
          <el-table-column prop="quantity" label="次数" width="80" align="right">
            <template #default="{ row }">
              <span :aria-label="`${row.quantity} 次`">{{ row.quantity }} 次</span>
            </template>
          </el-table-column>
          <el-table-column label="金额" width="120" align="right">
            <template #default="{ row }">
              <span :aria-label="`金额 ${yuanReadable(rowAmount(row))}`">¥{{ rowAmount(row).toFixed(2) }}</span>
            </template>
          </el-table-column>
          <el-table-column label="提成" width="110" align="right">
            <template #default="{ row }">
              <span :aria-label="`提成 ${yuanReadable(row.commissionAmount)}`">¥{{ row.commissionAmount.toFixed(2) }}</span>
            </template>
          </el-table-column>
          <el-table-column
            v-if="detail.status === 'Pending' || detail.status === 'InProgress' || detail.status === 'Completed'"
            label="操作"
            width="120"
            fixed="right"
          >
            <template #default="{ row }">
              <el-button
                v-if="detail!.status === 'Pending' || detail!.status === 'InProgress'"
                size="large"
                :aria-label="`转钟，把 ${row.serviceName} 的技师 ${row.technicianName ?? ''} 换成其他人`"
                @click="openTransfer(row)"
              >转钟</el-button>
              <el-button
                v-else
                size="large"
                type="warning"
                :aria-label="`登记投诉，针对 ${row.serviceName} 的技师 ${row.technicianName ?? ''}`"
                @click="openComplaint(row)"
              >投诉</el-button>
            </template>
          </el-table-column>
        </el-table>

        <template v-if="(detail.roomCharges?.length ?? 0) > 0">
          <el-divider class="detail-divider">计时房费</el-divider>
          <el-table
            :data="detail.roomCharges ?? []"
            class="detail-items"
            stripe
            :aria-label="`订单 ${detail.orderNo} 的计时房费明细，共 ${detail.roomCharges!.length} 条`"
          >
            <el-table-column label="房间" min-width="120">
              <template #default="{ row }">
                <el-tag type="primary" size="small" style="margin-right:6px" aria-label="计时房">计时房</el-tag>
                <span class="item-name" :aria-label="roomChargeRowAriaLabel(row)">{{ row.roomNo }}</span>
              </template>
            </el-table-column>
            <el-table-column label="时长" width="110" align="right">
              <template #default="{ row }">
                <span :aria-label="`时长 ${row.minutes} 分钟`">{{ row.minutes }} 分钟</span>
              </template>
            </el-table-column>
            <el-table-column label="单价" width="130" align="right">
              <template #default="{ row }">
                <span :aria-label="`单价 ${yuanReadable(row.hourlyRate)} 每小时`">¥{{ row.hourlyRate.toFixed(2) }} / 时</span>
              </template>
            </el-table-column>
            <el-table-column label="金额" width="120" align="right">
              <template #default="{ row }">
                <span :aria-label="`金额 ${yuanReadable(row.amount)}`">¥{{ row.amount.toFixed(2) }}</span>
              </template>
            </el-table-column>
            <el-table-column label="状态" width="100">
              <template #default="{ row }">
                <el-tag
                  :type="roomStatusType(row.status)"
                  size="small"
                  :aria-label="`状态 ${roomStatusLabel(row.status)}`"
                >{{ roomStatusLabel(row.status) }}</el-tag>
              </template>
            </el-table-column>
          </el-table>
        </template>

        <div class="actions" role="group" aria-label="订单操作">
          <el-button
            v-if="detail.status === 'Completed'"
            type="danger"
            size="large"
            :aria-label="`退款，把已完成订单 ${detail.orderNo} 实收 ${yuanReadable(detail.paidAmount)} 退还`"
            @click="onRefund"
          >退款</el-button>
          <el-button
            v-if="detail.status === 'Pending'"
            size="large"
            :aria-label="`取消未结账订单 ${detail.orderNo}`"
            @click="onCancel"
          >取消订单</el-button>
        </div>
      </div>
    </el-drawer>

    <el-dialog
      v-model="transferOpen"
      title="转钟（更换技师）"
      :aria-label="transferTarget ? `转钟对话框：把 ${transferTarget.serviceName} 的技师 ${transferTarget.technicianName ?? ''} 换成其他人` : '转钟对话框'"
      width="460px"
    >
      <el-form label-width="100px" size="large">
        <el-form-item label="项目">
          <span :aria-label="`项目 ${transferTarget?.serviceName ?? ''}`">{{ transferTarget?.serviceName }}</span>
        </el-form-item>
        <el-form-item label="原技师">
          <span :aria-label="`原技师 ${transferTarget?.technicianName ?? '未指派'}`">{{ transferTarget?.technicianName }}</span>
        </el-form-item>
        <el-form-item label="新技师" required>
          <el-select
            v-model="transferTo"
            placeholder="搜索工号或姓名，回车选中"
            filterable
            style="width: 100%"
            aria-label="选择新技师"
          >
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
          <el-input
            v-model="transferReason"
            maxlength="200"
            placeholder="如：客人要求换人 / 原技师有事"
            aria-label="转钟原因，可选"
          />
        </el-form-item>
      </el-form>
      <template #footer>
        <el-button
          size="large"
          :aria-label="'放弃转钟并关闭对话框'"
          @click="transferOpen = false"
        >取消</el-button>
        <el-button
          type="primary"
          size="large"
          :loading="transferring"
          :aria-label="'确认转钟，新技师生效'"
          @click="doTransfer"
        >确认转钟</el-button>
      </template>
    </el-dialog>

    <el-dialog
      v-model="complaintOpen"
      title="登记投诉"
      width="520px"
    >
      <el-form label-width="100px" size="large">
        <el-form-item label="项目">
          <span>{{ complaintTarget?.serviceName }}</span>
        </el-form-item>
        <el-form-item label="技师">
          <span>{{ complaintTarget?.technicianName }}</span>
        </el-form-item>
        <el-form-item label="标签">
          <el-input
            v-model="complaintForm.tags"
            placeholder="逗号分隔，如：态度差,力度不合适"
            maxlength="200"
          />
        </el-form-item>
        <el-form-item label="描述">
          <el-input
            v-model="complaintForm.comment"
            type="textarea"
            :rows="3"
            maxlength="500"
            show-word-limit
            placeholder="客户原话/补充"
          />
        </el-form-item>
      </el-form>
      <template #footer>
        <el-button size="large" @click="complaintOpen = false">取消</el-button>
        <el-button type="warning" size="large" :loading="complainting" @click="doComplaint">登记投诉</el-button>
      </template>
    </el-dialog>
  </div>
</template>

<script setup lang="ts">
import { onMounted, reactive, ref } from 'vue';
import { ElMessage, ElMessageBox } from 'element-plus';
import dayjs from 'dayjs';
import { complaintsApi, ordersApi, ordersTransferApi, staffApi } from '@/api/modules';
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

const complaintOpen = ref(false);
const complaintTarget = ref<OrderItem | null>(null);
const complaintForm = reactive({ tags: '', comment: '' });
const complainting = ref(false);

function openComplaint(row: OrderItem) {
  complaintTarget.value = row;
  complaintForm.tags = '';
  complaintForm.comment = '';
  complaintOpen.value = true;
}

async function doComplaint() {
  if (!complaintTarget.value) return;
  complainting.value = true;
  try {
    await complaintsApi.create({
      orderItemId: complaintTarget.value.id,
      tags: complaintForm.tags.trim() || null,
      comment: complaintForm.comment.trim() || null
    });
    ElMessage.success('已登记投诉');
    complaintOpen.value = false;
  } catch {
    /* http 已弹错 */
  } finally {
    complainting.value = false;
  }
}

const query = reactive<{ page: number; pageSize: number; status: string; keyword: string }>({
  page: 1,
  pageSize: 20,
  status: '',
  keyword: ''
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
  return dayjs(v).format('YYYY-MM-DD HH:mm:ss');
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

function roomStatusLabel(s: string) {
  return ({ Open: '计时中', Settled: '已结算', Cancelled: '已作废' } as Record<string, string>)[s] ?? s;
}
function roomStatusType(s: string) {
  return ({ Open: 'warning', Settled: 'success', Cancelled: 'info' } as Record<string, any>)[s] ?? '';
}

/// 金额朗读：避免读屏把"32.5"念成"三十二点五"，给出"32 元 5 角"
function yuanReadable(amount: number): string {
  const safe = Number.isFinite(amount) ? amount : 0;
  const yuan = Math.floor(safe);
  const jiao = Math.round((safe - yuan) * 10);
  return jiao === 0 ? `${yuan} 元` : `${yuan} 元 ${jiao} 角`;
}

/// 给抽屉一开就能朗读出来的综合述：订单号、状态、应收、含多少项、何时完成
function drawerAriaLabel(o: Order): string {
  const itemDesc = o.items.length > 0 ? `${o.items.length} 项服务` : '无服务项';
  const roomDesc = (o.roomCharges?.length ?? 0) > 0 ? `，${o.roomCharges!.length} 条计时房费` : '';
  const memberDesc = o.memberCardNo ? `，会员 ${o.memberCardNo}` : '';
  return `订单详情 ${o.orderNo}，状态 ${statusLabel(o.status)}，实收 ${yuanReadable(o.paidAmount)}${memberDesc}，含 ${itemDesc}${roomDesc}`;
}

/// 项目明细行的整体朗读：用 row-class-name 的回调里挂一个 aria 字符串到 className，
/// 但 Element Plus 表格不直接支持每行 aria-label；这里改在第一列的 item-name 上挂
function itemRowAriaLabel(row: OrderItem): string {
  const tech = row.technicianName ?? '未指派';
  const sourceDesc = row.assignmentSource === 'Rotation' ? '，上钟方式 轮钟'
    : row.assignmentSource === 'Designation' ? '，上钟方式 点钟'
    : '';
  const transferDesc = row.transferredAt ? '，已转钟' : '';
  const room = row.roomNo ? `，房间 ${row.roomNo}` : '';
  return `${row.serviceName}，技师 ${tech}${sourceDesc}${transferDesc}${room}，数量 ${row.quantity}，金额 ${yuanReadable(rowAmount(row))}，提成 ${yuanReadable(row.commissionAmount)}`;
}

function itemRowAriaClass() {
  // 占位钩子：保留以便后续 CSS 标记某些行（如已转钟）
  return '';
}

function roomChargeRowAriaLabel(row: NonNullable<Order['roomCharges']>[number]): string {
  return `计时房 ${row.roomNo}，时长 ${row.minutes} 分钟，单价 ${yuanReadable(row.hourlyRate)} 每小时，金额 ${yuanReadable(row.amount)}，状态 ${roomStatusLabel(row.status)}`;
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
      to: dateRange.value?.[1],
      keyword: query.keyword.trim() || undefined
    });
    rows.value = data.items;
    total.value = data.total;
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
/* 视口锁定：工具栏 / 分页固定，仅 .table-wrap 内的表格滚动 */
.page { height: 100%; display: flex; flex-direction: column; min-height: 0; }
.page > :deep(.el-card) { flex: 1 1 auto; display: flex; flex-direction: column; min-height: 0; }
.page > :deep(.el-card) > :deep(.el-card__body) {
  flex: 1 1 auto;
  display: flex;
  flex-direction: column;
  min-height: 0;
  overflow: hidden;
}
.toolbar { display: flex; gap: 8px; align-items: center; flex-wrap: wrap; flex: 0 0 auto; }
.table-wrap { flex: 1 1 auto; min-height: 0; margin-top: 12px; }
.pager { flex: 0 0 auto; }
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
