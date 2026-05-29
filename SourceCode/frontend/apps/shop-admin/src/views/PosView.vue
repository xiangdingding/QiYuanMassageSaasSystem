<template>
  <div class="pos" role="region" aria-label="收银台" :style="{ gridTemplateColumns: `1fr 6px ${rightWidth}px` }">
    <div class="left">
      <el-card shadow="never" class="services-card">
        <template #header>
          <div class="header-row">
            <h2 class="card-title">选择服务项目</h2>
            <el-input
              ref="serviceFilterInput"
              v-model="serviceFilter"
              placeholder="按编码或名称过滤，回车快速加入第一项"
              size="default"
              clearable
              style="width: 280px"
              :prefix-icon="Search"
              aria-label="搜索服务项目，按回车快速添加第一项"
              @keyup.enter="quickAddFirst"
            />
          </div>
        </template>
        <div class="services-grid" role="list" aria-label="服务项目列表">
          <el-card
            v-for="s in filteredServices"
            :key="s.id"
            class="service-card"
            shadow="hover"
            tabindex="0"
            role="button"
            :aria-label="`服务 ${s.name}，时长 ${s.durationMinutes} 分钟，标准价 ${s.price.toFixed(2)} 元，会员价 ${s.memberPrice.toFixed(2)} 元${s.description ? '，备注：' + s.description : ''}，当前模式将按${mode === 'member' ? '会员价' : '标准价'}下单，按回车添加`"
            @click="onPickService(s)"
            @keyup.enter="onPickService(s)"
            @keyup.space.prevent="onPickService(s)"
          >
            <div class="svc-name">
              <span class="svc-code">{{ s.code }}</span>
              <span class="svc-name-text">{{ s.name }}</span>
            </div>
            <div class="svc-meta">
              <el-tag size="small">{{ s.durationMinutes }} 分钟</el-tag>
              <el-tag
                size="small"
                :type="mode === 'walkin' ? 'success' : 'info'"
                :effect="mode === 'walkin' ? 'dark' : 'plain'"
              >标准 ¥{{ s.price.toFixed(2) }}</el-tag>
              <el-tag
                size="small"
                :type="mode === 'member' ? 'warning' : 'info'"
                :effect="mode === 'member' ? 'dark' : 'plain'"
              >会员 ¥{{ s.memberPrice.toFixed(2) }}</el-tag>
            </div>
            <div v-if="s.description" class="svc-desc" :title="s.description">{{ s.description }}</div>
          </el-card>
        </div>
      </el-card>

      <el-card v-if="timedRooms.length > 0" shadow="never" class="timed-card">
        <template #header>
          <div class="header-row">
            <h2 class="card-title">计时房费</h2>
            <span class="muted">点「开台」由收银开始计时；点「加入订单」把当前房费加入购物车一起结算</span>
          </div>
        </template>
        <div class="timed-grid" role="list" aria-label="计时房列表">
          <el-card
            v-for="r in timedRooms"
            :key="r.id"
            class="timed-card-item"
            shadow="hover"
            :class="{ open: openSessionOf(r.id), inCart: openSessionOf(r.id) && cartRoomSessionIds.has(openSessionOf(r.id)!.id) }"
          >
            <div class="timed-head">
              <span class="timed-no">{{ r.roomNo }}</span>
              <el-tag size="small">¥{{ r.hourlyRate.toFixed(2) }}/时</el-tag>
            </div>
            <template v-if="openSessionOf(r.id)">
              <div class="timed-info">
                <div>已计时 <strong>{{ openSessionOf(r.id)!.elapsedMinutes }}</strong> 分钟</div>
                <div class="muted">客 {{ openSessionOf(r.id)!.customerName || openSessionOf(r.id)!.memberName || '散客' }}</div>
                <div class="timed-amount">≈ ¥{{ (openSessionOf(r.id)!.elapsedMinutes / 60 * openSessionOf(r.id)!.hourlyRateSnapshot).toFixed(2) }}</div>
              </div>
              <div class="timed-actions">
                <el-button
                  type="primary"
                  size="small"
                  :disabled="cartRoomSessionIds.has(openSessionOf(r.id)!.id)"
                  :aria-label="`将 ${r.roomNo} 计时房费加入订单`"
                  @click="addRoomChargeToCart(r, openSessionOf(r.id)!)"
                >
                  {{ cartRoomSessionIds.has(openSessionOf(r.id)!.id) ? '已加入' : '加入订单' }}
                </el-button>
                <el-button
                  size="small"
                  type="danger"
                  :aria-label="`取消 ${r.roomNo} 的计时（不计费）`"
                  @click="cancelTiming(r, openSessionOf(r.id)!)"
                >
                  取消计时
                </el-button>
              </div>
            </template>
            <template v-else>
              <div class="timed-info muted">空闲</div>
              <el-button
                type="success"
                size="small"
                :aria-label="`为 ${r.roomNo} 开台计时`"
                @click="openStartTiming(r)"
              >开台</el-button>
            </template>
          </el-card>
        </div>
      </el-card>
    </div>

    <div
      class="splitter"
      role="separator"
      aria-orientation="vertical"
      aria-label="拖动调整结账区宽度"
      tabindex="0"
      @mousedown="startResize"
      @keydown.left="rightWidth = clampRight(rightWidth + 20)"
      @keydown.right="rightWidth = clampRight(rightWidth - 20)"
    ></div>

    <div class="right">
      <el-card shadow="never" class="cart">
        <template #header>
          <div class="header-row">
            <h2 class="card-title">当前订单</h2>
            <el-button v-if="cart.length > 0 || memberCards.length > 0" link type="danger" aria-label="清空当前订单" @click="resetAll">清空</el-button>
          </div>
        </template>

        <el-radio-group v-model="mode" class="mode-switch" aria-label="收银模式" @change="onModeChange">
          <el-radio-button value="member">会员收银</el-radio-button>
          <el-radio-button value="walkin">散客收银</el-radio-button>
        </el-radio-group>

        <template v-if="mode === 'member'">
          <div class="member-row">
            <el-input
              ref="memberInput"
              v-model="memberKeyword"
              placeholder="会员卡号 / 手机号"
              clearable
              :prefix-icon="User"
              style="flex: 1"
              aria-label="会员卡号或手机号，按 F2 快速聚焦"
              @keyup.enter="lookupMember"
            />
            <el-button :icon="Search" aria-label="查询会员" @click="lookupMember">查询</el-button>
          </div>

          <div v-if="memberCards.length > 0" class="member-info">
            <div class="m-line">
              <strong>{{ primaryMember?.name || primaryMember?.cardNo || memberCards[0].phone }}</strong>
              <el-tag size="small">{{ memberCards[0].phone }}</el-tag>
              <el-tag v-if="memberCards.length > 1" size="small" type="info">
                {{ memberCards.length }} 张卡 · 可合并结算
              </el-tag>
            </div>
            <div class="card-pick-list">
              <div v-for="c in memberCards" :key="c.id" class="card-pick-row" :class="{ closed: !c.isActive }">
                <el-checkbox
                  :model-value="selectedCardIds.includes(c.id)"
                  :disabled="!c.isActive || !isCardEligible(c)"
                  :aria-label="cardAriaLabel(c)"
                  @update:model-value="toggleCard(c.id, $event)"
                >
                  <span class="card-no">{{ c.cardNo }}</span>
                  <el-tag size="small" :type="c.memberTypeKind === 'StoredValue' ? 'warning' : (c.memberTypeKind === 'CountBased' ? 'success' : 'info')" style="margin: 0 6px">
                    {{ c.memberTypeName ?? '普通' }}
                  </el-tag>
                  <el-tag v-if="!c.isActive" size="small" type="info" style="margin-right:6px">已关闭</el-tag>
                  <el-tag v-else-if="c.memberTypeKind === 'CountBased' && !isCardEligible(c)" size="small" type="danger" style="margin-right:6px">
                    无匹配项目
                  </el-tag>
                  <span class="card-bal">余额 ¥{{ c.balance.toFixed(2) }}</span>
                  <span v-if="c.memberTypeKind === 'CountBased' && c.remainCount != null" class="muted">
                    · 剩 {{ c.remainCount }} 次
                  </span>
                  <span v-if="c.memberTypeKind === 'CountBased' && c.serviceItemName" class="muted">
                    · 绑定 {{ c.serviceItemName }}
                  </span>
                </el-checkbox>
              </div>
            </div>
            <div class="m-line">
              <span class="muted">已选 {{ selectedCardIds.length }} 张 · 合计余额</span>
              <strong style="color:#d9534f">¥{{ selectedBalance.toFixed(2) }}</strong>
              <el-button link type="danger" size="small" @click="clearMember">取消关联</el-button>
            </div>
          </div>
        </template>

        <el-divider style="margin: 8px 0" />

        <div v-if="cart.length === 0" class="empty" role="status">未添加任何项目</div>
        <div
          v-else
          class="cart-grid"
          role="table"
          :aria-label="`当前订单，共 ${cart.length} 项`"
        >
          <!-- 表头 -->
          <div class="cg-row cg-header" role="row">
            <span role="columnheader">项目</span>
            <span role="columnheader">技师</span>
            <span role="columnheader">排钟</span>
            <span role="columnheader">房间</span>
            <span role="columnheader" class="t-right">钟数</span>
            <span role="columnheader" class="t-right">小计</span>
            <span role="columnheader" aria-label="操作"></span>
          </div>

          <!-- 项目行 -->
          <div
            v-for="(it, idx) in cart"
            :key="idx"
            class="cg-row"
            role="row"
            :aria-label="cartRowAriaLabel(it, idx)"
          >
            <span role="cell" class="cg-name" :title="it.serviceName">
              <el-tag v-if="it.kind === 'roomCharge'" type="primary" size="small" style="margin-right:4px">计时房</el-tag>
              {{ it.serviceName }}
              <el-tag
                v-if="it.kind === 'roomCharge' && isRoomChargeMismatch(it)"
                type="danger"
                size="small"
                style="margin-left:4px"
                :aria-label="`计时房 ${it.roomNo} 开台绑定会员 ${it.boundMemberName ?? '未知'}，与当前结算会员不一致`"
              >会员不一致</el-tag>
            </span>
            <span role="cell">
              <el-select
                v-if="it.kind === 'service'"
                v-model="it.technicianId"
                placeholder="选技师"
                size="default"
                filterable
                style="width: 100%"
                :aria-label="`${it.serviceName} 的技师`"
              >
                <el-option
                  v-for="t in technicians"
                  :key="t.id"
                  :label="`${t.employeeNo ?? '-'} · ${t.realName ?? t.username}`"
                  :value="t.id"
                />
              </el-select>
              <span v-else class="muted">—</span>
            </span>
            <span role="cell">
              <el-select
                v-if="it.kind === 'service'"
                v-model="it.assignmentSource"
                size="default"
                style="width: 100%"
                :aria-label="`${it.serviceName} 的排钟方式，二选一`"
              >
                <el-option label="轮钟" value="Rotation" />
                <el-option label="点钟" value="Designation" />
              </el-select>
              <span v-else class="muted">—</span>
            </span>
            <span role="cell">
              <el-select
                v-if="it.kind === 'service'"
                v-model="it.roomId"
                placeholder="—"
                size="default"
                clearable
                style="width: 100%"
                :aria-label="`${it.serviceName} 的房间，可空`"
              >
                <el-option
                  v-for="r in availableRooms(it.roomId)"
                  :key="r.id"
                  :label="r.roomNo"
                  :value="r.id"
                />
              </el-select>
              <span v-else class="muted">{{ it.roomNo }}</span>
            </span>
            <span role="cell" class="t-right">
              <el-input-number
                v-if="it.kind === 'service'"
                v-model="it.quantity"
                :min="1"
                :max="20"
                size="default"
                controls-position="right"
                style="width: 100%"
                :aria-label="`${it.serviceName} 的钟数，最少 1 最多 20`"
              />
              <span
                v-else
                class="muted"
                :aria-label="`已计时 ${it.elapsedMinutes} 分钟`"
              >{{ it.elapsedMinutes }} 分</span>
            </span>
            <span role="cell" class="t-right">
              <strong
                class="ci-price"
                :aria-label="`小计 ${yuanReadable(it.kind === 'service' ? it.unitPrice * it.quantity : it.unitPrice)}`"
              >¥{{ (it.kind === 'service' ? it.unitPrice * it.quantity : it.unitPrice).toFixed(2) }}</strong>
            </span>
            <span role="cell" class="t-center">
              <el-button
                link
                type="danger"
                :aria-label="`从订单中移除第 ${idx + 1} 项 ${it.serviceName}`"
                @click="cart.splice(idx, 1)"
              >移除</el-button>
            </span>
          </div>
        </div>

        <div class="cart-footer" role="group" aria-label="结算操作">
          <div class="totals" role="group" aria-label="订单金额汇总">
            <div class="total-line">
              <span>合计</span>
              <span
                class="total-amount"
                :aria-label="`合计 ${yuanReadable(total)}`"
              >¥ {{ total.toFixed(2) }}</span>
            </div>
            <div class="total-line">
              <span>应收</span>
              <span
                class="total-amount"
                :aria-label="`应收 ${yuanReadable(payable)}`"
              >¥ {{ payable.toFixed(2) }}</span>
            </div>
          </div>

          <el-button
            type="primary"
            size="large"
            :disabled="!canCheckout"
            :loading="checkingOut"
            class="checkout-btn"
            :aria-label="`下单并结账，应收 ${yuanReadable(payable)}`"
            @click="openCheckout"
          >
            下单并结账（应收 ¥{{ payable.toFixed(2) }}）
          </el-button>
          <div class="hint" aria-live="polite">
            <span v-if="cart.length > 0 && mismatchedRoomCharges.length > 0" class="hint-danger">
              计时房费绑定的会员与当前结算会员不一致，请关联同一会员或将该房费从订单中移除后再结算
            </span>
            <span v-else-if="cart.length > 0 && !canCheckout">提示：请为每个项目都指派技师后才能结账</span>
          </div>
        </div>
      </el-card>
    </div>

    <PickTechnicianDialog
      v-model="pickOpen"
      :service="pickedService"
      :technicians="technicians"
      :is-member="!!primaryMember"
      @confirm="onTechnicianPicked"
    />

    <CheckoutDialog
      v-model="checkoutOpen"
      :total="total"
      :payable="payable"
      :has-member="!!primaryMember"
      :member-balance="selectedBalance"
      :loading="checkingOut"
      @submit="doCheckout"
    />

    <el-dialog v-model="receiptOpen" title="结账成功" width="480px">
      <div>
        <p v-if="lastOrder"><strong>订单号：</strong>{{ lastOrder.orderNo }}</p>
        <p>
          <strong>合计：</strong>¥ {{ receiptHeadlineTotal().toFixed(2) }}
          <span v-if="(lastOrder?.punchCardUsedCount ?? 0) > 0" class="muted" style="margin-left:6px">
            （面值，含次卡抵扣）
          </span>
        </p>
        <p>
          <strong>实收：</strong>¥ {{ receiptPaidTotal().toFixed(2) }}（{{ payMethodLabel(receiptPayMethod()) }}）
        </p>
        <p v-if="(lastOrder?.punchCardUsedCount ?? 0) > 0">
          <strong>消费次数：</strong>{{ lastOrder!.punchCardUsedCount }} 次（次卡核销）
        </p>
        <p v-if="(lastOrder?.discountAmount ?? 0) > 0">优惠：¥ {{ lastOrder!.discountAmount.toFixed(2) }}</p>

        <el-table v-if="lastOrder && lastOrder.items.length > 0" :data="lastOrder.items" size="small">
          <el-table-column prop="serviceName" label="项目" />
          <el-table-column label="技师" width="120">
            <template #default="{ row }">
              <span>{{ row.technicianName }}</span>
              <el-tag
                v-if="row.assignmentSource === 'Rotation'"
                size="small" type="info" style="margin-left:4px"
              >轮</el-tag>
              <el-tag
                v-else-if="row.assignmentSource === 'Designation'"
                size="small" type="warning" style="margin-left:4px"
              >点</el-tag>
            </template>
          </el-table-column>
          <el-table-column label="次数" width="60" align="right">
            <template #default="{ row }">{{ row.quantity }} 次</template>
          </el-table-column>
          <el-table-column label="金额" width="110" align="right">
            <template #default="{ row }">
              <span>¥{{ rowListAmount(row).toFixed(2) }}</span>
              <el-tag
                v-if="row.memberPackageId"
                size="small"
                type="success"
                style="margin-left:4px"
              >次卡</el-tag>
            </template>
          </el-table-column>
        </el-table>

        <el-table v-if="lastRoomCharges.length > 0" :data="lastRoomCharges" size="small" style="margin-top:8px">
          <el-table-column label="计时房" prop="roomNo" width="80" />
          <el-table-column label="时长" width="80" align="right">
            <template #default="{ row }">{{ row.minutes }} 分钟</template>
          </el-table-column>
          <el-table-column label="单价" width="100" align="right">
            <template #default="{ row }">¥{{ row.hourlyRate.toFixed(2) }}/时</template>
          </el-table-column>
          <el-table-column label="金额" width="100" align="right">
            <template #default="{ row }">¥{{ row.amount.toFixed(2) }}</template>
          </el-table-column>
        </el-table>
      </div>
      <template #footer>
        <el-button type="primary" @click="receiptOpen = false; resetAll()">完成</el-button>
      </template>
    </el-dialog>

    <el-dialog
      v-model="startTimingOpen"
      :title="`开台计时：${startTimingRoom?.roomNo ?? ''} 号房`"
      width="460px"
    >
      <el-form label-width="100px">
        <el-form-item label="单价">¥{{ startTimingRoom?.hourlyRate.toFixed(2) }} / 小时</el-form-item>
        <el-form-item label="关联会员">
          <el-select
            :model-value="startTimingForm.memberPhone"
            filterable
            remote
            clearable
            :remote-method="searchMembersForTiming"
            :loading="memberSearchLoading"
            placeholder="输入卡号或手机号搜索，按人聚合"
            style="width: 100%"
            aria-label="按卡号或手机号模糊查询，按人聚合，无需指定具体卡"
            @update:model-value="onMemberSelected"
          >
            <el-option
              v-for="g in memberSearchResults"
              :key="g.phone"
              :value="g.phone"
              :label="memberOptionLabel(g)"
            />
          </el-select>
          <div class="hint" style="margin-top:4px">
            按手机号聚合显示，一个会员只出一行（含名下所有卡）。未关联时按散客处理。已关联：
            <strong v-if="startTimingForm.memberLabel">{{ startTimingForm.memberLabel }}</strong>
            <span v-else class="muted">无</span>
          </div>
        </el-form-item>
        <el-form-item v-if="!startTimingForm.memberPhone" label="散客姓名">
          <el-input
            v-model="startTimingForm.customerName"
            placeholder="可空；如未关联会员可填客人称呼"
            maxlength="64"
          />
        </el-form-item>
        <el-form-item label="备注">
          <el-input v-model="startTimingForm.remark" maxlength="200" />
        </el-form-item>
      </el-form>
      <template #footer>
        <el-button @click="startTimingOpen = false">取消</el-button>
        <el-button type="primary" :loading="startingTimer" @click="doStartTiming">开台</el-button>
      </template>
    </el-dialog>
  </div>
