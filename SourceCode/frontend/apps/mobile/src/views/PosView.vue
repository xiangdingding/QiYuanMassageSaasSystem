<script lang="ts">
export default { name: 'PosView' };
</script>

<script setup lang="ts">
import { computed, onMounted, reactive, ref, watch } from 'vue';
import {
  NavBar as VanNavBar, Search as VanSearch, Button as VanButton, Tag as VanTag,
  Checkbox as VanCheckbox, Popup as VanPopup, Picker as VanPicker, Stepper as VanStepper,
  Field as VanField, Cell as VanCell, CellGroup as VanCellGroup, Divider as VanDivider,
  Empty as VanEmpty, Icon as VanIcon, Rate as VanRate,
  showToast, showSuccessToast, showFailToast, showConfirmDialog
} from 'vant';
import {
  servicesApi, staffApi, roomsApi, timedRoomsApi, ordersApi, vouchersApi, reviewsApi,
  membersApi, type TimedRoomSessionDto, type VoucherDto
} from '@/api/modules';
import { useAppStore } from '@/stores/app';
import { PAY_METHOD_LABELS, type Member, type Order, type OrderItem, type OrderRoomCharge, type Room, type ServiceItem, type Staff } from '@/api/types';

const appStore = useAppStore();

// ----- 购物车模型：service（要选技师/房间/数量） vs roomCharge（计时房费快照） -----
interface ServiceCartItem {
  kind: 'service';
  serviceId: number;
  serviceName: string;
  technicianId: number | null;
  roomId: number | null;
  unitPrice: number;
  quantity: number;
  durationMinutes: number;
  assignmentSource: 'Rotation' | 'Designation';
}
interface RoomChargeCartItem {
  kind: 'roomCharge';
  sessionId: number;
  roomId: number;
  roomNo: string;
  serviceName: string;
  unitPrice: number;
  elapsedMinutes: number;
  hourlyRate: number;
  boundMemberId: number | null;
  boundMemberName: string | null;
}
type CartItem = ServiceCartItem | RoomChargeCartItem;

const services = ref<ServiceItem[]>([]);
const technicians = ref<Staff[]>([]);
const rooms = ref<Room[]>([]);
const timedSessions = ref<TimedRoomSessionDto[]>([]);
const serviceFilter = ref('');
const loading = ref(false);

const cart = reactive<CartItem[]>([]);
const mode = ref<'walkin' | 'member'>('member');

// ----- 会员（一人多卡，可勾选合并结算） -----
const memberKeyword = ref('');
const memberCards = ref<Member[]>([]);
const selectedCardIds = ref<number[]>([]);
const looking = ref(false);

const primaryMember = computed<Member | null>(() =>
  selectedCardIds.value.length ? memberCards.value.find((c) => c.id === selectedCardIds.value[0]) ?? null : null
);
const selectedBalance = computed(() =>
  memberCards.value.filter((c) => selectedCardIds.value.includes(c.id)).reduce((s, c) => s + c.balance, 0)
);

function isCardEligible(card: Member): boolean {
  // 次卡按剩余次数核销，余额不参与；只看绑定服务是否命中购物车
  if (card.memberTypeKind === 'CountBased') {
    if (card.serviceItemId == null) return false;
    return cart.some((c) => c.kind === 'service' && c.serviceId === card.serviceItemId);
  }
  // 充值卡 / 普通卡按余额结算：余额为 0（或负）则不可用，默认不勾选、不可选
  return card.balance > 0;
}
function toggleCard(id: number, checked: boolean) {
  const idx = selectedCardIds.value.indexOf(id);
  if (checked && idx === -1) {
    const card = memberCards.value.find((c) => c.id === id);
    if (card && !isCardEligible(card)) {
      showToast(card.serviceItemName ? `次卡绑定的「${card.serviceItemName}」不在购物车里，不能选用` : '该次卡未绑定服务项目');
      return;
    }
    selectedCardIds.value = [...selectedCardIds.value, id];
  } else if (!checked && idx !== -1) {
    const arr = [...selectedCardIds.value];
    arr.splice(idx, 1);
    selectedCardIds.value = arr;
  }
}
function clearMember() {
  memberCards.value = [];
  selectedCardIds.value = [];
  memberKeyword.value = '';
}
watch(memberKeyword, (v) => {
  if (!v.trim() && memberCards.value.length) { memberCards.value = []; selectedCardIds.value = []; }
});

async function lookupMember() {
  const k = memberKeyword.value.trim();
  if (!k) return;
  looking.value = true;
  try {
    const first = await membersApi.list({ keyword: k, pageSize: 5, includeClosed: true });
    if (!first.items.length) { memberCards.value = []; selectedCardIds.value = []; showToast('未找到会员'); return; }
    const phone = first.items[0].phone;
    const all = await membersApi.list({ keyword: phone, pageSize: 100, includeClosed: true });
    memberCards.value = all.items.filter((m) => m.phone === phone);
    const hit = memberCards.value.find((c) => c.id === first.items[0].id);
    const pickable = (c: Member) => c.isActive && isCardEligible(c);
    const def = hit && pickable(hit) ? hit : memberCards.value.find(pickable);
    selectedCardIds.value = def ? [def.id] : [];
    repriceMember();
    showSuccessToast('已关联会员');
  } catch { /* 拦截器已提示 */ } finally { looking.value = false; }
}

