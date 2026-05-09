<template>
  <div class="pos">
    <div class="left">
      <el-card shadow="never">
        <template #header>
          <div class="header-row">
            <span>选择服务项目</span>
            <el-input
              v-model="serviceFilter"
              placeholder="按编码或名称过滤"
              size="default"
              clearable
              style="width: 220px"
              :prefix-icon="Search"
            />
          </div>
        </template>
        <div class="services-grid">
          <el-card
            v-for="s in filteredServices"
            :key="s.id"
            class="service-card"
            shadow="hover"
            @click="onPickService(s)"
          >
            <div class="svc-name">{{ s.name }}</div>
            <div class="svc-meta">
              <el-tag size="small">{{ s.durationMinutes }} 分钟</el-tag>
              <el-tag size="small" type="success">¥{{ s.price.toFixed(2) }}</el-tag>
              <el-tag v-if="member" size="small" type="warning">会员 ¥{{ s.memberPrice.toFixed(2) }}</el-tag>
            </div>
            <div class="svc-code">{{ s.code }}</div>
          </el-card>
        </div>
      </el-card>
    </div>

    <div class="right">
      <el-card shadow="never" class="cart">
        <template #header>
          <div class="header-row">
            <span>当前订单</span>
            <el-button v-if="cart.length > 0 || member" link type="danger" @click="resetAll">清空</el-button>
          </div>
        </template>

        <div class="member-row">
          <el-input
            v-model="memberKeyword"
            placeholder="会员卡号 / 手机号"
            clearable
            :prefix-icon="User"
            style="flex: 1"
            @keyup.enter="lookupMember"
          />
          <el-button :icon="Search" @click="lookupMember">查询</el-button>
        </div>
        <div v-if="member" class="member-info">
          <div class="m-line">
            <strong>{{ member.name || member.cardNo }}</strong>
            <el-tag size="small">余额 ¥{{ member.balance.toFixed(2) }}</el-tag>
            <el-tag size="small" type="warning" v-if="member.discount < 1">{{ (member.discount * 10).toFixed(1) }} 折</el-tag>
          </div>
          <div class="m-line muted">{{ member.phone }} · 累计消费 ¥{{ member.totalConsumed.toFixed(2) }}</div>
          <el-button link type="danger" @click="member = null; memberKeyword = ''">取消关联</el-button>
        </div>

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
                size="small"
                style="width: 130px"
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
                size="small"
                style="width: 110px"
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
              <el-input-number v-model="it.quantity" :min="1" :max="20" size="small" controls-position="right" style="width: 90px" />
              <span class="ci-price">¥{{ (it.unitPrice * it.quantity).toFixed(2) }}</span>
            </div>
          </div>
        </div>

        <div class="totals">
          <div class="total-line">
            <span>合计</span>
            <span class="total-amount">¥ {{ total.toFixed(2) }}</span>
          </div>
          <div v-if="member && member.discount < 1" class="total-line muted">
            <span>会员折扣（自动）</span>
            <span>-¥ {{ memberDiscount.toFixed(2) }}</span>
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
          @click="openCheckout"
        >
          下单并结账
        </el-button>
      </el-card>
    </div>

    <PickTechnicianDialog
      v-model="pickOpen"
      :service="pickedService"
      :technicians="technicians"
      :is-member="!!member"
      @confirm="onTechnicianPicked"
    />

    <CheckoutDialog
      v-model="checkoutOpen"
      :total="total"
      :payable="payable"
      :has-member="!!member"
      :member-balance="member?.balance ?? 0"
      :loading="checkingOut"
      @submit="doCheckout"
    />

    <el-dialog v-model="receiptOpen" title="结账成功" width="420px">
      <div v-if="lastOrder">
        <p><strong>订单号：</strong>{{ lastOrder.orderNo }}</p>
        <p><strong>合计：</strong>¥ {{ lastOrder.total.toFixed(2) }}</p>
        <p><strong>实收：</strong>¥ {{ lastOrder.paidAmount.toFixed(2) }}（{{ payMethodLabel(lastOrder.payMethod) }}）</p>
        <p v-if="lastOrder.discountAmount > 0">优惠：¥ {{ lastOrder.discountAmount.toFixed(2) }}</p>
        <el-table :data="lastOrder.items" size="small">
          <el-table-column prop="serviceName" label="项目" />
          <el-table-column prop="technicianName" label="技师" width="100" />
          <el-table-column label="金额" width="100">
            <template #default="{ row }">¥{{ row.itemTotal.toFixed(2) }}</template>
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
import { computed, onMounted, reactive, ref } from 'vue';
import { ElMessage, ElMessageBox } from 'element-plus';
import { Search, User } from '@element-plus/icons-vue';
import { membersApi, ordersApi, roomsApi, servicesApi, staffApi } from '@/api/modules';
import { useAppStore } from '@/stores/app';
import type { Member, Order, Room, ServiceItem, Staff } from '@/api/types';
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