</template>

<script setup lang="ts">
import { computed, onMounted, reactive, ref, watch } from 'vue';

// 给 keep-alive 用：MainLayout 的 <keep-alive :include="['PosView']"> 靠这个 name 命中，
// 实现切走再切回时购物车 / 已选会员 / 模式都保留，不重新加载目录。
defineOptions({ name: 'PosView' });
import { ElMessage, ElMessageBox } from 'element-plus';
import { Search, User } from '@element-plus/icons-vue';
import { membersApi, ordersApi, roomsApi, servicesApi, staffApi, timedRoomsApi, type TimedRoomSessionDto } from '@/api/modules';
import { useAppStore } from '@/stores/app';
import { useShortcuts } from '@/composables/useShortcuts';
import type { Member, MemberPhoneGroup, Order, OrderItem, OrderRoomCharge, Room, ServiceItem, Staff } from '@/api/types';
import PickTechnicianDialog from '@/views/components/PickTechnicianDialog.vue';
import CheckoutDialog from '@/views/components/CheckoutDialog.vue';

/// CartItem 区分两类：
/// - service：普通服务项，要选技师/房间/数量
/// - roomCharge：计时房费快照，已绑定 sessionId，无需技师/房间/数量
interface ServiceCartItem {
  kind: 'service';
  serviceId: number;
  serviceName: string;
  technicianId: number | null;
  roomId: number | null;
  unitPrice: number;
  quantity: number;
  durationMinutes: number;
  /** 上钟方式：Rotation 轮钟 / Designation 点钟。在购物车里手动改技师会自动 flip 到 Designation。 */
  assignmentSource: 'Rotation' | 'Designation';
}
interface RoomChargeCartItem {
  kind: 'roomCharge';
  sessionId: number;
  roomId: number;
  roomNo: string;
  serviceName: string;        // "VIP1 计时房 60 分钟"
  unitPrice: number;          // 抓取时点的预估金额
  elapsedMinutes: number;     // 抓取时的已计时分钟
  hourlyRate: number;
  // 开台时绑定的会员（cardId 维度，但语义按"人"判定）；null 表示散客开台无约束
  boundMemberId: number | null;
  boundMemberName: string | null;
}
type CartItem = ServiceCartItem | RoomChargeCartItem;

