<template>
  <div class="pos" role="region" aria-label="收银台" :style="{ gridTemplateColumns: `1fr 6px ${rightWidth}px` }">
    <div class="left">
      <el-card shadow="never">
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
            :aria-label="`服务 ${s.name}，时长 ${s.durationMinutes} 分钟，${mode === 'member' ? '会员价 ' + s.memberPrice.toFixed(2) : '标准价 ' + s.price.toFixed(2)} 元，按回车添加`"
            @click="onPickService(s)"
            @keyup.enter="onPickService(s)"
            @keyup.space.prevent="onPickService(s)"
          >
            <div class="svc-name">{{ s.name }}</div>
            <div class="svc-meta">
              <el-tag size="small">{{ s.durationMinutes }} 分钟</el-tag>
              <el-tag v-if="mode === 'walkin'" size="small" type="success">¥{{ s.price.toFixed(2) }}</el-tag>
              <el-tag v-else size="small" type="warning">会员价 ¥{{ s.memberPrice.toFixed(2) }}</el-tag>
            </div>
            <div class="svc-code">{{ s.code }}</div>
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

        <div v-if="cart.length === 0" class="empty">未添加任何项目</div>
        <div v-else class="cart-list">
          <div v-for="(it, idx) in cart" :key="idx" class="cart-item">
            <div class="ci-line">
              <span class="ci-name">{{ it.serviceName }}</span>
              <el-button link type="danger" size="small" @click="cart.splice(idx, 1)">移除</el-button>
            </div>
            <div class="ci-meta">
              <el-select
                v-model="it.technicianId"
                placeholder="选择技师"
                size="default"
                class="ci-tech"
                filterable
              >
                <el-option
                  v-for="t in technicians"
                  :key="t.id"
                  :label="`${t.employeeNo ?? '-'} · ${t.realName ?? t.username}`"
                  :value="t.id"
                />
              </el-select>
              <el-select
                v-model="it.roomId"
                placeholder="房间"
                size="default"
                class="ci-room"
                clearable
              >
                <el-option
                  v-for="r in availableRooms(it.roomId)"
                  :key="r.id"
                  :label="r.roomNo + (r.isOccupied ? '（占用中）' : '')"
                  :value="r.id"
                  :disabled="r.isOccupied && r.id !== it.roomId"
                />
              </el-select>
              <el-input-number v-model="it.quantity" :min="1" :max="20" size="default" controls-position="right" class="ci-qty" />
              <span class="ci-price">¥{{ (it.unitPrice * it.quantity).toFixed(2) }}</span>
            </div>
          </div>
        </div>

        <div class="totals">
          <div class="total-line">
            <span>合计</span>
            <span class="total-amount">¥ {{ total.toFixed(2) }}</span>
          </div>
          <div class="total-line">
            <span>应收</span>
            <span class="total-amount">¥ {{ payable.toFixed(2) }}</span>
          </div>
        </div>

        <el-button
          type="primary"
          size="large"
          :disabled="!canCheckout"
          :loading="checkingOut"
          style="width: 100%; margin-top: 12px"
          :aria-label="`下单并结账，应收 ${payable.toFixed(2)} 元`"
          @click="openCheckout"
        >
          下单并结账（应收 ¥{{ payable.toFixed(2) }}）
        </el-button>
        <div class="hint" aria-live="polite">
          <span v-if="cart.length > 0 && !canCheckout">提示：请为每个项目都指派技师后才能结账</span>
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

    <el-dialog v-model="receiptOpen" title="结账成功" width="460px">
      <div v-if="lastOrder">
        <p><strong>订单号：</strong>{{ lastOrder.orderNo }}</p>
        <p>
          <strong>合计：</strong>¥ {{ receiptListTotal(lastOrder).toFixed(2) }}
          <span v-if="(lastOrder.punchCardUsedCount ?? 0) > 0" class="muted" style="margin-left:6px">
            （面值，含次卡抵扣）
          </span>
        </p>
        <p>
          <strong>实收：</strong>¥ {{ lastOrder.paidAmount.toFixed(2) }}（{{ payMethodLabel(lastOrder.payMethod) }}）
        </p>
        <p v-if="(lastOrder.punchCardUsedCount ?? 0) > 0">
          <strong>消费次数：</strong>{{ lastOrder.punchCardUsedCount }} 次（次卡核销）
        </p>
        <p v-if="lastOrder.discountAmount > 0">优惠：¥ {{ lastOrder.discountAmount.toFixed(2) }}</p>
        <el-table :data="lastOrder.items" size="small">
          <el-table-column prop="serviceName" label="项目" />
          <el-table-column prop="technicianName" label="技师" width="90" />
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
      </div>
      <template #footer>
        <el-button type="primary" @click="receiptOpen = false; resetAll()">完成</el-button>
      </template>
    </el-dialog>
  </div>