// 次卡因购物车变化失配时自动取消勾选
watch(
  () => cart.filter((c) => c.kind === 'service').map((c) => (c as ServiceCartItem).serviceId).join(','),
  () => {
    if (!selectedCardIds.value.length) return;
    const dropped: string[] = [];
    const kept = selectedCardIds.value.filter((id) => {
      const card = memberCards.value.find((c) => c.id === id);
      if (!card || isCardEligible(card)) return true;
      dropped.push(card.cardNo);
      return false;
    });
    if (dropped.length) { selectedCardIds.value = kept; showToast(`次卡 ${dropped.join('、')} 无匹配服务，已取消`); }
  }
);

// ----- 服务目录 -----
const filteredServices = computed(() => {
  const k = serviceFilter.value.trim().toLowerCase();
  if (!k) return services.value;
  return services.value.filter((s) => s.code.toLowerCase().includes(k) || s.name.toLowerCase().includes(k));
});

// ----- 计时房 -----
const timedRooms = computed(() => rooms.value.filter((r) => r.isTimedRoom && r.isActive));
function openSessionOf(roomId: number) {
  return timedSessions.value.find((s) => s.roomId === roomId && s.status === 'Open');
}
const cartRoomSessionIds = computed(() =>
  new Set(cart.filter((c): c is RoomChargeCartItem => c.kind === 'roomCharge').map((c) => c.sessionId))
);
function isRoomChargeMismatch(item: RoomChargeCartItem): boolean {
  if (item.boundMemberId == null) return false;
  if (!primaryMember.value) return true;
  return !memberCards.value.some((c) => c.id === item.boundMemberId);
}
const mismatchedRoomCharges = computed(() =>
  cart.filter((c): c is RoomChargeCartItem => c.kind === 'roomCharge' && isRoomChargeMismatch(c))
);
function addRoomChargeToCart(room: Room, s: TimedRoomSessionDto) {
  if (cartRoomSessionIds.value.has(s.id)) { showToast('该房间已在订单里'); return; }
  const minutes = s.elapsedMinutes;
  const amount = Math.round((minutes / 60) * s.hourlyRateSnapshot * 100) / 100;
  cart.push({
    kind: 'roomCharge', sessionId: s.id, roomId: room.id, roomNo: room.roomNo,
    serviceName: `${room.roomNo} 计时房 ${minutes} 分钟`, unitPrice: amount,
    elapsedMinutes: minutes, hourlyRate: s.hourlyRateSnapshot,
    boundMemberId: s.memberId, boundMemberName: s.memberName
  });
}
async function cancelTiming(s: TimedRoomSessionDto) {
  try { await showConfirmDialog({ title: '取消计时', message: `取消 ${s.roomNo} 的计时？已计 ${s.elapsedMinutes} 分钟将作废、不计费。` }); }
  catch { return; }
  try {
    await timedRoomsApi.cancel(s.id);
    const idx = cart.findIndex((c) => c.kind === 'roomCharge' && (c as RoomChargeCartItem).sessionId === s.id);
    if (idx !== -1) cart.splice(idx, 1);
    await reloadSessions();
    showSuccessToast('已取消计时');
  } catch { /* */ }
}

// 开台
const startOpen = ref(false);
const startRoom = ref<Room | null>(null);
const startForm = reactive({ bindMember: false, customerName: '', remark: '' });
const starting = ref(false);
function openStart(room: Room) {
  startRoom.value = room;
  startForm.bindMember = !!primaryMember.value;
  startForm.customerName = '';
  startForm.remark = '';
  startOpen.value = true;
}
async function doStart() {
  if (!startRoom.value) return;
  starting.value = true;
  try {
    const bind = startForm.bindMember && primaryMember.value;
    await timedRoomsApi.start(startRoom.value.id, {
      memberId: bind ? primaryMember.value!.id : null,
      customerName: bind ? null : (startForm.customerName.trim() || null),
      remark: startForm.remark.trim() || null
    });
    startOpen.value = false;
    await reloadSessions();
    showSuccessToast(`${startRoom.value.roomNo} 已开台`);
  } catch { /* */ } finally { starting.value = false; }
}

// ----- 项目编辑器（新增/编辑共用） -----
const editorOpen = ref(false);
const editingIndex = ref<number | null>(null);
const editorService = ref<ServiceItem | null>(null);
const editor = reactive<{ technicianId: number | null; source: 'Rotation' | 'Designation'; roomId: number | null; quantity: number }>(
  { technicianId: null, source: 'Rotation', roomId: null, quantity: 1 }
);
const showTechPicker = ref(false);
const showRoomPicker = ref(false);

