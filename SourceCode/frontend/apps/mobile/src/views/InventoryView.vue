<script lang="ts">
export default { name: 'InventoryView' };
</script>

<script setup lang="ts">
import { computed, onMounted, reactive, ref } from 'vue';
import {
  NavBar as VanNavBar, PullRefresh as VanPullRefresh, Empty as VanEmpty, Tag as VanTag,
  Button as VanButton, Popup as VanPopup, Form as VanForm, Field as VanField, Stepper as VanStepper,
  Checkbox as VanCheckbox, Cell as VanCell, CellGroup as VanCellGroup,
  showSuccessToast, showToast
} from 'vant';
import { inventoryApi, type InventoryItemDto, type InventoryMovementDto } from '@/api/modules';
import { useAppStore } from '@/stores/app';

const appStore = useAppStore();
const rows = ref<InventoryItemDto[]>([]);
const loading = ref(false);
const refreshing = ref(false);
const onlyLowStock = ref(false);

const KIND_LABEL: Record<string, string> = {
  PurchaseIn: '采购入库', Consume: '领用', Adjust: '盘点', Discard: '报损'
};
function kindLabel(k: string) { return KIND_LABEL[k] ?? k; }

async function reload() {
  if (!appStore.activeStoreId) return;
  loading.value = true;
  try { rows.value = await inventoryApi.items(appStore.activeStoreId, onlyLowStock.value); }
  catch { /* */ } finally { loading.value = false; refreshing.value = false; }
}

// 建档
const formOpen = ref(false);
const saving = ref(false);
const form = reactive({ code: '', name: '', unit: '', quantity: 0, minQuantity: 0, unitCost: 0, remark: '' });
function openNew() {
  Object.assign(form, { code: '', name: '', unit: '', quantity: 0, minQuantity: 0, unitCost: 0, remark: '' });
  formOpen.value = true;
}
async function save() {
  if (!form.code.trim() || !form.name.trim()) { showToast('编码和名称必填'); return; }
  saving.value = true;
  try {
    await inventoryApi.createItem({
      storeId: appStore.activeStoreId, code: form.code.trim(), name: form.name.trim(),
      unit: form.unit || null, quantity: form.quantity, minQuantity: form.minQuantity,
      unitCost: form.unitCost || null, remark: form.remark || null
    });
    showSuccessToast('已建档');
    formOpen.value = false;
    reload();
  } catch { /* */ } finally { saving.value = false; }
}

// 出入库
const moveOpen = ref(false);
const moving = ref(false);
const move = reactive({ itemId: 0, itemName: '', kind: 'PurchaseIn', delta: 1, remark: '' });
const moveTitle = computed(() => kindLabel(move.kind));
const moveHint = computed(() => {
  if (move.kind === 'Consume' || move.kind === 'Discard') return '系统会自动取负数';
  if (move.kind === 'Adjust') return '正数=加，负数=减';
  return '正数表示新增库存';
});
function openMove(row: InventoryItemDto, kind: string) {
  Object.assign(move, { itemId: row.id, itemName: row.name, kind, delta: 1, remark: '' });
  moveOpen.value = true;
}
async function submitMove() {
  moving.value = true;
  try {
    await inventoryApi.move({ itemId: move.itemId, kind: move.kind, delta: move.delta, remark: move.remark || null });
    showSuccessToast('已记录');
    moveOpen.value = false;
    reload();
  } catch { /* */ } finally { moving.value = false; }
}

// 流水
const historyOpen = ref(false);
const historyName = ref('');
const movements = ref<InventoryMovementDto[]>([]);
async function viewMovements(row: InventoryItemDto) {
  historyName.value = row.name;
  movements.value = await inventoryApi.movements(row.id, 50).catch(() => []);
  historyOpen.value = true;
}

onMounted(async () => {
  if (!appStore.stores.length) await appStore.loadStores().catch(() => undefined);
  reload();
});
</script>