</template>

<script setup lang="ts">
import { computed, onMounted, reactive, ref, watch } from 'vue';
import { ElMessage, ElMessageBox } from 'element-plus';
import { Search, User } from '@element-plus/icons-vue';
import { membersApi, ordersApi, roomsApi, servicesApi, staffApi } from '@/api/modules';
import { useAppStore } from '@/stores/app';
import { useShortcuts } from '@/composables/useShortcuts';
import type { Member, Order, OrderItem, Room, ServiceItem, Staff } from '@/api/types';
import PickTechnicianDialog from '@/views/components/PickTechnicianDialog.vue';
import CheckoutDialog from '@/views/components/CheckoutDialog.vue';

interface CartItem {
  serviceId: number;
  serviceName: string;
  technicianId: number | null;
  roomId: number | null;
  unitPrice: number;
  quantity: number;
  durationMinutes: number;
}

const appStore = useAppStore();

const services = ref<ServiceItem[]>([]);
const technicians = ref<Staff[]>([]);
const rooms = ref<Room[]>([]);
const serviceFilter = ref('');
const serviceFilterInput = ref<{ focus: () => void } | null>(null);
const memberInput = ref<{ focus: () => void } | null>(null);

function quickAddFirst() {
  const first = filteredServices.value[0];
  if (first) onPickService(first);
}