const techColumns = computed(() => technicians.value.map((t) => ({ text: `${t.employeeNo ?? '-'} · ${t.realName || t.username}`, value: t.id })));
const roomColumns = computed(() => [{ text: '不指定', value: 0 }, ...rooms.value.filter((r) => !r.isTimedRoom).map((r) => ({ text: r.roomNo, value: r.id }))]);
function techName(id: number | null) {
  const t = technicians.value.find((x) => x.id === id);
  return t ? `${t.employeeNo ?? '-'} · ${t.realName || t.username}` : '选择技师';
}
function roomName(id: number | null) {
  if (id == null) return '不指定';
  return rooms.value.find((r) => r.id === id)?.roomNo ?? '不指定';
}

function openAddService(s: ServiceItem) {
  editingIndex.value = null;
  editorService.value = s;
  editor.technicianId = null;
  editor.source = 'Rotation';
  editor.roomId = null;
  editor.quantity = 1;
  editorOpen.value = true;
}
function openEditItem(idx: number) {
  const it = cart[idx];
  if (it.kind !== 'service') return;
  editingIndex.value = idx;
  editorService.value = services.value.find((s) => s.id === it.serviceId) ?? null;
  editor.technicianId = it.technicianId;
  editor.source = it.assignmentSource;
  editor.roomId = it.roomId;
  editor.quantity = it.quantity;
  editorOpen.value = true;
}
function onTechConfirm({ selectedValues }: { selectedValues: number[] }) {
  editor.technicianId = selectedValues[0] ?? null;
  showTechPicker.value = false;
}
function onRoomConfirm({ selectedValues }: { selectedValues: number[] }) {
  editor.roomId = selectedValues[0] || null;
  showRoomPicker.value = false;
}
function confirmEditor() {
  const s = editorService.value;
  if (!s) return;
  if (editor.technicianId == null) { showToast('请选择技师'); return; }
  const unit = mode.value === 'member' ? s.memberPrice : s.price;
  if (editingIndex.value == null) {
    cart.push({
      kind: 'service', serviceId: s.id, serviceName: s.name,
      technicianId: editor.technicianId, roomId: editor.roomId, unitPrice: unit,
      quantity: editor.quantity, durationMinutes: s.durationMinutes, assignmentSource: editor.source
    });
  } else {
    const it = cart[editingIndex.value];
    if (it.kind === 'service') {
      it.technicianId = editor.technicianId;
      it.roomId = editor.roomId;
      it.quantity = editor.quantity;
      it.assignmentSource = editor.source;
    }
  }
  editorOpen.value = false;
}

function removeItem(idx: number) { cart.splice(idx, 1); }

function repriceMember() {
  for (const c of cart) {
    if (c.kind !== 'service') continue;
    const svc = services.value.find((s) => s.id === c.serviceId);
    if (svc) c.unitPrice = mode.value === 'member' ? svc.memberPrice : svc.price;
  }
}
function onModeChange(m: 'walkin' | 'member') {
  mode.value = m;
  if (m === 'walkin') clearMember();
  repriceMember();
}

// ----- 优惠券 -----
const voucherCodeInput = ref('');
const appliedVoucher = ref<VoucherDto | null>(null);
const voucherApplying = ref(false);
const voucherDiscount = computed(() => {
  const v = appliedVoucher.value;
  if (!v) return 0;
  const t = total.value;
  if (t <= 0) return 0;
  const raw = v.discountPercent != null && v.discountPercent > 0
    ? Math.round(t * (1 - v.discountPercent) * 100) / 100
    : v.faceValue;
  return Math.min(raw, t);
});
async function applyVoucher() {
  const code = voucherCodeInput.value.trim();
  if (!code) { showToast('请填写券码'); return; }
  voucherApplying.value = true;
  try {
    const v = await vouchersApi.byCode(code);
    if (v.status !== 'Active') {
      showFailToast(v.status === 'Redeemed' ? '该券已被核销' : v.status === 'Expired' ? '该券已过期' : '该券已作废');
      return;
    }
    const now = Date.now();
    if (v.validFrom && now < new Date(v.validFrom).getTime()) { showFailToast('该券尚未生效'); return; }
    if (v.expiresAt && now > new Date(v.expiresAt).getTime()) { showFailToast('该券已过期'); return; }
    if (v.minOrderAmount > 0 && total.value < v.minOrderAmount) { showFailToast(`订单不足 ¥${v.minOrderAmount.toFixed(2)}`); return; }
    appliedVoucher.value = v;
    showSuccessToast(`券「${v.title}」已应用`);
  } catch { /* */ } finally { voucherApplying.value = false; }
}
function removeVoucher() { appliedVoucher.value = null; voucherCodeInput.value = ''; }