const appStore = useAppStore();

const services = ref<ServiceItem[]>([]);
const technicians = ref<Staff[]>([]);
const rooms = ref<Room[]>([]);
const serviceFilter = ref('');
const serviceFilterInput = ref<{ focus: () => void } | null>(null);
const memberInput = ref<{ focus: () => void } | null>(null);

/// 计时房列表（仅 isTimedRoom = true 的房间）
const timedRooms = computed(() => rooms.value.filter((r) => r.isTimedRoom && r.isActive));
/// 本店当前有 Open 状态的计时 session（用于快速查询某间房是否正在计时）
const timedSessions = ref<TimedRoomSessionDto[]>([]);
function openSessionOf(roomId: number): TimedRoomSessionDto | undefined {
  return timedSessions.value.find((s) => s.roomId === roomId && s.status === 'Open');
}
/// 已被某行 roomCharge cart item 占用的 sessionId 集合，避免重复加入
const cartRoomSessionIds = computed(
  () => new Set(cart.filter((c) => c.kind === 'roomCharge').map((c) => (c as RoomChargeCartItem).sessionId))
);

function quickAddFirst() {
  const first = filteredServices.value[0];
  if (first) onPickService(first);
}

function availableRooms(_currentRoomId: number | null): Room[] {
  // 房间是纯属性、无独占——同一房间可同时被多个购物车行引用，由前台自行协调实际物理使用。
  // 普通服务的房间下拉里仍排除计时房（计时房通过 roomCharge 走另一条线）。
  return rooms.value.filter((r) => !r.isTimedRoom);
}
const cart = reactive<CartItem[]>([]);
/// 该手机号下的所有卡（一人多卡：充值卡 + 次卡）
const memberCards = ref<Member[]>([]);
/// 已勾选用于结账的卡 id（第一个为主卡，其余为合并结算的次要卡）
const selectedCardIds = ref<number[]>([]);
const memberKeyword = ref('');
/// 收银模式：散客 = 用标准价；会员 = 用会员价（折扣已在充值/开卡时兑现，结账不再打折）
const mode = ref<'walkin' | 'member'>('member');