function availableRooms(currentRoomId: number | null): Room[] {
  // 当前 cart 中已选过的房间在其它项目里禁用，自己当前选的留可见
  const usedElsewhere = new Set(
    cart.filter((c) => c.roomId !== null && c.roomId !== currentRoomId).map((c) => c.roomId!)
  );
  return rooms.value.filter((r) => !usedElsewhere.has(r.id));
}
const cart = reactive<CartItem[]>([]);
const member = ref<Member | null>(null);
const memberKeyword = ref('');

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

const memberDiscount = computed(() => {
  if (!member.value || member.value.discount >= 1) return 0;
  return Math.round(total.value * (1 - member.value.discount) * 100) / 100;
});

const payable = computed(() => Math.max(0, total.value - memberDiscount.value));

const canCheckout = computed(
  () => cart.length > 0 && cart.every((c) => c.technicianId != null) && !!appStore.activeStoreId
);

function payMethodLabel(m: string) {
  return ({
    Cash: '现金', MemberCard: '会员卡', Wechat: '微信', Alipay: '支付宝', BankCard: '银行卡', Unpaid: '未支付'
  } as Record<string, string>)[m] ?? m;
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
  const unit = member.value ? s.memberPrice : s.price;
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

async function lookupMember() {
  const k = memberKeyword.value.trim();
  if (!k) return;
  const data = await membersApi.list({ keyword: k, pageSize: 5 });
  if (data.items.length === 0) {
    ElMessage.warning('未找到会员');
    return;
  }
  member.value = data.items[0];
  // 切换会员后重算价格
  for (const c of cart) {
    const svc = services.value.find((s) => s.id === c.serviceId);
    if (svc) c.unitPrice = member.value ? svc.memberPrice : svc.price;
  }
  ElMessage.success(`已关联会员 ${member.value!.name || member.value!.cardNo}`);
}

function openCheckout() {
  if (!canCheckout.value) {
    ElMessage.warning('请确保所有项目都指派了技师');
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
      memberId: member.value?.id ?? null,
      items: cart.map((c) => ({
        serviceId: c.serviceId,
        technicianId: c.technicianId!,
        quantity: c.quantity,
        roomId: c.roomId
      })),
      remark: null
    });
    const checked = await ordersApi.checkout(created.id, {
      payMethod: payload.payMethod,
      paidAmount: payload.paidAmount,
      discountAmount: 0,
      remark: payload.remark
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
  member.value = null;
  memberKeyword.value = '';
  lastOrder.value = null;
}

onMounted(async () => {
  await appStore.loadStores();
  await loadCatalog();
});
</script>

<style scoped>
.pos {
  display: grid;
  grid-template-columns: 1fr 380px;
  gap: 16px;
  height: calc(100vh - 100px);
}
.left, .right { min-height: 0; }
.right { display: flex; flex-direction: column; }
.cart { flex: 1; display: flex; flex-direction: column; }
.cart :deep(.el-card__body) { display: flex; flex-direction: column; flex: 1; overflow: hidden; }
.header-row { display: flex; justify-content: space-between; align-items: center; }

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

.member-row { display: flex; gap: 8px; }
.member-info { background: #f5f7fa; padding: 8px; border-radius: 4px; margin-top: 8px; }
.m-line { display: flex; gap: 8px; align-items: center; }
.m-line.muted { color: var(--el-text-color-secondary); font-size: 12px; margin-top: 4px; }

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
.ci-name { font-weight: 500; }
.ci-meta { display: flex; gap: 8px; align-items: center; margin-top: 6px; }
.ci-price { margin-left: auto; font-weight: 600; }

.totals { padding-top: 8px; border-top: 1px solid var(--el-border-color-light); margin-top: 8px; }
.total-line { display: flex; justify-content: space-between; padding: 4px 0; }
.total-line.muted { color: var(--el-text-color-secondary); font-size: 12px; }
.total-amount { font-size: 18px; font-weight: 700; color: #d9534f; }
</style>