// ----- 金额 -----
const total = computed(() => cart.reduce((s, c) => s + c.unitPrice * (c.kind === 'service' ? c.quantity : 1), 0));
const payable = computed(() => Math.max(0, total.value - voucherDiscount.value));
const canCheckout = computed(() => {
  if (!cart.length) return false;
  if (!cart.every((c) => c.kind !== 'service' || c.technicianId != null)) return false;
  if (!appStore.activeStoreId) return false;
  if (mode.value === 'member' && !primaryMember.value) return false;
  if (mismatchedRoomCharges.value.length) return false;
  return true;
});
function fmt(n?: number | null) { return (n ?? 0).toFixed(2); }
function payLabel(m: string) { return PAY_METHOD_LABELS[m] ?? m; }

// ----- 结账 -----
const checkoutOpen = ref(false);
const checkingOut = ref(false);
const checkout = reactive<{ payMethod: string; paidAmount: number; remark: string }>({ payMethod: 'Cash', paidAmount: 0, remark: '' });
const payMethods = [
  { v: 'Cash', label: '现金' }, { v: 'MemberCard', label: '会员卡' },
  { v: 'Wechat', label: '微信' }, { v: 'Alipay', label: '支付宝' }, { v: 'BankCard', label: '银行卡' }
];
const change = computed(() => checkout.payMethod === 'Cash' && checkout.paidAmount > payable.value ? checkout.paidAmount - payable.value : 0);
const checkoutDisabled = computed(() => {
  if (checkout.payMethod === 'MemberCard' && selectedBalance.value < payable.value) return true;
  if (checkout.payMethod === 'Cash' && checkout.paidAmount < payable.value) return true;
  return false;
});
function openCheckout() {
  if (!canCheckout.value) {
    if (mismatchedRoomCharges.value.length) showFailToast('计时房费会员与结算会员不一致，请关联同一会员或移除该房费');
    else if (mode.value === 'member' && !primaryMember.value) showToast('会员收银必须先关联会员');
    else showToast('请为每个服务项目指派技师');
    return;
  }
  checkout.payMethod = primaryMember.value ? 'MemberCard' : 'Cash';
  checkout.paidAmount = payable.value;
  checkout.remark = '';
  checkoutOpen.value = true;
}
watch(() => checkout.payMethod, (m) => { if (m !== 'Cash') checkout.paidAmount = payable.value; });

// ----- 小票 + 评价 -----
const receiptOpen = ref(false);
const lastOrder = ref<Order | null>(null);
const reviewRatings = reactive<Record<number, number>>({});
const reviewSubmitting = ref(false);
const lastRoomCharges = computed<OrderRoomCharge[]>(() => lastOrder.value?.roomCharges ?? []);
function rowListAmount(row: OrderItem): number {
  if (row.listAmount != null && row.listAmount > 0) return row.listAmount;
  if (row.listUnitPrice != null && row.listUnitPrice > 0) return Math.round(row.listUnitPrice * row.quantity * 100) / 100;
  return row.itemTotal;
}

async function doCheckout() {
  if (!appStore.activeStoreId) { showFailToast('未选择门店'); return; }
  const svc = cart.filter((c): c is ServiceCartItem => c.kind === 'service');
  const roomCharges = cart.filter((c): c is RoomChargeCartItem => c.kind === 'roomCharge');
  if (!svc.length && !roomCharges.length) { showToast('购物车为空'); return; }
  checkingOut.value = true;
  try {
    const created = await ordersApi.create({
      storeId: appStore.activeStoreId,
      memberId: primaryMember.value?.id ?? null,
      items: svc.map((c) => ({ serviceId: c.serviceId, technicianId: c.technicianId!, quantity: c.quantity, roomId: c.roomId, assignmentSource: c.assignmentSource })),
      remark: null,
      roomSessionIds: roomCharges.map((r) => r.sessionId)
    });
    if (appliedVoucher.value) {
      try { await vouchersApi.redeem({ code: appliedVoucher.value.code, orderId: created.id }); }
      catch { showFailToast('优惠券核销失败，订单已创建但未结账，可到订单流水继续处理'); return; }
    }
    const secondary = selectedCardIds.value.slice(1);
    const checked = await ordersApi.checkout(created.id, {
      payMethod: checkout.payMethod,
      paidAmount: checkout.payMethod === 'Cash' ? checkout.paidAmount : null,
      discountAmount: 0,
      remark: checkout.remark.trim() || null,
      secondaryMemberIds: secondary.length ? secondary : undefined
    });
    lastOrder.value = checked;
    for (const k of Object.keys(reviewRatings)) delete reviewRatings[Number(k)];
    for (const it of checked.items) reviewRatings[it.id] = 4;
    checkoutOpen.value = false;
    receiptOpen.value = true;
    showSuccessToast('结账成功');
  } catch { /* */ } finally { checkingOut.value = false; }
}

async function finishReceipt() {
  if (lastOrder.value && lastOrder.value.items.length) {
    reviewSubmitting.value = true;
    try {
      for (const it of lastOrder.value.items) {
        await reviewsApi.submit({ orderId: lastOrder.value.id, orderItemId: it.id, rating: reviewRatings[it.id] ?? 4, tags: null, comment: null });
      }
    } catch { /* 不阻断完成 */ } finally { reviewSubmitting.value = false; }
  }
  receiptOpen.value = false;
  await resetAll(true);
}