/// 主卡：勾选列表中的第一张；用作 order.memberId
const primaryMember = computed<Member | null>(() => {
  if (selectedCardIds.value.length === 0) return null;
  return memberCards.value.find((c) => c.id === selectedCardIds.value[0]) ?? null;
});
/// 已勾选卡的余额合计（决定会员卡结算是否可行）
const selectedBalance = computed(() =>
  memberCards.value
    .filter((c) => selectedCardIds.value.includes(c.id))
    .reduce((s, c) => s + c.balance, 0)
);

function toggleCard(id: number, checked: boolean | string | number) {
  const on = !!checked;
  const idx = selectedCardIds.value.indexOf(id);
  if (on && idx === -1) {
    const card = memberCards.value.find((c) => c.id === id);
    if (card && !isCardEligible(card)) {
      ElMessage.warning(
        card.serviceItemName
          ? `次卡绑定的「${card.serviceItemName}」不在购物车里，不能选用`
          : '该次卡未绑定服务项目，不能用于结算'
      );
      return;
    }
    selectedCardIds.value = [...selectedCardIds.value, id];
  } else if (!on && idx !== -1) {
    const arr = [...selectedCardIds.value];
    arr.splice(idx, 1);
    selectedCardIds.value = arr;
  }
}

/// 次卡只有绑定的服务项目出现在购物车里才能结算；其它卡（充值卡 / 无类型）不受限。
/// roomCharge 不参与次卡匹配（次卡不可能绑定到虚拟的"计时房费"）
function isCardEligible(card: Member): boolean {
  if (card.memberTypeKind !== 'CountBased') return true;
  if (card.serviceItemId == null) return false;
  return cart.some((c) => c.kind === 'service' && (c as ServiceCartItem).serviceId === card.serviceItemId);
}

function cardAriaLabel(c: Member): string {
  const parts = [`选择卡号 ${c.cardNo}`, `余额 ${c.balance.toFixed(2)} 元`];
  if (c.memberTypeName) parts.push(c.memberTypeName);
  if (!c.isActive) parts.push('已关闭');
  else if (c.memberTypeKind === 'CountBased' && !isCardEligible(c))
    parts.push(c.serviceItemName ? `无匹配项目，需添加 ${c.serviceItemName}` : '无绑定服务，不可结算');
  return parts.join('，');
}

function clearMember() {
  memberCards.value = [];
  selectedCardIds.value = [];
  memberKeyword.value = '';
}

/// 会员输入框被清空（点 X 或手动删完）时，已搜出来的会员卡自动一起清空，
/// 避免"输入框空了但下面还挂着上次的会员"的迷惑状态。
watch(memberKeyword, (v) => {
  if (!v.trim() && memberCards.value.length > 0) {
    memberCards.value = [];
    selectedCardIds.value = [];
  }
});

/// 购物车变化（增删/换项）后，若某张已勾选的次卡绑定的服务不再出现，则自动取消勾选并提示。
/// 避免出现"勾选了但实际无法结算"的歧义状态。次卡匹配只看 service 行，roomCharge 不参与。
watch(
  () => cart.filter((c) => c.kind === 'service').map((c) => (c as ServiceCartItem).serviceId).join(','),
  () => {
    if (selectedCardIds.value.length === 0) return;
    const dropped: string[] = [];
    const kept = selectedCardIds.value.filter((id) => {
      const card = memberCards.value.find((c) => c.id === id);
      if (!card) return true;
      if (isCardEligible(card)) return true;
      dropped.push(card.serviceItemName ? `${card.cardNo}（${card.serviceItemName}）` : card.cardNo);
      return false;
    });
    if (dropped.length > 0) {
      selectedCardIds.value = kept;
      ElMessage.warning(`次卡 ${dropped.join('、')} 因购物车中无匹配服务已自动取消`);
    }
  }
);

/// 右侧结账区宽度（持久化到 localStorage，用户拖动调整）
const RIGHT_WIDTH_KEY = 'pos:rightWidth';
// 购物车走 CSS Grid，项目列 minmax(0,1fr) 自适应，永远不会出现横向滚动。
// 这里只挑一个让"项目列也够用"的舒适宽度做默认；用户拖到 MIN 也只是项目名被截断
const RIGHT_MIN = 600;
const RIGHT_MAX = 1100;
function clampRight(v: number) {
  return Math.max(RIGHT_MIN, Math.min(RIGHT_MAX, Math.round(v)));
}
const rightWidth = ref(clampRight(Number(localStorage.getItem(RIGHT_WIDTH_KEY)) || 760));

function startResize(ev: MouseEvent) {
  ev.preventDefault();
  const startX = ev.clientX;
  const startWidth = rightWidth.value;
  const onMove = (e: MouseEvent) => {
    // 鼠标右移 → 右栏变窄；左移 → 右栏变宽
    rightWidth.value = clampRight(startWidth - (e.clientX - startX));
  };
  const onUp = () => {
    document.removeEventListener('mousemove', onMove);
    document.removeEventListener('mouseup', onUp);
    localStorage.setItem(RIGHT_WIDTH_KEY, String(rightWidth.value));
  };
  document.addEventListener('mousemove', onMove);
  document.addEventListener('mouseup', onUp);
}

const pickOpen = ref(false);
const pickedService = ref<ServiceItem | null>(null);
const checkoutOpen = ref(false);
const receiptOpen = ref(false);
const checkingOut = ref(false);
const lastOrder = ref<Order | null>(null);
/// 收据弹窗用：本次结账挂在订单上的计时房费列表。由后端在 OrderDto.roomCharges 返回。
const lastRoomCharges = computed<OrderRoomCharge[]>(() => lastOrder.value?.roomCharges ?? []);