function availableRooms(currentRoomId: number | null): Room[] {
  // 当前 cart 中已选过的房间在其它项目里禁用，自己当前选的留可见
  const usedElsewhere = new Set(
    cart.filter((c) => c.roomId !== null && c.roomId !== currentRoomId).map((c) => c.roomId!)
  );
  return rooms.value.filter((r) => !usedElsewhere.has(r.id));
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
function isCardEligible(card: Member): boolean {
  if (card.memberTypeKind !== 'CountBased') return true;
  if (card.serviceItemId == null) return false;
  return cart.some((c) => c.serviceId === card.serviceItemId);
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

/// 购物车变化（增删/换项）后，若某张已勾选的次卡绑定的服务不再出现，则自动取消勾选并提示。
/// 避免出现"勾选了但实际无法结算"的歧义状态。
watch(
  () => cart.map((c) => c.serviceId).join(','),
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
const RIGHT_MIN = 360;
const RIGHT_MAX = 900;
function clampRight(v: number) {
  return Math.max(RIGHT_MIN, Math.min(RIGHT_MAX, Math.round(v)));
}
const rightWidth = ref(clampRight(Number(localStorage.getItem(RIGHT_WIDTH_KEY)) || 520));

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

const filteredServices = computed(() => {
  const k = serviceFilter.value.trim().toLowerCase();
  if (!k) return services.value;
  return services.value.filter((s) =>
    s.code.toLowerCase().includes(k) || s.name.toLowerCase().includes(k)
  );
});

const total = computed(() =>
  cart.reduce((sum, c) => sum + c.unitPrice * c.quantity, 0)
);

// 折扣已在充值/开卡时兑现（按"现金价 × 折扣"收的现金），结账时按服务的会员价/标准价直接结算
const payable = computed(() => total.value);

const canCheckout = computed(() => {
  if (cart.length === 0) return false;
  if (!cart.every((c) => c.technicianId != null)) return false;
  if (!appStore.activeStoreId) return false;
  if (mode.value === 'member' && !primaryMember.value) return false;
  return true;
});

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

async function loadCatalog() {
  const sid = appStore.activeStoreId;
  const [s, t, r] = await Promise.all([
    servicesApi.list(false),
    staffApi.list({ role: 'Technician', pageSize: 200, storeId: sid ?? undefined }),
    sid ? roomsApi.list(sid).catch((): Room[] => []) : Promise.resolve([] as Room[])
  ]);
  services.value = s;
  technicians.value = t.items;
  rooms.value = r;
}

function onPickService(s: ServiceItem) {
  pickedService.value = s;
  pickOpen.value = true;
}

function onTechnicianPicked(payload: { technicianId: number; quantity: number }) {
  if (!pickedService.value) return;
  const s = pickedService.value;
  const unit = mode.value === 'member' ? s.memberPrice : s.price;
  cart.push({
    serviceId: s.id,
    serviceName: s.name,
    technicianId: payload.technicianId,
    roomId: null,
    unitPrice: unit,
    quantity: payload.quantity,
    durationMinutes: s.durationMinutes
  });
  pickOpen.value = false;
}

/// 切换收银模式：清掉相关状态，重新按新模式计算单价
function onModeChange() {
  if (mode.value === 'walkin') {
    clearMember();
  }
  for (const c of cart) {
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
    ElMessage.warning('未找到会员');
    return;
  }
  const phone = first.items[0].phone;
  const all = await membersApi.list({ keyword: phone, pageSize: 100, includeClosed: true });
  memberCards.value = all.items.filter((m) => m.phone === phone);
  // 默认选中命中的那张（若已关闭则选第一张可用卡）
  const hitId = first.items[0].id;
  const hit = memberCards.value.find((c) => c.id === hitId);
  const firstActive = memberCards.value.find((c) => c.isActive);
  const defaultPick = hit?.isActive ? hit : firstActive;
  selectedCardIds.value = defaultPick ? [defaultPick.id] : [];

  for (const c of cart) {
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
  if (cart.length === 0 || !cart.every((c) => c.technicianId != null)) {
    ElMessage.warning('请确保所有项目都指派了技师');
    return;
  }
  if (mode.value === 'member' && !primaryMember.value) {
    ElMessage.warning('会员收银必须先关联会员');
    return;
  }
  checkoutOpen.value = true;
}

async function doCheckout(payload: { payMethod: string; paidAmount: number | null; remark: string | null }) {
  if (!appStore.activeStoreId) {
    ElMessage.error('未选择门店');
    return;
  }
  checkingOut.value = true;
  try {
    const created = await ordersApi.create({
      storeId: appStore.activeStoreId,
      memberId: primaryMember.value?.id ?? null,
      items: cart.map((c) => ({
        serviceId: c.serviceId,
        technicianId: c.technicianId!,
        quantity: c.quantity,
        roomId: c.roomId
      })),
      remark: null
    });
    // 合并结算：主卡之外勾选的卡走 secondaryMemberIds（仅 MemberCard 支付时后端会用）
    const secondary = selectedCardIds.value.slice(1);
    const checked = await ordersApi.checkout(created.id, {
      payMethod: payload.payMethod,
      paidAmount: payload.paidAmount,
      discountAmount: 0,
      remark: payload.remark,
      secondaryMemberIds: secondary.length > 0 ? secondary : undefined
    });
    lastOrder.value = checked;
    checkoutOpen.value = false;
    receiptOpen.value = true;
    ElMessage.success('结账成功');
  } catch {
    /* http 已弹错 */
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
  grid-template-columns: 1fr 6px 520px;
  gap: 8px;
  height: calc(100vh - 100px);
}
.left, .right { min-height: 0; min-width: 0; }
.right { display: flex; flex-direction: column; }
.cart { flex: 1; display: flex; flex-direction: column; }
.cart :deep(.el-card__body) { display: flex; flex-direction: column; flex: 1; overflow: hidden; }
.header-row { display: flex; justify-content: space-between; align-items: center; }

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

.services-grid {
  display: grid;
  grid-template-columns: repeat(auto-fill, minmax(180px, 1fr));
  gap: 12px;
  max-height: calc(100vh - 200px);
  overflow-y: auto;
  padding-right: 4px;
}
.service-card { cursor: pointer; }
.service-card :deep(.el-card__body) { padding: 12px; }
.svc-name { font-weight: 600; margin-bottom: 6px; }
.svc-meta { display: flex; flex-wrap: wrap; gap: 4px; margin-bottom: 6px; }
.svc-code { color: var(--el-text-color-secondary); font-size: 12px; }

.mode-switch { display: flex; margin-bottom: 10px; width: 100%; }
.mode-switch :deep(.el-radio-button) { flex: 1; }
.mode-switch :deep(.el-radio-button__inner) { width: 100%; }
.member-row { display: flex; gap: 8px; }
.member-info { background: #f5f7fa; padding: 8px; border-radius: 4px; margin-top: 8px; }
.m-line { display: flex; gap: 8px; align-items: center; flex-wrap: wrap; }
.m-line.muted { color: var(--el-text-color-secondary); font-size: 12px; margin-top: 4px; }
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

.cart-list { flex: 1; overflow-y: auto; margin-top: 4px; }
.cart-item {
  border-bottom: 1px dashed var(--el-border-color-light);
  padding: 8px 0;
}
.ci-line { display: flex; justify-content: space-between; align-items: center; }
.ci-name { font-weight: 500; font-size: 15px; }
.ci-meta { display: flex; gap: 8px; align-items: center; margin-top: 8px; flex-wrap: wrap; }
.ci-tech { flex: 1 1 200px; min-width: 180px; }
.ci-room { flex: 0 1 140px; min-width: 120px; }
.ci-qty { flex: 0 0 140px; }
.ci-price { margin-left: auto; font-weight: 700; font-size: 16px; color: #d9534f; }

.totals { padding-top: 8px; border-top: 1px solid var(--el-border-color-light); margin-top: 8px; }
.total-line { display: flex; justify-content: space-between; padding: 4px 0; }
.total-line.muted { color: var(--el-text-color-secondary); font-size: 12px; }
.total-amount { font-size: 18px; font-weight: 700; color: #d9534f; }
</style>