async function resetAll(skipConfirm = false) {
  if (!skipConfirm && cart.length) {
    try { await showConfirmDialog({ title: '清空当前订单', message: '确认清空？' }); } catch { return; }
  }
  cart.splice(0, cart.length);
  clearMember();
  mode.value = 'member';
  lastOrder.value = null;
  appliedVoucher.value = null;
  voucherCodeInput.value = '';
  reloadSessions();
}

// ----- 加载 -----
async function loadCatalog() {
  const sid = appStore.activeStoreId;
  loading.value = true;
  try {
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
  } catch { /* */ } finally { loading.value = false; }
}
async function reloadSessions() {
  const sid = appStore.activeStoreId;
  if (!sid) return;
  try { timedSessions.value = await timedRoomsApi.sessions(sid); } catch { /* */ }
}

onMounted(async () => {
  if (!appStore.stores.length) await appStore.loadStores().catch(() => undefined);
  loadCatalog();
});
</script>

<template>
  <div class="qy-page pos">
    <van-nav-bar title="收银台" left-text="返回" left-arrow @click-left="$router.back()">
      <template #right><span v-if="cart.length || memberCards.length" class="nav-clear" @click="resetAll(false)">清空</span></template>
    </van-nav-bar>

    <!-- 模式切换 -->
    <div class="seg">
      <button :class="{ on: mode === 'member' }" @click="onModeChange('member')">会员收银</button>
      <button :class="{ on: mode === 'walkin' }" @click="onModeChange('walkin')">散客收银</button>
    </div>

    <!-- 会员区 -->
    <div v-if="mode === 'member'" class="card">
      <div class="member-search">
        <van-search v-model="memberKeyword" placeholder="会员卡号 / 手机号" @search="lookupMember" />
        <van-button size="small" type="primary" :loading="looking" @click="lookupMember">查询</van-button>
      </div>
      <div v-if="memberCards.length" class="member-cards">
        <div class="mc-head">
          <strong>{{ primaryMember?.name || memberCards[0].phone }}</strong>
          <van-tag v-if="memberCards.length > 1" type="primary">{{ memberCards.length }} 张卡 · 可合并</van-tag>
          <span class="mc-clear" @click="clearMember">取消关联</span>
        </div>
        <label v-for="c in memberCards" :key="c.id" class="mc-row" :class="{ closed: !c.isActive }">
          <van-checkbox
            :model-value="selectedCardIds.includes(c.id)"
            :disabled="!c.isActive || !isCardEligible(c)"
            @update:model-value="(v: boolean) => toggleCard(c.id, v)"
          />
          <div class="mc-info">
            <div class="mc-line1">
              <span class="mc-no">{{ c.cardNo }}</span>
              <van-tag plain :type="c.memberTypeKind === 'CountBased' ? 'success' : 'warning'">{{ c.memberTypeName || '普通' }}</van-tag>
              <van-tag v-if="!c.isActive" type="default">已关闭</van-tag>
              <van-tag v-else-if="c.memberTypeKind === 'CountBased' && !isCardEligible(c)" type="danger">无匹配项目</van-tag>
              <van-tag v-else-if="c.memberTypeKind !== 'CountBased' && c.balance <= 0" type="danger">余额为0</van-tag>
            </div>
            <div class="mc-line2">
              余额 ¥{{ fmt(c.balance) }}
              <span v-if="c.memberTypeKind === 'CountBased' && c.remainCount != null"> · 剩 {{ c.remainCount }} 次</span>
            </div>
          </div>
        </label>
        <div class="mc-sum">已选 {{ selectedCardIds.length }} 张 · 合计余额 <b class="qy-money">¥{{ fmt(selectedBalance) }}</b></div>
      </div>
    </div>

    <!-- 服务目录 -->
    <p class="qy-section-title">选择服务项目</p>
    <van-search v-model="serviceFilter" placeholder="按编码或名称过滤" />
    <div class="svc-grid">
      <button v-for="s in filteredServices" :key="s.id" class="svc" @click="openAddService(s)">
        <div class="svc-name">{{ s.name }}</div>
        <div class="svc-meta">{{ s.durationMinutes }}分 · <span class="qy-money">¥{{ mode === 'member' ? fmt(s.memberPrice) : fmt(s.price) }}</span></div>
      </button>
    </div>

    <!-- 计时房 -->
    <template v-if="timedRooms.length">
      <p class="qy-section-title">计时房费</p>
      <div class="timed-grid">
        <div v-for="r in timedRooms" :key="r.id" class="timed" :class="{ open: openSessionOf(r.id) }">
          <div class="t-head"><span class="t-no">{{ r.roomNo }}</span><van-tag plain>¥{{ fmt(r.hourlyRate) }}/时</van-tag></div>
          <template v-if="openSessionOf(r.id)">
            <div class="t-info">
              已计时 <b>{{ openSessionOf(r.id)!.elapsedMinutes }}</b> 分 · 客 {{ openSessionOf(r.id)!.customerName || openSessionOf(r.id)!.memberName || '散客' }}
            </div>
            <div class="t-actions">
              <van-button size="mini" type="primary" :disabled="cartRoomSessionIds.has(openSessionOf(r.id)!.id)" @click="addRoomChargeToCart(r, openSessionOf(r.id)!)">
                {{ cartRoomSessionIds.has(openSessionOf(r.id)!.id) ? '已加入' : '加入订单' }}
              </van-button>
              <van-button size="mini" type="danger" plain @click="cancelTiming(openSessionOf(r.id)!)">取消</van-button>
            </div>
          </template>
          <template v-else>
            <div class="t-info muted">空闲</div>
            <van-button size="mini" type="success" @click="openStart(r)">开台</van-button>
          </template>
        </div>
      </div>
    </template>

    <!-- 购物车 -->
    <p class="qy-section-title">当前订单（{{ cart.length }}）</p>
    <van-empty v-if="!cart.length" description="未添加任何项目" />
    <div v-else class="cart-list">
      <div v-for="(it, idx) in cart" :key="idx" class="ci" @click="it.kind === 'service' && openEditItem(idx)">
        <div class="ci-main">
          <div class="ci-name">
            <van-tag v-if="it.kind === 'roomCharge'" type="primary">计时房</van-tag>
            {{ it.serviceName }}
            <van-tag v-if="it.kind === 'roomCharge' && isRoomChargeMismatch(it)" type="danger">会员不一致</van-tag>
          </div>
          <div v-if="it.kind === 'service'" class="ci-sub">
            {{ techName(it.technicianId) }} · {{ it.assignmentSource === 'Rotation' ? '轮钟' : '点钟' }}
            <span v-if="it.roomId"> · {{ roomName(it.roomId) }}房</span> · ×{{ it.quantity }}
          </div>
          <div v-else class="ci-sub">已计时 {{ it.elapsedMinutes }} 分钟</div>
        </div>
        <div class="ci-right">
          <span class="ci-amt qy-money">¥{{ fmt(it.unitPrice * (it.kind === 'service' ? it.quantity : 1)) }}</span>
          <van-icon name="cross" class="ci-del" @click.stop="removeItem(idx)" />
        </div>
      </div>
    </div>

    <!-- 优惠券 -->
    <div class="voucher card">
      <span class="v-label">优惠券</span>
      <template v-if="!appliedVoucher">
        <input v-model="voucherCodeInput" class="v-input" placeholder="录入券码" />
        <van-button size="small" :loading="voucherApplying" :disabled="!voucherCodeInput.trim() || !cart.length" @click="applyVoucher">应用</van-button>
      </template>
      <template v-else>
        <span class="v-applied"><van-tag type="success">{{ appliedVoucher.code }}</van-tag> -¥{{ fmt(voucherDiscount) }}</span>
        <span class="mc-clear" @click="removeVoucher">移除</span>
      </template>
    </div>

    <div class="bottom-spacer"></div>

    <!-- 底部结算栏 -->
    <div class="checkout-bar">
      <div class="cb-amt">
        <span>应收</span>
        <b class="qy-money">¥{{ fmt(payable) }}</b>
        <span v-if="voucherDiscount > 0" class="cb-cut">（券-¥{{ fmt(voucherDiscount) }}）</span>
      </div>
      <van-button type="primary" :disabled="!canCheckout" @click="openCheckout">下单并结账</van-button>
    </div>

    <!-- 项目编辑器 -->
    <van-popup v-model:show="editorOpen" position="bottom" round>
      <div class="sheet">
        <div class="sheet-title">{{ editorService?.name }}</div>
        <van-cell-group inset>
          <van-cell title="单价" :value="`¥ ${fmt(mode === 'member' ? editorService?.memberPrice : editorService?.price)}`" />
          <van-field label="技师" :model-value="techName(editor.technicianId)" readonly is-link @click="showTechPicker = true" />
          <van-field label="上钟方式">
            <template #input>
              <div class="src-seg">
                <button :class="{ on: editor.source === 'Rotation' }" @click="editor.source = 'Rotation'">轮钟</button>
                <button :class="{ on: editor.source === 'Designation' }" @click="editor.source = 'Designation'">点钟</button>
              </div>
            </template>
          </van-field>
          <van-field label="房间" :model-value="roomName(editor.roomId)" readonly is-link @click="showRoomPicker = true" />
          <van-field label="数量">
            <template #input><van-stepper v-model="editor.quantity" :min="1" :max="20" /></template>
          </van-field>
        </van-cell-group>
        <div class="sheet-actions">
          <van-button block type="primary" @click="confirmEditor">{{ editingIndex == null ? '加入订单' : '保存修改' }}</van-button>
        </div>
      </div>
    </van-popup>
    <van-popup v-model:show="showTechPicker" position="bottom" round>
      <van-picker :columns="techColumns" @confirm="onTechConfirm" @cancel="showTechPicker = false" />
    </van-popup>
    <van-popup v-model:show="showRoomPicker" position="bottom" round>
      <van-picker :columns="roomColumns" @confirm="onRoomConfirm" @cancel="showRoomPicker = false" />
    </van-popup>

    <!-- 开台 -->
    <van-popup v-model:show="startOpen" position="bottom" round>
      <div class="sheet">
        <div class="sheet-title">开台计时：{{ startRoom?.roomNo }}</div>
        <van-cell-group inset>
          <van-cell title="单价" :value="`¥ ${fmt(startRoom?.hourlyRate)} / 小时`" />
          <van-cell v-if="primaryMember" title="关联当前会员">
            <template #right-icon><van-checkbox v-model="startForm.bindMember" /></template>
          </van-cell>
          <van-field v-if="!startForm.bindMember" v-model="startForm.customerName" label="散客姓名" placeholder="可空" />
          <van-field v-model="startForm.remark" label="备注" placeholder="可空" />
        </van-cell-group>
        <div class="sheet-actions">
          <van-button block type="primary" :loading="starting" @click="doStart">开台</van-button>
        </div>
      </div>
    </van-popup>

    <!-- 结账 -->
    <van-popup v-model:show="checkoutOpen" position="bottom" round>
      <div class="sheet">
        <div class="sheet-title">结账</div>
        <div class="co-payable">应收 <b class="qy-money">¥{{ fmt(payable) }}</b>
          <span v-if="total !== payable" class="muted">（合计 ¥{{ fmt(total) }}）</span>
        </div>
        <div class="pay-seg">
          <button
            v-for="pm in payMethods" :key="pm.v"
            :class="{ on: checkout.payMethod === pm.v }"
            :disabled="pm.v === 'MemberCard' && !primaryMember"
            @click="checkout.payMethod = pm.v"
          >{{ pm.label }}</button>
        </div>
        <van-cell-group inset>
          <van-cell v-if="checkout.payMethod === 'MemberCard'" title="会员余额"
            :value="`¥ ${fmt(selectedBalance)}`" :label="selectedBalance < payable ? '余额不足' : ''" />
          <van-field v-if="checkout.payMethod === 'Cash'" label="实收">
            <template #input><van-stepper v-model="checkout.paidAmount" :min="payable" :step="10" :decimal-length="2" /></template>
          </van-field>
          <van-cell v-if="change > 0" title="找零" :value="`¥ ${fmt(change)}`" />
          <van-field v-model="checkout.remark" label="备注" type="textarea" rows="1" autosize placeholder="可选" />
        </van-cell-group>
        <div class="sheet-actions">
          <van-button block type="primary" :loading="checkingOut" :disabled="checkoutDisabled" @click="doCheckout">
            确认结账 ¥{{ fmt(payable) }}
          </van-button>
        </div>
      </div>
    </van-popup>

    <!-- 小票 + 评价 -->
    <van-popup v-model:show="receiptOpen" position="bottom" round :close-on-click-overlay="false" :style="{ maxHeight: '88%' }">
      <div class="sheet" v-if="lastOrder">
        <div class="sheet-title">结账成功</div>
        <van-cell-group inset>
          <van-cell title="订单号" :value="lastOrder.orderNo" />
          <van-cell title="实收" :value="`¥ ${fmt(lastOrder.paidAmount)}（${payLabel(lastOrder.payMethod)}）`" />
          <van-cell v-if="(lastOrder.punchCardUsedCount ?? 0) > 0" title="次卡核销" :value="`${lastOrder.punchCardUsedCount} 次`" />
          <van-cell v-if="(lastOrder.discountAmount ?? 0) > 0" title="优惠" :value="`¥ ${fmt(lastOrder.discountAmount)}`" />
        </van-cell-group>

        <div v-if="lastOrder.items.length" class="receipt-review">
          <div class="rr-head">服务评价 <span class="muted">默认满意，可逐项调整</span></div>
          <div v-for="it in lastOrder.items" :key="it.id" class="rr-line">
            <div class="rr-svc">{{ it.serviceName }} · {{ it.technicianName }} · ¥{{ fmt(rowListAmount(it)) }}</div>
            <van-rate v-model="reviewRatings[it.id]" :count="5" />
          </div>
        </div>

        <van-cell-group v-if="lastRoomCharges.length" inset title="计时房费">
          <van-cell v-for="rc in lastRoomCharges" :key="rc.sessionId" :title="`${rc.roomNo} · ${rc.minutes}分`" :value="`¥ ${fmt(rc.amount)}`" />
        </van-cell-group>

        <div class="sheet-actions">
          <van-button block type="primary" :loading="reviewSubmitting" @click="finishReceipt">完成</van-button>
        </div>
      </div>
    </van-popup>
  </div>