/// 开始计时弹窗（在 PosView 内开台，避免再切到房间管理页）
const startTimingOpen = ref(false);
const startTimingRoom = ref<Room | null>(null);
/// 开台表单：按"人"（手机号）关联会员，不指定具体卡。
/// memberId 仍然按现行 schema 传一张卡的 Id（取选中人下首张可用卡）以满足后端 FK；
/// 但前端只让收银选择"人"。
const startTimingForm = reactive<{
  memberId: number | null;        // 实际传给后端的 cardId（首张可用卡）
  memberPhone: string;            // select 的 v-model（按手机号唯一选人）
  memberLabel: string;
  customerName: string;
  remark: string;
}>({ memberId: null, memberPhone: '', memberLabel: '', customerName: '', remark: '' });
const startingTimer = ref(false);
/// 模糊搜索：debounce 由 el-select 的 remote 自带触发；按手机号聚合，结果取前 10 个人
const memberSearchResults = ref<MemberPhoneGroup[]>([]);
const memberSearchLoading = ref(false);

function openStartTiming(room: Room) {
  startTimingRoom.value = room;
  startTimingForm.memberId = null;
  startTimingForm.memberPhone = '';
  startTimingForm.memberLabel = '';
  startTimingForm.customerName = '';
  startTimingForm.remark = '';
  memberSearchResults.value = [];
  startTimingOpen.value = true;
}

/// el-select remote-method：输入即查询；按手机号聚合，相同会员只出一行
async function searchMembersForTiming(keyword: string) {
  const k = keyword.trim();
  if (!k) { memberSearchResults.value = []; return; }
  memberSearchLoading.value = true;
  try {
    const r = await membersApi.grouped({
      keyword: k, pageSize: 10, includeClosed: false,
      storeId: appStore.activeStoreId ?? undefined
    });
    memberSearchResults.value = r.items;
  } catch {
    memberSearchResults.value = [];
  } finally {
    memberSearchLoading.value = false;
  }
}

function memberOptionLabel(g: MemberPhoneGroup): string {
  const name = g.primaryName ?? '未填';
  return `${g.phone} · ${name} · ${g.cardCount} 张卡`;
}

/// 选中"人"后，挑该人名下任意一张可用卡满足后端 memberId（cardId）参数；
/// 没有可用卡则降级为散客（不传 memberId）
function onMemberSelected(phone: string | null) {
  if (!phone) {
    startTimingForm.memberId = null;
    startTimingForm.memberPhone = '';
    startTimingForm.memberLabel = '';
    return;
  }
  const group = memberSearchResults.value.find((g) => g.phone === phone);
  if (!group) return;
  const activeCard = group.cards.find((c) => c.isActive) ?? group.cards[0];
  startTimingForm.memberId = activeCard?.id ?? null;
  startTimingForm.memberPhone = phone;
  startTimingForm.memberLabel = memberOptionLabel(group);
}

async function doStartTiming() {
  if (!startTimingRoom.value) return;
  startingTimer.value = true;
  try {
    await timedRoomsApi.start(startTimingRoom.value.id, {
      memberId: startTimingForm.memberId,
      // 选了人就不再传散客姓名（后端按会员名展示）；纯散客分支才用 customerName
      customerName: startTimingForm.memberPhone
        ? null
        : (startTimingForm.customerName.trim() || null),
      remark: startTimingForm.remark.trim() || null
    });
    startTimingOpen.value = false;
    await reloadTimedSessions();
    ElMessage.success(`${startTimingRoom.value.roomNo} 已开台计时`);
  } catch {
    /* http 已弹错 */
  } finally {
    startingTimer.value = false;
  }
}

/// 取消一段进行中的计时（误开台 / 客人临时不消费）：session 标为 Cancelled，不计费。
/// 若该 session 已加入购物车，先从车里移除避免提交时引用废 session。
async function cancelTiming(room: Room, session: TimedRoomSessionDto) {
  const ok = await ElMessageBox.confirm(
    `确认取消 ${room.roomNo} 的计时？已计 ${session.elapsedMinutes} 分钟将被作废、不计费。`,
    '取消计时',
    { type: 'warning', confirmButtonText: '确认取消', cancelButtonText: '不取消' }
  ).then(() => true).catch(() => false);
  if (!ok) return;
  try {
    await timedRoomsApi.cancel(session.id);
    // 同步把购物车里这条 roomCharge 拿掉
    const idx = cart.findIndex((c) => c.kind === 'roomCharge' && (c as RoomChargeCartItem).sessionId === session.id);
    if (idx !== -1) cart.splice(idx, 1);
    await reloadTimedSessions();
    ElMessage.success(`${room.roomNo} 计时已取消`);
  } catch { /* http 已弹错 */ }
}

/// 把进行中的计时 session 加入 cart 作为一行 roomCharge。
/// 同步将开台时绑定的会员快照进 cart item，结算前会校验"绑定会员=结算会员"。
function addRoomChargeToCart(room: Room, session: TimedRoomSessionDto) {
  if (cartRoomSessionIds.value.has(session.id)) {
    ElMessage.info('该房间已在订单里');
    return;
  }
  const minutes = session.elapsedMinutes;
  const amount = Math.round((minutes / 60) * session.hourlyRateSnapshot * 100) / 100;
  const item: RoomChargeCartItem = {
    kind: 'roomCharge',
    sessionId: session.id,
    roomId: room.id,
    roomNo: room.roomNo,
    serviceName: `${room.roomNo} 计时房 ${minutes} 分钟`,
    unitPrice: amount,
    elapsedMinutes: minutes,
    hourlyRate: session.hourlyRateSnapshot,
    boundMemberId: session.memberId,
    boundMemberName: session.memberName
  };
  cart.push(item);
  if (isRoomChargeMismatch(item)) {
    ElMessage.warning(
      `${room.roomNo} 开台绑定的会员（${item.boundMemberName ?? '—'}）与当前结算会员不一致，结算前请关联同一会员`
    );
  }
}

/// 计时房费"绑定会员 vs 结算会员"匹配规则：
/// - 散客开台（boundMemberId == null）→ 无约束，任意模式都可结算
/// - 绑定会员开台 → 当前必须在会员模式且已查询同一手机号下的卡组（cardId 落在 memberCards 内）
///   说明：开台 form 选"人"后传的是首张可用卡的 id，所以同一手机号下的任何一张卡都视为同一人。
function isRoomChargeMismatch(item: RoomChargeCartItem): boolean {
  if (item.boundMemberId == null) return false;
  if (!primaryMember.value) return true;
  return !memberCards.value.some((c) => c.id === item.boundMemberId);
}

const mismatchedRoomCharges = computed(() =>
  cart.filter((c): c is RoomChargeCartItem => c.kind === 'roomCharge' && isRoomChargeMismatch(c))
);

const filteredServices = computed(() => {
  const k = serviceFilter.value.trim().toLowerCase();
  if (!k) return services.value;
  return services.value.filter((s) =>
    s.code.toLowerCase().includes(k) || s.name.toLowerCase().includes(k)
  );
});

const total = computed(() =>
  cart.reduce((sum, c) => sum + c.unitPrice * (c.kind === 'service' ? c.quantity : 1), 0)
);