<template>
  <div class="qy-page inventory">
    <van-nav-bar title="物耗库存" left-text="返回" left-arrow @click-left="$router.back()">
      <template #right><span class="nav-add" @click="openNew">建档</span></template>
    </van-nav-bar>
    <div class="bar"><van-checkbox v-model="onlyLowStock" shape="square" @change="reload">仅显示库存预警</van-checkbox></div>

    <van-pull-refresh v-model="refreshing" @refresh="reload">
      <van-empty v-if="!loading && rows.length === 0" description="暂无物品" />
      <div v-for="it in rows" :key="it.id" class="inv">
        <div class="iv-l1">
          <span class="iv-code">{{ it.code }}</span>{{ it.name }}
          <van-tag v-if="it.lowStock" type="danger">预警</van-tag>
        </div>
        <div class="iv-l2">
          库存 <b :class="{ low: it.lowStock }">{{ it.quantity }} {{ it.unit || '' }}</b>
          · 预警阈值 {{ it.minQuantity }}<span v-if="it.unitCost != null"> · 单价 ¥{{ it.unitCost }}</span>
        </div>
        <div class="iv-actions">
          <van-button size="mini" type="primary" @click="openMove(it, 'PurchaseIn')">入库</van-button>
          <van-button size="mini" @click="openMove(it, 'Consume')">领用</van-button>
          <van-button size="mini" @click="openMove(it, 'Adjust')">盘点</van-button>
          <van-button size="mini" plain @click="viewMovements(it)">流水</van-button>
        </div>
      </div>
    </van-pull-refresh>

    <!-- 建档 -->
    <van-popup v-model:show="formOpen" position="bottom" round :style="{ maxHeight: '90%' }">
      <div class="sheet">
        <div class="sheet-title">物品建档</div>
        <van-form @submit="save">
          <van-cell-group inset>
            <van-field v-model="form.code" label="编码" placeholder="必填" />
            <van-field v-model="form.name" label="名称" placeholder="必填" />
            <van-field v-model="form.unit" label="单位" placeholder="瓶/包/个" />
            <van-field label="初始库存"><template #input><van-stepper v-model="form.quantity" :min="0" :decimal-length="3" /></template></van-field>
            <van-field label="预警阈值"><template #input><van-stepper v-model="form.minQuantity" :min="0" :decimal-length="3" /></template></van-field>
            <van-field label="单价"><template #input><van-stepper v-model="form.unitCost" :min="0" :decimal-length="2" /></template></van-field>
            <van-field v-model="form.remark" label="备注" type="textarea" rows="1" autosize placeholder="选填" />
          </van-cell-group>
          <div class="sheet-actions"><van-button block type="primary" native-type="submit" :loading="saving">保存</van-button></div>
        </van-form>
      </div>
    </van-popup>

    <!-- 出入库 -->
    <van-popup v-model:show="moveOpen" position="bottom" round>
      <div class="sheet">
        <div class="sheet-title">{{ moveTitle }}</div>
        <van-cell-group inset>
          <van-cell title="物品" :value="move.itemName" />
          <van-field label="数量"><template #input><van-stepper v-model="move.delta" :decimal-length="3" /></template></van-field>
          <van-cell :title="moveHint" />
          <van-field v-model="move.remark" label="备注" placeholder="选填" />
        </van-cell-group>
        <div class="sheet-actions"><van-button block type="primary" :loading="moving" @click="submitMove">提交</van-button></div>
      </div>
    </van-popup>

    <!-- 流水 -->
    <van-popup v-model:show="historyOpen" position="bottom" round :style="{ maxHeight: '80%' }">
      <div class="sheet">
        <div class="sheet-title">{{ historyName }} 出入库流水</div>
        <van-empty v-if="!movements.length" description="暂无流水" />
        <div v-for="m in movements" :key="m.id" class="mv">
          <div class="mv-l">
            <van-tag :type="m.delta >= 0 ? 'success' : 'danger'">{{ kindLabel(m.kind) }}</van-tag>
            <span class="mv-delta">{{ m.delta >= 0 ? '+' : '' }}{{ m.delta }}</span>
          </div>
          <div class="mv-r">
            <div>结余 {{ m.quantityAfter }}<span v-if="m.operatorName"> · {{ m.operatorName }}</span></div>
            <div class="mv-time">{{ m.createdAt.slice(0, 19).replace('T', ' ') }}</div>
          </div>
        </div>
      </div>
    </van-popup>
  </div>
</template>

<style scoped>
.nav-add { color: var(--qy-brand); font-size: 15px; }
.bar { padding: 10px 14px; }
.inv { background: #fff; margin: 8px 12px; padding: 14px; border-radius: 12px; }
.iv-l1 { font-size: 15px; font-weight: 600; display: flex; align-items: center; gap: 6px; flex-wrap: wrap; }
.iv-code { color: #b0b8c4; font-weight: 400; font-size: 13px; margin-right: 4px; }
.iv-l2 { margin-top: 6px; color: #6b7280; font-size: 13px; }
.iv-l2 b.low { color: #ee4d4d; }
.iv-actions { display: flex; gap: 8px; margin-top: 12px; flex-wrap: wrap; }
.sheet { padding: 16px 0 24px; }
.sheet-title { text-align: center; font-size: 17px; font-weight: 700; margin-bottom: 12px; }
.sheet-actions { padding: 16px 16px 0; }
.mv { display: flex; justify-content: space-between; padding: 12px 16px; border-bottom: 1px solid #f5f6f8; }
.mv-l { display: flex; align-items: center; gap: 8px; }
.mv-delta { font-weight: 700; }
.mv-r { text-align: right; color: #6b7280; font-size: 13px; }
.mv-time { color: #b0b8c4; font-size: 12px; margin-top: 2px; }
</style>