</template>

<style scoped>
.pos { background: var(--qy-bg); }
.nav-clear { color: #ee4d4d; font-size: 14px; }
.card { background: #fff; margin: 10px 12px; border-radius: 12px; padding: 12px; }
.muted { color: #98a2b3; }

.seg, .src-seg, .pay-seg { display: flex; gap: 8px; }
.seg { padding: 10px 12px; }
.seg button, .src-seg button, .pay-seg button {
  flex: 1; border: 1px solid #d6dbe2; background: #fff; color: #4b5563;
  border-radius: 8px; padding: 8px 0; font-size: 14px;
}
.seg button.on, .src-seg button.on, .pay-seg button.on { background: var(--qy-brand); color: #fff; border-color: var(--qy-brand); }
.pay-seg { flex-wrap: wrap; padding: 0 16px 4px; }
.pay-seg button { flex: 0 0 calc(33% - 6px); padding: 7px 0; font-size: 13px; }
.pay-seg button:disabled { opacity: .4; }

.member-search { display: flex; align-items: center; gap: 8px; }
.member-search .van-search { flex: 1; padding: 0; }
.member-cards { margin-top: 10px; }
.mc-head { display: flex; align-items: center; gap: 8px; }
.mc-clear { color: #ee4d4d; font-size: 13px; margin-left: auto; }
.mc-row { display: flex; align-items: center; gap: 10px; padding: 10px 0; border-top: 1px solid #f1f3f5; }
.mc-row.closed { opacity: .55; }
.mc-info { flex: 1; }
.mc-line1 { display: flex; align-items: center; gap: 6px; font-weight: 600; }
.mc-no { font-size: 15px; }
.mc-line2 { margin-top: 4px; color: #98a2b3; font-size: 13px; }
.mc-sum { margin-top: 8px; font-size: 13px; color: #6b7280; }
.mc-sum b { color: #ee4d4d; }

.svc-grid { display: grid; grid-template-columns: repeat(3, 1fr); gap: 8px; padding: 0 12px; }
.svc { background: #fff; border: none; border-radius: 10px; padding: 12px 8px; text-align: left; }
.svc-name { font-size: 14px; font-weight: 600; color: #1f2733; }
.svc-meta { margin-top: 6px; font-size: 12px; color: #98a2b3; }

.timed-grid { display: grid; grid-template-columns: repeat(2, 1fr); gap: 8px; padding: 0 12px; }
.timed { background: #fff; border-radius: 10px; padding: 12px; }
.timed.open { box-shadow: 0 0 0 2px #f0a020 inset; }
.t-head { display: flex; justify-content: space-between; align-items: center; }
.t-no { font-weight: 700; }
.t-info { margin: 8px 0; font-size: 13px; color: #4b5563; }
.t-info b { color: #b06a00; }
.t-actions { display: flex; gap: 8px; }

.cart-list { padding: 0 12px; }
.ci { display: flex; align-items: center; gap: 10px; background: #fff; border-radius: 10px; padding: 12px; margin-bottom: 8px; }
.ci-main { flex: 1; min-width: 0; }
.ci-name { font-size: 15px; font-weight: 600; display: flex; align-items: center; gap: 6px; flex-wrap: wrap; }
.ci-sub { margin-top: 4px; color: #98a2b3; font-size: 13px; }
.ci-right { display: flex; align-items: center; gap: 12px; }
.ci-amt { font-weight: 700; color: var(--qy-brand); }
.ci-del { color: #c8ced6; font-size: 18px; }

.voucher { display: flex; align-items: center; gap: 8px; }
.v-label { color: #6b7280; font-size: 14px; }
.v-input { flex: 1; border: 1px solid #e3e7ec; border-radius: 8px; padding: 8px 10px; font-size: 14px; outline: none; }
.v-applied { flex: 1; display: flex; align-items: center; gap: 8px; color: #16a34a; font-weight: 600; }

.bottom-spacer { height: 70px; }
.checkout-bar {
  position: fixed; left: 50%; transform: translateX(-50%); bottom: 0; width: 100%; max-width: 640px;
  background: #fff; border-top: 1px solid #eef1f4; padding: 10px 14px calc(10px + env(safe-area-inset-bottom, 0px));
  display: flex; align-items: center; justify-content: space-between; z-index: 10;
}
.cb-amt span { color: #98a2b3; font-size: 13px; }
.cb-amt b { font-size: 22px; margin-left: 6px; color: #ee4d4d; }
.cb-cut { color: #16a34a !important; }

.sheet { padding: 16px 0 24px; }
.sheet-title { text-align: center; font-size: 17px; font-weight: 700; margin-bottom: 12px; }
.sheet-actions { padding: 16px 16px 0; }
.co-payable { text-align: center; margin-bottom: 12px; }
.co-payable b { font-size: 24px; color: #ee4d4d; }
.src-seg { width: 100%; }
.receipt-review { margin: 12px 16px 0; }
.rr-head { font-weight: 600; margin-bottom: 8px; }
.rr-line { padding: 8px 0; border-top: 1px solid #f1f3f5; }
.rr-svc { font-size: 14px; margin-bottom: 6px; }
</style>