// 折扣已在充值/开卡时兑现（按"现金价 × 折扣"收的现金），结账时按服务的会员价/标准价直接结算
const payable = computed(() => total.value);

const canCheckout = computed(() => {
  if (cart.length === 0) return false;
  // 仅要求 service 行选好技师；roomCharge 行不参与该校验
  if (!cart.every((c) => c.kind !== 'service' || c.technicianId != null)) return false;
  if (!appStore.activeStoreId) return false;
  if (mode.value === 'member' && !primaryMember.value) return false;
  // 计时房绑定会员必须与当前结算会员一致，否则不允许结算（避免归账到错的会员）
  if (mismatchedRoomCharges.value.length > 0) return false;
  return true;
});

/// 金额朗读：盲人收银员/店长依赖读屏；"32.5" 念成"三十二点五"无意义，转成"32 元 5 角"
function yuanReadable(amount: number): string {
  const safe = Number.isFinite(amount) ? amount : 0;
  const yuan = Math.floor(safe);
  const jiao = Math.round((safe - yuan) * 10);
  return jiao === 0 ? `${yuan} 元` : `${yuan} 元 ${jiao} 角`;
}

/// 给一行 cart-item 拼一句总述，让读屏一开口就知道"第几项、什么服务、哪位技师、轮/点、几次、多少钱"
function cartRowAriaLabel(it: CartItem, idx: number): string {
  const seq = `第 ${idx + 1} 项`;
  if (it.kind === 'roomCharge') {
    return `${seq}，计时房 ${it.roomNo}，已计时 ${it.elapsedMinutes} 分钟，房费 ${yuanReadable(it.unitPrice)}`;
  }
  const tech = technicians.value.find((t) => t.id === it.technicianId);
  const techDesc = tech ? `技师 ${tech.employeeNo ?? '-'} 号 ${tech.realName ?? tech.username}` : '未指派技师';
  const sourceDesc = it.assignmentSource === 'Rotation' ? '轮钟' : '点钟';
  const room = rooms.value.find((r) => r.id === it.roomId);
  const roomDesc = room ? `房间 ${room.roomNo}` : '未指定房间';
  const amount = yuanReadable(it.unitPrice * it.quantity);
  return `${seq}，${it.serviceName}，${techDesc}，上钟方式 ${sourceDesc}，${roomDesc}，数量 ${it.quantity}，小计 ${amount}`;
}

function payMethodLabel(m: string) {
  return ({
    Cash: '现金', MemberCard: '会员卡', Wechat: '微信', Alipay: '支付宝', BankCard: '银行卡', Unpaid: '未支付'
  } as Record<string, string>)[m] ?? m;
}

/// 小票合计金额：优先用后端的 listTotal（含次卡面值），缺失时退回 total（现金合计）
function receiptListTotal(o: Order): number {
  if (o.listTotal != null && o.listTotal > 0) return o.listTotal;
  return o.total;
}

/// 小票明细金额：优先 listAmount，缺失时回退到 itemTotal（旧订单兼容）
function rowListAmount(row: OrderItem): number {
  if (row.listAmount != null && row.listAmount > 0) return row.listAmount;
  if (row.listUnitPrice != null && row.listUnitPrice > 0)
    return Math.round(row.listUnitPrice * row.quantity * 100) / 100;
  return row.itemTotal;
}

/// 订单 DTO 的 listTotal 已包含房费面值；paidAmount 已包含房费实收，所以直接复用即可
function receiptHeadlineTotal() {
  return lastOrder.value ? receiptListTotal(lastOrder.value) : 0;
}
function receiptPaidTotal() {
  return lastOrder.value?.paidAmount ?? 0;
}
function receiptPayMethod() {
  return lastOrder.value?.payMethod ?? 'Cash';
}

async function loadCatalog() {
  const sid = appStore.activeStoreId;
  const [s, t, r, sess] = await Promise.all([
    servicesApi.list(false),
    staffApi.list({ role: 'Technician', pageSize: 200, storeId: sid ?? undefined }),
    sid ? roomsApi.list(sid).catch((): Room[] => []) : Promise.resolve([] as Room[]),
    sid ? timedRoomsApi.sessions(sid).catch((): TimedRoomSessionDto[] => []) : Promise.resolve([] as TimedRoomSessionDto[])
  ]);
  services.value = s;
  technicians.value = t.items;
  rooms.value = r;
  timedSessions.value = sess;
}

/// 刷新计时 session（开台后或加入订单前调用，避免拿过期分钟数）
async function reloadTimedSessions() {
  const sid = appStore.activeStoreId;
  if (!sid) return;
  try { timedSessions.value = await timedRoomsApi.sessions(sid); } catch { /* ignore */ }
}

function onPickService(s: ServiceItem) {
  pickedService.value = s;
  pickOpen.value = true;
}

function onTechnicianPicked(payload: { technicianId: number; quantity: number; source: 'Rotation' | 'Designation' }) {
  if (!pickedService.value) return;
  const s = pickedService.value;
  const unit = mode.value === 'member' ? s.memberPrice : s.price;
  cart.push({
    kind: 'service',
    serviceId: s.id,
    serviceName: s.name,
    technicianId: payload.technicianId,
    roomId: null,
    unitPrice: unit,
    quantity: payload.quantity,
    durationMinutes: s.durationMinutes,
    // 由弹窗带入，店员可在"当前订单"行的下拉里随时改
    assignmentSource: payload.source
  });
  pickOpen.value = false;
}

/// 切换收银模式：清掉相关状态，重新按新模式计算单价（仅作用于 service 行；
/// roomCharge 是按时长 × 单价的快照，不随会员价/标准价切换）
function onModeChange() {
  if (mode.value === 'walkin') {
    clearMember();
  }
  for (const c of cart) {
    if (c.kind !== 'service') continue;
    const svc = services.value.find((s) => s.id === c.serviceId);
    if (svc) c.unitPrice = mode.value === 'member' ? svc.memberPrice : svc.price;
  }
}

async function lookupMember() {
  const k = memberKeyword.value.trim();
  if (!k) return;
  // 先用关键字定位一张卡（可能是卡号或手机号），再按 phone 把同手机号下所有卡拉出来
  // includeClosed=true 让被退卡/转赠的历史卡也显示出来，UI 区分但不可勾选
  const first = await membersApi.list({ keyword: k, pageSize: 5, includeClosed: true });
  if (first.items.length === 0) {
    // 新关键字搜不到时，旧的命中也要顺手清掉，避免"提示未找到却仍挂着上一个会员"的歧义
    memberCards.value = [];
    selectedCardIds.value = [];
    ElMessage.warning('未找到会员');
    return;
  }
  const phone = first.items[0].phone;
  const all = await membersApi.list({ keyword: phone, pageSize: 100, includeClosed: true });
  memberCards.value = all.items.filter((m) => m.phone === phone);
  // 默认选中命中的那张（若已关闭则选第一张可用卡）；
  // 次卡若购物车中没有对应绑定服务则视为不可结算，不参与默认勾选，避免落到 400 PunchCardMismatch
  const hitId = first.items[0].id;
  const hit = memberCards.value.find((c) => c.id === hitId);
  const isPickable = (c: Member) => c.isActive && isCardEligible(c);
  const defaultPick = hit && isPickable(hit) ? hit : memberCards.value.find(isPickable);
  selectedCardIds.value = defaultPick ? [defaultPick.id] : [];

  for (const c of cart) {
    if (c.kind !== 'service') continue;
    const svc = services.value.find((s) => s.id === c.serviceId);
    if (svc) c.unitPrice = svc.memberPrice;
  }
  const activeCnt = memberCards.value.filter((c) => c.isActive).length;
  const closedCnt = memberCards.value.length - activeCnt;
  ElMessage.success(
    activeCnt > 1
      ? `已找到 ${activeCnt} 张可用卡${closedCnt > 0 ? `（另 ${closedCnt} 张已关闭）` : ''}，可勾选合并结算`
      : `已关联会员 ${primaryMember.value?.name || primaryMember.value?.cardNo || phone}`
  );
}

function openCheckout() {
  if (cart.length === 0) { ElMessage.warning('请先添加项目或计时房费'); return; }
  if (!cart.every((c) => c.kind !== 'service' || c.technicianId != null)) {
    ElMessage.warning('请为每个服务项目都指派技师');
    return;
  }
  if (mode.value === 'member' && !primaryMember.value) {
    ElMessage.warning('会员收银必须先关联会员');
    return;
  }
  if (mismatchedRoomCharges.value.length > 0) {
    const desc = mismatchedRoomCharges.value
      .map((r) => `${r.roomNo}（开台 ${r.boundMemberName ?? '—'}）`)
      .join('、');
    ElMessage.error(`计时房费 ${desc} 与当前结算会员不一致，请关联同一会员或将该房费从订单中移除`);
    return;
  }
  checkoutOpen.value = true;
}

async function doCheckout(payload: { payMethod: string; paidAmount: number | null; remark: string | null }) {
  if (!appStore.activeStoreId) {
    ElMessage.error('未选择门店');
    return;
  }
  const services = cart.filter((c): c is ServiceCartItem => c.kind === 'service');
  const roomCharges = cart.filter((c): c is RoomChargeCartItem => c.kind === 'roomCharge');

  if (services.length === 0 && roomCharges.length === 0) {
    ElMessage.warning('购物车为空');
    return;
  }

  checkingOut.value = true;
  try {
    // 服务 + 计时房费统一走订单流程：CreateOrder 时把 roomSessionIds 一起带上，
    // Checkout 会同步收尾所有挂在该订单上的 session，金额并入订单总额。
    const created = await ordersApi.create({
      storeId: appStore.activeStoreId,
      memberId: primaryMember.value?.id ?? null,
      items: services.map((c) => ({
        serviceId: c.serviceId,
        technicianId: c.technicianId!,
        quantity: c.quantity,
        roomId: c.roomId,
        assignmentSource: c.assignmentSource
      })),
      remark: null,
      roomSessionIds: roomCharges.map((r) => r.sessionId)
    });
    // 合并结算：主卡之外勾选的卡走 secondaryMemberIds（仅 MemberCard 支付时后端会用）
    const secondary = selectedCardIds.value.slice(1);
    const checkedOrder = await ordersApi.checkout(created.id, {
      payMethod: payload.payMethod,
      paidAmount: payload.paidAmount,
      discountAmount: 0,
      remark: payload.remark,
      secondaryMemberIds: secondary.length > 0 ? secondary : undefined
    });

    lastOrder.value = checkedOrder;
    checkoutOpen.value = false;
    receiptOpen.value = true;
    ElMessage.success('结账成功');
  } catch {
    /* http 已弹错；订单已落库或 session 已被领走时不重试 */
  } finally {
    checkingOut.value = false;
  }
}

async function resetAll() {
  if (cart.length > 0) {
    await ElMessageBox.confirm('确认清空当前订单？', '提示', { type: 'warning' }).catch(() => null);
  }
  cart.splice(0, cart.length);
  clearMember();
  mode.value = 'member';
  lastOrder.value = null;
  // 结账后刷新计时 session（已结算的 session 会消失）
  reloadTimedSessions();
}

onMounted(async () => {
  await appStore.loadStores();
  await loadCatalog();
});

useShortcuts({
  onMemberSearch: () => memberInput.value?.focus(),
  onRefresh: () => loadCatalog(),
  onPrimary: () => { if (canCheckout.value) openCheckout(); }
});
</script>

<style scoped>
.pos {
  display: grid;
  /* grid-template-columns is set inline so the splitter can drag */
  grid-template-columns: 1fr 6px 760px;
  gap: 8px;
  /* 外层不滚动，整页限制在视口内；左右栏各自的网格/表格按需出自己的滚动条 */
  height: calc(100vh - 100px);
  overflow: hidden;
}
.left, .right { min-height: 0; min-width: 0; }
.left { display: flex; flex-direction: column; gap: 8px; min-height: 0; }
/* "选择服务项目"分到剩余高度；其内部 .services-grid 自己滚动 */
.services-card { flex: 1; min-height: 0; display: flex; flex-direction: column; }
.services-card :deep(.el-card__body) { flex: 1; overflow: hidden; display: flex; flex-direction: column; padding: 8px 10px; }
/* 卡片标题区压缩：减小 padding、字号、控件高度，把更多空间让给项目网格 */
.services-card :deep(.el-card__header),
.timed-card :deep(.el-card__header),
.cart :deep(.el-card__header) { padding: 6px 10px; }
.card-title { margin: 0; font-size: 14px; font-weight: 600; }
.timed-card { flex: 0 0 auto; max-height: 42%; display: flex; flex-direction: column; }
.timed-card :deep(.el-card__body) { padding: 8px 10px; flex: 1; overflow: hidden; display: flex; flex-direction: column; }
.right { display: flex; flex-direction: column; min-height: 0; }
.cart { flex: 1; min-height: 0; display: flex; flex-direction: column; }
.cart :deep(.el-card__body) { display: flex; flex-direction: column; flex: 1; overflow: hidden; padding: 8px 10px; }
.header-row { display: flex; justify-content: space-between; align-items: center; gap: 8px; flex-wrap: wrap; min-height: 24px; }
.header-row :deep(.el-input__wrapper) { padding: 0 8px; }
.header-row :deep(.el-input) { --el-input-height: 26px; }

.splitter {
  background: var(--el-border-color-light);
  cursor: col-resize;
  border-radius: 3px;
  transition: background-color 0.15s;
}
.splitter:hover, .splitter:focus-visible {
  background: var(--el-color-primary);
  outline: none;
}

/* 严格每排 3 个等宽（1fr），不足 3 个时左对齐不拉伸；
   行数超出由自身 overflow-y: auto 出滚动条（外层不滚） */
.services-grid {
  display: grid;
  grid-template-columns: repeat(3, minmax(0, 1fr));
  gap: 6px;
  overflow-y: auto;
  padding-right: 4px;
  flex: 1;
  min-height: 0;
}
.timed-grid {
  display: grid;
  grid-template-columns: repeat(3, minmax(0, 1fr));
  gap: 6px;
  overflow-y: auto;
  flex: 1;
  min-height: 0;
}
/* 计时房卡身用 flex 列布局，按钮始终贴卡底；空闲卡的"开台"铺满整宽。
   固定高度 116px：行数超出时由 .timed-grid 出滚动条 */
.timed-card-item {
  height: 116px;
  display: flex;
  flex-direction: column;
}
.timed-card-item :deep(.el-card__body) {
  padding: 6px 8px;
  flex: 1;
  display: flex;
  flex-direction: column;
  overflow: hidden;
}
.timed-card-item.open { border-color: var(--el-color-warning); }
.timed-card-item.inCart { border-color: var(--el-color-success); background: #f6ffed; }
.timed-head { display: flex; justify-content: space-between; align-items: center; margin-bottom: 3px; }
.timed-no { font-weight: 600; font-size: 13px; }
.timed-info { font-size: 12px; margin-bottom: 4px; }
.timed-info .muted { font-size: 11px; }
.timed-amount { color: #d9534f; font-weight: 600; margin-top: 2px; }
/* 计时中状态的按钮组贴底 */
.timed-actions { display: flex; gap: 4px; flex-wrap: wrap; margin-top: auto; }
/* 空闲状态的"开台"按钮贴底 + 铺满整宽 */
.timed-card-item :deep(.el-card__body) > .el-button {
  margin-top: auto;
  width: 100%;
}
.room-charge-meta { gap: 12px; }
/* 服务卡：每排 3 个等宽（grid 1fr）+ 固定高度，所有卡视觉一致；
   高度不够时由 .services-grid 自身出滚动条 */
.service-card {
  cursor: pointer;
  height: 126px;
  display: flex;
  flex-direction: column;
}
.service-card :deep(.el-card__body) {
  padding: 6px 8px;
  flex: 1;
  display: flex;
  flex-direction: column;
  justify-content: flex-start;
  overflow: hidden;
}
/* 名称行：编号在前，名称在后，同行展示；超长名称按单元格宽度截断 */
.svc-name {
  display: flex;
  align-items: baseline;
  gap: 6px;
  font-size: 15px;
  line-height: 1.3;
  min-width: 0;
}
.svc-name-text {
  font-weight: 600;
  white-space: nowrap;
  overflow: hidden;
  text-overflow: ellipsis;
  min-width: 0;
}
.svc-code {
  flex: 0 0 auto;
  color: var(--el-text-color-secondary);
  font-size: 13px;
  font-family: ui-monospace, SFMono-Regular, Menlo, Consolas, monospace;
}
.svc-meta { display: flex; flex-wrap: wrap; gap: 4px; margin-top: 4px; }
.svc-meta :deep(.el-tag) { font-size: 12px; padding: 0 6px; height: 20px; line-height: 20px; }
/* 备注：2 行截断显示，hover 看完整 title；无备注的卡同样高度保持整排对齐 */
.svc-desc {
  color: var(--el-text-color-regular);
  font-size: 12px;
  line-height: 1.4;
  margin-top: 4px;
  display: -webkit-box;
  -webkit-line-clamp: 2;
  -webkit-box-orient: vertical;
  overflow: hidden;
  word-break: break-all;
}

.mode-switch { display: flex; margin-bottom: 10px; width: 100%; }
.mode-switch :deep(.el-radio-button) { flex: 1; }
.mode-switch :deep(.el-radio-button__inner) { width: 100%; }
.member-row { display: flex; gap: 8px; }
.member-info { background: #f5f7fa; padding: 8px; border-radius: 4px; margin-top: 8px; }
.m-line { display: flex; gap: 8px; align-items: center; flex-wrap: wrap; }
.m-line.muted { color: var(--el-text-color-secondary); font-size: 12px; margin-top: 4px; }
.muted { color: var(--el-text-color-secondary); font-size: 12px; }
.card-pick-list { margin: 6px 0; max-height: 160px; overflow-y: auto; }
.card-pick-row { padding: 2px 0; }
.card-pick-row.closed { opacity: 0.55; }
.card-pick-row :deep(.el-checkbox__label) { display: inline-flex; align-items: center; gap: 2px; }
.card-no { font-weight: 600; }
.card-bal { color: #d9534f; font-weight: 600; }

.empty {
  text-align: center;
  color: var(--el-text-color-secondary);
  padding: 32px 0;
}

/* CSS Grid "fake table"：项目列 minmax(0, 1fr) 保证溢出时 ellipsis，
   永远不会撑破右栏宽度。固定列合计 540px，项目列自适应剩余。 */
.cart-grid {
  /* flex: 0 1 auto + min-height:0 + overflow-y:auto：
     内容少时按内容自身高度，不出滚动条；内容多到撑破才在自身出滚动条。
     不会强行占满中间空间，所以"列表缩小后多余的滚动条"问题消失。 */
  flex: 0 1 auto;
  min-height: 0;
  overflow-y: auto;
  margin-top: 4px;
  border: 1px solid var(--el-border-color-light);
  border-radius: 4px;
  background: #fff;
}
.cg-row {
  display: grid;
  /* 全 fr 比例分配：760px 容器下分别约 140/160/100/100/100/100/60，
     拖宽自动按比例展开，拖窄按比例收缩但 minmax(0, …) 保证永不溢出。 */
  grid-template-columns:
    minmax(0, 1.4fr)
    minmax(0, 1.6fr)
    minmax(0, 1fr)
    minmax(0, 1fr)
    minmax(0, 1fr)
    minmax(0, 1fr)
    minmax(0, 0.6fr);
  align-items: center;
  border-bottom: 1px solid var(--el-border-color-light);
}
.cg-row:last-child { border-bottom: none; }
.cg-row > * {
  padding: 6px 8px;
  min-width: 0;
  white-space: nowrap;
  overflow: hidden;
  text-overflow: ellipsis;
  /* 列分隔线：右侧实线把每一列切开，最后一列不画 */
  border-right: 1px solid var(--el-border-color-light);
}
.cg-row > *:last-child { border-right: none; }
.cg-header {
  background: #f5f7fa;
  font-weight: 600;
  font-size: 13px;
  color: var(--el-text-color-regular);
  position: sticky;
  top: 0;
  z-index: 1;
}
.cg-header > * {
  border-bottom: 1px solid var(--el-border-color);
  /* 列头一律居中，盖过单元格自身的 .t-right / .t-center 等对齐 */
  text-align: center;
  justify-content: center;
  display: flex;
  align-items: center;
}
.cg-name { font-weight: 500; }
.t-right { text-align: right; justify-content: flex-end; display: flex; }
.t-center { text-align: center; justify-content: center; display: flex; }
.ci-price { font-weight: 700; font-size: 15px; color: #d9534f; }
/* 让单元里的下拉/数字框继承单元格宽度（已 width:100% 设置 inline） */
.cg-row :deep(.el-input-number .el-input__inner) { padding-right: 28px; }

/* 结算页脚：永远贴在右栏底部。
   margin-top: auto 让它把上面的空白吃掉，自身靠 flex-shrink:0 不可压缩。
   购物车短时 footer 仍在底部、上面留白；长时只有 cart-grid 自己出滚动条。 */
.cart-footer {
  flex: 0 0 auto;
  margin-top: auto;
  padding-top: 12px;
  border-top: 2px solid var(--el-border-color-light);
  background: #fff;
}
.checkout-btn { width: 100%; margin-top: 12px; }
.total-line { display: flex; justify-content: space-between; padding: 4px 0; }
.total-line.muted { color: var(--el-text-color-secondary); font-size: 12px; }
.total-amount { font-size: 18px; font-weight: 700; color: #d9534f; }
.hint { font-size: 12px; color: var(--el-text-color-secondary); margin-top: 6px; min-height: 18px; }
.hint-danger { color: #d9534f; font-weight: 600; }
</style>
